# KjcBusinessHub

The purpose of KJC Business Hub is to help me import transactions done on a bank account and to link these to source documents that should be sent every month via mail. 

## Runtime modes

- **Production mode** (default): uses `%LOCALAPPDATA%/KjcBusinessHub`
  - Database: `kjcbusinesshub.db`
  - Settings: `settings.json`
  - Logs: `logs/kjcbusinesshub-*.log`
- **Development mode**: uses `%LOCALAPPDATA%/KjcBusinessHub.Dev`
  - Database: `kjcbusinesshub.dev.db`
  - Settings: `settings.dev.json`
  - Logs: `logs/kjcbusinesshub-*.log`

Enable development mode with either:

- CLI argument: `--mode=development`
- Environment variable: `KJCBH_RUNTIME_MODE=development`

## First Run - Windows Defender SmartScreen Warning

When you run the installer for the first time, Windows Defender SmartScreen may show:
> "Windows protected your PC - Microsoft Defender SmartScreen prevented an unrecognized app from starting."

This is normal for unsigned applications. To proceed:
1. Click **"More info"**
2. Click **"Run anyway"**

The application is safe to run. We plan to add code signing in future releases.
