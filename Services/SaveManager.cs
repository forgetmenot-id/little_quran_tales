using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using LittleQuranTales.Models;

namespace LittleQuranTales.Services;

public class SaveManager
{
    private readonly string _savePath;
    public SaveData Data { get; private set; } = new();

    public SaveManager()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LittleQuranTales");
        Directory.CreateDirectory(dir);
        _savePath = Path.Combine(dir, "save.json");
        Load();
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(_savePath)) return;
            var json = File.ReadAllText(_savePath);
            Data = JsonSerializer.Deserialize<SaveData>(json) ?? new SaveData();
        }
        catch
        {
            Data = new SaveData();
        }
    }

    public void Save()
    {
        var snapshot = new SaveData
        {
            BgmVolume = Data.BgmVolume,
            SfxVolume = Data.SfxVolume,
            Language = Data.Language,
            CompletedChapters = new System.Collections.Generic.List<string>(Data.CompletedChapters),
            MiniGameScores = new System.Collections.Generic.Dictionary<string, int>(Data.MiniGameScores)
        };
        Task.Run(() =>
        {
            try
            {
                var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_savePath, json);
            }
            catch { }
        });
    }

    public void MarkChapterCompleted(string chapterId)
    {
        if (!Data.CompletedChapters.Contains(chapterId))
            Data.CompletedChapters.Add(chapterId);
        Save();
    }

    public bool IsChapterCompleted(string chapterId)
    {
        return Data.CompletedChapters.Contains(chapterId);
    }

    public void SetHighScore(string gameId, int score)
    {
        if (!Data.MiniGameScores.ContainsKey(gameId) || Data.MiniGameScores[gameId] < score)
            Data.MiniGameScores[gameId] = score;
        Save();
    }

    public int GetHighScore(string gameId)
    {
        return Data.MiniGameScores.TryGetValue(gameId, out var s) ? s : 0;
    }

    public void ResetAll()
    {
        Data.CompletedChapters.Clear();
        Data.MiniGameScores.Clear();
        Save();
    }
}
