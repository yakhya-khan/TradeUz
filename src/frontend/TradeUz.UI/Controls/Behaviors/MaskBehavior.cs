using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using TradeUz.UI.Controls.Masking;

namespace TradeUz.UI.Controls.Behaviors;

public class MaskBehavior : Behavior<TextBox>
{
    public int Decimals { get; set; } = 2;
    public bool Grouping { get; set; } = true;

    private IMaskEngine? _engine;

    protected override void OnAttached()
    {
        base.OnAttached();

        _engine = new NumericMaskEngine(
            Decimals,
            Grouping,
            false);

        AssociatedObject.AddHandler(
            InputElement.TextInputEvent,
            OnTextInput,
            RoutingStrategies.Tunnel);

        AssociatedObject.AddHandler(
            InputElement.KeyDownEvent,
            OnKeyDown,
            RoutingStrategies.Tunnel);
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (_engine == null)
            return;

        e.Handled = true;

        var result = _engine.Insert(
            AssociatedObject.Text ?? "",
            AssociatedObject.CaretIndex,
            e.Text);

        Apply(result);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_engine == null)
            return;

        if (e.Key == Key.Back)
        {
            e.Handled = true;
            Apply(_engine.Backspace(
                AssociatedObject.Text ?? "",
                AssociatedObject.CaretIndex));
        }

        if (e.Key == Key.Delete)
        {
            e.Handled = true;
            Apply(_engine.Delete(
                AssociatedObject.Text ?? "",
                AssociatedObject.CaretIndex));
        }
    }

    private void Apply(MaskResult result)
    {
        AssociatedObject.Text = result.Text;
        AssociatedObject.CaretIndex = result.Caret;
    }
}