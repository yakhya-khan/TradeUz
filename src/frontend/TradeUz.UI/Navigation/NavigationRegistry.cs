using System;
using System.Collections.Generic;
using TradeUz.UI.Pages.Dashboard;
using TradeUz.UI.Pages.Orders;
using TradeUz.UI.Pages.Sales;
using TradeUz.UI.Pages.Supply;

namespace TradeUz.UI.Navigation;

public static class NavigationRegistry
{
    public static readonly Dictionary<string, Type> Pages = new()
    {
        { "Dashboard", typeof(DashboardViewModel) },
        { "Supply", typeof(SupplyViewModel) },
        { "Sales", typeof(SalesViewModel) },
        { "Orders", typeof(OrdersViewModel) }
    };
}