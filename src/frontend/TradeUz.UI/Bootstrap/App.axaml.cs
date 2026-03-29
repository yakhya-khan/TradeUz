using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using TradeUz.UI.Infrastructure.Localization;
using TradeUz.UI.Infrastructure.Theming;
using TradeUz.UI.Shell;

namespace TradeUz.UI.Bootstrap
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Собираем контейнер зависимостей один раз при старте приложения.
            var services = new ServiceCollection();
            services.AddApplicationServices();
            var provider = services.BuildServiceProvider();

            // Инициализируем локализацию и тему до показа окна,
            // чтобы shell сразу открылся в правильном состоянии.
            provider.GetRequiredService<ILocalizationService>().Initialize();
            provider.GetRequiredService<IThemeService>().Initialize();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Shell является главным окном и корнем навигации всего приложения.
                var shell = provider.GetRequiredService<ShellView>();
                shell.DataContext = provider.GetRequiredService<ShellViewModel>();
                desktop.MainWindow = shell;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
