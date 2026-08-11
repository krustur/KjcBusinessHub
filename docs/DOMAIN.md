# Domain Model

This document defines the core domain entities, value objects, and validation rules for KjcBusinessHub.

---

## Entities

### Transaction
Represents a single entry on a bank statement.

| Property        | Type        | Description                                   |
|-----------------|-------------|-----------------------------------------------|
| Id              | Guid        | Unique identifier                             |
| AccountingDate  | DateOnly    | The date a transaction is recorded            |
| TransactionDate | DateOnly    | Date the transaction occurred                 |
| Amount          | decimal     | Positive = credit, Negative = debit           |
| Balance         | decimal     | Balance after this Transaction occured        |
| Description     | string      | Bank-provided transaction description         |
| Status          | enum        | `Unlinked`, `Linked`, `OsRemoved`, `Removed`  |
| SourceDocuments | List\<SourceDocument\> | Linked source documents            |

**Validation Rules:**
- `Description` is required and must not exceed 500 characters.

---

### SourceDocument
Represents a receipt, invoice, or other document that justifies a transaction.

| Property             | Type     | Description                                    |
|----------------------|----------|------------------------------------------------|
| Id                   | Guid     | Unique identifier                              |
| DocumentCreationDate | DateOnly | Date on the document                           |
| Amount               | decimal  | Amount stated on the document                  |
| FileSubPath          | string?  | Sub path to the document                       |

**Validation Rules:**
- `DocumentCreationDate` is required.
- `FileSubPath` is required.

---

## Enumerations

### TransactionStatus
```
Unlinked        // No source documents linked
Linked          // Fully covered by source documents
OsRemoved       // Removed outside of the app
Removed         // Removed in the App domain
```

---

## Business Rules



---

