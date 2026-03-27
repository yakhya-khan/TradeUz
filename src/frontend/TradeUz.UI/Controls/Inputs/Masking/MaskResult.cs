namespace TradeUz.UI.Controls.Inputs.Masking;

public sealed class MaskResult
{
    public string Text { get; }
    public int Caret { get; }

    public MaskResult(string text, int caret)
    {
        Text = text;
        Caret = caret;
    }
}