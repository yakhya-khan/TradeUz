using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeUz.UI.Pages.Common;

namespace TradeUz.UI.Pages.Supply
{
    public partial class SupplyViewModel: BasePageViewModel
    {
        [ObservableProperty]
        private DateTime? supplyDate= DateTime.Now ;
    }
}
