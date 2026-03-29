using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TradeUz.UI.Infrastructure.Storage;

public class LocalSettingsService : ILocalSettingsService
{
    private readonly string _filePath;
    private Dictionary<string, string> _settings = new();

    public LocalSettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var folder = Path.Combine(appData, "TradeUz.UI");

        // Настройки сохраняем в профиле пользователя, а не рядом с exe.
        Directory.CreateDirectory(folder);

        _filePath = Path.Combine(folder, "settings.json");

        if (File.Exists(_filePath))
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                _settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                            ?? new Dictionary<string, string>();
            }
            catch
            {
                // Если файл повреждён, запускаем приложение с пустыми настройками.
                _settings = new Dictionary<string, string>();
            }
        }
    }

    public void Save<T>(string key, T value)
    {
        _settings[key] = JsonSerializer.Serialize(value);
        Persist();
    }

    public T? Load<T>(string key)
    {
        if (_settings.TryGetValue(key, out var json))
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                // если файл повреждён — просто игнорируем
                return default;
            }
        }

        return default;
    }

    private void Persist()
    {
        // Объём настроек маленький, поэтому просто пересохраняем весь файл.
        var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_filePath, json);
    }
}
