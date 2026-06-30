# Little Quran Tales

An educational Islamic visual novel + minigame built with **MonoGame**.  
Follow "Little Kid" and the talking bird **Hudhud** as they journey into Quranic stories to restore lost tales and bring light back to the world.

## Features

- **Visual Novel** — JSON-driven dialogue scenes with character sprites, backgrounds, screen effects (fade, shake, glow), and bilingual text (Indonesian/English)
- **"Ababil Defense" Minigame** — Real-time action game: control a bird dropping stones on war elephants. Features charge mechanics, combo system, wave-based progression, shield/piercing, and boss enemies
- **"Susun Kata" Word Game** — Arrange shuffled Arabic Quranic verses into correct order (drag & drop or tap-to-select)
- **"Quiz Al-Alaq"** — Multiple-choice quiz about Surah Al-Alaq
- **Two Modes per Game** — Story Mode and Endless Mode (high score)
- **Quranic Recitation** — Murottal playback on chapter completion
- **Library** — Unlockable surah reader with Arabic text + translation + recitation audio
- **Settings** — BGM/SFX volume sliders, language toggle (ID/EN)
- **Save System** — Persistent progress, high scores, and preferences

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Engine | MonoGame 3.8.x |
| Language | C# 12 (.NET 9.0) |
| Platforms | Windows (DesktopGL), Android |
| Arabic Rendering | SkiaSharp + HarfBuzzSharp (proper RTL shaping) |
| Quran Data | SQLite (114 surahs, full verse database) |
| Content Pipeline | MGCB (`.xnb` compiled assets) |
| Data | JSON (chapters, localization, saves) |

## Chapters

| # | Surah | Status |
|---|-------|--------|
| Prolog | Gerbang Kiamat dan Kitab Kosong | ✅ Complete |
| 1 | Al-Fil (The Elephant) | ✅ Complete |
| 2 | Al-Alaq (The Clot) | ✅ Complete |

## For Developers

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- MonoGame Workload: `dotnet workload install maui-android`
- For Android: Android SDK (API 21+), JDK 17+

### Running on Windows

```bash
dotnet run
```

### Building for Android (APK)

```bash
dotnet publish -f net9.0-android -c Release
```

Output: `bin/Release/net9.0-android/publish/com.littlequrantales.app-Signed.apk`

### Building for Play Store (AAB)

```bash
dotnet publish -f net9.0-android -c Release -p:AndroidPackageFormat=aab
```

Output: `bin/Release/net9.0-android/publish/com.littlequrantales.app-Signed.aab`

### Installing APK on Device

1. Enable Developer Options & USB Debugging on your Android device
2. Connect via USB and verify: `adb devices`
3. Install the APK:

```bash
adb install -r "bin/Release/net9.0-android/publish/com.littlequrantales.app-Signed.apk"
```

Or copy the `.apk` to your device and open it with a file manager.

### Project Structure

```
├── Scenes/       # Scene implementations (IScene interface)
│   ├── IScene.cs / SceneManager.cs
│   ├── SplashScene.cs       # Logo animation → menu
│   ├── LoadingScene.cs      # Loading bar (Android asset loading)
│   ├── MenuScene.cs         # Main hub (story, minigames, library, settings)
│   ├── DialogueScene.cs     # Visual novel engine (JSON-driven)
│   ├── MiniGameScene.cs     # Ababil Defense (bird + elephants)
│   ├── WordOrderScene.cs    # Susun Kata word ordering game
│   ├── AlaqGameScene.cs     # Quiz Al-Alaq
│   ├── LibraryScene.cs      # Surah reader + murottal
│   ├── StorySelectionScene.cs  # Chapter grid selector
│   ├── MiniGameGalleryScene.cs # Minigame picker
│   ├── SettingsScene.cs     # Volume, language, reset
│   └── TitleScene.cs        # Legacy chapter title card
├── Services/     # Singleton services
│   ├── AudioManager.cs      # BGM/SFX playback
│   ├── SaveManager.cs       # JSON persistence
│   ├── LocalizationManager.cs  # ID/EN translations
│   ├── ArabicTextRenderer.cs   # Skia + HarfBuzz RTL renderer
│   └── LogHelper.cs         # Debug + crash logging
├── Data/         # Chapter JSONs, DB, constants
│   ├── Constants.cs         # Scene IDs, paths, config
│   ├── QuranDbService.cs    # Quran SQLite access
│   ├── ArabicFixer.cs       # Arabic string utilities
│   ├── chapters/            # Dialogue JSON files
│   │   ├── prolog.json
│   │   ├── al-fil.json
│   │   ├── ending.json
│   │   ├── al-alaq.json
│   │   └── al-alaq_end.json
│   └── lang.json            # ID/EN strings
├── Models/       # Data models
│   ├── ChapterData.cs       # JSON scene/sprite/choice schema
│   └── SaveData.cs          # Save file schema
├── Content/      # Game assets (processed by MGCB → .xnb)
│   ├── Images/  (BGs, Sprites, UI)
│   ├── Audio/   (BGM, SFX, library/murottal)
│   ├── Fonts/   (spritefonts + Amiri-Regular.ttf)
│   └── quran.db             # Quranic verse database
├── docs/         # Architecture docs
├── Game1.cs                 # Main Game class
├── MainActivity.cs          # Android activity
└── LittleQuranTales.csproj  # MSBuild (dual-target: net9.0 + net9.0-android)
```

### Key Architecture Notes

- **Virtual Resolution**: 1280×720 rendered to `RenderTarget2D`, scaled to screen
- **Touch Input**: Unified via `TouchState` (TouchPanel on Android, Mouse on Desktop)
- **Scene Lifecycle**: `IScene { Load(), Update(), Draw(), Unload() }` — scenes are singletons, reused on switch
- **Try-Finally**: All `Draw()` methods use try-finally on `Begin/End` + `SetRenderTarget` to prevent hard freezes on exceptions
- **Arabic Text**: Uses SkiaSharp + HarfBuzzSharp (not MonoGame SpriteFont) — caches rendered glyphs as Texture2D
- **Story Pipeline**: DialogueScene → minigame → LibraryScene (murottal) → DialogueScene (epilogue) → Menu

See `docs/ARCHITECTURE.md` for complete details.

## License

All rights reserved.
