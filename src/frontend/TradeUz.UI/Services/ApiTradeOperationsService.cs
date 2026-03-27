using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TradeUz.Contracts;

namespace TradeUz.UI.Services;

public sealed class ApiTradeOperationsService : ITradeOperationsService
{
    private readonly HttpClient _httpClient;

    public ApiTradeOperationsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public IReadOnlyList<TradeOrder> GetOrders()
        => Send(() => _httpClient.GetFromJsonAsync<List<TradeOrder>>("api/orders").GetAwaiter().GetResult() ?? []);

    public IReadOnlyList<DeliveryRoute> GetRoutes()
        => Send(() => _httpClient.GetFromJsonAsync<List<DeliveryRoute>>("api/routes").GetAwaiter().GetResult() ?? []);

    public TradeOrder CreateOrder(CreateTradeOrderRequest request)
    {
        return Send(() =>
        {
            var response = _httpClient.PostAsJsonAsync("api/orders", request).GetAwaiter().GetResult();
            EnsureSuccess(response, "сохранение заказа");
            return response.Content.ReadFromJsonAsync<TradeOrder>().GetAwaiter().GetResult()
                ?? throw new InvalidOperationException("API не вернул созданный заказ.");
        });
    }

    public void AdvanceOrder(string orderNumber)
        => Patch($"api/orders/{Uri.EscapeDataString(orderNumber)}/advance", "обновление статуса заказа");

    public void StartRoute(string routeCode)
        => Patch($"api/routes/{Uri.EscapeDataString(routeCode)}/start", "запуск маршрута");

    public void CompleteNextStop(string routeCode)
        => Patch($"api/routes/{Uri.EscapeDataString(routeCode)}/complete-next-stop", "подтверждение точки доставки");

    private void Patch(string url, string actionName)
    {
        Send(() =>
        {
            var response = _httpClient.PatchAsync(url, content: null).GetAwaiter().GetResult();
            EnsureSuccess(response, actionName);
            return true;
        });
    }

    private static void EnsureSuccess(HttpResponseMessage response, string actionName)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        throw new InvalidOperationException($"TradeUz API вернул ошибку при операции '{actionName}': {(int)response.StatusCode} {response.StatusCode}. {errorBody}".Trim());
    }

    private static T Send<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new InvalidOperationException("¬нешний TradeUz API недоступен. ѕроверьте backend и повторите действие.", ex);
        }
    }
}
