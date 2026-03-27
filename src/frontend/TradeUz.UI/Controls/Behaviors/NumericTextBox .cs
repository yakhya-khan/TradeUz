using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Globalization;

namespace TradeUz.UI.Controls;

public class NumericTextBox : TextBox
{
    private bool _internalUpdate;

    public int Decimals { get; set; } = 2;

    public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

    public NumericTextBox()
    {
        // НИЧЕГО не делаем в конструкторе
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        AddHandler(TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        LostFocus += OnLostFocus;
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (_internalUpdate)
            return;

        if (!char.IsDigit(e.Text[0]))
        {
            e.Handled = true;
            return;
        }

        e.Handled = true;

        var text = Text ?? "";

        text += e.Text;

        UpdateText(text);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_internalUpdate)
            return;

        if (e.Key == Key.Back)
        {
            e.Handled = true;

            var text = Text ?? "";

            if (text.Length > 0)
                text = text.Substring(0, text.Length - 1);

            UpdateText(text);
        }
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        UpdateText(Text ?? "");
    }

    private void UpdateText(string raw)
    {
        if (decimal.TryParse(raw, out var value))
        {
            _internalUpdate = true;

            Text = value.ToString("N" + Decimals, Culture);

            CaretIndex = Text.Length;

            _internalUpdate = false;
        }
    }
}