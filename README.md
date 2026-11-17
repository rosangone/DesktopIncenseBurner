# DesktopIncenseBurner

A lightweight, offline Windows desktop incense burner built with C# / WPF on .NET 8.

## Target
- Windows 10 x64
- .NET 8 WPF
- Local/offline operation; no server required

## Current first-version features
- Transparent, borderless floating desktop window
- Dragging and resizing
- Vector-drawn bronze incense burner
- Animated smoke particles
- 1–3 incense sticks
- Burn countdown, pause/resume and relight
- Right-click menu
- Settings dialog for incense count, burn duration, smoke density/speed, opacity and always-on-top
- Local JSON settings under `%LOCALAPPDATA%\DesktopIncenseBurner\settings.json`
- GitHub Actions Windows x64 self-contained publish

## Build locally on Windows
Install the .NET 8 SDK, then run:

```powershell
dotnet restore .\DesktopIncenseBurner.csproj
dotnet build .\DesktopIncenseBurner.csproj -c Release
dotnet publish .\DesktopIncenseBurner.csproj -c Release -r win-x64 --self-contained true -o .\publish
```

The self-contained publish bundles the .NET runtime, so the target Windows x64 machine does not need a separate .NET 8 Desktop Runtime installation.

## Known first-version limitations
- This is an initial implementation; the first real Windows build is being validated by GitHub Actions.
- Smoke is a lightweight particle effect, not a physically accurate simulation.
- The timer uses a configurable duration; it does not estimate actual incense consumption.
- Startup registration, tray integration, configurable finish sound and installer packaging are not implemented yet.
- The current window includes a standard resize grip; fine-grained resizing behavior should be tested on Windows.

## GitHub Actions
Push to `main`, open a pull request targeting `main`, or run the workflow manually. The workflow publishes a self-contained `win-x64` artifact.
