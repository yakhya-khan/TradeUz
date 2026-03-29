using System;

namespace TradeUz.UI.Controls.Inputs.Masking;

internal sealed class NumericRenderResult
{
    private readonly int[] _rawToDisplayCarets;

    public NumericRenderResult(string text, int[] rawToDisplayCarets)
    {
        Text = text;
        _rawToDisplayCarets = rawToDisplayCarets;
    }

    public string Text { get; }

    public int MapRawToDisplayCaret(int rawCaret)
    {
        if (_rawToDisplayCarets.Length == 0)
            return 0;

        rawCaret = Math.Clamp(rawCaret, 0, _rawToDisplayCarets.Length - 1);
        return _rawToDisplayCarets[rawCaret];
    }

    public int MapDisplayToRawCaret(int displayCaret)
    {
        displayCaret = Math.Clamp(displayCaret, 0, Text.Length);

        var rawCaret = 0;

        for (var i = 0; i < _rawToDisplayCarets.Length; i++)
        {
            if (_rawToDisplayCarets[i] > displayCaret)
                break;

            rawCaret = i;
        }

        return rawCaret;
    }
}
