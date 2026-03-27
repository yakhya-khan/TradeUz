using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using TradeUz.UI.Core;
using TradeUz.UI.Infrastructure.Storage;
using TradeUz.UI.Infrastructure.Theming;
using TradeUz.UI.Navigation;
using TradeUz.UI.Pages.Dashboard;
using TradeUz.UI.Pages.Orders;
using TradeUz.UI.Pages.Sales;
using TradeUz.UI.Pages.Supply;
using TradeUz.UI.Services;
using TradeUz.UI.Shell;

namespace TradeUz.UI.Bootstrap
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var apiBaseUrl = Environment.GetEnvironmentVariable("TRADEUZ_API_BASE_URL") ?? "http://localhost:8080/";
            if (!apiBaseUrl.EndsWith("/", StringComparison.Ordinal))
            {
                apiBaseUrl += "/";
            }

            services.AddSingleton<IRouter, Router>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton(new HttpClient { BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute) });
            services.AddSingleton<ITradeOperationsService, ApiTradeOperationsService>();

            services.AddSingleton<ShellViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<SupplyViewModel>();
            services.AddTransient<SalesViewModel>();
            services.AddTransient<RetailSalesViewModel>();
            services.AddTransient<OrdersViewModel>();

            services.AddSingleton<ShellView>();
            services.AddSingleton<IUserContext, UserContext>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeService, ThemeService>();

            return services;
        }
    }
}
