using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeUz.UI.Pages.Common;

namespace TradeUz.UI.Pages.Sales
{
    public partial class RetailSalesViewModel: BasePageViewModel
    {
        [ObservableProperty]
        private DateTime? salesDate= DateTime.Now;

        public ObservableCollection<SoldProductRow> SoldProducts { get; } =
    [
        new SoldProductRow("Coffee Beans, 1kg", "2", "125 000", "250 000"),
        new SoldProductRow("Paper Cup, 350ml", "5", "2 700", "13 500"),
        new SoldProductRow("Caramel Syrup", "1", "38 000", "38 000")
    ];
    }
        public sealed record SoldProductRow(string Name, string Quantity, string Price, string Total);
}
