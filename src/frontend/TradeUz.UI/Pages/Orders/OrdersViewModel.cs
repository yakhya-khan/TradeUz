
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TradeUz.Contracts;
using TradeUz.UI.Pages.Common;
using TradeUz.UI.Services;

namespace TradeUz.UI.Pages.Orders;

public partial class OrdersViewModel : BasePageViewModel
{
    private readonly ITradeOperationsService _operationsService;
    private readonly List<OrderCardViewModel> _allOrders = [];

    [ObservableProperty] private OrderCardViewModel? _selectedOrder;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _selectedStatusFilter = "Все";
    [ObservableProperty] private bool _isCreateOrderFormOpen;
    [ObservableProperty] private string _newAgentName = string.Empty;
    [ObservableProperty] private string _newRetailPointName = string.Empty;
    [ObservableProperty] private string _newAddress = string.Empty;
    [ObservableProperty] private string _newCity = "Toshkent";
    [ObservableProperty] private string _newPaymentType = "Naqd pul";
    [ObservableProperty] private string _newComment = string.Empty;
    [ObservableProperty] private bool _newIsPriority;
    [ObservableProperty] private DateTime? _newPlannedDeliveryDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private string _newDeliveryTimeText = "14:00";
    [ObservableProperty] private string _newBoxCountText = "1";
    [ObservableProperty] private string _formMessage = string.Empty;
    [ObservableProperty] private bool _isSavingNewOrder;

    public ObservableCollection<MetricCardViewModel> Metrics { get; } = [];
    public ObservableCollection<OrderCardViewModel> Orders { get; } = [];
    public ObservableCollection<string> StatusFilters { get; } = ["Все", "Новые", "Сборка", "В пути", "Проблемные"];
    public ObservableCollection<string> PaymentOptions { get; } = ["Naqd pul", "Перечисление", "Терминал"];
    public ObservableCollection<OrderDraftLineViewModel> DraftLines { get; } = [];

    public OrdersViewModel() : this(new DemoTradeOperationsService()) { }

    public OrdersViewModel(ITradeOperationsService operationsService)
    {
        _operationsService = operationsService;
        ResetDraft();
        Load();
    }

    public string SelectedOrderActionText => SelectedOrder is null ? "Выберите заказ" : GetActionText(SelectedOrder.StatusCode);
    public bool CanAdvanceSelectedOrder => SelectedOrder is not null && SelectedOrder.StatusCode != OrderStatus.Delivered;
    public bool CanSubmitNewOrder => !IsSavingNewOrder;
    public bool HasFormMessage => !string.IsNullOrWhiteSpace(FormMessage);

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedStatusFilterChanged(string value) => ApplyFilters();
    partial void OnFormMessageChanged(string value) => OnPropertyChanged(nameof(HasFormMessage));
    partial void OnIsSavingNewOrderChanged(bool value) => SubmitNewOrderCommand.NotifyCanExecuteChanged();

    partial void OnSelectedOrderChanged(OrderCardViewModel? value)
    {
        OnPropertyChanged(nameof(SelectedOrderActionText));
        AdvanceSelectedOrderCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void OpenCreateOrderForm()
    {
        ResetDraft();
        IsCreateOrderFormOpen = true;
    }

    [RelayCommand]
    private void CancelCreateOrder()
    {
        ResetDraft();
        IsCreateOrderFormOpen = false;
    }

    [RelayCommand]
    private void AddDraftLine()
    {
        DraftLines.Add(new OrderDraftLineViewModel());
    }

    [RelayCommand(CanExecute = nameof(CanAdvanceSelectedOrder))]
    private void AdvanceSelectedOrder()
    {
        if (SelectedOrder is null) return;
        _operationsService.AdvanceOrder(SelectedOrder.Number);
        Load(SelectedOrder.Number);
    }

    [RelayCommand(CanExecute = nameof(CanSubmitNewOrder))]
    private void SubmitNewOrder()
    {
        if (!TryBuildRequest(out var request, out var errorMessage))
        {
            FormMessage = errorMessage;
            return;
        }

        try
        {
            IsSavingNewOrder = true;
            var created = _operationsService.CreateOrder(request!);
            Load(created.Number);
            FormMessage = $"Заказ {created.Number} сохранен.";
            IsCreateOrderFormOpen = false;
            ResetDraft(keepMessage: true);
        }
        catch (Exception ex)
        {
            FormMessage = ex.Message;
        }
        finally
        {
            IsSavingNewOrder = false;
        }
    }

    private bool TryBuildRequest(out CreateTradeOrderRequest? request, out string errorMessage)
    {
        request = null;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NewAgentName) || string.IsNullOrWhiteSpace(NewRetailPointName) || string.IsNullOrWhiteSpace(NewAddress))
        {
            errorMessage = "Заполните агента, торговую точку и адрес доставки.";
            return false;
        }

