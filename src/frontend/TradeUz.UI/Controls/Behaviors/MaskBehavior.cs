//using Avalonia.Controls;
//using Avalonia.Input;
//using Avalonia.Interactivity;
//using Avalonia.Xaml.Interactivity;
//using TradeUz.UI.Controls.Inputs.Masking;

//namespace TradeUz.UI.Controls.Behaviors;

//public class MaskBehavior : Behavior<TextBox>
//{
//    private NumericMaskEngine? _engine;
//    private bool _internalUpdate;

//    public int Decimals { get; set; } = 2;

//    protected override void OnAttached()
//    {
//        base.OnAttached();

//        _engine = new NumericMaskEngine(Decimals);

//        AssociatedObject.AddHandler(
//            InputElement.TextInputEvent,
//            OnTextInput,
//            RoutingStrategies.Tunnel);

//        AssociatedObject.AddHandler(
//            InputElement.KeyDownEvent,
//            OnKeyDown,
//            RoutingStrategies.Tunnel);

//        AssociatedObject.PastingFromClipboard += OnPaste;

//        AssociatedObject.LostFocus += OnLostFocus;
//    }

//    protected override void OnDetaching()
//    {
//        base.OnDetaching();

//        AssociatedObject.RemoveHandler(InputElement.TextInputEvent, OnTextInput);
//        AssociatedObject.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);

//        AssociatedObject.PastingFromClipboard -= OnPaste;

//        AssociatedObject.LostFocus -= OnLostFocus;
//    }

//    private void OnTextInput(object? sender, TextInputEventArgs e)
//    {
//        if (_internalUpdate || _engine == null)
//            return;

//        if (string.IsNullOrEmpty(e.Text))
//            return;

//        e.Handled = true;

//        var result = _engine.Insert(
//            AssociatedObject.Text ?? "",
//            AssociatedObject.CaretIndex,
//            e.Text[0]);

//        Apply(result);
//    }

//    private void OnKeyDown(object? sender, KeyEventArgs e)
//    {
//        if (_internalUpdate || _engine == null)
//            return;

//        if (e.Key == Key.Back)
//        {
//            e.Handled = true;

//            var result = _engine.Backspace(
//                AssociatedObject.Text ?? "",
//                AssociatedObject.CaretIndex);

//            Apply(result);
//        }

//        if (e.Key == Key.Delete)
//        {
//            e.Handled = true;

//            var result = _engine.Delete(
//                AssociatedObject.Text ?? "",
//                AssociatedObject.CaretIndex);

//            Apply(result);
//        }
//    }

//    private void OnPaste(object? sender, RoutedEventArgs e)
//    {
//        if (_internalUpdate || _engine == null)
//            return;

//        var text = AssociatedObject.Text ?? "";

//        var result = _engine.Normalize(text);

//        Apply(result);
//    }

//    private void OnLostFocus(object? sender, RoutedEventArgs e)
//    {
//        if (_engine == null)
//            return;

//        var result = _engine.Normalize(
//            AssociatedObject.Text ?? "");

//        Apply(result);
//    }

//    private void Apply(MaskResult result)
//    {
//        _internalUpdate = true;

//        AssociatedObject.Text = result.Text;
//        AssociatedObject.CaretIndex = result.Caret;

//        _internalUpdate = false;
//    }
//}