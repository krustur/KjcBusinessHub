# Architecture

This document describes the code structure, layers, and architectural patterns used in KjcBusinessHub.

---

## Overview

KjcBusinessHub is a web application built with **ASP.NET Core** (backend) and **Blazor** or **React** (frontend). It follows a **Clean Architecture** / **Layered Architecture** approach to keep domain logic independent from infrastructure concerns.

---

## Project Structure

```
KjcBusinessHub/
├── src/
│   ├── KjcBusinessHub.Domain/          # Entities, value objects, business rules, interfaces
│   ├── KjcBusinessHub.Application/     # Use cases, commands, queries (CQRS), DTOs
│   ├── KjcBusinessHub.Infrastructure/  # EF Core, file storage, external integrations
│   └── KjcBusinessHub.Web/             # Blazor/MVC UI, controllers, pages, components
├── tests/
│   ├── KjcBusinessHub.Domain.Tests/
│   ├── KjcBusinessHub.Application.Tests/
│   └── KjcBusinessHub.Integration.Tests/
└── docs/
```

---

## Layers

### Domain Layer (`KjcBusinessHub.Domain`)
- Contains all **entities**, **value objects**, **enumerations**, and **domain interfaces**.
- Has **zero dependencies** on other projects or NuGet packages except core .NET libraries.
- All business rules live here (see `DOMAIN.md`).

### Application Layer (`KjcBusinessHub.Application`)
- Orchestrates use cases using **commands and queries** (CQRS via MediatR).
- References Domain only.
- Contains **DTOs**, **validators** (FluentValidation), and **service interfaces**.

**Example — Import Transactions Command:**
```csharp
public record ImportTransactionsCommand(Stream FileStream, string FileName) : IRequest<ImportResult>;

public class ImportTransactionsHandler : IRequestHandler<ImportTransactionsCommand, ImportResult>
{
    // Parses file, deduplicates, persists via ITransactionRepository
}
```

### Infrastructure Layer (`KjcBusinessHub.Infrastructure`)
- Implements interfaces defined in Domain/Application.
- Contains **EF Core DbContext**, **repository implementations**, file storage, and third-party integrations.
- References Application and Domain.

### Web Layer (`KjcBusinessHub.Web`)
- Hosts the UI (Blazor components or MVC views) and API controllers.
- References Application only — never Domain or Infrastructure directly.

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
Web → Application → Domain
Infrastructure → Application → Domain
```

Infrastructure is registered at startup via dependency injection. No layer should reference Infrastructure directly except the Web layer's DI setup.

---

## Future / Planned

> _Placeholder: Update when tech stack decisions are finalized._

- Authentication/authorization approach (e.g., ASP.NET Identity, OIDC)
- Background job processing (e.g., Hangfire for scheduled imports)
- Event-driven patterns for cross-feature communication
