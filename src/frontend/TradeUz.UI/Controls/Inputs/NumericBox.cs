using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Input.Platform;
using Avalonia.Media;
using TradeUz.UI.Controls.Inputs.Masking;

namespace TradeUz.UI.Controls.Inputs;

/// <summary>
/// Универсальное поле ввода чисел.
/// Поддерживает целые и дробные значения, системные разделители и группировку на лету.
/// </summary>
public class NumericBox : TextBox
{
    // Используем стандартный template обычного TextBox,
    // чтобы кастомный контрол корректно отображался без отдельной темы.
    protected override Type StyleKeyOverride => typeof(TextBox);

    public static readonly StyledProperty<decimal?> ValueProperty =
        AvaloniaProperty.Register<NumericBox, decimal?>(
            nameof(Value),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> DecimalPlacesProperty =
        AvaloniaProperty.Register<NumericBox, int>(nameof(DecimalPlaces), 0);

    public static readonly StyledProperty<bool> UseGroupingProperty =
        AvaloniaProperty.Register<NumericBox, bool>(nameof(UseGrouping));

    public static readonly StyledProperty<bool> AllowNegativeProperty =
        AvaloniaProperty.Register<NumericBox, bool>(nameof(AllowNegative), true);

    public static readonly StyledProperty<decimal?> MinValueProperty =
        AvaloniaProperty.Register<NumericBox, decimal?>(nameof(MinValue));

    public static readonly StyledProperty<decimal?> MaxValueProperty =
        AvaloniaProperty.Register<NumericBox, decimal?>(nameof(MaxValue));

    public static readonly StyledProperty<bool> AllowEmptyProperty =
        AvaloniaProperty.Register<NumericBox, bool>(nameof(AllowEmpty), true);

    public static readonly StyledProperty<bool> PadFractionZerosOnBlurProperty =
        AvaloniaProperty.Register<NumericBox, bool>(nameof(PadFractionZerosOnBlur));

    private bool _internalUpdate;
    private string _rawText = string.Empty;

    public NumericBox()
    {
        TextAlignment = TextAlignment.Right;

        AddHandler(TextInputEvent, OnTextInput, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        PastingFromClipboard += OnPaste;
        LostFocus += OnLostFocus;
        PropertyChanged += OnControlPropertyChanged;
    }

    public decimal? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int DecimalPlaces
    {
        get => GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
    }

    public bool UseGrouping
    {
        get => GetValue(UseGroupingProperty);
        set => SetValue(UseGroupingProperty, value);
    }

    public bool AllowNegative
    {
        get => GetValue(AllowNegativeProperty);
        set => SetValue(AllowNegativeProperty, value);
    }

    public decimal? MinValue
    {
        get => GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public decimal? MaxValue
    {
        get => GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public bool AllowEmpty
    {
        get => GetValue(AllowEmptyProperty);
        set => SetValue(AllowEmptyProperty, value);
    }

    public bool PadFractionZerosOnBlur
    {
        get => GetValue(PadFractionZerosOnBlurProperty);
        set => SetValue(PadFractionZerosOnBlurProperty, value);
    }

    private void OnControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_internalUpdate)
            return;

        if (e.Property == ValueProperty)
        {
            SyncFromValue();
            return;
        }

        if (e.Property == DecimalPlacesProperty ||
            e.Property == UseGroupingProperty ||
            e.Property == AllowNegativeProperty ||
            e.Property == MinValueProperty ||
            e.Property == MaxValueProperty ||
            e.Property == AllowEmptyProperty ||
            e.Property == PadFractionZerosOnBlurProperty)
        {
            RefreshTextFromCurrentState();
        }
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (_internalUpdate || string.IsNullOrEmpty(e.Text))
            return;

        var render = GetRenderResult();
        var (rawSelectionStart, rawSelectionLength) = GetRawSelection(render);
        var rawCaret = render.MapDisplayToRawCaret(CaretIndex);
        var editCaret = rawSelectionLength > 0 ? rawSelectionStart : rawCaret;
        var engine = CreateEngine();
        var result = engine.Insert(_rawText, editCaret, rawSelectionLength, e.Text);

        e.Handled = true;
        ApplyRawResult(result);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_internalUpdate)
            return;

        var render = GetRenderResult();
        var (rawSelectionStart, rawSelectionLength) = GetRawSelection(render);
        var rawCaret = render.MapDisplayToRawCaret(CaretIndex);
        var engine = CreateEngine();
        NumericEditResult? result = null;

        if (e.Key == Key.Back)
            result = engine.Backspace(_rawText, rawSelectionStart == rawCaret ? rawCaret : rawSelectionStart, rawSelectionLength);

        if (e.Key == Key.Delete)
            result = engine.Delete(_rawText, rawSelectionStart, rawSelectionLength);

        if (result == null)
            return;

        e.Handled = true;
        ApplyRawResult(result);
    }

    private async void OnPaste(object? sender, RoutedEventArgs e)
    {
        if (_internalUpdate)
            return;

        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel?.Clipboard == null)
            return;

        var clipboardText = await topLevel.Clipboard.TryGetTextAsync();

        if (string.IsNullOrWhiteSpace(clipboardText))
            return;

        var render = GetRenderResult();
        var (rawSelectionStart, rawSelectionLength) = GetRawSelection(render);
        var engine = CreateEngine();
        var result = engine.Paste(_rawText, rawSelectionStart, rawSelectionLength, clipboardText);

        e.Handled = true;
        ApplyRawResult(result);
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        var engine = CreateEngine();
        _rawText = engine.NormalizeOnLostFocus(
            _rawText,
            AllowEmpty,
            PadFractionZerosOnBlur,
            MinValue,
            MaxValue);

        ApplyRawText(_rawText, _rawText.Length);
    }

