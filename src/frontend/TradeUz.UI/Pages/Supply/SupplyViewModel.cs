using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TradeUz.Contracts;
using TradeUz.UI.Pages.Common;
using TradeUz.UI.Pages.Orders;
using TradeUz.UI.Services;

namespace TradeUz.UI.Pages.Supply;

public partial class SupplyViewModel : BasePageViewModel
{
    private readonly ITradeOperationsService _operationsService;
    private readonly List<RouteCardViewModel> _allRoutes = [];

    [ObservableProperty]
    private DateTime? _supplyDate = DateTime.Today;

    [ObservableProperty]
    private RouteCardViewModel? _selectedRoute;

    [ObservableProperty]
    private string _selectedRouteFilter = "Все";

    public ObservableCollection<MetricCardViewModel> Metrics { get; } = [];
    public ObservableCollection<RouteCardViewModel> Routes { get; } = [];
    public ObservableCollection<string> RouteFilters { get; } = ["Все", "Сегодня", "Активные", "Проблемные"];

    public SupplyViewModel()
        : this(new DemoTradeOperationsService())
    {
    }

    public SupplyViewModel(ITradeOperationsService operationsService)
    {
        _operationsService = operationsService;
        Load();
    }

    public string SelectedRouteActionText => SelectedRoute is null
        ? "Выберите маршрут"
        : SelectedRoute.StatusCode switch
        {
            DeliveryRouteStatus.Planned => "Начать маршрут",
            DeliveryRouteStatus.Completed => "Маршрут завершен",
            _ => "Закрыть следующую точку"
        };

    public bool CanProgressSelectedRoute => SelectedRoute is not null
        && SelectedRoute.StatusCode != DeliveryRouteStatus.Completed;

    partial void OnSelectedRouteChanged(RouteCardViewModel? value)
    {
        OnPropertyChanged(nameof(SelectedRouteActionText));
        ProgressSelectedRouteCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedRouteFilterChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSupplyDateChanged(DateTime? value)
    {
        ApplyFilters();
    }

    [RelayCommand(CanExecute = nameof(CanProgressSelectedRoute))]
    private void ProgressSelectedRoute()
    {
        if (SelectedRoute is null)
        {
            return;
        }

        if (SelectedRoute.StatusCode == DeliveryRouteStatus.Planned)
        {
            _operationsService.StartRoute(SelectedRoute.RouteCode);
        }
        else
        {
            _operationsService.CompleteNextStop(SelectedRoute.RouteCode);
        }

        Load(SelectedRoute.RouteCode);
    }

    private void Load(string? preferredRouteCode = null)
    {
        var routes = _operationsService.GetRoutes();

        _allRoutes.Clear();
        _allRoutes.AddRange(routes.Select(MapRoute));

        Metrics.Clear();
        Metrics.Add(new MetricCardViewModel(
            "Маршрутов на сегодня",
            routes.Count.ToString(),
            $"{routes.Sum(route => route.Stops.Count)} торговых точек",
            "#3F6FFF"));
        Metrics.Add(new MetricCardViewModel(
            "Активные рейсы",
            routes.Count(route => route.Status is DeliveryRouteStatus.OnRoute or DeliveryRouteStatus.Delayed).ToString(),
            "Водители уже на линии",
            "#2FBF71"));
        Metrics.Add(new MetricCardViewModel(
            "Выгружено точек",
            routes.Sum(route => route.DeliveredStops).ToString(),
            "Подтверждено по факту",
            "#FF9F43"));
        Metrics.Add(new MetricCardViewModel(
            "Проблемные остановки",
            routes.Sum(route => route.Stops.Count(stop => stop.Status == DeliveryStopStatus.Delayed)).ToString(),
            "Требуют звонка диспетчера",
            "#D9485F"));

        ApplyFilters(preferredRouteCode);
    }

    private void ApplyFilters(string? preferredRouteCode = null)
    {
        var filtered = _allRoutes
            .Where(MatchesFilter)
            .OrderBy(route => route.StatusCode == DeliveryRouteStatus.OnRoute ? 0 : route.StatusCode == DeliveryRouteStatus.Delayed ? 1 : 2)
            .ThenBy(route => route.PlannedDepartureAt)
            .ToList();

        Routes.Clear();
        foreach (var route in filtered)
        {
            Routes.Add(route);
        }

        SelectedRoute = filtered.FirstOrDefault(route => route.RouteCode == preferredRouteCode)
            ?? filtered.FirstOrDefault();
    }

    private bool MatchesFilter(RouteCardViewModel route)
    {
        return SelectedRouteFilter switch
        {
            "Сегодня" => route.PlannedDepartureAt.Date == (SupplyDate ?? DateTime.Today).Date,
            "Активные" => route.StatusCode is DeliveryRouteStatus.OnRoute or DeliveryRouteStatus.Delayed,
            "Проблемные" => route.Stops.Any(stop => stop.StatusCode == DeliveryStopStatus.Delayed),
            _ => true
        };
    }

    private static RouteCardViewModel MapRoute(DeliveryRoute route)
    {
        var visuals = route.Status switch
        {
            DeliveryRouteStatus.Planned => new StatusVisuals("Запланирован", "#3F6FFF"),
            DeliveryRouteStatus.OnRoute => new StatusVisuals("На линии", "#2FBF71"),
            DeliveryRouteStatus.Completed => new StatusVisuals("Завершен", "#8C9AAE"),
            DeliveryRouteStatus.Delayed => new StatusVisuals("Есть отклонение", "#D9485F"),
            _ => new StatusVisuals("Неизвестно", "#8C9AAE")
        };

        return new RouteCardViewModel(
            route.RouteCode,
            route.Region,
            route.DriverName,
            route.VehicleNumber,
            route.DispatcherName,
            route.PlannedDepartureAt,
            route.Status,
            route.DeliveredStops,
            route.Stops.Count,
            route.LoadAmount,
            visuals,
            route.Stops
                .OrderBy(stop => stop.Sequence)
                .Select(stop => new StopCardViewModel(
                    stop.Sequence,
                    stop.OrderNumber,
                    stop.RetailPointName,
                    stop.Address,
                    stop.ContactPhone,
                    stop.PlannedArrivalAt,
                    $"{stop.Amount:N0} UZS",
                    stop.Status,
                    MapStopVisuals(stop.Status)))
                .ToList());
    }

    private static StatusVisuals MapStopVisuals(DeliveryStopStatus status)
    {
        return status switch
        {
            DeliveryStopStatus.Planned => new StatusVisuals("Ожидает", "#3F6FFF"),
            DeliveryStopStatus.OnRoute => new StatusVisuals("Следующая точка", "#2FBF71"),
            DeliveryStopStatus.Delivered => new StatusVisuals("Доставлено", "#8C9AAE"),
            DeliveryStopStatus.Delayed => new StatusVisuals("Опаздывает", "#D9485F"),
            _ => new StatusVisuals("Неизвестно", "#8C9AAE")
        };
    }
}

public sealed class RouteCardViewModel
{
    public RouteCardViewModel(
        string routeCode,
        string region,
        string driverName,
        string vehicleNumber,
        string dispatcherName,
        DateTime plannedDepartureAt,
        DeliveryRouteStatus statusCode,
        int deliveredStops,
        int totalStops,
        decimal loadAmount,
        StatusVisuals visuals,
        IReadOnlyList<StopCardViewModel> stops)
    {
        RouteCode = routeCode;
        Region = region;
        DriverName = driverName;
        VehicleNumber = vehicleNumber;
        DispatcherName = dispatcherName;
        PlannedDepartureAt = plannedDepartureAt;
        StatusCode = statusCode;
        DeliveredStops = deliveredStops;
        TotalStops = totalStops;
        LoadAmount = $"{loadAmount:N0} UZS";
        StatusTitle = visuals.Title;
        StatusBrush = visuals.Foreground;
        StatusBackground = visuals.Background;
        Stops = stops;
    }

