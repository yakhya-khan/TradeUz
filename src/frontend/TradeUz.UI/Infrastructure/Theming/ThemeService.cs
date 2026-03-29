using System;
using Avalonia;
using Avalonia.Styling;
using TradeUz.UI.Infrastructure.Storage;

namespace TradeUz.UI.Infrastructure.Theming;

public class ThemeService : IThemeService
{
    private const string ThemeKey = "AppTheme";
    private readonly ILocalSettingsService _settings;

    public ThemeService(ILocalSettingsService settings)
    {
        _settings = settings;
    }

    public event EventHandler? ThemeChanged;

    public ThemeVariant CurrentTheme =>
        Application.Current?.RequestedThemeVariant ?? ThemeVariant.Light;

    public void Initialize()
    {
        // При запуске восстанавливаем последнюю выбранную тему пользователя.
        if (Application.Current == null)
            return;

        var saved = _settings.Load<string>(ThemeKey);

        if (saved == "Dark")
            Apply(ThemeVariant.Dark);
        else
            Apply(ThemeVariant.Light);
    }

    public void SetLight() => Apply(ThemeVariant.Light);

    public void SetDark() => Apply(ThemeVariant.Dark);

    public void Toggle()
    {
        var next =
            CurrentTheme == ThemeVariant.Dark
                ? ThemeVariant.Light
                : ThemeVariant.Dark;

        Apply(next);
    }

    private void Apply(ThemeVariant variant)
    {
        if (Application.Current == null)
            return;

        // RequestedThemeVariant переключает словари темы для всего приложения.
        Application.Current.RequestedThemeVariant = variant;

        _settings.Save(
            ThemeKey,
            variant == ThemeVariant.Dark ? "Dark" : "Light");

        // Событие нужно shell и другим viewmodel для обновления иконок и подсказок.
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
