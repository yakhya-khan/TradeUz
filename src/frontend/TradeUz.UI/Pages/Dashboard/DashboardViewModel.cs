using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeUz.Contracts;
using TradeUz.UI.Pages.Common;
using TradeUz.UI.Services;

namespace TradeUz.UI.Pages.Dashboard;

public partial class DashboardViewModel : BasePageViewModel
{
    private readonly ITradeOperationsService _operationsService;

    [ObservableProperty]
    private string _welcomeMessage = "TradeUz Logistics";

    [ObservableProperty]
    private string _description = "Заказы агентов, сборка склада и доставка по торговым точкам в одном окне.";

    public ObservableCollection<MetricCardViewModel> Metrics { get; } = [];
    public ObservableCollection<WorkflowStepViewModel> WorkflowSteps { get; } = [];
    public ObservableCollection<string> FocusItems { get; } = [];

    public DashboardViewModel()
        : this(new DemoTradeOperationsService())
    {
    }

    public DashboardViewModel(ITradeOperationsService operationsService)
    {
        _operationsService = operationsService;
        Load();
    }

    private void Load()
    {
        var orders = _operationsService.GetOrders();
        var routes = _operationsService.GetRoutes();

        Metrics.Clear();
        Metrics.Add(new MetricCardViewModel(
            "Заказы за день",
            orders.Count.ToString(),
            $"{orders.Count(order => order.Status is OrderStatus.New or OrderStatus.Approved)} ждут подтверждения",
            "#3F6FFF"));
        Metrics.Add(new MetricCardViewModel(
            "На сборке",
            orders.Count(order => order.Status == OrderStatus.Picking).ToString(),
            $"{orders.Where(order => order.Status == OrderStatus.Picking).Sum(order => order.BoxCount)} коробок к отгрузке",
            "#FF9F43"));
        Metrics.Add(new MetricCardViewModel(
            "Активные маршруты",
            routes.Count(route => route.Status is DeliveryRouteStatus.OnRoute or DeliveryRouteStatus.Delayed).ToString(),
            $"{routes.Sum(route => route.Stops.Count)} торговых точек в плане",
            "#2FBF71"));
        Metrics.Add(new MetricCardViewModel(
            "Проблемные точки",
            orders.Count(order => order.Status == OrderStatus.Delayed).ToString(),
            "Нужен звонок клиенту и перепланирование",
            "#D9485F"));

        WorkflowSteps.Clear();
        WorkflowSteps.Add(new WorkflowStepViewModel("1. Заказ агента", "Агент оформляет заказ в торговой точке, сразу фиксируются сумма, состав и окно доставки."));
        WorkflowSteps.Add(new WorkflowStepViewModel("2. Сборка склада", "Склад видит приоритетные заказы и коробочный план, чтобы готовить отгрузку без ручных списков."));
        WorkflowSteps.Add(new WorkflowStepViewModel("3. Доставка", "Диспетчер запускает маршрут, водитель закрывает точки по порядку и подтверждает доставку."));

        FocusItems.Clear();
        FocusItems.Add($"Приоритетные заказы: {orders.Count(order => order.IsPriority)}");
        FocusItems.Add($"Общая сумма заказов: {orders.Sum(order => order.TotalAmount):N0} UZS");
        FocusItems.Add($"Следующий выезд: {routes.Where(route => route.Status == DeliveryRouteStatus.Planned).OrderBy(route => route.PlannedDepartureAt).Select(route => route.RouteCode + " в " + route.PlannedDepartureAt.ToString("HH:mm")).FirstOrDefault() ?? "нет запланированных"}");
    }
}

public sealed class WorkflowStepViewModel
{
    public WorkflowStepViewModel(string title, string description)
    {
        Title = title;
        Description = description;
    }

    public string Title { get; }
    public string Description { get; }
}

