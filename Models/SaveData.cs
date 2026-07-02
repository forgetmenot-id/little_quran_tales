using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LittleQuranTales.Models;

public class SaveData
{
    [JsonPropertyName("bgmVolume")]
    public float BgmVolume { get; set; } = 0.5f;

    [JsonPropertyName("sfxVolume")]
    public float SfxVolume { get; set; } = 0.5f;

    [JsonPropertyName("language")]
    public string Language { get; set; } = "id";

    [JsonPropertyName("completedChapters")]
    public List<string> CompletedChapters { get; set; } = new();

    [JsonPropertyName("miniGameScores")]
    public Dictionary<string, int> MiniGameScores { get; set; } = new();

    [JsonPropertyName("hasAgreedToTerms")]
    public bool HasAgreedToTerms { get; set; } = false;
}
