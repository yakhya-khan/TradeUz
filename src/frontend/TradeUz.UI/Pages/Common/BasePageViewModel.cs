using CommunityToolkit.Mvvm.ComponentModel;
using System;
using TradeUz.UI.Infrastructure.Localization;

namespace TradeUz.UI.Pages.Common;

public abstract class BasePageViewModel : ObservableObject
{
    protected BasePageViewModel()
        : this(DesignLocalizationService.Instance)
    {
    }

    protected BasePageViewModel(ILocalizationService localization)
    {
        L = localization;
        L.LanguageChanged += HandleLanguageChanged;
    }

    public ILocalizationService L { get; }

    protected virtual void OnLanguageChanged()
    {
        // Сообщаем Avalonia, что индексер локализации надо перечитать прямо
        // на уже открытом экране, без повторной навигации.
        OnPropertyChanged(nameof(L));
    }

    private void HandleLanguageChanged(object? sender, EventArgs e)
    {
        OnLanguageChanged();
    }
}
