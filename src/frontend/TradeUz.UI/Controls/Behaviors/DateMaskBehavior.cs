using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using System;
using System.Globalization;
using System.Linq;
using TradeUz.UI.Infrastructure.Localization;

namespace TradeUz.UI.Controls.Behaviors;

public class DateMaskBehavior : Behavior<CalendarDatePicker>
{
    private TextBox? _textBox;
    private bool _updating;

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is { } associatedObject)
            associatedObject.TemplateApplied += OnTemplateApplied;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject is { } associatedObject)
            associatedObject.TemplateApplied -= OnTemplateApplied;

        if (_textBox != null)
        {
            _textBox.RemoveHandler(InputElement.TextInputEvent, OnTextInput);
            _textBox.RemoveHandler(InputElement.KeyUpEvent, OnKeyUp);
            _textBox.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            _textBox.RemoveHandler(InputElement.LostFocusEvent, OnLostFocus);
        }
    }

    private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        _textBox = e.NameScope.Find<TextBox>("PART_TextBox");

        if (_textBox == null)
            return;

        _textBox.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
        _textBox.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
        _textBox.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        _textBox.AddHandler(InputElement.LostFocusEvent, OnLostFocus, RoutingStrategies.Bubble);
    }

    // 🚫 Только цифры
    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text) || !e.Text.All(char.IsDigit))
            e.Handled = true;
    }

    // 🧠 Навигация мышью — выделение цифры
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_textBox == null)
            return;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var text = _textBox.Text ?? "";
            if (text.Length == 0)
                return;

            int caret = _textBox.CaretIndex;

            if (caret < text.Length && text[caret] == '.')
                caret++;

            if (caret < text.Length)
            {
                _textBox.SelectionStart = caret;
                _textBox.SelectionEnd = caret + 1;
            }
        });
    }

    // 🧠 Редактирование без уничтожения UX
    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (_updating || _textBox == null)
            return;

        // Эти клавиши используются для перехода по форме, а не для редактирования даты.
        if (e.Key is Key.Tab or Key.Enter or Key.Return or Key.Up or Key.Down)
            return;

        var text = _textBox.Text ?? "";
        var caret = _textBox.CaretIndex;

        // стрелка влево
        if (e.Key == Key.Left && caret > 0)
        {
            if (text[caret - 1] == '.' || text[caret - 1] == '/')
                caret--;

            _textBox.SelectionStart = caret - 1;
            _textBox.SelectionEnd = caret;
            return;
        }

        // стрелка вправо
        if (e.Key == Key.Right && caret < text.Length)
        {
            if (text[caret] == '.' || text[caret] == '/')
                caret++;

            if (caret < text.Length)
            {
                _textBox.SelectionStart = caret;
                _textBox.SelectionEnd = caret + 1;
            }
            return;
        }
        
        if (e.Key == Key.Home)
        {
            _textBox.SelectionStart = 0;
            _textBox.SelectionEnd = 1;
            return;
        }
        
        if (e.Key == Key.End)
        {
            _textBox.SelectionStart = text.Length - 1;
            _textBox.SelectionEnd = text.Length;
            return;
        }
         if (e.Key is Key.Delete or Key.Back)
        {
            _textBox.Text="";
            return;
        }
        // ввод цифры — выделить следующую
        if (caret < text.Length) 
        {
            caret = (text[caret] == '.' || text[caret] == '/')  ? caret + 1 : caret;
            _textBox.SelectionStart = caret;
            _textBox.SelectionEnd = caret + 1;
        }


        // Маска пересобирается только при вводе в конце строки.
        // Так мы не ломаем позицию каретки при ручном редактировании даты.
        if (caret == text.Length)
        {
            var digits = new string(text.Where(char.IsDigit).ToArray());

            if (digits.Length >= 2)
                digits = digits.Insert(2, ".");

            if (digits.Length >= 5)
                digits = digits.Insert(5, ".");

            if (digits.Length > 10)
                digits = digits.Substring(0, 10);

            _updating = true;
            _textBox.Text = digits;
            _textBox.CaretIndex = digits.Length;
            _updating = false;
        }
    }

    // ✅ Валидация
    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_textBox == null)
            return;

        try
        {
            if (string.IsNullOrWhiteSpace(_textBox.Text))
                return;

            var digits = new string(_textBox.Text.Where(char.IsDigit).ToArray());

            if (digits.Length < 6)
            {
                SetInvalid(LocalizationProvider.Get("ValidationDateIncomplete"));
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
                SetInvalid(LocalizationProvider.Get("ValidationDateInvalidMonth"));
                return;
            }

            if (day < 1 || day > DateTime.DaysInMonth(year, month))
            {
                SetInvalid(LocalizationProvider.Get("ValidationDateInvalidDay"));
                return;
            }

            var parsed = new DateTime(year, month, day);

            var associatedObject = AssociatedObject;
            if (associatedObject == null)
                return;

            if (associatedObject.DisplayDateStart.HasValue &&
                parsed < associatedObject.DisplayDateStart.Value)
            {
                SetInvalid(LocalizationProvider.Get("ValidationDateBeforeMin"));
                return;
            }

            if (associatedObject.DisplayDateEnd.HasValue &&
                parsed > associatedObject.DisplayDateEnd.Value)
            {
                SetInvalid(LocalizationProvider.Get("ValidationDateAfterMax"));
                return;
            }

            ClearInvalid();
            associatedObject.SelectedDate = parsed;
            _textBox.Text = parsed.ToString("dd.MM.yyyy");
        }
        finally
        {
            ResetCaretState();
        }
    }

    private void SetInvalid(string message)
    {
        if (_textBox == null)
            return;

        _textBox.Classes.Add("invalid");
        ToolTip.SetTip(_textBox, message);
    }

    private void ClearInvalid()
    {
        if (_textBox == null)
            return;

        _textBox.Classes.Remove("invalid");
        ToolTip.SetTip(_textBox, null);
    }

    private void ResetCaretState()
    {
        if (_textBox == null)
            return;

        _textBox.SelectionStart = 0;
        _textBox.SelectionEnd = 0;
        _textBox.CaretIndex = 0;
    }
}
