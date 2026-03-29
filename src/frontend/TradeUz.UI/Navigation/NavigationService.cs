using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace TradeUz.UI.Navigation;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _provider;
    private readonly IRouter _router;

    // Кэшируем уже созданные viewmodel страниц, чтобы не терять состояние
    // при навигации между разделами.
    private readonly Dictionary<Type, object> _cache = new();

    public NavigationService(IServiceProvider provider, IRouter router)
    {
        _provider = provider;
        _router = router;
    }

    public void NavigateTo<T>() where T : class
    {
        var type = typeof(T);

        if (!_cache.ContainsKey(type))
            _cache[type] = _provider.GetRequiredService<T>();

        _router.SetCurrent(_cache[type]);
    }
}
