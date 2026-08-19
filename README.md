# KjcBusinessHub

The purpose of KJC Business Hub is to help me import transactions done on a bank account and to link these to source documents that should be sent every month via mail. 

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