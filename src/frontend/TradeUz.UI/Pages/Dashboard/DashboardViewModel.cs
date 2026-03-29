using TradeUz.UI.Infrastructure.Localization;
using TradeUz.UI.Pages.Common;

namespace TradeUz.UI.Pages.Dashboard
{
    public partial class DashboardViewModel : BasePageViewModel
    {
        public DashboardViewModel()
            : base()
        {
        }

        public DashboardViewModel(ILocalizationService localization)
            : base(localization)
        {
        }

        public string WelcomeMessage => L["DashboardWelcomeMessage"];

        public string Description => L["DashboardDescription"];

        protected override void OnLanguageChanged()
        {
            base.OnLanguageChanged();
            OnPropertyChanged(nameof(WelcomeMessage));
            OnPropertyChanged(nameof(Description));
        }
    }
}
