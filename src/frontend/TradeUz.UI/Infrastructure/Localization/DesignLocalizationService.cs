using System;
using System.ComponentModel;

namespace TradeUz.UI.Infrastructure.Localization;

internal sealed class DesignLocalizationService : ILocalizationService
{
    public static DesignLocalizationService Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add { }
        remove { }
    }

    public event EventHandler? LanguageChanged
    {
        add { }
        remove { }
    }

    public AppLanguage CurrentLanguage => AppLanguage.UzbekLatin;

    public string CurrentLanguageCode => LocalizationCatalog.GetLanguageCode(CurrentLanguage);

    public string this[string key] => Get(key);

    public string Get(string key) => LocalizationCatalog.Get(CurrentLanguage, key);

    public void Initialize()
    {
        // Для design-time режима отдельная инициализация не требуется.
    }

    public void SetLanguage(AppLanguage language)
    {
        // Дизайнер не управляет состоянием приложения, поэтому метод намеренно пустой.
    }
}
