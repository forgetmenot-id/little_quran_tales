# Little Quran Tales — Architecture

## Scene Flow

```
SplashScene ──(auto)──→ LoadingScene ──→ MenuScene
                                                │
                              ┌─────────────────┼──────────────────┐
                              ▼                 ▼                  ▼
                     StorySelectionScene  MiniGameGalleryScene  LibraryScene
                              │                 │                  │
                              ▼                 ▼                  ▼
                     DialogueScene      MiniGameScene        (surah reading)
                              │         WordOrderScene             │
                              │         AlaqGameScene          (murottal)
                              ▼                                    │
                     (chapter end)                                 ▼
                              │                             DialogueScene
                              │                            (epilogue chapter)
                              ▼
                ┌─────────────┼─────────────┐
                ▼             ▼             ▼
          MiniGameScene  WordOrderScene  AlaqGameScene
          (story mode)   (story mode)    (story mode, ch.2)
                │             │             │
                ▼             ▼             ▼
          LibraryScene   LibraryScene   DialogueScene
          (murottal)     (murottal)     (al-alaq_end)
                │             │             │
                ▼             ▼             ▼
          DialogueScene   DialogueScene  MenuScene
          (ending.json)   (al-alaq_end)   (return_story)
                │             │
                ▼             ▼
           MenuScene      MenuScene
          (return_story)  (return_story)
```

## Story Pipeline (per chapter)

Each chapter JSON in `Data/chapters/` has a `nextChapter` field:

| Chapter File     | `nextChapter`      | Result                                  |
|------------------|--------------------|-----------------------------------------|
| `prolog.json`    | `"al-fil"`         | Loads `al-fil.json`                     |
| `al-fil.json`    | `"minigame"`       | Switch to MiniGameScene (story mode)    |
| `ending.json`    | `"return_story"`   | Switch to MenuScene                     |
| `al-alaq.json`   | `"word_order"`     | Switch to WordOrderScene (story mode)   |
| `al-alaq_end.json`| `"return_story"`  | Switch to MenuScene                     |

### Chapter 1 (Al-Fil) flow:
**DialogueScene → MiniGameScene (Ababil Defense, story) → LibraryScene → DialogueScene (ending.json) → MenuScene**

### Chapter 2 (Al-Alaq) flow:
**DialogueScene → WordOrderScene (story) → LibraryScene → DialogueScene (al-alaq_end.json) → MenuScene**

Chapter 2's AlaqGameScene is accessed from StorySelection and is separate from the main story flow.

## Scene Manager

`SceneManager` (`Scenes/SceneManager.cs`) is a dictionary-based state machine:

- **Register**(id, scene) — stores scene by string key
- **SwitchTo**(id) — calls `Unload()` on current scene, then `Load()` on new scene
- **SwitchTo**(id, skipLoad) — same but skips `Load()` (used by LoadingScene)
- **GetScene**(id) — returns a scene instance for direct property access

Scene IDs (`Data/Constants.cs`): `splash`, `menu`, `title`, `dialogue`, `minigame`, `minigame_gallery`, `library`, `settings`, `loading`, `word_order`, `story_selection`, `alaq_game`

## Scene Interface

```csharp
interface IScene { Load(); Update(float dt); Draw(); Unload(); }
```

Each scene is instantiated once in `Game1.Initialize()` and reused. `Load()`/`Unload()` are called every switch. Some scenes use `_loaded` flag to prevent re-loading (MenuScene, StorySelectionScene) — `Unload()` sets it to `false`.

## Game1 Core (`Game1.cs`)

- Monogame `Game` subclass
- Renders at **1280×720 virtual resolution** to a `RenderTarget2D`, then scales to actual screen
- **Touch input**: wraps `TouchPanel.GetState()` (Android) or `Mouse.GetState()` (Desktop) into unified `TouchState`
- **Screen scaling**: `ScreenToVirtual()` maps touch coords to virtual 1280×720 space
  - Android uses `Math.Max(scaleX, scaleY)` (letterbox)
  - Desktop uses `Math.Min(scaleX, scaleY)` (fit)
- Public services: `Audio`, `Save`, `Loc`, `Quran`, `ArabicText`
- `Draw()` has try-finally around `SetRenderTarget(null)` and `_spriteBatch.End()` to prevent freeze on exception

## Service Layer

