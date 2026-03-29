using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TradeUz.UI.Controls.Inputs.Masking;

internal sealed class NumericInputEngine
{
    private readonly int _decimalPlaces;
    private readonly bool _useGrouping;
    private readonly bool _allowNegative;
    private readonly CultureInfo _culture;
    private readonly string _decimalSeparator;
    private readonly string _groupSeparator;
    private readonly int[] _groupSizes;

    public NumericInputEngine(
        int decimalPlaces,
        bool useGrouping,
        bool allowNegative,
        CultureInfo culture)
    {
        _decimalPlaces = Math.Clamp(decimalPlaces, 0, 28);
        _useGrouping = useGrouping;
        _allowNegative = allowNegative;
        _culture = culture;
        _decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
        _groupSeparator = culture.NumberFormat.NumberGroupSeparator;
        _groupSizes = culture.NumberFormat.NumberGroupSizes
            .Where(size => size > 0)
            .DefaultIfEmpty(3)
            .ToArray();
    }

    public NumericEditResult Insert(string rawText, int rawCaret, int rawSelectionLength, string input)
    {
        var candidate = SanitizeRaw(rawText);
        rawCaret = Math.Clamp(rawCaret, 0, candidate.Length);
        rawSelectionLength = Math.Clamp(rawSelectionLength, 0, candidate.Length - rawCaret);

        if (rawSelectionLength > 0)
            candidate = candidate.Remove(rawCaret, rawSelectionLength);

        foreach (var token in NormalizeTypedInput(input))
        {
            if (TryInsertToken(candidate, rawCaret, token, out var updatedText, out var updatedCaret))
            {
                candidate = updatedText;
                rawCaret = updatedCaret;
            }
        }

        return NormalizeEditingResult(candidate, rawCaret);
    }

    public NumericEditResult Paste(string rawText, int rawCaret, int rawSelectionLength, string pasteText)
    {
        var candidate = SanitizeRaw(rawText);
        rawCaret = Math.Clamp(rawCaret, 0, candidate.Length);
        rawSelectionLength = Math.Clamp(rawSelectionLength, 0, candidate.Length - rawCaret);

        if (rawSelectionLength > 0)
            candidate = candidate.Remove(rawCaret, rawSelectionLength);

        foreach (var token in NormalizePastedInput(pasteText))
        {
            if (TryInsertToken(candidate, rawCaret, token, out var updatedText, out var updatedCaret))
            {
                candidate = updatedText;
                rawCaret = updatedCaret;
            }
        }

        return NormalizeEditingResult(candidate, rawCaret);
    }

    public NumericEditResult Backspace(string rawText, int rawCaret, int rawSelectionLength)
    {
        var candidate = SanitizeRaw(rawText);
        rawCaret = Math.Clamp(rawCaret, 0, candidate.Length);
        rawSelectionLength = Math.Clamp(rawSelectionLength, 0, candidate.Length - rawCaret);

        if (rawSelectionLength > 0)
        {
            candidate = candidate.Remove(rawCaret, rawSelectionLength);
            return NormalizeEditingResult(candidate, rawCaret);
        }

        if (rawCaret == 0)
            return new NumericEditResult(candidate, 0);

        candidate = candidate.Remove(rawCaret - 1, 1);
        return NormalizeEditingResult(candidate, rawCaret - 1);
    }

    public NumericEditResult Delete(string rawText, int rawCaret, int rawSelectionLength)
    {
        var candidate = SanitizeRaw(rawText);
        rawCaret = Math.Clamp(rawCaret, 0, candidate.Length);
        rawSelectionLength = Math.Clamp(rawSelectionLength, 0, candidate.Length - rawCaret);

        if (rawSelectionLength > 0)
        {
            candidate = candidate.Remove(rawCaret, rawSelectionLength);
            return NormalizeEditingResult(candidate, rawCaret);
        }

        if (rawCaret >= candidate.Length)
            return new NumericEditResult(candidate, candidate.Length);

        candidate = candidate.Remove(rawCaret, 1);
        return NormalizeEditingResult(candidate, rawCaret);
    }