        if (NewPlannedDeliveryDate is null)
        {
            errorMessage = "Укажите дату доставки.";
            return false;
        }

        if (!TimeSpan.TryParse(NewDeliveryTimeText, CultureInfo.InvariantCulture, out var deliveryTime))
        {
            errorMessage = "Время доставки укажите в формате ЧЧ:ММ, например 14:00.";
            return false;
        }

        if (!int.TryParse(NewBoxCountText, out var boxCount) || boxCount <= 0)
        {
            errorMessage = "Количество коробок должно быть положительным числом.";
            return false;
        }

        var lines = new List<CreateTradeOrderLineRequest>();
        foreach (var line in DraftLines)
        {
            if (string.IsNullOrWhiteSpace(line.ProductName))
            {
                errorMessage = "Укажите название товара в каждой строке.";
                return false;
            }

            if (!int.TryParse(line.QuantityText, out var quantity) || quantity <= 0)
            {
                errorMessage = "Количество товара должно быть положительным числом.";
                return false;
            }

            if (!decimal.TryParse(line.UnitPriceText, NumberStyles.Number, CultureInfo.InvariantCulture, out var unitPrice) || unitPrice <= 0)
            {
                errorMessage = "Цена товара должна быть положительным числом. Используйте точку или запятую.";
                return false;
            }

            lines.Add(new CreateTradeOrderLineRequest { ProductName = line.ProductName.Trim(), Quantity = quantity, Unit = string.IsNullOrWhiteSpace(line.Unit) ? "шт" : line.Unit.Trim(), UnitPrice = unitPrice });
        }

        if (lines.Count == 0)
        {
            errorMessage = "Добавьте хотя бы одну товарную позицию.";
            return false;
        }

        request = new CreateTradeOrderRequest
        {
            AgentName = NewAgentName.Trim(),
            RetailPointName = NewRetailPointName.Trim(),
            Address = NewAddress.Trim(),
            City = string.IsNullOrWhiteSpace(NewCity) ? "Toshkent" : NewCity.Trim(),
            PlannedDeliveryAt = NewPlannedDeliveryDate.Value.Date.Add(deliveryTime),
            PaymentType = NewPaymentType,
            Comment = NewComment.Trim(),
            IsPriority = NewIsPriority,
            BoxCount = boxCount,
            Lines = lines
        };
        return true;
    }
    private void ResetDraft(bool keepMessage = false)
    {
        NewAgentName = string.Empty;
        NewRetailPointName = string.Empty;
        NewAddress = string.Empty;
        NewCity = "Toshkent";
        NewPaymentType = PaymentOptions.First();
        NewComment = string.Empty;
        NewIsPriority = false;
        NewPlannedDeliveryDate = DateTime.Today.AddDays(1);
        NewDeliveryTimeText = "14:00";
        NewBoxCountText = "1";
        DraftLines.Clear();
        DraftLines.Add(new OrderDraftLineViewModel());
        DraftLines.Add(new OrderDraftLineViewModel());
        if (!keepMessage) FormMessage = string.Empty;
    }

    private void Load(string? preferredOrderNumber = null)
    {
        var orders = _operationsService.GetOrders();
        _allOrders.Clear();
        _allOrders.AddRange(orders.Select(MapOrder));

        Metrics.Clear();
        Metrics.Add(new MetricCardViewModel("Всего заказов", orders.Count.ToString(), $"{orders.Sum(order => order.TotalAmount):N0} UZS за смену", "#3F6FFF"));
        Metrics.Add(new MetricCardViewModel("Приоритетные", orders.Count(order => order.IsPriority).ToString(), "Требуют подтверждения и контроля окна доставки", "#D9485F"));
        Metrics.Add(new MetricCardViewModel("На маршруте", orders.Count(order => order.Status == OrderStatus.OnRoute).ToString(), "Можно отслеживать по факту доставки", "#2FBF71"));
        Metrics.Add(new MetricCardViewModel("Проблемные", orders.Count(order => order.Status == OrderStatus.Delayed).ToString(), "Есть риск срыва SLA по точке", "#FF9F43"));

        ApplyFilters(preferredOrderNumber);
    }

    private void ApplyFilters(string? preferredOrderNumber = null)
    {
        var filtered = _allOrders.Where(MatchesFilter).OrderByDescending(order => order.IsPriority).ThenBy(order => order.PlannedDeliveryAt).ToList();
        Orders.Clear();
        foreach (var order in filtered) Orders.Add(order);
        SelectedOrder = filtered.FirstOrDefault(order => order.Number == preferredOrderNumber) ?? filtered.FirstOrDefault();
    }

    private bool MatchesFilter(OrderCardViewModel order)
    {
        var matchesSearch = string.IsNullOrWhiteSpace(SearchText)
            || order.Number.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || order.AgentName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || order.RetailPointName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || order.Address.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

        if (!matchesSearch) return false;
        return SelectedStatusFilter switch
        {
            "Новые" => order.StatusCode is OrderStatus.New or OrderStatus.Approved,
            "Сборка" => order.StatusCode == OrderStatus.Picking,
            "В пути" => order.StatusCode == OrderStatus.OnRoute,
            "Проблемные" => order.StatusCode == OrderStatus.Delayed,
            _ => true
        };
    }

    private static OrderCardViewModel MapOrder(TradeOrder order)
    {
        var visuals = order.Status switch
        {
            OrderStatus.New => new StatusVisuals("Новый", "#3F6FFF"),
            OrderStatus.Approved => new StatusVisuals("Подтвержден", "#2E8BC0"),
            OrderStatus.Picking => new StatusVisuals("На сборке", "#FF9F43"),
            OrderStatus.OnRoute => new StatusVisuals("В пути", "#2FBF71"),
            OrderStatus.Delivered => new StatusVisuals("Доставлен", "#8C9AAE"),
            OrderStatus.Delayed => new StatusVisuals("Есть риск", "#D9485F"),
            _ => new StatusVisuals("Неизвестно", "#8C9AAE")
        };

        return new OrderCardViewModel(order.Number, order.AgentName, order.RetailPointName, order.Address, order.City, order.CreatedAt, order.PlannedDeliveryAt, order.TotalAmount, order.PaymentType, order.Comment, order.IsPriority, order.BoxCount, order.TotalQuantity, order.Status, visuals, order.Lines.Select(line => new OrderLineViewModel(line.ProductName, $"{line.Quantity} {line.Unit}", $"{line.UnitPrice:N0} UZS", $"{line.Total:N0} UZS")).ToList());
    }

    private static string GetActionText(OrderStatus status) => status switch
    {
        OrderStatus.New => "Подтвердить заказ",
        OrderStatus.Approved => "Передать в сборку",
        OrderStatus.Picking => "Отгрузить на маршрут",
        OrderStatus.Delayed => "Вернуть на маршрут",
        OrderStatus.OnRoute => "Подтвердить доставку",
        _ => "Статус закрыт"
    };
}

