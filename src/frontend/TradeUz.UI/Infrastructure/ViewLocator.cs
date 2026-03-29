using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace TradeUz.UI.Infrastructure;

public class ViewLocator : IDataTemplate
{
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private static readonly Dictionary<Type, Type?> _viewCache = new();

    public Control Build(object? data)
    {
        if (data == null)
            return new TextBlock { Text = "No ViewModel" };

        var viewModelType = data.GetType();

        if (!_viewCache.TryGetValue(viewModelType, out var viewType))
        {
            // Используем соглашение по именованию: DashboardViewModel -> DashboardView.
            var viewName = viewModelType.Name.Replace("ViewModel", "View");

            viewType = _assembly
                .GetTypes()
                .FirstOrDefault(t => t.Name == viewName);

            // Кэшируем результат, чтобы не сканировать assembly повторно.
            _viewCache[viewModelType] = viewType;
        }

        if (viewType == null)
            return new TextBlock { Text = $"View not found for {viewModelType.Name}" };

        return (Control)Activator.CreateInstance(viewType)!;
    }

    public bool Match(object? data)
        // Этот template обслуживает только viewmodel, чтобы не перехватывать другие типы.
        => data != null && data.GetType().Name.EndsWith("ViewModel");
}
