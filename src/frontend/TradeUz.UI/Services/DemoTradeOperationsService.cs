using System;
using System.Collections.Generic;
using System.Linq;
using TradeUz.Contracts;

namespace TradeUz.UI.Services;

public sealed class DemoTradeOperationsService : ITradeOperationsService
{
    private readonly List<TradeOrder> _orders;
    private readonly List<DeliveryRoute> _routes;

    public DemoTradeOperationsService()
    {
        var snapshot = TradeDemoSeed.Create();
        _orders = snapshot.Orders;
        _routes = snapshot.Routes;
    }

    public IReadOnlyList<TradeOrder> GetOrders() => _orders.OrderByDescending(order => order.IsPriority).ThenBy(order => order.PlannedDeliveryAt).ToList();

    public IReadOnlyList<DeliveryRoute> GetRoutes() => _routes.OrderBy(route => route.Status == DeliveryRouteStatus.OnRoute ? 0 : 1).ThenBy(route => route.PlannedDepartureAt).ToList();

    public TradeOrder CreateOrder(CreateTradeOrderRequest request)
    {
        var order = new TradeOrder
        {
            Number = GenerateOrderNumber(request.PlannedDeliveryAt),
            AgentName = request.AgentName,
            RetailPointName = request.RetailPointName,
            Address = request.Address,
            City = request.City,
            CreatedAt = DateTime.Now,
            PlannedDeliveryAt = request.PlannedDeliveryAt,
            TotalAmount = request.Lines.Sum(line => line.Quantity * line.UnitPrice),
            PaymentType = request.PaymentType,
            Comment = request.Comment,
            IsPriority = request.IsPriority,
            BoxCount = request.BoxCount,
            Status = OrderStatus.New
        };

        foreach (var line in request.Lines)
        {
            order.Lines.Add(new OrderLine { ProductName = line.ProductName, Quantity = line.Quantity, Unit = line.Unit, UnitPrice = line.UnitPrice });
        }

        _orders.Add(order);
        return order;
    }

    public void AdvanceOrder(string orderNumber)
    {
        var order = _orders.FirstOrDefault(item => item.Number == orderNumber);
        if (order is null)
        {
            return;
        }

        order.Status = order.Status switch
        {
            OrderStatus.New => OrderStatus.Approved,
            OrderStatus.Approved => OrderStatus.Picking,
            OrderStatus.Picking => OrderStatus.OnRoute,
            OrderStatus.Delayed => OrderStatus.OnRoute,
            OrderStatus.OnRoute => OrderStatus.Delivered,
            _ => order.Status
        };
    }

    public void StartRoute(string routeCode)
    {
        var route = _routes.FirstOrDefault(item => item.RouteCode == routeCode);
        if (route is null || route.Status == DeliveryRouteStatus.Completed)
        {
            return;
        }

        route.Status = DeliveryRouteStatus.OnRoute;
        foreach (var stop in route.Stops.Where(stop => stop.Status == DeliveryStopStatus.Planned))
        {
            stop.Status = DeliveryStopStatus.OnRoute;
        }
    }

    public void CompleteNextStop(string routeCode)
    {
        var route = _routes.FirstOrDefault(item => item.RouteCode == routeCode);
        if (route is null || route.Status == DeliveryRouteStatus.Completed)
        {
            return;
        }

        var stop = route.Stops.OrderBy(item => item.Sequence).FirstOrDefault(item => item.Status != DeliveryStopStatus.Delivered);
        if (stop is null)
        {
            route.Status = DeliveryRouteStatus.Completed;
            return;
        }

        stop.Status = DeliveryStopStatus.Delivered;
        var order = _orders.FirstOrDefault(item => item.Number == stop.OrderNumber);
        if (order is not null)
        {
            order.Status = OrderStatus.Delivered;
        }

        route.Status = route.Stops.All(item => item.Status == DeliveryStopStatus.Delivered)
            ? DeliveryRouteStatus.Completed
            : DeliveryRouteStatus.OnRoute;
    }

    private string GenerateOrderNumber(DateTime plannedDate)
    {
        var prefix = $"ORD-{plannedDate:yyMMdd}";
        var sameDayOrders = _orders.Where(order => order.Number.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        return $"{prefix}-{sameDayOrders.Count + 1:000}";
    }
}
