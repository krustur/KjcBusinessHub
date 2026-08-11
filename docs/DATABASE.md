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

## Schema Overview

### Transactions

| Column          | Type           | Constraints                |
|-----------------|----------------|----------------------------|
| Id              | UNIQUEIDENTIFIER | PK, NOT NULL             |
| AccountingDate  | DATETIMEOFFSET | NOT NULL                   |
| TransactionDate | DATETIMEOFFSET | NOT NULL                   |
| Amount          | DECIMAL(18,2)  | NOT NULL                   |
| Balance         | DECIMAL(18,2)  | NOT NULL                   |
| Description     | NVARCHAR(500)  | NOT NULL                   |
| Status          | INT            | NOT NULL, default 0        |
| CreatedAt       | DATETIME2      | NOT NULL                   |
| UpdatedAt       | DATETIME2      | NOT NULL                   |

---

### SourceDocuments

| Column           | Type           | Constraints                |
|------------------|----------------|----------------------------|
| Id               | UNIQUEIDENTIFIER | PK, NOT NULL             |
| FileCreationDate | DATETIMEOFFSET | NOT NULL                   |
| Amount           | DECIMAL(18,2)  | NOT NULL                   |
| FileSubPath      | NVARCHAR(1000) | NULL                       |
| CreatedAt        | DATETIMEOFFSET | NOT NULL                   |
| UpdatedAt        | DATETIMEOFFSET | NOT NULL                   |

---

### TransactionSourceDocuments (join table)

| Column             | Type             | Constraints     |
|--------------------|------------------|-----------------|
| TransactionId      | UNIQUEIDENTIFIER | PK, FK          |
| SourceDocumentId   | UNIQUEIDENTIFIER | PK, FK          |

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

```csharp
public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Amount).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Description).HasMaxLength(500).IsRequired();

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
