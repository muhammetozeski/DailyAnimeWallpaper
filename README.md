# DailyAnimeWallpaper

![Platform](https://img.shields.io/badge/Platform-Windows-0078D6)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

Downloads and applies a random anime desktop wallpaper on Windows with a single launch, backed by 27 built-in anime, waifu, booru, and ACG wallpaper APIs with automatic provider failover.

## Features

- **One-Click Headless Execution:** Run `DailyAnimeWallpaper.exe` with no arguments (or place a shortcut in Startup) to download and apply a new anime wallpaper immediately and exit.
- **27 Built-in Anime APIs & Failover:** Ships with 27 verified anime/waifu/booru/ACG sources and 160+ subcategories. If a provider fails or shuts down, the engine automatically falls back to the remaining enabled providers.
- **Settings GUI (`settings` argument):** Run `DailyAnimeWallpaper.exe settings` to configure enabled APIs, subcategories (`API -> Subcategories`), NSFW filters (`Follow category selections`, `SFW only`, `NSFW only`), save directory, and interface language (Turkish / English). NSFW subcategories are disabled by default.
- **Custom Sources:** Add custom APIs in the Settings GUI using either a JSON element path (`url`, `items[0].url`, `items[*].url`, `image.original.url`) or a Direct Image / Direct URL endpoint.
- **CSPRNG `UInt128` Weighted Selection:** Every API and subcategory accepts an integer weight in `[0, 2147483647]`. Selection uses `RandomNumberGenerator.Fill` and `UInt128` cumulative weight totals.
- **Portable Data Layout:** Settings (`UserData/settings.txt`), source catalog (`UserData/sources.json`), downloaded wallpapers (`Pictures/` by default), and logs (`AppCache/Logs/`) stay relative to the portable application root.

## Usage

```powershell
# Download and set a new anime wallpaper immediately
.\DailyAnimeWallpaper.exe

# Open the graphical settings and API catalog editor
.\DailyAnimeWallpaper.exe settings
```

## Installation

- `DailyAnimeWallpaper.exe`: Self-contained single-file Windows executable (includes the .NET 10 runtime).
- `DailyAnimeWallpaper-FrameworkDependent-RequiresNET10.exe`: Compact single-file executable requiring the **.NET 10 Windows Desktop Runtime (x64)**.

The executables are digitally signed. To let Windows verify the signature, run `Install-Certificate.cmd` from `SignatureTrust.zip` once. The programs run without it; only the signature stays unverified.

## Documentation

Project knowledge and verified API integration notes start at [Docs/İçindekiler.md](Docs/İçindekiler.md).