    public NumericRenderResult Render(string rawText)
    {
        var canonical = SanitizeRaw(rawText);

        if (string.IsNullOrEmpty(canonical))
            return new NumericRenderResult(string.Empty, new[] { 0 });

        var rawToDisplay = new int[canonical.Length + 1];
        var display = new StringBuilder();
        var rawIndex = 0;

        rawToDisplay[0] = 0;

        if (canonical.StartsWith("-", StringComparison.Ordinal))
        {
            display.Append('-');
            rawIndex++;
            rawToDisplay[rawIndex] = display.Length;
        }

        var numberStart = canonical.StartsWith("-", StringComparison.Ordinal) ? 1 : 0;
        var decimalIndex = canonical.IndexOf('.');
        var hasDecimalSeparator = decimalIndex >= 0;
        var integerLength = hasDecimalSeparator
            ? decimalIndex - numberStart
            : canonical.Length - numberStart;

        for (var integerIndex = 0; integerIndex < integerLength; integerIndex++)
        {
            if (_useGrouping && ShouldInsertGroupSeparator(integerIndex, integerLength))
                display.Append(_groupSeparator);

            display.Append(canonical[numberStart + integerIndex]);
            rawIndex++;
            rawToDisplay[rawIndex] = display.Length;
        }

        if (hasDecimalSeparator)
        {
            display.Append(_decimalSeparator);
            rawIndex++;
            rawToDisplay[rawIndex] = display.Length;

            for (var i = decimalIndex + 1; i < canonical.Length; i++)
            {
                display.Append(canonical[i]);
                rawIndex++;
                rawToDisplay[rawIndex] = display.Length;
            }
        }

        return new NumericRenderResult(display.ToString(), rawToDisplay);
    }

    public bool TryParseValue(string rawText, out decimal value)
    {
        var candidate = SanitizeRaw(rawText);

        return TryParseCanonicalValue(candidate, out value);
    }

    private static bool TryParseCanonicalValue(string candidate, out decimal value)
    {
        // Здесь ожидаем уже каноническую "сырую" строку и не вызываем SanitizeRaw,
        // чтобы не попасть в рекурсивный цикл при нормализации.
        if (string.IsNullOrEmpty(candidate) || candidate == "-")
        {
            value = default;
            return false;
        }

        if (candidate.EndsWith(".", StringComparison.Ordinal))
            candidate = candidate[..^1];

        return decimal.TryParse(
            candidate,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out value);
    }

    public string NormalizeOnLostFocus(
        string rawText,
        bool allowEmpty,
        bool padFractionZerosOnBlur,
        decimal? minValue,
        decimal? maxValue)
    {
        var normalizedRaw = SanitizeRaw(rawText);

        if (string.IsNullOrEmpty(normalizedRaw) || normalizedRaw == "-")
            return allowEmpty
                ? string.Empty
                : FormatValueAsRaw(0m, padFractionZerosOnBlur);

        var normalizedCompletedRaw = NormalizeCompletedRaw(normalizedRaw);

        if (!TryParseValue(normalizedCompletedRaw, out var currentValue))
            return allowEmpty
                ? string.Empty
                : FormatValueAsRaw(0m, padFractionZerosOnBlur);

        var adjustedValue = currentValue;

        if (minValue.HasValue && adjustedValue < minValue.Value)
            adjustedValue = minValue.Value;

        if (maxValue.HasValue && adjustedValue > maxValue.Value)
            adjustedValue = maxValue.Value;

        adjustedValue = decimal.Round(
            adjustedValue,
            _decimalPlaces,
            MidpointRounding.AwayFromZero);

        if (!padFractionZerosOnBlur && adjustedValue == currentValue)
            return normalizedCompletedRaw;

        return FormatValueAsRaw(adjustedValue, padFractionZerosOnBlur);
    }

    public string FormatValueAsRaw(decimal value, bool padFractionZeros)
    {
        if (!_allowNegative && value < 0)
            value = 0;

        value = decimal.Round(value, _decimalPlaces, MidpointRounding.AwayFromZero);

        if (_decimalPlaces == 0)
            return value.ToString("0", CultureInfo.InvariantCulture);

        var format = padFractionZeros
            ? $"F{_decimalPlaces}"
            : $"0.{new string('#', _decimalPlaces)}";

        return value.ToString(format, CultureInfo.InvariantCulture);
    }

    public string SanitizeRaw(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return string.Empty;

        var builder = new StringBuilder();
        var hasDecimalSeparator = false;

        foreach (var character in rawText.Trim())
        {
            if (char.IsDigit(character))
            {
                builder.Append(character);
                continue;
            }

            if (_allowNegative && character == '-' && builder.Length == 0)
            {
                builder.Append(character);
                continue;
            }

            if (_decimalPlaces > 0 && !hasDecimalSeparator && (character == '.' || character == ','))
            {
                builder.Append('.');
                hasDecimalSeparator = true;
            }
        }

        var canonical = EnsureLeadingZero(builder.ToString());
        canonical = LimitFractionLength(canonical);

        return NormalizeCompletedRaw(canonical, preserveTrailingSeparator: canonical.EndsWith(".", StringComparison.Ordinal));
    }

