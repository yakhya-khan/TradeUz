using System;
using System.Collections.Generic;
using System.Linq;

namespace TradeUz.Contracts;

public sealed class TradeOrder
{
    public string Number { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string RetailPointName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime PlannedDeliveryAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public bool IsPriority { get; set; }
    public int BoxCount { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderLine> Lines { get; } = [];

    public int TotalQuantity => Lines.Sum(line => line.Quantity);
}

public sealed class OrderLine
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }

    public decimal Total => Quantity * UnitPrice;
}

public enum OrderStatus
{
    New,
    Approved,
    Picking,
    OnRoute,
    Delivered,
    Delayed
}

public sealed class DeliveryRoute
{
    public string RouteCode { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string VehicleNumber { get; set; } = string.Empty;
    public string DispatcherName { get; set; } = string.Empty;
    public DateTime PlannedDepartureAt { get; set; }
    public DeliveryRouteStatus Status { get; set; }
    public List<DeliveryStop> Stops { get; } = [];

    public int DeliveredStops => Stops.Count(stop => stop.Status == DeliveryStopStatus.Delivered);
    public decimal LoadAmount => Stops.Sum(stop => stop.Amount);
}

public sealed class DeliveryStop
{
    public int Sequence { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string RetailPointName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public DateTime PlannedArrivalAt { get; set; }
    public decimal Amount { get; set; }
    public DeliveryStopStatus Status { get; set; }
}

public enum DeliveryRouteStatus
{
    Planned,
    OnRoute,
    Completed,
    Delayed
}

public enum DeliveryStopStatus
{
    Planned,
    OnRoute,
    Delivered,
    Delayed
}

public sealed class CreateTradeOrderRequest
{
    public string AgentName { get; set; } = string.Empty;
    public string RetailPointName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime PlannedDeliveryAt { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public bool IsPriority { get; set; }
    public int BoxCount { get; set; }
    public List<CreateTradeOrderLineRequest> Lines { get; set; } = [];
}

public sealed class CreateTradeOrderLineRequest
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}

public sealed class TradeSeedSnapshot
{
    public List<TradeOrder> Orders { get; init; } = [];
    public List<DeliveryRoute> Routes { get; init; } = [];
}

public static class TradeDemoSeed
{
    public static TradeSeedSnapshot Create()
    {
        var today = DateTime.Today;
        var orders = new List<TradeOrder>
        {
            CreateOrder("ORD-24031", "Aziza Rahimova", "Navruz Market", "Yashnobod, Parkent ko'chasi 14", "Toshkent", today.AddHours(8).AddMinutes(15), today.AddHours(13), "Naqd pul", "Срочная доставка до обеда.", true, 14, OrderStatus.New, ("Газированная вода 1.5L", 48, "шт", 7500m), ("Чай черный 200г", 24, "шт", 18000m)),
            CreateOrder("ORD-24032", "Bekzod Umarov", "Oqtepa Mini Shop", "Chilonzor, Qatortol 23", "Toshkent", today.AddHours(9).AddMinutes(40), today.AddHours(16), "Перечисление", "Доставка до 16:00.", false, 9, OrderStatus.Picking, ("Сахар 1кг", 30, "шт", 14000m), ("Печенье ассорти", 40, "шт", 9500m)),
            CreateOrder("ORD-24033", "Shahnoza Aliyeva", "Green Line Store", "Mirzo Ulug'bek, Buyuk Ipak Yo'li 88", "Toshkent", today.AddHours(7).AddMinutes(55), today.AddHours(12).AddMinutes(30), "Naqd pul", "Выгрузка через задний вход.", true, 18, OrderStatus.OnRoute, ("Энергетик 0.45L", 96, "шт", 11000m), ("Минеральная вода 1L", 60, "шт", 6000m))
        };

        var routes = new List<DeliveryRoute>
        {
            CreateRoute("TSH-01", "Toshkent shahri", "Jahongir Qodirov", "01 A 734 TA", "Kamola Ergasheva", today.AddHours(10), DeliveryRouteStatus.OnRoute, CreateStop(1, "ORD-24033", "Green Line Store", "Mirzo Ulug'bek, Buyuk Ipak Yo'li 88", "+998 90 441 22 10", today.AddHours(11).AddMinutes(10), 1416000m, DeliveryStopStatus.OnRoute)),
            CreateRoute("TSH-02", "Toshkent g'arbiy sektor", "Akbar Aliyev", "10 B 112 QA", "Malika Turdiyeva", today.AddHours(15), DeliveryRouteStatus.Planned, CreateStop(1, "ORD-24032", "Oqtepa Mini Shop", "Chilonzor, Qatortol 23", "+998 93 555 00 21", today.AddHours(16).AddMinutes(10), 800000m, DeliveryStopStatus.Planned))
        };

        return new TradeSeedSnapshot { Orders = orders, Routes = routes };
    }

    private static TradeOrder CreateOrder(string number, string agentName, string retailPointName, string address, string city, DateTime createdAt, DateTime plannedDeliveryAt, string paymentType, string comment, bool isPriority, int boxCount, OrderStatus status, params (string ProductName, int Quantity, string Unit, decimal UnitPrice)[] lines)
    {
        var order = new TradeOrder
        {
            Number = number,
            AgentName = agentName,
            RetailPointName = retailPointName,
            Address = address,
            City = city,
            CreatedAt = createdAt,
            PlannedDeliveryAt = plannedDeliveryAt,
            PaymentType = paymentType,
            Comment = comment,
            IsPriority = isPriority,
            BoxCount = boxCount,
            Status = status
        };

        foreach (var line in lines)
        {
            order.Lines.Add(new OrderLine { ProductName = line.ProductName, Quantity = line.Quantity, Unit = line.Unit, UnitPrice = line.UnitPrice });
        }

        order.TotalAmount = order.Lines.Sum(line => line.Total);
        return order;
    }

    private static DeliveryRoute CreateRoute(string routeCode, string region, string driverName, string vehicleNumber, string dispatcherName, DateTime plannedDepartureAt, DeliveryRouteStatus status, params DeliveryStop[] stops)
    {
        var route = new DeliveryRoute
        {
            RouteCode = routeCode,
            Region = region,
            DriverName = driverName,
            VehicleNumber = vehicleNumber,
            DispatcherName = dispatcherName,
            PlannedDepartureAt = plannedDepartureAt,
            Status = status
        };

        route.Stops.AddRange(stops);
        return route;
    }

    private static DeliveryStop CreateStop(int sequence, string orderNumber, string retailPointName, string address, string contactPhone, DateTime plannedArrivalAt, decimal amount, DeliveryStopStatus status)
        => new()
        {
            Sequence = sequence,
            OrderNumber = orderNumber,
            RetailPointName = retailPointName,
            Address = address,
            ContactPhone = contactPhone,
            PlannedArrivalAt = plannedArrivalAt,
            Amount = amount,
            Status = status
        };
}
