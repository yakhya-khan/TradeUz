using System.Globalization;
using System.Linq;

namespace TradeUz.UI.Controls.Masking;

public sealed class NumericMaskEngine : IMaskEngine
{
    private readonly int _decimals;
    private readonly bool _grouping;
    private readonly bool _allowNegative;
    private readonly CultureInfo _culture;

    public NumericMaskEngine(
        int decimals,
        bool grouping,
        bool allowNegative,
        CultureInfo? culture = null)
    {
        _decimals = decimals;
        _grouping = grouping;
        _allowNegative = allowNegative;
        _culture = culture ?? CultureInfo.CurrentCulture;
    }

    public MaskResult Insert(string text, int caret, string input)
    {
        if (string.IsNullOrEmpty(input))
            return new MaskResult(text, caret);

        var raw = RemoveFormatting(text);

        var logicalCaret = GetRawCaret(text, caret);

        raw = raw.Insert(logicalCaret, input);

        raw = SanitizeRaw(raw);

        return BuildResult(raw, logicalCaret + input.Length);
    }

    public MaskResult Backspace(string text, int caret)
    {
        if (caret == 0)
            return new MaskResult(text, caret);

        var raw = RemoveFormatting(text);
        var logicalCaret = GetRawCaret(text, caret);

        if (logicalCaret > 0)
            raw = raw.Remove(logicalCaret - 1, 1);

        return BuildResult(raw, logicalCaret - 1);
    }

    public MaskResult Delete(string text, int caret)
    {
        var raw = RemoveFormatting(text);
        var logicalCaret = GetRawCaret(text, caret);

        if (logicalCaret < raw.Length)
            raw = raw.Remove(logicalCaret, 1);

        return BuildResult(raw, logicalCaret);
    }

    // --------------------

    private string RemoveFormatting(string text)
    {
        var group = _culture.NumberFormat.NumberGroupSeparator;
        return text.Replace(group, "");
    }

    private string SanitizeRaw(string raw)
    {
        raw = new string(raw.Where(char.IsDigit).ToArray());

        if (_decimals > 0 && raw.Length > _decimals)
        {
            var intPart = raw[..^_decimals];
            var fracPart = raw[^_decimals..];
            raw = intPart + _culture.NumberFormat.NumberDecimalSeparator + fracPart;
        }

        return raw;
    }

    private MaskResult BuildResult(string raw, int logicalCaret)
    {
        if (!decimal.TryParse(raw,
            NumberStyles.Number,
            _culture,
            out var value))
            value = 0;

        var format = _grouping
            ? $"N{_decimals}"
            : $"F{_decimals}";

        var formatted = value.ToString(format, _culture);

        var visualCaret = GetVisualCaret(formatted, logicalCaret);

        return new MaskResult(formatted, visualCaret);
    }

    private int GetRawCaret(string text, int visualCaret)
    {
        var group = _culture.NumberFormat.NumberGroupSeparator;

        return text
            .Take(visualCaret)
            .Count(c => c.ToString() != group);
    }

    private int GetVisualCaret(string formatted, int logicalCaret)
    {
        var group = _culture.NumberFormat.NumberGroupSeparator;

        int count = 0;

        for (int i = 0; i < formatted.Length; i++)
        {
            if (formatted[i].ToString() != group)
                count++;

            if (count >= logicalCaret)
                return i + 1;
        }

        return formatted.Length;
    }
}