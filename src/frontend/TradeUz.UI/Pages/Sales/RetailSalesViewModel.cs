using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using TradeUz.UI.Infrastructure.Localization;
using TradeUz.UI.Pages.Common;

namespace TradeUz.UI.Pages.Sales
{
    public partial class RetailSalesViewModel : BasePageViewModel
    {
        public RetailSalesViewModel()
            : base()
        {
        }

        public RetailSalesViewModel(ILocalizationService localization)
            : base(localization)
        {
        }

        [ObservableProperty]
        private DateTime? salesDate = DateTime.Now;

        public string Title => L["RetailSalesTitle"];
        public string NewSaleText => L["RetailSalesNewSale"];
        public string SearchWatermark => L["RetailSalesSearchWatermark"];
        public string ItemHeader => L["RetailSalesColItem"];
        public string QuantityHeader => L["RetailSalesColQty"];
        public string PriceHeader => L["RetailSalesColPrice"];
        public string TotalHeader => L["RetailSalesColTotal"];
        public string SummaryPlaceholder => L["RetailSalesSummaryPlaceholder"];
        public string PaymentPlaceholder => L["RetailSalesPaymentPlaceholder"];
        public string NumpadTitle => L["RetailSalesNumpad"];

        public ObservableCollection<SoldProductRow> SoldProducts { get; } =
        [
            new SoldProductRow("Coffee Beans, 1kg", "2", "125 000", "250 000"),
            new SoldProductRow("Paper Cup, 350ml", "5", "2 700", "13 500"),
            new SoldProductRow("Caramel Syrup", "1", "38 000", "38 000")
        ];

        protected override void OnLanguageChanged()
        {
            base.OnLanguageChanged();
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(NewSaleText));
            OnPropertyChanged(nameof(SearchWatermark));
            OnPropertyChanged(nameof(ItemHeader));
            OnPropertyChanged(nameof(QuantityHeader));
            OnPropertyChanged(nameof(PriceHeader));
            OnPropertyChanged(nameof(TotalHeader));
            OnPropertyChanged(nameof(SummaryPlaceholder));
            OnPropertyChanged(nameof(PaymentPlaceholder));
            OnPropertyChanged(nameof(NumpadTitle));
        }
    }

    public sealed record SoldProductRow(string Name, string Quantity, string Price, string Total);
}