    private NumericEditResult NormalizeEditingResult(string rawText, int rawCaret)
    {
        var canonical = EnsureLeadingZero(rawText);
        canonical = LimitFractionLength(canonical);
        canonical = NormalizeLeadingZeros(canonical, ref rawCaret);

        if (!CanRepresent(canonical))
            return new NumericEditResult(SanitizeRaw(rawText), Math.Clamp(rawCaret, 0, SanitizeRaw(rawText).Length));

        rawCaret = Math.Clamp(rawCaret, 0, canonical.Length);

        return new NumericEditResult(canonical, rawCaret);
    }

    private bool TryInsertToken(string rawText, int rawCaret, char token, out string updatedText, out int updatedCaret)
    {
        updatedText = rawText;
        updatedCaret = rawCaret;

        if (char.IsDigit(token))
        {
            if (HasReachedFractionLimit(rawText, rawCaret))
                return false;

            if (ShouldReplaceStandaloneZero(rawText, rawCaret))
            {
                updatedText = rawText[0] == '-'
                    ? $"-{token}"
                    : token.ToString();
                updatedCaret = updatedText.Length;
            }
            else
            {
                updatedText = rawText.Insert(rawCaret, token.ToString());
                updatedCaret = rawCaret + 1;
            }

            if (!CanRepresent(updatedText))
            {
                updatedText = rawText;
                updatedCaret = rawCaret;
                return false;
            }

            return true;
        }

        if (token == '.')
        {
            if (_decimalPlaces == 0 || rawText.Contains('.'))
                return false;

            if (string.IsNullOrEmpty(rawText))
            {
                updatedText = "0.";
                updatedCaret = 2;
                return true;
            }

            if (rawText == "-")
            {
                updatedText = "-0.";
                updatedCaret = 3;
                return true;
            }

            updatedText = rawText.Insert(rawCaret, ".");
            updatedCaret = rawCaret + 1;

            if (!CanRepresent(updatedText))
            {
                updatedText = rawText;
                updatedCaret = rawCaret;
                return false;
            }

            return true;
        }

        if (token == '-')
        {
            if (!_allowNegative || rawCaret != 0 || rawText.StartsWith("-", StringComparison.Ordinal))
                return false;

            updatedText = "-" + rawText;
            updatedCaret = 1;
            return true;
        }

        return false;
    }

    private IEnumerable<char> NormalizeTypedInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            yield break;

