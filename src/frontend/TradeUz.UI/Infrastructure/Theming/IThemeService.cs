using System;
using Avalonia.Styling;

public interface IThemeService
{
    event EventHandler? ThemeChanged;

    ThemeVariant CurrentTheme { get; }

    void Initialize();
    void SetLight();
    void SetDark();
    void Toggle();
}
