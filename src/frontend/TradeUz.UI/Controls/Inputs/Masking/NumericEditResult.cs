namespace TradeUz.UI.Controls.Inputs.Masking;

internal sealed class NumericEditResult
{
    public NumericEditResult(string rawText, int rawCaret)
    {
        RawText = rawText;
        RawCaret = rawCaret;
    }

    public string RawText { get; }

    public int RawCaret { get; }
}
