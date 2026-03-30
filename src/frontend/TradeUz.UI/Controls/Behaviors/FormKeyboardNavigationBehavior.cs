using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using TradeUz.UI.Controls.Inputs;

namespace TradeUz.UI.Controls.Behaviors;

/// <summary>
/// Поведение для форменной навигации по Enter и вертикальным стрелкам.
/// Работает в пределах текущего экрана и использует TabIndex как основной порядок перехода.
/// </summary>
public class FormKeyboardNavigationBehavior : Behavior<Control>
{
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is { } root)
            root.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject is { } root)
            root.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || AssociatedObject == null)
            return;

        if (!TryGetStep(e, out var step))
            return;

        var candidates = GetNavigationCandidates();
        if (candidates.Count == 0)
            return;

        var focusedElement = TopLevel.GetTopLevel(AssociatedObject)?.FocusManager?.GetFocusedElement();
        var sourceControl = FindOwningCandidate(focusedElement, candidates);

        if (sourceControl == null || ShouldSkipNavigation(sourceControl))
            return;

        var sourceIndex = candidates.IndexOf(sourceControl);
        if (sourceIndex < 0)
            return;

        var targetIndex = sourceIndex + step;
        if (targetIndex < 0)
            targetIndex = candidates.Count - 1;
        else if (targetIndex >= candidates.Count)
            targetIndex = 0;

        if (FocusTarget(candidates[targetIndex], e.KeyModifiers))
            e.Handled = true;
    }

    private static bool TryGetStep(KeyEventArgs e, out int step)
    {
        step = 0;

        // Поддерживаем только чистую навигацию без Ctrl/Alt.
        // Shift нужен только для обратного перехода по Enter.
        if ((e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) != KeyModifiers.None)
            return false;

        if (e.Key is Key.Enter or Key.Return)
        {
            step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : 1;
            return true;
        }

        if (e.Key == Key.Down && e.KeyModifiers == KeyModifiers.None)
        {
            step = 1;
            return true;
        }

        if (e.Key == Key.Up && e.KeyModifiers == KeyModifiers.None)
        {
            step = -1;
            return true;
        }

        return false;
    }

    private static Control? FindOwningCandidate(IInputElement? focusedElement, IReadOnlyList<Control> candidates)
    {
        if (focusedElement is not AvaloniaObject focusedObject)
            return null;

        for (var index = 0; index < candidates.Count; index++)
        {
            if (ContainsElement(candidates[index], focusedObject))
                return candidates[index];
        }

        return null;
    }

    private bool ShouldSkipNavigation(Control sourceControl)
    {
        // В многострочном тексте Enter и стрелки должны оставаться у самого редактора.
        if (sourceControl is TextBox textBox && textBox.AcceptsReturn)
            return true;

        // Если popup уже открыт, не вмешиваемся в встроенную навигацию списка/календаря.
        if (sourceControl is ComboBox comboBox && comboBox.IsDropDownOpen)
            return true;

        if (sourceControl is CalendarDatePicker datePicker && datePicker.IsDropDownOpen)
            return true;

        return false;
    }

    private List<Control> GetNavigationCandidates()
    {
        if (AssociatedObject == null)
            return [];

        return AssociatedObject
            .GetVisualDescendants()
            .OfType<Control>()
            .Select((control, index) => new { Control = control, Index = index })
            .Where(x => IsNavigationCandidate(x.Control))
            .OrderBy(x => x.Control.TabIndex)
            .ThenBy(x => x.Index)
            .Select(x => x.Control)
            .ToList();
    }

    private bool IsNavigationCandidate(Control control)
    {
        if (!control.IsEffectivelyEnabled || !control.IsVisible)
            return false;

        if (!IsNavigationTarget(control))
            return false;

        // Для составных контролов вроде editable ComboBox внешний контрол не всегда
        // сам держит keyboard-focus, но всё равно должен участвовать в порядке перехода.
        if (control is not ComboBox and not CalendarDatePicker && !control.Focusable)
            return false;

        // Внутренние template-элементы составных контролов не должны попадать в общий порядок.
        if (HasCompositeOwner(control))
            return false;

        return true;
    }

    private static bool IsNavigationSource(Control control)
        => control is TextBox or ComboBox or CalendarDatePicker;

    private static bool IsNavigationTarget(Control control)
        => control is TextBox
            or ComboBox
            or CalendarDatePicker
            or Button
            or CheckBox
            or RadioButton
            or ToggleButton;

    private static bool HasCompositeOwner(Control control)
    {
        for (var current = (control as Visual)?.GetVisualParent(); current != null; current = current.GetVisualParent())
        {
            if (current is ComboBox or CalendarDatePicker)
                return true;
        }

        return false;
    }

    private static bool ContainsElement(Control candidate, AvaloniaObject focusedObject)
    {
        for (var current = focusedObject; current != null; current = (current as Visual)?.GetVisualParent())
        {
            if (ReferenceEquals(current, candidate))
                return true;
        }

        return false;
    }

    private static bool FocusTarget(Control target, KeyModifiers keyModifiers)
    {
        if (TryFocusInnerEditor(target, keyModifiers))
            return true;

        if (target.Focus(NavigationMethod.Tab, keyModifiers))
            return true;

        return false;
    }

    private static bool TryFocusInnerEditor(Control target, KeyModifiers keyModifiers)
    {
        if (target is not ComboBox and not CalendarDatePicker)
            return false;

        var innerTextBox = target
            .GetVisualDescendants()
            .OfType<TextBox>()
            .FirstOrDefault(textBox => textBox.Focusable && textBox.IsEffectivelyEnabled && textBox.IsVisible);

        if (innerTextBox == null)
            return false;

        if (!innerTextBox.Focus(NavigationMethod.Tab, keyModifiers))
            return false;

        // Для даты после входа по клавиатуре не оставляем старую каретку.
        // Выделяем первый сегмент целиком, чтобы начало редактирования было предсказуемым.
        if (target is CalendarDatePicker)
            Dispatcher.UIThread.Post(innerTextBox.SelectAll);

        return true;
    }
}
