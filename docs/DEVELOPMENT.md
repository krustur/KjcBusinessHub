# Development Guide

This document provides setup, build, and run instructions for KjcBusinessHub.

---

## Prerequisites

| Tool             | Minimum Version | Notes                              |
|------------------|-----------------|------------------------------------|
| .NET SDK         | 8.0             | https://dotnet.microsoft.com       |
| Git              | 2.x             |                                    |

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/krustur/KjcBusinessHub.git
cd KjcBusinessHub
```

### 2. Restore dependencies

```bash
dotnet restore
```

### 3. Configure the application

Copy the example settings file and fill in your local values:

```bash
cp src/KjcBusinessHub.Web/appsettings.Development.example.json \
   src/KjcBusinessHub.Web/appsettings.Development.json
```

Edit `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=kjcbusinesshub.db"
  }
}
```

> _For SQLite (default development setup), no extra server is needed._

### 4. Apply database migrations

```bash
dotnet ef database update \
  --project src/KjcBusinessHub.Infrastructure \
  --startup-project src/KjcBusinessHub.Web
```

### 5. Run the application

```bash
dotnet run --project src/KjcBusinessHub.Web
```

The app will be available at `https://localhost:5001` (or the port shown in the console).

---

## Running Tests

```bash
dotnet test
```

To run tests for a specific project:

```bash
dotnet test tests/KjcBusinessHub.Application.Tests
```

---

## Project Scripts

> _Placeholder: Add any build scripts or Makefile targets here._

---

## Code Style

- Follow the **.editorconfig** in the repository root.
- Run `dotnet format` before committing to auto-fix formatting issues.

```bash
dotnet format
```

---

## Branching Strategy

| Branch       | Purpose                                  |
|--------------|------------------------------------------|
| `main`       | Stable, production-ready code            |
| `feature/*`  | New features (branch from `main`)        |
| `fix/*`      | Bug fixes                                |
| `chore/*`    | Non-functional changes (docs, CI, etc.)  |

---

## Pull Requests

- Reference the related GitHub Issue in the PR description.
- Ensure all tests pass before requesting review.
- Follow the PR template if one exists in `.github/`.

---

