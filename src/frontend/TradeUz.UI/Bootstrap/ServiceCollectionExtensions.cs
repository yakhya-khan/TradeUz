using TradeUz.UI.Core;
using TradeUz.UI.Infrastructure.Storage;
using TradeUz.UI.Infrastructure.Theming;
using TradeUz.UI.Navigation;
using TradeUz.UI.Pages.Dashboard;
using TradeUz.UI.Pages.Orders;
using TradeUz.UI.Shell;
using Microsoft.Extensions.DependencyInjection;
using TradeUz.UI.Pages.Sales;
using TradeUz.UI.Pages.Supply;

namespace TradeUz.UI.Bootstrap
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Router
            services.AddSingleton<IRouter, Router>();

            // Navigation
            services.AddSingleton<INavigationService, NavigationService>();

            // ViewsModels
            services.AddSingleton<ShellViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<SupplyViewModel>();
            services.AddTransient<SalesViewModel>();
            services.AddTransient<RetailSalesViewModel>();
            services.AddTransient<OrdersViewModel>();

            // Views
            services.AddSingleton<ShellView>();

            // roles
            services.AddSingleton<IUserContext, UserContext>();

            // theming
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeService, ThemeService>();

            return services;
        }
    }
}
