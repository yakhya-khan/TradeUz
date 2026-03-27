using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Globalization;
using System.Linq;

namespace TradeUz.UI.Controls.Inputs;

/// <summary>
/// NumericTextBox — финансовый редактор чисел (POS / Trading UI).
/// Каноническое состояние — строка цифр (digits). Форматирование — через Culture.
/// </summary>
public class NumericTextBox : TextBox
{
    // ==============================
    // Bindable Value (decimal?)
    // ==============================
    public static readonly StyledProperty<decimal?> ValueProperty =
        AvaloniaProperty.Register<NumericTextBox, decimal?>(nameof(Value));

    public decimal? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    // ==============================
    // Параметры
    // ==============================
    public int Decimals { get; set; } = 2;

    public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// ReplaceMode (как в 1С): цифра заменяет текущую цифру под кареткой.
    /// Если false — вставка.
    /// </summary>
    public bool ReplaceMode { get; set; } = true;

    // ==============================
    // Внутреннее состояние
    // ==============================
    private bool _internal;
    private string _digits = ""; // канонические цифры (без разделителей)

    // ==============================
    // ctor
    // ==============================
    public NumericTextBox()
    {
        AddHandler(TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        PastingFromClipboard += OnPaste;
        LostFocus += OnLostFocus;
        GotFocus += OnGotFocus;

        PropertyChanged += OnPropertyChanged;
    }

    // ==============================
    // Синхронизация Value -> Text
    // ==============================
    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ValueProperty && !_internal)
        {
            if (Value.HasValue)
            {
                var scaled = (long)Math.Round(Value.Value * (decimal)Math.Pow(10, Decimals));
                _digits = Math.Abs(scaled).ToString();
                Apply(Build(_digits, _digits.Length));
            }
            else
            {
                _digits = "";
                Text = "";
            }
        }
    }

    // ==============================
    // Input
    // ==============================
    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (_internal || string.IsNullOrEmpty(e.Text))
            return;

        if (!char.IsDigit(e.Text[0]))
            return;

        e.Handled = true;

        var digitCaret = VisualToDigitCaret(Text ?? "", CaretIndex);

        if (SelectionEnd > SelectionStart)
        {
            var start = VisualToDigitCaret(Text ?? "", SelectionStart);
            var end = VisualToDigitCaret(Text ?? "", SelectionEnd);
            _digits = _digits.Remove(start, end - start);
            digitCaret = start;
        }

        if (ReplaceMode && digitCaret < _digits.Length)
        {
            _digits = _digits.Remove(digitCaret, 1)
                             .Insert(digitCaret, e.Text);
        }
        else
        {
            _digits = _digits.Insert(digitCaret, e.Text);
        }

        Apply(Build(_digits, digitCaret + 1));
    }

    // ==============================
    // Keyboard
    // ==============================
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_internal)
            return;

        if (e.Key == Key.Back)
        {
            e.Handled = true;
            Backspace();
        }

        if (e.Key == Key.Delete)
        {
            e.Handled = true;
            Delete();
        }
    }

    private void Backspace()
    {
        var digitCaret = VisualToDigitCaret(Text ?? "", CaretIndex);

        if (SelectionEnd > SelectionStart)
        {
            var start = VisualToDigitCaret(Text ?? "", SelectionStart);
            var end = VisualToDigitCaret(Text ?? "", SelectionEnd);
            _digits = _digits.Remove(start, end - start);
            Apply(Build(_digits, start));
            return;
        }

        if (digitCaret == 0)
            return;

        _digits = _digits.Remove(digitCaret - 1, 1);
        Apply(Build(_digits, digitCaret - 1));
    }

    private void Delete()
    {
        var digitCaret = VisualToDigitCaret(Text ?? "", CaretIndex);

        if (SelectionEnd > SelectionStart)
        {
            var start = VisualToDigitCaret(Text ?? "", SelectionStart);
            var end = VisualToDigitCaret(Text ?? "", SelectionEnd);
            _digits = _digits.Remove(start, end - start);
            Apply(Build(_digits, start));
            return;
        }

        if (digitCaret >= _digits.Length)
            return;

        _digits = _digits.Remove(digitCaret, 1);
        Apply(Build(_digits, digitCaret));
    }

    // ==============================
    // Paste
    // ==============================
    private async void OnPaste(object? sender, RoutedEventArgs e)
    {
        if (_internal)
            return;

        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard == null)
            return;

        var text = await top.Clipboard.GetTextAsync();
        if (string.IsNullOrWhiteSpace(text))
            return;

        var digits = new string(text.Where(char.IsDigit).ToArray());

        var digitCaret = VisualToDigitCaret(Text ?? "", CaretIndex);

        if (SelectionEnd > SelectionStart)
        {
            var start = VisualToDigitCaret(Text ?? "", SelectionStart);
            var end = VisualToDigitCaret(Text ?? "", SelectionEnd);
            _digits = _digits.Remove(start, end - start);
            digitCaret = start;
        }

        _digits = _digits.Insert(digitCaret, digits);

        Apply(Build(_digits, digitCaret + digits.Length));
    }

    // ==============================
    // Focus
    // ==============================
    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (decimal.TryParse(Text, NumberStyles.Number, Culture, out var v))
        {
            if (MinValue.HasValue && v < MinValue.Value)
                v = MinValue.Value;

            if (MaxValue.HasValue && v > MaxValue.Value)
                v = MaxValue.Value;

            Value = v;
        }
    }

    private void OnGotFocus(object? sender, RoutedEventArgs e)
    {
        SelectAll();
    }

    // ==============================
    // Formatting
    // ==============================
    private MaskResult Build(string digits, int digitCaret)
    {
        if (string.IsNullOrEmpty(digits))
            return new MaskResult("0", 1);

        decimal value = decimal.Parse(digits) /
                        (decimal)Math.Pow(10, Decimals);

        var formatted = value.ToString("N" + Decimals, Culture);

        var visualCaret = DigitToVisualCaret(formatted, digitCaret);

        return new MaskResult(formatted, visualCaret);
    }

    // ==============================
    // Apply
    // ==============================
    private void Apply(MaskResult result)
    {
        _internal = true;

        Text = result.Text;
        CaretIndex = result.Caret;

        if (decimal.TryParse(result.Text, NumberStyles.Number, Culture, out var v))
            Value = v;

        _internal = false;
    }

    // ==============================
    // Caret mapping
    // ==============================
    private static int VisualToDigitCaret(string text, int caret)
    {
        int digits = 0;

        for (int i = 0; i < caret && i < text.Length; i++)
            if (char.IsDigit(text[i]))
                digits++;

        return digits;
    }

    private static int DigitToVisualCaret(string formatted, int digitCaret)
    {
        int digits = 0;

        for (int i = 0; i < formatted.Length; i++)
        {
            if (char.IsDigit(formatted[i]))
                digits++;

            if (digits >= digitCaret)
                return i + 1;
        }

        return formatted.Length;
    }

    // ==============================
    // helper
    // ==============================
    private sealed class MaskResult
    {
        public string Text { get; }
        public int Caret { get; }

        public MaskResult(string text, int caret)
        {
            Text = text;
            Caret = caret;
        }
    }
}