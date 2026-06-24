using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LittleQuranTales.Models;

public class ChapterData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("nextChapter")]
    public string NextChapter { get; set; }

    [JsonPropertyName("scenes")]
    public List<DialogueSceneData> Scenes { get; set; }
}

public class DialogueSceneData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("background")]
    public string Background { get; set; }

    [JsonPropertyName("bgm")]
    public string Bgm { get; set; }

    [JsonPropertyName("sfx")]
    public string Sfx { get; set; }

    [JsonPropertyName("effect")]
    public string Effect { get; set; }

    [JsonPropertyName("narration")]
    public bool Narration { get; set; }

    [JsonPropertyName("speaker")]
    public string Speaker { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("textEn")]
    public string TextEn { get; set; }

    [JsonPropertyName("sprites")]
    public List<SpriteData> Sprites { get; set; }

    [JsonPropertyName("choices")]
    public List<ChoiceData> Choices { get; set; }
}

public class SpriteData
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("scale")]
    public float Scale { get; set; } = 1f;
}

public class ChoiceData
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("nextScene")]
    public string NextScene { get; set; }
}
