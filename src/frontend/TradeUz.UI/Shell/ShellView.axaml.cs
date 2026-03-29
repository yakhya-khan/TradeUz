using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Input;

namespace TradeUz.UI.Shell
{
    public partial class ShellView : Window
    {
        public ShellView()
        {
            InitializeComponent();
            PropertyChanged += OnWindowPropertyChanged;
            UpdateWindowStateIcon();
        }

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;

            if (e.ClickCount == 2 && CanMaximize)
            {
                WindowState =
                    WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
                return;
            }

            BeginMoveDrag(e);
        }

        private void MinimizeWindow_Click(object? sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void ToggleMaximizeWindow_Click(object? sender, RoutedEventArgs e)
        {
            WindowState =
                WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
        }

        private void CloseWindow_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        // Переключаем иконку между "развернуть" и "восстановить" в зависимости от состояния окна.
        private void UpdateWindowStateIcon()
        {
            var isMaximized = WindowState == WindowState.Maximized;

            if (this.FindControl<Path>("MaximizeIcon") is { } maximizeIcon)
                maximizeIcon.IsVisible = !isMaximized;

            if (this.FindControl<Path>("RestoreIcon") is { } restoreIcon)
                restoreIcon.IsVisible = isMaximized;
        }

        private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == WindowStateProperty)
                UpdateWindowStateIcon();
        }
    }
}
