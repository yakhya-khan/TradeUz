using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
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
            var services = new ServiceCollection();
            services.AddApplicationServices();
            var provider = services.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var shell = provider.GetRequiredService<ShellView>();
                shell.DataContext = provider.GetRequiredService<ShellViewModel>();
                desktop.MainWindow = shell;
                provider.GetRequiredService<IThemeService>().Initialize();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
