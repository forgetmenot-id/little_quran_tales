using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace LittleQuranTales.Services;

public class LocalizationManager
{
    private Dictionary<string, Dictionary<string, string>> _strings;
    private string _currentLang = "id";

    public string CurrentLang => _currentLang;

    public void Load(string path)
    {
        using var stream = TitleContainer.OpenStream(path);
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        _strings = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
        if (_strings == null)
            _strings = new Dictionary<string, Dictionary<string, string>>();
    }

    public void SetLanguage(string lang)
    {
        if (_strings != null && _strings.ContainsKey(lang))
            _currentLang = lang;
    }

    public string Get(string key)
    {
        if (_strings != null &&
            _strings.TryGetValue(_currentLang, out var langDict) &&
            langDict.TryGetValue(key, out var val))
            return val;
        return key;
    }

    public string Format(string key, params object[] args)
    {
        var s = Get(key);
        try { return string.Format(s, args); }
        catch { return s; }
    }
}
