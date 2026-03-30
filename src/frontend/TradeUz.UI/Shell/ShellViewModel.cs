using System;
using Avalonia.Controls;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TradeUz.UI.Infrastructure.Localization;
using TradeUz.UI.Infrastructure.Theming;
using TradeUz.UI.Navigation;
using TradeUz.UI.Pages.Dashboard;

namespace TradeUz.UI.Shell
{
    public partial class ShellViewModel : ObservableObject
    {
        private const double ExpandedSidebarWidth = 248;
        private const double CollapsedSidebarWidth = 72;

        [ObservableProperty]
        private bool _sideMenuExpanded = true;

        [ObservableProperty]
        private bool _isLanguageMenuOpen;

        private readonly INavigationService _navigation;
        private readonly ILocalizationService _localization;
        private readonly IThemeService _themeService;
        private string _currentSectionKey = "Dashboard";

        public IRouter Router { get; }
        public ILocalizationService L { get; }
        public IRelayCommand<string?> NavigateCommand { get; }
        public IRelayCommand ToggleThemeCommand { get; }
        public IRelayCommand ToggleLanguageMenuCommand { get; }
        public IRelayCommand<string?> SetLanguageCommand { get; }
        public bool IsDarkTheme => _themeService?.CurrentTheme == ThemeVariant.Dark;
        public string HomeText => L.Get("ShellNavHome");
        public string SupplyText => L.Get("ShellNavSupply");
        public string RetailSalesText => L.Get("ShellNavRetailSales");
        public string SalesText => L.Get("ShellNavSales");
        public string OrdersText => L.Get("ShellNavOrders");
        public string CurrentLanguageCode => L.CurrentLanguageCode;
        public string CurrentSectionTitle => GetSectionTitle(_currentSectionKey);
        public double SidebarWidth => SideMenuExpanded ? ExpandedSidebarWidth : CollapsedSidebarWidth;
        public string? HomeTooltip => GetSidebarTooltip("Dashboard", HomeText);
        public string? SupplyTooltip => GetSidebarTooltip("Supply", SupplyText);
        public string? RetailSalesTooltip => GetSidebarTooltip("Retail Sales", RetailSalesText);
        public string? SalesTooltip => GetSidebarTooltip("Sales", SalesText);
        public string? OrdersTooltip => GetSidebarTooltip("Orders", OrdersText);
        public bool IsUzbekSelected => L.CurrentLanguage == AppLanguage.UzbekLatin;
        public bool IsRussianSelected => L.CurrentLanguage == AppLanguage.Russian;
        public bool IsEnglishSelected => L.CurrentLanguage == AppLanguage.English;
        public string LanguageToggleTooltip => L.Get("ShellLanguageTooltip");
        public string ThemeToggleTooltip =>
            IsDarkTheme
                ? L.Get("ShellThemeTooltipDark")
                : L.Get("ShellThemeTooltipLight");

        public ShellViewModel(
            INavigationService navigation,
            IRouter router,
            ILocalizationService localization,
            IThemeService themeService)
        {
            _navigation = navigation;
            Router = router;
            _localization = localization;
            L = localization;
            _themeService = themeService;

            _localization.LanguageChanged += OnLanguageChanged;
            _themeService.ThemeChanged += OnThemeChanged;

            NavigateCommand = new RelayCommand<string?>(Navigate);
            ToggleThemeCommand = new RelayCommand(ToggleTheme);
            ToggleLanguageMenuCommand = new RelayCommand(ToggleLanguageMenu);
            SetLanguageCommand = new RelayCommand<string?>(SetLanguage);

            _navigation.NavigateTo<DashboardViewModel>();
        }

        private void ToggleTheme()
        {
            _themeService.Toggle();
        }

        private void ToggleLanguageMenu()
        {
            IsLanguageMenuOpen = !IsLanguageMenuOpen;
        }

        private void SetLanguage(string? languageName)
        {
            if (!Enum.TryParse<AppLanguage>(languageName, out var language))
            {
                IsLanguageMenuOpen = false;
                return;
            }

            _localization.SetLanguage(language);
            IsLanguageMenuOpen = false;
        }

