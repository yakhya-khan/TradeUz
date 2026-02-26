using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using System;
using System.Globalization;
using System.Linq;

namespace TradeUz.UI.Controls.Behaviors;

public class DateMaskBehavior : Behavior<CalendarDatePicker>
{
    private Control? _textBox;
    private bool _internalUpdate;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.TemplateApplied += OnTemplateApplied;
    }

    private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        // В Avalonia 11 правильное имя:
        _textBox = e.NameScope.Find<Control>("PART_TextBox");

        if (_textBox == null)
            return;

        _textBox.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
        _textBox.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
        _textBox.AddHandler(InputElement.LostFocusEvent, OnLostFocus, RoutingStrategies.Bubble);
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (!e.Text.All(char.IsDigit))
            e.Handled = true;
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (_internalUpdate || _textBox is not TextBox tb)
            return;

        if (e.Key is Key.Back or Key.Delete or Key.Tab or Key.Left or Key.Right)
            return;

        var rawText = tb.Text ?? string.Empty;

        // Если поле уже полностью заполнено — начинаем заново
        if (rawText.Length == 10 && tb.CaretIndex == rawText.Length)
        {
            _internalUpdate = true;
            tb.Text = "";
            tb.CaretIndex = 0;
            _internalUpdate = false;
        }

        var digits = new string(tb.Text?.Where(char.IsDigit).ToArray());

        if (string.IsNullOrEmpty(digits))
            return;

        if (digits.Length >= 2)
            digits = digits.Insert(2, ".");

        if (digits.Length >= 5)
            digits = digits.Insert(5, ".");

        if (digits.Length > 10)
            digits = digits.Substring(0, 10);

        _internalUpdate = true;
        tb.Text = digits;
        tb.CaretIndex = tb.Text.Length;
        _internalUpdate = false;
    }
    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_textBox is not TextBox tb || string.IsNullOrWhiteSpace(tb.Text))
            return;

        var digits = new string(tb.Text.Where(char.IsDigit).ToArray());

        if (digits.Length < 6)
        {
            SetInvalid(tb, "Дата введена не полностью");
            return;
        }

        var day = int.Parse(digits.Substring(0, 2));
        var month = int.Parse(digits.Substring(2, 2));
        var yearPart = digits.Substring(4);

        if (yearPart.Length == 2)
        {
            var shortYear = int.Parse(yearPart);
            yearPart = shortYear <= 50 ? $"20{yearPart}" : $"19{yearPart}";
        }

        var year = int.Parse(yearPart);

        if (month < 1 || month > 12)
        {
            SetInvalid(tb, "Некорректный месяц");
            return;
        }

        if (day < 1 || day > DateTime.DaysInMonth(year, month))
        {
            SetInvalid(tb, "Некорректный день");
            return;
        }

        var parsed = new DateTime(year, month, day);

        // Проверка бизнес-ограничений
        if (AssociatedObject.DisplayDateStart.HasValue &&
            parsed < AssociatedObject.DisplayDateStart.Value)
        {
            SetInvalid(tb, "Дата меньше допустимого значения");
            return;
        }

        if (AssociatedObject.DisplayDateEnd.HasValue &&
            parsed > AssociatedObject.DisplayDateEnd.Value)
        {
            SetInvalid(tb, "Дата больше допустимого значения");
            return;
        }

        ClearInvalid(tb);

        AssociatedObject.SelectedDate = parsed;
        tb.Text = parsed.ToString("dd.MM.yyyy");
    }
    private void SetInvalid(TextBox tb, string message)
    {
        tb.Classes.Add("invalid");
        ToolTip.SetTip(tb, message);
    }

    private void ClearInvalid(TextBox tb)
    {
        tb.Classes.Remove("invalid");
        ToolTip.SetTip(tb, null);
    }
}