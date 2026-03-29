using TradeUz.UI.Core;
using TradeUz.UI.Infrastructure.Localization;
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
            // Router хранит текущую страницу внутри shell.
            services.AddSingleton<IRouter, Router>();

            // Навигация использует router и DI для переключения страниц.
            services.AddSingleton<INavigationService, NavigationService>();

            // Shell живёт весь срок работы приложения,
            // остальные страницы создаются по мере открытия.
            services.AddSingleton<ShellViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<SupplyViewModel>();
            services.AddTransient<SalesViewModel>();
            services.AddTransient<RetailSalesViewModel>();
            services.AddTransient<OrdersViewModel>();

            // Главное окно одно на всё приложение.
            services.AddSingleton<ShellView>();

            // Контекст пользователя оставлен как точка расширения для ролей и авторизации.
            services.AddSingleton<IUserContext, UserContext>();

            // Общие инфраструктурные сервисы пользовательского интерфейса.
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddSingleton<IThemeService, ThemeService>();

            return services;
        }
    }
}
