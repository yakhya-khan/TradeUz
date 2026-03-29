namespace TradeUz.UI.Infrastructure.Localization;

public static class LocalizationProvider
{
    // В дизайнере XAML DI-контейнер недоступен, поэтому здесь есть безопасное значение по умолчанию.
    public static ILocalizationService Current { get; set; } = DesignLocalizationService.Instance;

    public static string Get(string key) => Current.Get(key);
}