### AudioManager
- Wraps `MediaPlayer` for BGM, `SoundEffect.Play()` for SFX
- Independent `BgmVolume` and `SfxVolume` (0.0–1.0)
- `PlayBgm(Song)` — stops current, sets volume, plays
- `PlaySfx(SoundEffect)` — plays at current SFX volume

### SaveManager
- Persists to `%LOCALAPPDATA%/LittleQuranTales/save.json`
- Fields: `CompletedChapters` (List\<string\>), `MiniGameScores` (Dictionary\<string,int\>), `BgmVolume`, `SfxVolume`, `Language`
- `MarkChapterCompleted(id)` / `IsChapterCompleted(id)`
- `SetHighScore(gameId, score)` / `GetHighScore(gameId)`
- `ResetAll()` — clears chapters + scores
- Save is async (fire-and-forget Task.Run)

### LocalizationManager
- Loads `Data/lang.json` (dictionary of language → key → string)
- Languages: "id" (Indonesian), "en" (English)
- `Get(key)` returns translated string or the key itself if missing
- `Format(key, args...)` for format strings like `"Wave {0}/{1}"`

### QuranDbService
- Loads `Content/quran.db` (SQLite database using Microsoft.Data.Sqlite)
- Provides `SurahInfo` (114 surahs), `VerseInfo` (verses), `WordInfo`
- Key methods: `GetSurah(number)`, `GetAyahs(surahNumber)`, `GetAyah(surahNumber, ayahNumber)`, `GetWords(verseId)`, `GetAyahWords(surahNumber)`
- `GetSurahNumberFromId(string)` — maps chapter IDs like "al-fatiha" → 1

### ArabicTextRenderer
- Renders Arabic text using **SkiaSharp** + **HarfBuzzSharp** for proper RTL shaping
- `DrawString(batch, text, fontSize, position, color)` — renders to Texture2D cache, draws via SpriteBatch
- `MeasureString(text, fontSize)` — returns Vector2 dimensions
- Caches up to 256 rendered textures; `.ClearCache()` disposes all
- Fallback font: `Content/Fonts/Amiri-Regular.ttf` (extracted from Android assets on first run)

### LogHelper
- Writes to `%LOCALAPPDATA%/LittleQuranTales/trace.log` and `crash.log`
- `Trace(msg)` — file + Console.Error
- `WriteCrash(ex)` — writes exception details to file

## Chapter Data Model

`Models/ChapterData.cs`:

```csharp
ChapterData {
    string Id, Title, NextChapter;
    List<DialogueSceneData> Scenes;
}

DialogueSceneData {
    string Id, Background, Bgm, Sfx, Effect;
    bool Narration;
    string Speaker, Text, TextEn;
    List<SpriteData> Sprites;
    List<ChoiceData> Choices;  // currently unused
}

SpriteData { string Name; float X, Y, Scale; }
ChoiceData { string Text, NextScene; }
```

## Audio Asset Paths

BGM and SFX loaded via `Content.Load` — must match MGCB pipeline build.

| Type | Example path                             | Content.Load call                        |
|------|------------------------------------------|------------------------------------------|
| BGM  | `Content/Audio/BGM/bgm_menu.ogg`         | `Content.Load<Song>("Audio/BGM/bgm_menu")` |
| SFX  | `Content/Audio/SFX/sfx_click.wav`        | `Content.Load<SoundEffect>("Audio/SFX/sfx_click")` |
| Murottal | `Content/Audio/library/al-alaq.ogg`  | `Content.Load<Song>("Audio/library/al-alaq")` |

## Scene Details

### SplashScene
- Shows logo with fade-in/hold/fade-out animation (~3s total)
- Transitions through LoadingScene to MenuScene

### LoadingScene
- Shows loading bar with percentage
- Calls target scene's `Load()` after delay, then `SwitchTo(skipLoad: true)`
- Required because Android Content.Load can take time; prevents missed Draw frames

### MenuScene
- **Scenes and Navigation**: 4 buttons (Main Story, Mini Games, Library, Settings) with hover/click effects
- **Chapter progress panel**: Shows next uncompleted chapter title (bottom-right)
- Uses `_loaded` flag to prevent duplicate Load; `Unload()` resets it
- **Next chapter logic** (`GetNextChapterKey()`): iterates `(prolog, al-fil, al-alaq)`, returns first uncompleted chapter's title key, or "chapter_3_title" if all done
- BGM: `bgm_menu`, restarts if stopped

