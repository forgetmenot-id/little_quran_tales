namespace LittleQuranTales.Data;

/// <summary>Centralized scene identifiers for SceneManager registration and navigation.</summary>
public static class SceneId
{
    public const string Splash = "splash";
    public const string Menu = "menu";
    public const string Title = "title";
    public const string Dialogue = "dialogue";
    public const string Minigame = "minigame";
    public const string Settings = "settings";
    public const string Library = "library";
    public const string MinigameGallery = "minigame_gallery";
    public const string Loading = "loading";
}

/// <summary>Localization keys used in menu button lookups.</summary>
public static class SceneKey
{
    public const string MainStory = "main_story";
    public const string MiniGames = "mini_games";
    public const string Library = "library";
    public const string Settings = "settings";
}

/// <summary>File paths for chapter JSON data.</summary>
public static class ChapterPath
{
    /// <summary>Directory prefix for all chapter JSON files.</summary>
    public const string Directory = "Data/chapters/";
    public const string Prolog = "Data/chapters/prolog.json";
    public const string AlFil = "Data/chapters/al-fil.json";
    public const string Ending = "Data/chapters/ending.json";
}

/// <summary>SpriteFont asset paths registered in the MGCP pipeline.</summary>
public static class FontPath
{
    public const string GameFont = "Fonts/GameFont";
    public const string SettingsFont = "Fonts/SettingsFont";
}

/// <summary>Global game configuration constants (resolution, timing, etc.).</summary>
public static class GameConfig
{
    /// <summary>Virtual resolution width used for rendering and input scaling.</summary>
    public const int VirtualWidth = 1280;
    /// <summary>Virtual resolution height used for rendering and input scaling.</summary>
    public const int VirtualHeight = 720;
    /// <summary>Cooldown (seconds) after button clicks to prevent double-tap.</summary>
    public const float ClickCooldown = 0.3f;
    /// <summary>Short input debounce (seconds) for instant interactions.</summary>
    public const float InputDelay = 0.2f;
}
