using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using System;
using System.Globalization;
using System.Linq;

namespace TradeUz.UI.Controls.Behaviors;

public class DateMaskBehavior : Behavior<CalendarDatePicker>
{
    private TextBox? _textBox;
    private bool _internalUpdate;

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.ApplyTemplate();

        _textBox = AssociatedObject.GetTemplateChildren()
            .OfType<TextBox>()
            .FirstOrDefault();

        if (_textBox != null)
        {
            _textBox.AddHandler(InputElement.TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
            _textBox.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
            _textBox.LostFocus += OnLostFocus;
        }
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (!e.Text.All(char.IsDigit))
            e.Handled = true;
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (_internalUpdate || _textBox == null)
            return;

        if (e.Key is Key.Back or Key.Delete)
            return;

        var digits = new string(_textBox.Text?.Where(char.IsDigit).ToArray());

        if (string.IsNullOrEmpty(digits))
            return;

        if (digits.Length >= 2)
            digits = digits.Insert(2, ".");

        if (digits.Length >= 5)
            digits = digits.Insert(5, ".");

        _internalUpdate = true;
        _textBox.Text = digits;
        _textBox.CaretIndex = _textBox.Text.Length;
        _internalUpdate = false;
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_textBox == null || string.IsNullOrWhiteSpace(_textBox.Text))
            return;

        var digits = new string(_textBox.Text.Where(char.IsDigit).ToArray());

        if (digits.Length < 6)
            return;

        var day = digits.Substring(0, 2);
        var month = digits.Substring(2, 2);
        var year = digits.Substring(4);

        if (year.Length == 2)
        {
            var shortYear = int.Parse(year);
            year = shortYear <= 50 ? $"20{year}" : $"19{year}";
        }

        var formatted = $"{day}.{month}.{year}";

        if (DateTime.TryParseExact(
            formatted,
            "dd.MM.yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed))
        {
            AssociatedObject.SelectedDate = parsed;
            _textBox.Text = parsed.ToString("dd.MM.yyyy");
        }
    }
}