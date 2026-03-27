using Avalonia.Media;

namespace TradeUz.UI.Pages.Common;

public sealed class MetricCardViewModel
{
    public MetricCardViewModel(string title, string value, string subtitle, string accentHex)
    {
        Title = title;
        Value = value;
        Subtitle = subtitle;

        var color = Color.Parse(accentHex);
        AccentBrush = new SolidColorBrush(color);
        AccentBackground = new SolidColorBrush(color, 0.14);
    }

    public string Title { get; }
    public string Value { get; }
    public string Subtitle { get; }
    public IBrush AccentBrush { get; }
    public IBrush AccentBackground { get; }
}
