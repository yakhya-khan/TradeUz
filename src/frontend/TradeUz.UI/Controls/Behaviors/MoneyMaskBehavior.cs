namespace TradeUz.UI.Controls.Behaviors;

public class MoneyMaskBehavior : NumericMaskBehavior
{
    public MoneyMaskBehavior()
    {
        Decimals = 2;
        UseGrouping = true;
        AllowNegative = false;
    }
}