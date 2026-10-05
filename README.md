# DailyAnimeWallpaper
Put the exe to startup folder. You will get different anime wallpapers every time program run.

This program gets wallpapers from https://waifu.pics
Thanks to this repository https://github.com/Waifu-pics/waifu-api

## Image storage

Set `imageSaveDirectory` in `appsettings.json` beside the executable to choose where downloaded wallpapers are saved. Relative paths are resolved from the application directory; an absolute path can also be used.

```json
{
  "imageSaveDirectory": "pictures"
}
```
