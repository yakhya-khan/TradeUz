using System;
using System.ComponentModel;

namespace TradeUz.UI.Infrastructure.Localization;

public interface ILocalizationService : INotifyPropertyChanged
{
    event EventHandler? LanguageChanged;

    AppLanguage CurrentLanguage { get; }
    string CurrentLanguageCode { get; }
    string this[string key] { get; }

    string Get(string key);
    void Initialize();
    void SetLanguage(AppLanguage language);
}
