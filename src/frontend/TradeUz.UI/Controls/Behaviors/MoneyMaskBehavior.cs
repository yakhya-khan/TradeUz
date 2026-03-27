//using Avalonia.Controls;
//using Avalonia.Input;
//using Avalonia.Interactivity;
//using Avalonia.Xaml.Interactivity;
//using TradeUz.UI.Controls.Inputs.Masking;

//namespace TradeUz.UI.Controls.Behaviors;

//public class MoneyMaskBehavior : Behavior<TextBox>
//{
//    private NumericMaskEngine? _engine;

//    protected override void OnAttached()
//    {
//        base.OnAttached();

//        _engine = new NumericMaskEngine(2);

//        AssociatedObject.AddHandler(
//            InputElement.TextInputEvent,
//            OnTextInput,
//            RoutingStrategies.Tunnel);

//        AssociatedObject.AddHandler(
//            InputElement.KeyDownEvent,
//            OnKeyDown,
//            RoutingStrategies.Tunnel);
//    }

//    private void OnTextInput(object? sender, TextInputEventArgs e)
//    {
//        e.Handled = true;

//        var result = _engine!.Insert(
//            AssociatedObject.Text ?? "",
//            AssociatedObject.CaretIndex,
//            e.Text[0]);

//        Apply(result);
//    }

//    private void OnKeyDown(object? sender, KeyEventArgs e)
//    {
//        if (e.Key == Key.Back)
//        {
//            e.Handled = true;

//            var result = _engine!.Backspace(
//                AssociatedObject.Text ?? "",
//                AssociatedObject.CaretIndex);

//            Apply(result);
//        }
//    }

//    private void Apply(MaskResult result)
//    {
//        AssociatedObject.Text = result.Text;
//        AssociatedObject.CaretIndex = result.Caret;
//    }
//}