public sealed class OrderCardViewModel
{
    public OrderCardViewModel(string number, string agentName, string retailPointName, string address, string city, DateTime createdAt, DateTime plannedDeliveryAt, decimal totalAmount, string paymentType, string comment, bool isPriority, int boxCount, int totalQuantity, OrderStatus statusCode, StatusVisuals visuals, IReadOnlyList<OrderLineViewModel> lines)
    {
        Number = number; AgentName = agentName; RetailPointName = retailPointName; Address = address; City = city; CreatedAt = createdAt; PlannedDeliveryAt = plannedDeliveryAt; TotalAmount = $"{totalAmount:N0} UZS"; PaymentType = paymentType; Comment = comment; IsPriority = isPriority; BoxCount = boxCount; TotalQuantity = totalQuantity; StatusCode = statusCode; StatusTitle = visuals.Title; StatusBrush = visuals.Foreground; StatusBackground = visuals.Background; Lines = lines;
    }

    public string Number { get; }
    public string AgentName { get; }
    public string RetailPointName { get; }
    public string Address { get; }
    public string City { get; }
    public DateTime CreatedAt { get; }
    public DateTime PlannedDeliveryAt { get; }
    public string TotalAmount { get; }
    public string PaymentType { get; }
    public string Comment { get; }
    public bool IsPriority { get; }
    public int BoxCount { get; }
    public int TotalQuantity { get; }
    public OrderStatus StatusCode { get; }
    public string StatusTitle { get; }
    public IBrush StatusBrush { get; }
    public IBrush StatusBackground { get; }
    public IReadOnlyList<OrderLineViewModel> Lines { get; }
    public string TimelineText => $"{CreatedAt:HH:mm} создан, доставка до {PlannedDeliveryAt:dd.MM HH:mm}";
    public string MetaText => $"{BoxCount} коробок, {TotalQuantity} единиц";
}

public sealed class OrderLineViewModel
{
    public OrderLineViewModel(string name, string quantity, string price, string total) { Name = name; Quantity = quantity; Price = price; Total = total; }
    public string Name { get; }
    public string Quantity { get; }
    public string Price { get; }
    public string Total { get; }
}

public sealed class StatusVisuals
{
    public StatusVisuals(string title, string colorHex) { Title = title; var color = Color.Parse(colorHex); Foreground = new SolidColorBrush(color); Background = new SolidColorBrush(color, 0.14); }
    public string Title { get; }
    public IBrush Foreground { get; }
    public IBrush Background { get; }
}

public partial class OrderDraftLineViewModel : ObservableObject
{
    [ObservableProperty] private string _productName = string.Empty;
    [ObservableProperty] private string _quantityText = "1";
    [ObservableProperty] private string _unit = "шт";
    [ObservableProperty] private string _unitPriceText = "0";
}

