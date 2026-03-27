namespace TradeUz.UI.Controls.Inputs.Masking;

public interface IMaskEngine
{
    MaskResult Insert(string text, int caret, string input);
    MaskResult Backspace(string text, int caret);
    MaskResult Delete(string text, int caret);
}