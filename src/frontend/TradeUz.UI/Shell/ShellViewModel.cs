using TradeUz.UI.Infrastructure.Theming;
using TradeUz.UI.Navigation;
using TradeUz.UI.Pages.Dashboard;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using Avalonia.Controls;

namespace TradeUz.UI.Shell
{
    public partial class ShellViewModel: ObservableObject
    {
        [ObservableProperty]
        private bool _sideMenuExpanded =true;
        
        private readonly INavigationService _navigation;
        private readonly IThemeService _themeService;
        public IRouter Router { get; }
        public IRelayCommand<string?> NavigateCommand { get; }
        public IRelayCommand ToggleThemeCommand { get; }

        public ShellViewModel(
    INavigationService navigation,
    IRouter router,
    IThemeService themeService)
        {
            _navigation = navigation;
            Router = router;
            _themeService = themeService;

            NavigateCommand = new RelayCommand<string?>(Navigate);
            ToggleThemeCommand = new RelayCommand(() => _themeService.Toggle());

            _navigation.NavigateTo<DashboardViewModel>();
        }

        private void ToggleTheme()
        {
            _themeService.Toggle();
        }

        private void Navigate(string? page)
        {
            if (string.IsNullOrEmpty(page))
                return;

            if (NavigationRegistry.Pages.TryGetValue(page, out var pageType))
            {
                var method= typeof(INavigationService)
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
        }
        #region Commands

        [RelayCommand]
        private void SideMenuResize()
        {
            SideMenuExpanded = !SideMenuExpanded;
        }

        #endregion
    }
}