    private void SyncFromValue()
    {
        var engine = CreateEngine();

        if (Value.HasValue)
            _rawText = engine.FormatValueAsRaw(Value.Value, PadFractionZerosOnBlur && !IsFocused);
        else
            _rawText = string.Empty;

        ApplyRawText(_rawText, _rawText.Length);
    }

    private void RefreshTextFromCurrentState()
    {
        var engine = CreateEngine();
        var currentValue = Value;

        if (currentValue.HasValue)
            _rawText = engine.FormatValueAsRaw(currentValue.Value, PadFractionZerosOnBlur && !IsFocused);
        else
            _rawText = engine.SanitizeRaw(_rawText);

        ApplyRawText(_rawText, Math.Min(_rawText.Length, CaretIndex));
    }

    private void ApplyRawResult(NumericEditResult result)
    {
        _rawText = result.RawText;
        ApplyRawText(_rawText, result.RawCaret);
    }

    private void ApplyRawText(string rawText, int rawCaret)
    {
        var render = CreateEngine().Render(rawText);

        _internalUpdate = true;

        Text = render.Text;
        CaretIndex = render.MapRawToDisplayCaret(rawCaret);
        SelectionStart = CaretIndex;
        SelectionEnd = CaretIndex;

        if (CreateEngine().TryParseValue(rawText, out var value))
            Value = value;
        else
            Value = null;

        _internalUpdate = false;
    }

    private NumericInputEngine CreateEngine()
        => new(
            decimalPlaces: Math.Clamp(DecimalPlaces, 0, 28),
            useGrouping: UseGrouping,
            allowNegative: AllowNegative,
            culture: CultureInfo.CurrentCulture);

    private NumericRenderResult GetRenderResult()
        => CreateEngine().Render(_rawText);

    private (int Start, int Length) GetRawSelection(NumericRenderResult render)
    {
        var selectionStart = Math.Min(SelectionStart, SelectionEnd);
        var selectionEnd = Math.Max(SelectionStart, SelectionEnd);
        var rawStart = render.MapDisplayToRawCaret(selectionStart);
        var rawEnd = render.MapDisplayToRawCaret(selectionEnd);

        return (rawStart, rawEnd - rawStart);
    }
}
