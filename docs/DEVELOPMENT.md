# Development Guide

This document provides setup, build, run, and release instructions for KjcBusinessHub.

---

## Prerequisites

| Tool | Minimum Version |
|---|---|
| .NET SDK | 10.0 |
| Git | 2.x |

---

## Build and test

```bash
dotnet restore
dotnet build KjcBusinessHub.slnx
dotnet test KjcBusinessHub.slnx
```

---

## Runtime modes

The desktop app supports two explicit runtime modes.

### Production mode (default)

Storage root:

- `%LOCALAPPDATA%/KjcBusinessHub`

Files:

- `kjcbusinesshub.db`
- `settings.json`
- `logs/kjcbusinesshub-*.log`

### Development mode

Storage root:

- `%LOCALAPPDATA%/KjcBusinessHub.Dev`

Files:

- `kjcbusinesshub.dev.db`
- `settings.dev.json`
- `logs/kjcbusinesshub-*.log`

Enable development mode with either:

```bash
dotnet run --project src/KjcBusinessHub.UI -- --mode=development
```

or:

```bash
KJCBH_RUNTIME_MODE=development dotnet run --project src/KjcBusinessHub.UI
```

---

## Tray + close behavior

- Tray icon is created on startup and removed on app shutdown.
- Tray menu includes:
  - `Close to system tray` (persisted checkbox)
  - `Check for Updates`
  - `Check for Updates (pre-release)`
  - `About`
  - `Quit`
- Window close behavior:
  - If `Close to system tray` is enabled, close hides the app to tray.
  - If disabled, close triggers quit confirmation.
- Quit actions always prompt: **"Are you sure you want to quit KJC Business Hub?"**

---

## Auto-updates (Velopack)

- Velopack bootstrap runs at app startup.
- Production installs check GitHub Releases for updates.
- Manual update checks are available from the tray menu and About dialog for both stable and pre-release channels.
- When an update is found, the package is downloaded and the app restarts to apply it.
- When a manual update check does not find an update, the app shows a user-facing message.
- Optional channel override: `KJCBH_UPDATE_CHANNEL=prerelease`.

---

## Release process

A release workflow is provided in `.github/workflows/release.yml`.

Supported triggers:

- Push tags: `v*.*.*` (stable)
- Manual workflow dispatch with version + channel

Workflow behavior:

- Build publish output (`win-x64`)
- Package with Velopack (`vpk pack`)
- Upload artifacts
- Create GitHub release and attach Velopack packages

Use `stable` or `prerelease` channels to control release visibility.
