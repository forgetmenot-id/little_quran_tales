# Little Quran Tales — Architecture

## Scene Flow

```
SplashScene ──(auto)──→ MenuScene ──(Main Story)──→ TitleScene → DialogueScene
                               │                        ↑              │
                               │                   (chapter end)      ↓
                               │                   ┌──────────────────┘
                               │                   │
                               ├──(Mini Games)──→ MiniGameGalleryScene
                               │                        │
                               │                   (pilih difficulty)
                               │                        ↓
                               │                   MiniGameScene
                               │                        │
                               │                   (game over)──→ MenuScene
                               │
                               ├──(Library)──→ LibraryScene
                               │
                               └──(Settings)──→ SettingsScene
```

All transitions use `_game.SceneManager.SwitchTo("scene_id")`.

## Scene Manager

`SceneManager` (`Scenes/SceneManager.cs`) is a simple dictionary-based state machine:

- **Register**(id, scene) — stores scene by string key
- **SwitchTo**(id) — calls `Unload()` on current scene, then `Load()` on new scene
- **GetScene**(id) — returns a scene instance for direct property access (used to pass difficulty to MiniGameScene)

Scene IDs: `splash`, `menu`, `title`, `dialogue`, `minigame`, `minigame_gallery`, `library`, `settings`

## Service Layer

Services are instantiated in `Game1.cs` and exposed as public properties:

```
Game1
 ├── Audio  (AudioManager)
 └── Save   (SaveManager)
```

### AudioManager
- Wraps `MediaPlayer` for BGM, `SoundEffect.Play(volume,...)` for SFX
- Maintains independent `BgmVolume` and `SfxVolume` (0.0–1.0)
- `PlayBgm(Song)` — sets volume then plays
- `PlaySfx(SoundEffect)` — plays at current SFX volume
- `StopBgm()` — stops music

### SaveManager
- Persists to `%LOCALAPPDATA%/LittleQuranTales/save.json`
- Tracks: `CompletedChapters` (List\<string\>), `MiniGameScores` (Dictionary\<string,int\>), `BgmVolume`, `SfxVolume`
- Key methods: `MarkChapterCompleted`, `IsChapterCompleted`, `SetHighScore`, `GetHighScore`, `ResetAll`
- Settings volume is applied on game start via `Game1.LoadVolume()`

## Data Flow

```
JSON files (Data/chapters/*.json)
  ↓ File.ReadAllText + JsonSerializer.Deserialize
ChapterData model
  ↓
DialogueScene reads scenes[] sequentially
  ↓ on each scene:
  - Loads background texture (Content.Load<Texture2D>)
  - Loads/plays BGM (Content.Load<Song> → AudioManager.PlayBgm)
  - Plays SFX (Content.Load<SoundEffect> → AudioManager.PlaySfx)
  - Shows sprites at configured positions
  - Types text character-by-character
  ↓ on chapter end:
  - MarkChapterCompleted via SaveManager
  - Load next chapter JSON or switch to next scene
```

## Mini Game System

`MiniGameScene` has a `Difficulty` property (`normal`/`hard`). Before switching:

```csharp
var mg = (MiniGameScene)_game.SceneManager.GetScene("minigame");
mg.Difficulty = "hard";  // or "normal"
_game.SceneManager.SwitchTo("minigame");
```

Hard mode differences:
- 8 waves (vs 5)
- Enemy count × 1.6
- Max ammo ÷ 1.6 (less ammo)
- Spawn interval ÷ 1.6 (faster)
- Turbulence × 1.6

High scores are saved with keys `ababil_defense_normal` / `ababil_defense_hard`.

## Screen & Coordinate System

- **Resolution**: 1280 × 720 (fixed)
- **HUD bar**: top 48px
- **Ground level**: Y = 670 (Height - 50)
- **UI convention**: `(cx, cy) = (Width/2, Height/2)`
