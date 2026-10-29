# Nagara

**While you work, while you watch, while you play — your music controls always go to the same place.**

[日本語 README](README.md)

Nagara is a tiny free menu-bar utility for macOS that lets you pin your keyboard media keys (play/pause, next, previous) to the app you choose. A Windows version is in development.

## The problem

Media keys normally go to whichever app grabbed the media session last. If you listen to music on Spotify while a video is open in your browser, the keys often control the wrong app. Nagara removes the fight by routing media keys to a fixed target of your choice.

## Download and install (macOS)

1. Download the latest `Nagara-vX.Y.Z-mac.zip` from [Releases](https://github.com/Ren9618/Nagara/releases), unzip it, and move `Nagara.app` to your Applications folder.
2. First launch: the app is not Developer ID signed, so macOS may refuse to open it. Right-click `Nagara.app` and choose "Open", or allow it under System Settings → Privacy & Security.
3. Media-key interception requires Accessibility permission. Follow the in-app guidance and turn Nagara ON under System Settings → Privacy & Security → Accessibility.
4. Controlling Spotify / Apple Music requires Automation permission. macOS will ask the first time Nagara controls them.

## Features

- Pin media keys to a target: Spotify / Apple Music / Automatic (the player that is playing)
- Switch the target with one click from the menu bar
- If the target app is not running, the Play key launches it and starts playback
- Optional launch at login
- Multilingual UI: English / Japanese (follows the OS language)
- No network access, no telemetry. Settings are stored locally only.

## Platforms

- macOS 14.6 or later (Universal Binary: Apple Silicon / Intel)
  - Verified on: macOS 27.0.1
- Windows: in development

## Known limitations

- In Automatic mode, the routing decision is an approximation based on the last target Nagara sent to and the playback state at key-press time. If you start or stop playback manually outside Nagara, the decision may be stale.
- Replacing the app may require re-granting the Accessibility permission, due to how macOS TCC works.

## Building (macOS)

Requires Xcode.

```bash
cd macos
DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer \
  xcodebuild -project Nagara.xcodeproj -scheme Nagara \
  -configuration Release build
```

## Spec

See the spec draft in [docs/spec.md](docs/spec.md) (Japanese).

## License

MIT License

## Author

Rayno Works
- Megrab Studio (video downloader): https://megrab.mikumiku.online
- X: https://x.com/Rayno_works

Note: This is a personal project. Issues are read, but responses are not guaranteed.
