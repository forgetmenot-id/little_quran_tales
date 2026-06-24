# Little Quran Tales

An educational Islamic visual novel + minigame built with **MonoGame**.  
Follow "Little Kid" and the talking bird **Hudhud** as they journey into Quranic stories to restore lost tales and bring light back to the world.

## Features

- **Visual Novel** — JSON-driven dialogue scenes with character sprites, backgrounds, screen effects (fade, shake, glow), and bilingual text (Indonesian/English)
- **"Ababil Defense" Minigame** — Real-time action game: control a bird dropping stones on war elephants. Features charge mechanics, combo system, wave-based progression, shield/piercing, and boss enemies
- **Two Modes** — Normal (5 waves → story continuation) and Endless (infinite waves, high score)
- **Quranic Recitation** — Murottal playback on chapter completion
- **Library** — Unlockable surah reader with Arabic text + translation + audio recitation
- **Settings** — BGM/SFX volume sliders, language toggle (ID/EN)
- **Save System** — Persistent progress, high scores, and preferences
- **Android Support** — Targets both Windows (DesktopGL) and Android

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Engine | MonoGame 3.8.x |
| Language | C# 12 (.NET 9.0) |
| Platforms | Windows (DesktopGL), Android |
| Content | MGCB pipeline (`.xnb`) |
| Data | JSON (chapters, localization, saves) |

## Chapters

| # | Surah | Status |
|---|-------|--------|
| Prolog | Gerbang Kiamat dan Kitab Kosong | ✅ Complete |
| 1 | Al-Fil (The Elephant) | ✅ Complete |
| 2 | Al-Quraisy | 🔜 Coming Soon |

## How to Run

```bash
dotnet run
```

Requires [.NET 9.0 SDK](https://dotnet.microsoft.com/download) installed.

### Android Build

```bash
dotnet build -t:Run -f net9.0-android
```

## Project Structure

```
├── Scenes/       # Scene implementations (menu, dialogue, minigame, library, settings)
├── Services/     # Audio, Save, Localization managers
├── Models/       # Data models (ChapterData, SaveData)
├── Data/         # JSON chapters, localization strings, surah data
├── Content/      # Game assets (images, audio, fonts)
└── docs/         # Architecture & structure documentation
```

## License

All rights reserved.
