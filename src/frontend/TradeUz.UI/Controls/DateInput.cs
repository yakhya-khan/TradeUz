using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;
using System;
using System.Globalization;
using Calendar = Avalonia.Controls.Calendar;

namespace TradeUz.UI.Controls;

public class DateInput : TemplatedControl
{
    public static readonly StyledProperty<DateTime?> SelectedDateProperty =
        AvaloniaProperty.Register<DateInput, DateTime?>(
            nameof(SelectedDate),
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<DateTime?> MinDateProperty =
        AvaloniaProperty.Register<DateInput, DateTime?>(
            nameof(MinDate));

    public static readonly StyledProperty<DateTime?> MaxDateProperty =
        AvaloniaProperty.Register<DateInput, DateTime?>(
            nameof(MaxDate));

    public static readonly StyledProperty<string> DateFormatProperty =
        AvaloniaProperty.Register<DateInput, string>(
            nameof(DateFormat),
            "dd.MM.yyyy");

    private TextBox? _textBox;
    private Popup? _popup;
    private Calendar? _calendar;

    public DateTime? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public DateTime? MinDate
    {
        get => GetValue(MinDateProperty);
        set => SetValue(MinDateProperty, value);
    }

    public DateTime? MaxDate
    {
        get => GetValue(MaxDateProperty);
        set => SetValue(MaxDateProperty, value);
    }

    public string DateFormat
    {
        get => GetValue(DateFormatProperty);
        set => SetValue(DateFormatProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _textBox = e.NameScope.Find<TextBox>("PART_TextBox");
        _popup = e.NameScope.Find<Popup>("PART_Popup");
        _calendar = e.NameScope.Find<Calendar>("PART_Calendar");

        if (_textBox != null)
        {
            _textBox.LostFocus += OnTextBoxLostFocus;
            UpdateText();
        }

        if (_calendar != null)
        {
            _calendar.SelectedDatesChanged += (_, _) =>
            {
                SelectedDate = _calendar.SelectedDate;
                _popup!.IsOpen = false;
            };
        }
    }

    private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (_textBox == null)
            return;

        if (DateTime.TryParseExact(
                _textBox.Text,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            SelectedDate = parsed;
        }

        UpdateText();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedDateProperty)
            UpdateText();
    }

    private void UpdateText()
    {
        if (_textBox != null)
            _textBox.Text = SelectedDate?.ToString(DateFormat);
    }

    public void OpenPopup()
    {
        if (_popup != null)
            _popup.IsOpen = true;
    }
}