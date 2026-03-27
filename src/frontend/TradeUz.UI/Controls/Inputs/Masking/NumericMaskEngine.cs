using System;
using System.Globalization;
using System.Linq;

namespace TradeUz.UI.Controls.Inputs.Masking;

/// <summary>
/// Engine не зависит от Avalonia. Работает только со строкой и caret.
/// </summary>
public sealed class NumericMaskEngine
{
    private readonly int _decimals;
    private readonly CultureInfo _culture;

    public NumericMaskEngine(int decimals, CultureInfo culture)
    {
        _decimals = decimals;
        _culture = culture;
    }

    // INSERT (с учетом selection)
    public MaskResult Insert(string text, int caret, int selLength, string input)
    {
        var raw = ExtractDigits(text);

        var digitCaret = VisualToDigitCaret(text, caret);

        if (selLength > 0)
        {
            var end = VisualToDigitCaret(text, caret + selLength);
            raw = raw.Remove(digitCaret, end - digitCaret);
        }

        var digits = new string(input.Where(char.IsDigit).ToArray());

        raw = raw.Insert(digitCaret, digits);

        return Build(raw, digitCaret + digits.Length);
    }

    // BACKSPACE
    public MaskResult Backspace(string text, int caret, int selLength)
    {
        var raw = ExtractDigits(text);

        var digitCaret = VisualToDigitCaret(text, caret);

        if (selLength > 0)
        {
            var end = VisualToDigitCaret(text, caret + selLength);
            raw = raw.Remove(digitCaret, end - digitCaret);
            return Build(raw, digitCaret);
        }

        if (digitCaret == 0)
            return new MaskResult(text, caret);

        raw = raw.Remove(digitCaret - 1, 1);

        return Build(raw, digitCaret - 1);
    }

    // DELETE
    public MaskResult Delete(string text, int caret, int selLength)
    {
        var raw = ExtractDigits(text);

        var digitCaret = VisualToDigitCaret(text, caret);

        if (selLength > 0)
        {
            var end = VisualToDigitCaret(text, caret + selLength);
            raw = raw.Remove(digitCaret, end - digitCaret);
            return Build(raw, digitCaret);
        }

        if (digitCaret >= raw.Length)
            return new MaskResult(text, caret);

        raw = raw.Remove(digitCaret, 1);

        return Build(raw, digitCaret);
    }

    // PASTE
    public MaskResult Paste(string text, int caret, int selLength, string paste)
    {
        var digits = new string(paste.Where(char.IsDigit).ToArray());

        return Insert(text, caret, selLength, digits);
    }

    // BUILD formatted string
    private MaskResult Build(string digits, int digitCaret)
    {
        if (string.IsNullOrEmpty(digits))
            return new MaskResult("0", 1);

        decimal value = decimal.Parse(digits) /
                        (decimal)Math.Pow(10, _decimals);

        var formatted = value.ToString("N" + _decimals, _culture);

        var visualCaret = DigitToVisualCaret(formatted, digitCaret);

        return new MaskResult(formatted, visualCaret);
    }

    private static string ExtractDigits(string text)
        => new string(text.Where(char.IsDigit).ToArray());

    // caret mapping
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
}