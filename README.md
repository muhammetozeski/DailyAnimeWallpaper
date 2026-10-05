# DailyAnimeWallpaper

Downloads a new anime image from [waifu.pics](https://waifu.pics) and sets it as the Windows desktop wallpaper each time it starts.
Place the executable or its shortcut in the Windows Startup folder to change the wallpaper when you sign in.

## Configure the image folder

On first launch, the application creates `UserData/settings.txt` beside the executable. Set `ImageSaveDirectory` to choose where downloaded images are kept. Relative paths are resolved from the application folder; absolute paths are also supported.

```text
ImageSaveDirectory = pictures
```

The `Language` setting accepts `system`, `tr`, or `en`. Logs are written under `AppCache/Logs` when `EnableLogging` is true.

## Downloads

- `DailyAnimeWallpaper.exe` is self-contained and includes the .NET runtime.
- `DailyAnimeWallpaper-FrameworkDependent-RequiresNET7.exe` requires the .NET 7 x86 runtime.

The executables are digitally signed. To let Windows verify the signature, run `Install-Certificate.cmd` from `SignatureTrust.zip` once. The programs run without it; only the signature stays unverified.

Thanks to the [waifu-api repository](https://github.com/Waifu-pics/waifu-api).
