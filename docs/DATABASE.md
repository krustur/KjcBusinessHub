# Database

This document describes the database schema, migrations strategy, and EF Core setup for KjcBusinessHub.

---

## Technology

- **Database:** SQLite (development)
- **ORM:** Entity Framework Core
- **Migrations:** EF Core code-first migrations

---

## Principles

- Prefer DATETIMEOFFSET

- Data may never be DELETED, use Statuses when deleting entities

## Schema Overview

### Transactions

| Column          | Type             | Constraints                |
|-----------------|------------------|----------------------------|
| Id              | UNIQUEIDENTIFIER | PK, NOT NULL               |
| AccountingDate  | DATE             | NOT NULL                   |
| TransactionDate | DATE             | NOT NULL                   |
| Amount          | DECIMAL(18,2)    | NOT NULL                   |
| Balance         | DECIMAL(18,2)    | NOT NULL                   |
| Description     | NVARCHAR(500)    | NOT NULL                   |
| Status          | INT              | NOT NULL, default 0        |
| CreatedAt       | DATETIMEOFFSET   | NOT NULL                   |
| UpdatedAt       | DATETIMEOFFSET   | NULL                       |

---

### SourceDocuments

| Column               | Type             | Constraints                |
|----------------------|------------------|----------------------------|
| Id                   | UNIQUEIDENTIFIER | PK, NOT NULL               |
| FileSubPath          | NVARCHAR(1000)   | NOT NULL                   |
| FileHash             | TEXT             | NOT NULL                   |
| FileNameDate         | DATE             | NOT NULL                   |
| Description          | NVARCHAR(1000)   | NOT NULL                   |
| Amount               | DECIMAL(18,2)    | NULL                       |
| FileCreatedDate      | DATETIMEOFFSET   | NOT NULL                   |
| FileModifiedDate     | DATETIMEOFFSET   | NOT NULL                   |
| Status               | INT              | NOT NULL, default 0        |
| IsFutureTransaction  | BIT              | NOT NULL, default 0        |
| CreatedAt            | DATETIMEOFFSET   | NOT NULL                   |
| UpdatedAt            | DATETIMEOFFSET   | NULL                       |

---

### TransactionSourceDocuments (join table)

| Column             | Type             | Constraints     |
|--------------------|------------------|-----------------|
| TransactionId      | UNIQUEIDENTIFIER | PK, FK          |
| SourceDocumentId   | UNIQUEIDENTIFIER | PK, FK          |
| CreatedAt          | DATETIMEOFFSET   | NOT NULL        |

---

## EF Core Setup

### DbContext Example

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<SourceDocument> SourceDocuments => Set<SourceDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

### Entity Configuration Example

All entity configuration — including keys, properties, relationships, and computed/unmapped properties — must be done exclusively via `IEntityTypeConfiguration<T>` classes. Do **not** use data annotation attributes (e.g. `[NotMapped]`, `[Column]`) on entity classes; use `builder.Ignore()` and other fluent API calls instead.

```csharp
public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Amount).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Description).HasMaxLength(500).IsRequired();

        // Computed properties that are not persisted must be ignored here,
        // not annotated with [NotMapped] on the entity class.
        builder.Ignore(t => t.IsLinked);

        builder.HasMany(t => t.SourceDocuments)
               .WithMany()
               .UsingEntity("TransactionSourceDocuments");
    }
}
```

---

## Migrations

### Creating a Migration

```bash
dotnet ef migrations add <MigrationName> --project src/KjcBusinessHub.Infrastructure --startup-project src/KjcBusinessHub.Web
```

### Applying Migrations

```bash
dotnet ef database update --project src/KjcBusinessHub.Infrastructure --startup-project src/KjcBusinessHub.Web
```

### Rolling Back

```bash
dotnet ef database update <PreviousMigrationName> --project src/KjcBusinessHub.Infrastructure --startup-project src/KjcBusinessHub.Web
```

---

## Seeding

> _Placeholder: Describe data seeding strategy (e.g., dev seed data, reference data for expense categories)._

---