    public string RouteCode { get; }
    public string Region { get; }
    public string DriverName { get; }
    public string VehicleNumber { get; }
    public string DispatcherName { get; }
    public DateTime PlannedDepartureAt { get; }
    public DeliveryRouteStatus StatusCode { get; }
    public int DeliveredStops { get; }
    public int TotalStops { get; }
    public string LoadAmount { get; }
    public string StatusTitle { get; }
    public IBrush StatusBrush { get; }
    public IBrush StatusBackground { get; }
    public IReadOnlyList<StopCardViewModel> Stops { get; }

    public string ProgressText => $"{DeliveredStops}/{TotalStops} точек";
    public string SummaryText => $"{PlannedDepartureAt:HH:mm} выезд, водитель {DriverName}";
}

public sealed class StopCardViewModel
{
    public StopCardViewModel(
        int sequence,
        string orderNumber,
        string retailPointName,
        string address,
        string contactPhone,
        DateTime plannedArrivalAt,
        string amount,
        DeliveryStopStatus statusCode,
        StatusVisuals visuals)
    {
        Sequence = sequence;
        OrderNumber = orderNumber;
        RetailPointName = retailPointName;
        Address = address;
        ContactPhone = contactPhone;
        PlannedArrivalAt = plannedArrivalAt;
        Amount = amount;
        StatusCode = statusCode;
        StatusTitle = visuals.Title;
        StatusBrush = visuals.Foreground;
        StatusBackground = visuals.Background;
    }

    public int Sequence { get; }
    public string OrderNumber { get; }
    public string RetailPointName { get; }
    public string Address { get; }
    public string ContactPhone { get; }
    public DateTime PlannedArrivalAt { get; }
    public string Amount { get; }
    public DeliveryStopStatus StatusCode { get; }
    public string StatusTitle { get; }
    public IBrush StatusBrush { get; }
    public IBrush StatusBackground { get; }
}

