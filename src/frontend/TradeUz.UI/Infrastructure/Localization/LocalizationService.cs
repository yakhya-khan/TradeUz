using System;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeUz.UI.Infrastructure.Storage;

namespace TradeUz.UI.Infrastructure.Localization;

public class LocalizationService : ObservableObject, ILocalizationService
{
    private const string LanguageKey = "AppLanguage";
    private readonly ILocalSettingsService _settings;

    private AppLanguage _currentLanguage = AppLanguage.UzbekLatin;

    public LocalizationService(ILocalSettingsService settings)
    {
        _settings = settings;
        LocalizationProvider.Current = this;
    }

    public event EventHandler? LanguageChanged;

    public AppLanguage CurrentLanguage => _currentLanguage;

    public string CurrentLanguageCode => LocalizationCatalog.GetLanguageCode(CurrentLanguage);

    public string this[string key] => Get(key);

    public string Get(string key) => LocalizationCatalog.Get(CurrentLanguage, key);

    public void Initialize()
    {
        var saved = _settings.Load<string>(LanguageKey);

        var language =
            saved switch
            {
                nameof(AppLanguage.Russian) => AppLanguage.Russian,
                nameof(AppLanguage.English) => AppLanguage.English,
                _ => AppLanguage.UzbekLatin
            };

        Apply(language, persist: false, force: true);
    }

    public void SetLanguage(AppLanguage language)
    {
        Apply(language, persist: true, force: false);
    }

    private void Apply(AppLanguage language, bool persist, bool force)
    {
        if (!force && _currentLanguage == language)
            return;

        // Язык приложения влияет только на тексты интерфейса.
        // Форматы дат и чисел остаются системными и не меняются здесь.
        _currentLanguage = language;

        if (persist)
            _settings.Save(LanguageKey, language.ToString());

        OnPropertyChanged(nameof(CurrentLanguage));
        OnPropertyChanged(nameof(CurrentLanguageCode));
        OnPropertyChanged("Item[]");

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}
