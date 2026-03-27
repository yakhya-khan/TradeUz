using System.Collections.Generic;
using TradeUz.Contracts;

namespace TradeUz.UI.Services;

public interface ITradeOperationsService
{
    IReadOnlyList<TradeOrder> GetOrders();
    IReadOnlyList<DeliveryRoute> GetRoutes();
    TradeOrder CreateOrder(CreateTradeOrderRequest request);
    void AdvanceOrder(string orderNumber);
    void StartRoute(string routeCode);
    void CompleteNextStop(string routeCode);
}
