using CommunityToolkit.Mvvm.ComponentModel;
using TradeUz.UI.Infrastructure.Localization;
using TradeUz.UI.Pages.Common;

namespace TradeUz.UI.Pages.Orders;

public partial class OrdersViewModel : BasePageViewModel
{
    [ObservableProperty] 
    private decimal? currentPrice;
    
    public OrdersViewModel()
        : base()
    {
    }

    public OrdersViewModel(ILocalizationService localization)
        : base(localization)
    {
    }

    public string Title => L["OrdersPageTitle"];

    protected override void OnLanguageChanged()
    {
        base.OnLanguageChanged();
        OnPropertyChanged(nameof(Title));
    }

}