        private void OnThemeChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(ThemeToggleTooltip));
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(L));
            OnPropertyChanged(nameof(HomeText));
            OnPropertyChanged(nameof(SupplyText));
            OnPropertyChanged(nameof(RetailSalesText));
            OnPropertyChanged(nameof(SalesText));
            OnPropertyChanged(nameof(OrdersText));
            OnPropertyChanged(nameof(CurrentLanguageCode));
            OnPropertyChanged(nameof(CurrentSectionTitle));
            OnPropertyChanged(nameof(HomeTooltip));
            OnPropertyChanged(nameof(SupplyTooltip));
            OnPropertyChanged(nameof(RetailSalesTooltip));
            OnPropertyChanged(nameof(SalesTooltip));
            OnPropertyChanged(nameof(OrdersTooltip));
            OnPropertyChanged(nameof(IsUzbekSelected));
            OnPropertyChanged(nameof(IsRussianSelected));
            OnPropertyChanged(nameof(IsEnglishSelected));
            OnPropertyChanged(nameof(LanguageToggleTooltip));
            OnPropertyChanged(nameof(ThemeToggleTooltip));
        }

        private void Navigate(string? page)
        {
            if (string.IsNullOrEmpty(page))
                return;

            if (NavigationRegistry.Pages.TryGetValue(page, out var pageType))
            {
                _currentSectionKey = page;
                OnPropertyChanged(nameof(CurrentSectionTitle));
                OnPropertyChanged(nameof(HomeTooltip));
                OnPropertyChanged(nameof(SupplyTooltip));
                OnPropertyChanged(nameof(RetailSalesTooltip));
                OnPropertyChanged(nameof(SalesTooltip));
                OnPropertyChanged(nameof(OrdersTooltip));

                var method = typeof(INavigationService)
                    .GetMethod(nameof(INavigationService.NavigateTo))!
                    .MakeGenericMethod(pageType);
                method.Invoke(_navigation, null);
            }
        }

        public ShellViewModel()
        {
            if (!Design.IsDesignMode)
                throw new InvalidOperationException(
                    "This constructor is for design-time only.");

            L = DesignLocalizationService.Instance;
            Router = new Router();
            _navigation = null!;
            _localization = DesignLocalizationService.Instance;
            _themeService = new DesignThemeService();
            NavigateCommand = new RelayCommand<string?>(_ => { });
            ToggleThemeCommand = new RelayCommand(() => { });
            ToggleLanguageMenuCommand = new RelayCommand(() => { });
            SetLanguageCommand = new RelayCommand<string?>(_ => { });
        }

        [RelayCommand]
        private void SideMenuResize()
        {
            SideMenuExpanded = !SideMenuExpanded;
        }

        partial void OnSideMenuExpandedChanged(bool value)
        {
            // Ширина левой панели фиксируется в двух состояниях:
            // в развернутом и свернутом виде она больше не зависит от содержимого.
            OnPropertyChanged(nameof(SidebarWidth));
            OnPropertyChanged(nameof(HomeTooltip));
            OnPropertyChanged(nameof(SupplyTooltip));
            OnPropertyChanged(nameof(RetailSalesTooltip));
            OnPropertyChanged(nameof(SalesTooltip));
            OnPropertyChanged(nameof(OrdersTooltip));
        }

        private string? GetSidebarTooltip(string pageKey, string text) =>
            !SideMenuExpanded && !string.Equals(_currentSectionKey, pageKey, StringComparison.Ordinal)
                ? text
                : null;

        private string GetSectionTitle(string pageKey) =>
            pageKey switch
            {
                "Dashboard" => HomeText,
                "Supply" => SupplyText,
                "Retail Sales" => RetailSalesText,
                "Sales" => SalesText,
                "Orders" => OrdersText,
                _ => "TradeUz"
            };

        private sealed class DesignThemeService : IThemeService
        {
            public event EventHandler? ThemeChanged
            {
                add { }
                remove { }
            }

            public ThemeVariant CurrentTheme => ThemeVariant.Light;

            public void Initialize()
            {
            }

            public void SetLight()
            {
            }

            public void SetDark()
            {
            }

            public void Toggle()
            {
            }
        }
    }
}
