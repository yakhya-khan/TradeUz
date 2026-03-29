using CommunityToolkit.Mvvm.ComponentModel;
using System;
using TradeUz.UI.Pages.Common;

namespace TradeUz.UI.Pages.Supply
{
    public partial class SupplyViewModel : BasePageViewModel
    {
        [ObservableProperty]
        private DateTime? supplyDate = DateTime.Now;
        [ObservableProperty]
        public string? infoText = string.Empty;
        [ObservableProperty]
        private decimal numericValue;

    }
}
