using System;
using System.Collections.Generic;
using TradeUz.UI.Pages.Dashboard;
using TradeUz.UI.Pages.Orders;
using TradeUz.UI.Pages.Sales;
using TradeUz.UI.Pages.Supply;

namespace TradeUz.UI.Navigation;

public static class NavigationRegistry
{
    // Реестр связывает строковые ключи меню с конкретными типами страниц.
    public static readonly Dictionary<string, Type> Pages = new()
    {
        { "Dashboard", typeof(DashboardViewModel) },
        { "Supply", typeof(SupplyViewModel) },
        { "Sales", typeof(SalesViewModel) },
        { "Retail Sales", typeof(RetailSalesViewModel) },
        { "Orders", typeof(OrdersViewModel) }
    };
}