        foreach (var character in input)
        {
            if (char.IsDigit(character))
            {
                yield return character;
                continue;
            }

            if (character == '-')
            {
                yield return character;
                continue;
            }

            if (character == '.' || character == ',' || _decimalSeparator.Contains(character))
                yield return '.';
        }
    }

    private IEnumerable<char> NormalizePastedInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            yield break;

        var trimmed = input.Trim();
        var decimalIndex = FindLastDecimalSeparatorIndex(trimmed);
        var isNegative = _allowNegative && trimmed.StartsWith("-", StringComparison.Ordinal);

        if (isNegative)
            yield return '-';

        for (var i = 0; i < trimmed.Length; i++)
        {
            var character = trimmed[i];

            if (char.IsDigit(character))
            {
                yield return character;
                continue;
            }

            if (_decimalPlaces > 0 && i == decimalIndex)
                yield return '.';
        }
    }

    private int FindLastDecimalSeparatorIndex(string input)
    {
        if (_decimalPlaces == 0)
            return -1;

        for (var i = input.Length - 1; i >= 0; i--)
        {
            var character = input[i];

            if (character == '.' || character == ',' || _decimalSeparator.Contains(character))
                return i;
        }

        return -1;
    }

    private bool HasReachedFractionLimit(string rawText, int rawCaret)
    {
        if (_decimalPlaces == 0)
            return false;

        var decimalIndex = rawText.IndexOf('.');

        if (decimalIndex < 0 || rawCaret <= decimalIndex)
            return false;

        var fractionLength = rawText.Length - decimalIndex - 1;
        return fractionLength >= _decimalPlaces;
    }

    private bool ShouldReplaceStandaloneZero(string rawText, int rawCaret)
    {
        if (rawText == "0" && rawCaret >= 1)
            return true;

        return rawText == "-0" && rawCaret >= 2;
    }

    private bool CanRepresent(string rawText)
    {
        if (string.IsNullOrEmpty(rawText) || rawText == "-")
            return true;

        var candidate = rawText.EndsWith(".", StringComparison.Ordinal)
            ? rawText[..^1]
            : rawText;

        if (candidate == "-" || candidate.Length == 0)
            return true;

        return decimal.TryParse(
            candidate,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out _);
    }

    private string EnsureLeadingZero(string rawText)
    {
        if (string.IsNullOrEmpty(rawText) || rawText == "-")
            return rawText;

        if (rawText[0] == '.')
            return "0" + rawText;

        if (rawText.StartsWith("-.", StringComparison.Ordinal))
            return rawText.Insert(1, "0");

        return rawText;
    }

    private string LimitFractionLength(string rawText)
    {
        if (_decimalPlaces == 0)
            return rawText.Replace(".", string.Empty, StringComparison.Ordinal);

        var decimalIndex = rawText.IndexOf('.');

        if (decimalIndex < 0)
            return rawText;

        var fractionLength = rawText.Length - decimalIndex - 1;

        if (fractionLength <= _decimalPlaces)
            return rawText;

        return rawText[..(decimalIndex + _decimalPlaces + 1)];
    }

    private string NormalizeCompletedRaw(string rawText, bool preserveTrailingSeparator = false)
    {
        if (string.IsNullOrEmpty(rawText) || rawText == "-")
            return rawText;

        var canonical = EnsureLeadingZero(rawText);

        if (!preserveTrailingSeparator && canonical.EndsWith(".", StringComparison.Ordinal))
            canonical = canonical[..^1];

        var caret = canonical.Length;
        canonical = NormalizeLeadingZeros(canonical, ref caret);

        if (TryParseCanonicalValue(canonical, out var value) && value == 0m)
        {
            if (preserveTrailingSeparator && _decimalPlaces > 0)
                return "0.";

            return FormatValueAsRaw(0m, padFractionZeros: false);
        }

        return canonical;
    }

    private string NormalizeLeadingZeros(string rawText, ref int rawCaret)
    {
        if (string.IsNullOrEmpty(rawText) || rawText == "-")
            return rawText;

        var hasSign = rawText.StartsWith("-", StringComparison.Ordinal);
        var numberStart = hasSign ? 1 : 0;
        var decimalIndex = rawText.IndexOf('.');
        var integerLength = decimalIndex >= 0
            ? decimalIndex - numberStart
            : rawText.Length - numberStart;

        if (integerLength <= 1)
            return rawText;

        var integerPart = rawText.Substring(numberStart, integerLength);
        var normalizedIntegerPart = integerPart.TrimStart('0');

        if (normalizedIntegerPart.Length == 0)
            normalizedIntegerPart = "0";

        if (normalizedIntegerPart.Length == integerPart.Length)
            return rawText;

        var removed = integerPart.Length - normalizedIntegerPart.Length;
        var suffix = rawText[(numberStart + integerLength)..];
        var normalized = (hasSign ? "-" : string.Empty) + normalizedIntegerPart + suffix;

        var firstIntegerCaret = numberStart;
        var oldIntegerEndCaret = numberStart + integerPart.Length;

        if (rawCaret <= firstIntegerCaret + removed)
            rawCaret = firstIntegerCaret;
        else if (rawCaret <= oldIntegerEndCaret)
            rawCaret -= removed;
        else
            rawCaret -= removed;

        return normalized;
    }

    private bool ShouldInsertGroupSeparator(int integerIndex, int integerLength)
    {
        if (integerIndex == 0 || string.IsNullOrEmpty(_groupSeparator))
            return false;

        var digitsToRight = integerLength - integerIndex;
        var groupSize = _groupSizes[0];

        if (_groupSizes.Length == 1)
            return digitsToRight % groupSize == 0;

        var remainingDigits = digitsToRight;
        var currentGroupIndex = 0;

        while (remainingDigits > _groupSizes[currentGroupIndex])
        {
            remainingDigits -= _groupSizes[currentGroupIndex];

            if (currentGroupIndex < _groupSizes.Length - 1)
                currentGroupIndex++;
        }

        return remainingDigits == _groupSizes[currentGroupIndex];
    }
}
