# Architecture

This document describes the code structure, layers, and architectural patterns used in KjcBusinessHub.

---

## Overview

KjcBusinessHub is a desktop application built with **ASP.NET Core** (backend) and **Avalonia UI** (frontend). It follows a **Clean Architecture** / **Layered Architecture** approach to keep domain logic independent from infrastructure concerns.

---

## Project Structure

```
KjcBusinessHub/
├── src/
│   ├── KjcBusinessHub.Application/     # Entities, value objects, business rules, services, queries, DTOs
│   ├── KjcBusinessHub.Infrastructure/  # EF Core, file storage, external integrations
│   └── KjcBusinessHub.UI/              # Avalonia UI views, view models, components
├── tests/
│   ├── KjcBusinessHub.Application.Tests/
│   └── KjcBusinessHub.Integration.Tests/
└── docs/
```

---

## Layers

### Application Layer (`KjcBusinessHub.Application`)
- Contains all **entities**, **value objects**, **enumerations**, and **domain interfaces**.
- Exposes **services** and **queries** as simple method calls.
- Contains **DTOs**, **validators** (pure C#), and **service interfaces**.
- Has **zero dependencies** on Infrastructure.
- All business rules live here (see `DOMAIN.md`).

**Example — Import Transactions Service:**
```csharp
public class TransactionService(ITransactionRepository repository)
{
    public async Task<ImportResult> PreviewImportAsync(string pastedText)
    {
        // Parses pasted text, classifies rows, and prepares import results
    }
}
```

### Infrastructure Layer (`KjcBusinessHub.Infrastructure`)
- Implements interfaces defined in Application.
- Contains **EF Core DbContext**, **repository implementations**, file storage, and third-party integrations.
- References Application.

### UI Layer (`KjcBusinessHub.UI`)
- Hosts the Avalonia UI views and view models.
- Calls the Application layer directly — never references Infrastructure.

---

## Key Patterns

| Pattern           | Where Used                                  |
|-------------------|---------------------------------------------|
| Service pattern   | Application layer — simple service classes - each service has it's own parameter- and result-dtos - don't leak domain objects! |
| Repository        | Infrastructure — data access abstraction    |
| Clean Architecture| Layer dependency rules (app layer at center)|
| Result pattern    | Return `Result<T>` instead of throwing exceptions |

## Coding Style

- **Always use curly braces** for conditional and loop bodies, even for single-line statements:
  ```csharp
  // ✅ Correct
  if (condition) { return; }

  // ❌ Wrong
  if (condition) return;
  ```

---

## Package References

Keep external packages to a minimum. Every new dependency must be justified and approved.

The following packages are **absolute no-go's** — do not add them under any circumstances:

- **FluentValidation** — use pure C# validators instead
- **AutoMapper** — use explicit manual mapping instead
- **MediatR** — use direct service calls instead

---

## Dependency Flow

```
UI → Application
Infrastructure → Application
```

Infrastructure is registered at startup via dependency injection. No layer should reference Infrastructure directly except the UI layer's DI setup.

---

## Future / Planned

> _Placeholder: Update when tech stack decisions are finalized._

- Event-driven patterns for cross-feature communication