### DialogueScene
- Visual novel engine driven by JSON chapter files
- **Flow**: `Load()` → `LoadChapter()` → displays scenes sequentially via `Advance()`
- **Scene rendering**:
  - Background loaded on-demand (`ApplyData`) with fade-in brightness
  - Sprites positioned: characters right-aligned, buku_tua centered
  - Active speaker's sprite is full-bright; other characters dimmed with "..."
  - Text types character-by-character with `CharDelay = 0.03s`
  - Effects: screen_shake, fade_in, fade_black/fade_white, golden_glow
- **Advance()** — increments scene index; on chapter end:
  - Marks chapter complete via `Save.MarkChapterCompleted(id)`
  - If `nextChapter` is an ID → loads next JSON file
  - If `"minigame"` → sets `MiniGameScene.IsStoryMode = true; Difficulty = "normal"` → switches to minigame
  - If `"word_order"` → sets `WordOrderScene.SetStoryMode(true)` → switches to WordOrderScene
  - If `"return_story"` → switches to MenuScene
  - Otherwise → switches to MenuScene
- **Exit confirm**: Home icon → popup with Ya/Tidak; Yes → switches to MenuScene
- **Unload**: stops BGM, disposes procedurally-generated glow texture

### MiniGameScene (Ababil Defense)
- Tower-defense style: bird drops stones on advancing elephants to protect Ka'bah
- **Input**: Touch/click to charge and release stone (swipe for bird movement)
- **Stones**: charge time determines radius (tap=0, 0.3s=small AoE, 0.8s=medium, 1.4s=large+pierce)
- **Enemies**: elephants advance from right to left; shield blocks first hit; bosses appear every 2 waves
- **Waves**: 5 in normal mode, infinite in endless mode; difficulty scales with wave number
- **Combo**: every 3 kills gives +1 ammo (normal) or +5 score (endless)
- **Victory**: story mode → `MarkChapterCompleted("al-fil")` → LibraryScene (victory murottal)
- **Game Over**: normal → retry; endless → score saved, back to menu
- Exit confirm: Yes → saves high score, switches to MenuScene
- High score keys: `ababil_defense_normal`, `ababil_defense_endless`

### WordOrderScene (Susun Kata)
- Players arrange shuffled Arabic words of Quranic verses into correct order
- **Modes**: Normal (Surah Al-Alaq sequentially), Story (5 random verses), Endless (random surahs, 60s timer)
- **Verses**: loaded from Quran database by surah number
- **Interaction**: tap word then tap slot, or drag-and-drop; long press for drag
- **Scoring**: words × 2 + 10 per correct answer
- **Story mode**: 2 wrong attempts per ayah = failed; complete all → `MarkChapterCompleted("al-alaq")` → LibraryScene (victory murottal)
- **Endless mode**: time limit per ayah (words × 8s) + total 60s; score accumulates across rounds
- **Timer**: per-ayah countdown; time up = auto-fill with wrong answers → reveal

### AlaqGameScene (Quiz Al-Alaq)
- Quiz about Surah Al-Alaq (8 questions)
- **Input**: tap one of 4 answers; 2 attempts max per question
- **Victory flow**: Phase 0 (win screen) → Phase 1 (white fade) → Phase 2 (listen to murottal) → Phase 3 (auto-advance to DialogueScene with al-alaq_end.json)
- Completes chapter `"al-alaq"` in Phase 3

### LibraryScene
- Scrollable list of chapters with surah info
- **Chapter entries**: prolog (no surah), al-fil (105), al-alaq (96), al-fatihah (1), al-lahab (111), an-nashr (110)
- **Detail panel**: surah name (Arabic + English), ayah count, revelation type, completion status
- **Reading mode**: full surah display with Arabic, translation, transliteration; scrollable with scissor rect
- **Murottal playback**: play/stop button; auto-played on story victory
- **Victory mode**: `VictorySurahNumber` set → auto-load surah + play murottal → auto-advance to DialogueScene epilogue

### StorySelectionScene
- Grid of 5 chapter cards (2 columns, scrollable)
- Card statuses: Available, Completed (green badge), Upcoming (locked)
- Unlock logic: chapters unlock sequentially; chapter i+1 unlocked only when chapter i completed
- Playing a chapter loads its path into DialogueScene and switches

