using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using System.Globalization;
using System.Linq;
using TradeUz.UI.Controls.Formatting;

namespace TradeUz.UI.Controls.Behaviors;

public class NumericMaskBehavior : Behavior<TextBox>
{
    public int Decimals { get; set; } = 0;
    public bool UseGrouping { get; set; } = false;
    public bool AllowNegative { get; set; } = false;

    private bool _updating;
    private CultureInfo _culture = CultureInfo.CurrentCulture;

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.AddHandler(
            InputElement.TextInputEvent,
            OnTextInput,
            RoutingStrategies.Tunnel);

        AssociatedObject.AddHandler(
            InputElement.KeyUpEvent,
            OnKeyUp,
            RoutingStrategies.Tunnel);

        AssociatedObject.LostFocus += OnLostFocus;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.RemoveHandler(
            InputElement.TextInputEvent,
            OnTextInput);

        AssociatedObject.RemoveHandler(
            InputElement.KeyUpEvent,
            OnKeyUp);

        AssociatedObject.LostFocus -= OnLostFocus;
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        var allowed = e.Text.All(c =>
            char.IsDigit(c) ||
            (AllowNegative && c == '-') ||
            (Decimals > 0 && c.ToString() == _culture.NumberFormat.NumberDecimalSeparator));

        if (!allowed)
            e.Handled = true;
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (_updating)
            return;

        if (e.Key is Key.Tab or Key.Left or Key.Right)
            return;

        if (!NumericFormatter.TryParse(
            AssociatedObject.Text,
            _culture,
            out var value))
            return;

        _updating = true;

        var formatted = NumericFormatter.Format(
            value,
            Decimals,
            UseGrouping,
            _culture);

        AssociatedObject.Text = formatted;
        AssociatedObject.CaretIndex = formatted.Length;

        _updating = false;
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (!NumericFormatter.TryParse(
            AssociatedObject.Text,
            _culture,
            out var value))
            return;

        var formatted = NumericFormatter.Format(
            value,
            Decimals,
            UseGrouping,
            _culture);

        AssociatedObject.Text = formatted;
    }
}