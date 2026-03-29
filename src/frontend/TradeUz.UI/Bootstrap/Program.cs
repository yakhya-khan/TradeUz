using System;
using Avalonia;
namespace TradeUz.UI.Bootstrap
{
    internal class Program
    {
        // Точка входа должна оставаться максимально лёгкой:
        // инфраструктура Avalonia ещё не поднята.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Общая конфигурация Avalonia для обычного запуска и дизайнера XAML.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