### MiniGameGalleryScene
- Horizontal scrollable carousel of mini game cards
- Games: Ababil Defense, Word Order, + 3 dummy/upcoming slots
- Each game has Normal/Endless buttons
- **Unlock**: Ababil Defense unlocked after completing al-fil; Word Order after completing al-alaq
- Horizontal scroll with drag + momentum + arrow buttons + scroll wheel

### SettingsScene
- BGM volume slider, SFX volume slider
- Language toggle: Indonesia / English
- Reset progress button (with confirmation popup)
- Uses `SettingsFont` spritefont (larger than GameFont)
- **Unload**: saves current volumes to SaveManager

### TitleScene
- Legacy scene (title card for Chapter Al-Fil)
- Press Enter/tap to advance to DialogueScene
- Currently used only as initial entry point from Menu (Main Story)

## Android Build Setup

### csproj Config
- Target: `net9.0-android` (alongside `net9.0` for desktop)
- Android packaging: `AndroidApplication=true`, `ApplicationId=com.littlequrantales.app`
- Assets included via `AndroidAsset` items:
  - `Content\bin\Android\Content\**\*.*` → `Content/...` (compiled XNB files)
  - `Content\Fonts\**\*.*` → `Content/Fonts/...` (Amiri-Regular.ttf)
  - `Data\**\*.json` → flat in assets root
  - `Content\quran.db` → `Content/quran.db`

### Font Extraction
`Amiri-Regular.ttf` is an Android asset (`Content/Fonts/Amiri-Regular.ttf`). On first load, `Game1.LoadContent()` extracts it from assets to `Content/Fonts/Amiri-Regular.ttf` on the filesystem. This file must exist for `ArabicTextRenderer` to initialize.

### Build & Deploy
```bash
dotnet publish -f net9.0-android -c Release
adb install -r "bin/Release/net9.0-android/publish/com.littlequrantales.app-Signed.apk"
```

## Coordinate System

- **Virtual resolution**: 1280 × 720 (defined in `GameConfig.VirtualWidth/Height`)
- All scenes render to `_virtualTarget`, then scaled to actual display
- `ScreenToVirtual(sx, sy)` converts actual screen coords to virtual (for touch input)
- On Android: `Math.Max(scaleX, scaleY)` with letterbox black bars
- On Desktop: `Math.Min(scaleX, scaleY)` with centered view

## Error Handling

- `Game1.Update()` and `Game1.Draw()` wrap scene calls in try-catch, logging via `LogHelper`
- `Draw()` has nested try-finally:
  ```csharp
  try {
      try { SetRenderTarget(target); scene.Draw(); }
      finally { SetRenderTarget(null); }
      try { spriteBatch.Begin(); drawRenderTarget(); }
      finally { spriteBatch.End(); }
  } catch { LogHelper.Trace + WriteCrash }
  ```
- All scene `Draw()` methods that use `Begin()`/`End()` should wrap the drawing code in try-finally to ensure `End()` is always called.
- `Content.Load` failures inside `DialogueScene.ApplyData()` are caught individually per asset to prevent one missing asset from crashing the scene.

## Section Reference

| File | Purpose |
|------|---------|
| `Game1.cs` | Main game loop, service init, scene registration, rendering |
| `Program.cs` | Entry point (`new Game1().Run()`) |
| `MainActivity.cs` | Android activity (reflection to set Game._instance) |
| `Scenes/IScene.cs` | Scene interface |
| `Scenes/SceneManager.cs` | Scene state machine |
| `Services/AudioManager.cs` | BGM/SFX playback |
| `Services/SaveManager.cs` | Progress persistence |
| `Services/LocalizationManager.cs` | Multi-language strings |
| `Services/ArabicTextRenderer.cs` | Skia+HarfBuzz Arabic rendering |
| `Services/LogHelper.cs` | Debug logging |
| `Data/Constants.cs` | Scene IDs, paths, config |
| `Data/QuranDbService.cs` | Quran SQLite database access |
| `Data/lang.json` | Localization strings (id, en) |
| `Models/ChapterData.cs` | JSON chapter schema |
| `Models/SaveData.cs` | Save file schema |
| `LittleQuranTales.csproj` | Build config, Android asset declarations |
