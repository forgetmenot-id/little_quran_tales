# Little Quran Tales — Project Structure

```
LittleQuranTales/
├── Content/                        ← Raw assets (processed by MonoGame Pipeline → .xnb)
│   ├── Images/
│   │   ├── BGs/                    ← Background images (*.jpg)
│   │   ├── Sprites/                ← Character/item sprites (*.png)
│   │   └── UI/                     ← UI elements: menu, icons, panels, splash (*.png)
│   ├── Audio/
│   │   ├── BGM/                    ← Background music tracks (*.oga, *.ogg)
│   │   └── SFX/                    ← Sound effects (*.wav)
│   ├── Fonts/                      ← SpriteFont definitions (*.spritefont)
│   └── Content.mgcb                ← Pipeline project file (tells mgcb what to build)
│
├── Data/
│   └── chapters/                   ← Story JSON files (one per chapter)
│       ├── prolog.json
│       └── al-fil.json
│
├── Models/                         ← C# data models (POCO / DTO)
│   ├── ChapterData.cs              ← JSON chapter/scene/sprite/choice models
│   └── SaveData.cs                 ← Persistence data model
│
├── Services/                       ← Singleton service classes
│   ├── AudioManager.cs             ← BGM/SFX global volume control
│   └── SaveManager.cs              ← Progress/settings JSON persistence
│
├── Scenes/                         ← Game scene implementations (IScene)
│   ├── IScene.cs                   ← Scene interface
│   ├── SceneManager.cs             ← Scene registry + state machine
│   ├── SplashScene.cs              ← Animated splash → auto-advance to menu
│   ├── MenuScene.cs                ← Main navigation hub
│   ├── DialogueScene.cs            ← Visual novel engine (driven by JSON)
│   ├── TitleScene.cs               ← Chapter title card
│   ├── MiniGameScene.cs            ← Ababil Defense minigame
│   ├── MiniGameGalleryScene.cs     ← Minigame picker (Normal/Hard)
│   ├── LibraryScene.cs             ← Chapter list + murottal player
│   └── SettingsScene.cs            ← Volume sliders + reset progress
│
├── docs/                           ← Project documentation
│   ├── STRUCTURE.md                ← This file
│   └── ARCHITECTURE.md             ← Architecture & data flow
│
├── Game1.cs                        ← Main Game class (entry point, scene registration)
├── Program.cs                      ← Creates Game1, calls Run()
├── LittleQuranTales.csproj         ← MSBuild project file
├── run.bat                         ← Quick launch script
├── app.manifest                    ← Windows DPI/manifest config
├── Icon.bmp                        ← Application icon (bitmap)
└── Icon.ico                        ← Application icon (.ico)
```

## Asset Naming Conventions

| Category  | Prefix        | Format            | Folder                | Load path example                    |
|-----------|---------------|-------------------|-----------------------|--------------------------------------|
| Background| `bg_`         | `*.jpg`           | `Content/Images/BGs/` | `Content.Load<Texture2D>("Images/BGs/bg_langit_kiamat")` |
| Sprite    | `spr_`        | `*.png`           | `Content/Images/Sprites/` | `Content.Load<Texture2D>("Images/Sprites/spr_little_kid")` |
| UI        | `icon_`/`panel_`/`menu_` | `*.png` | `Content/Images/UI/` | `Content.Load<Texture2D>("Images/UI/menu_bg")` |
| BGM       | `bgm_`        | `*.ogg`/`*.oga`   | `Content/Audio/BGM/`  | `Content.Load<Song>("Audio/BGM/bgm_menu")` |
| SFX       | `sfx_`        | `*.wav`           | `Content/Audio/SFX/`  | `Content.Load<SoundEffect>("Audio/SFX/sfx_click")` |
| Font      | `GameFont`    | `*.spritefont`    | `Content/Fonts/`      | `Content.Load<SpriteFont>("Fonts/GameFont")` |

### Adding new assets
1. Place the source file in the correct `Content/.../` subfolder
2. Open `Content.mgcb` and add an entry with the corresponding `#begin`, `/importer:`, `/processor:`, `/build:` lines
3. Load in code using `_game.Content.Load<T>("Category/Subcategory/FilenameWithoutExtension")`
4. If adding BGM to a chapter story, update the JSON file's `"bgm"` field to match the asset name (e.g., `"bgm_prolog"`)

## JSON Data Format

Each chapter JSON file follows the `ChapterData` schema defined in `Models/ChapterData.cs`:

```json
{
  "id": "chapter-id",
  "title": "Chapter Title",
  "bgm": "bgm_asset_name",
  "nextChapter": "next_chapter_id",
  "scenes": [
    {
      "text": "Narasi...",
      "background": "bg_asset_name",
      "bgm": "bgm_asset_name",
      "sfx": "sfx_asset_name",
      "sprites": [
        { "name": "spr_asset_name", "x": 100, "y": 300, "scale": 1.0 }
      ],
      "choices": [
        { "text": "Pilihan A", "nextScene": null }
      ]
    }
  ]
}
```
