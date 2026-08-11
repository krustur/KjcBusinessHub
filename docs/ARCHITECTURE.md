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
│   ├── KjcBusinessHub.Application/     # Entities, value objects, business rules, interfaces, use cases, commands, queries (CQRS), DTOs
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
- Orchestrates use cases using **commands and queries** (CQRS via MediatR).
- Contains **DTOs**, **validators** (FluentValidation), and **service interfaces**.
- Has **zero dependencies** on Infrastructure.
- All business rules live here (see `DOMAIN.md`).

**Example — Import Transactions Command:**
```csharp
public record ImportTransactionsCommand(Stream FileStream, string FileName) : IRequest<ImportResult>;

public class ImportTransactionsHandler : IRequestHandler<ImportTransactionsCommand, ImportResult>
{
    // Parses file, deduplicates, persists via ITransactionRepository
}
```

### Infrastructure Layer (`KjcBusinessHub.Infrastructure`)
- Implements interfaces defined in Application.
- Contains **EF Core DbContext**, **repository implementations**, file storage, and third-party integrations.
- References Application.

### UI Layer (`KjcBusinessHub.UI`)
- Hosts the Avalonia UI views, view models, and components.
- References Application only — never Infrastructure directly.

---

## Key Patterns

| Pattern           | Where Used                                  |
|-------------------|---------------------------------------------|
| CQRS (MediatR)    | Application layer — commands and queries    |
| Repository        | Infrastructure — data access abstraction    |
| Clean Architecture| Layer dependency rules (domain at center)   |
| FluentValidation  | Application — input validation              |
| Result pattern    | Return `Result<T>` instead of throwing exceptions |

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

- Authentication/authorization approach (e.g., ASP.NET Identity, OIDC)
- Background job processing (e.g., Hangfire for scheduled imports)
- Event-driven patterns for cross-feature communication
