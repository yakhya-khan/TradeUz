using System;
using System.Globalization;

namespace TradeUz.UI.Controls.Formatting;

public static class NumericFormatter
{
    public static string Format(
        decimal value,
        int decimals,
        bool useGrouping,
        CultureInfo culture)
    {
        var format = useGrouping
            ? $"N{decimals}"
            : $"F{decimals}";

        return value.ToString(format, culture);
    }

    public static bool TryParse(
        string? text,
        CultureInfo culture,
        out decimal result)
    {
        return decimal.TryParse(
            text,
            NumberStyles.Number,
            culture,
            out result);
    }
}