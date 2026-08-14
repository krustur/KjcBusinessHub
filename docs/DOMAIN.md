# Domain Model

This document defines the core domain entities, value objects, and validation rules for KjcBusinessHub.

---

## Entities

### Transaction
Represents a single entry on a bank statement.

| Property        | Type        | Description                             |
|-----------------|-------------|-----------------------------------------|
| Id              | Guid        | Unique identifier                       |
| AccountingDate  | DateOnly    | The date a transaction is recorded      |
| TransactionDate | DateOnly    | Date the transaction occurred           |
| Amount          | decimal     | Positive = credit, Negative = debit     |
| Balance         | decimal     | Balance after this Transaction occured  |
| Description     | string      | Bank-provided transaction description   |
| Status          | enum        | `Active`, `RemovedFromFile`, `Removed`  |
| SourceDocuments | List\<SourceDocument\> | Linked source documents      |

**Validation Rules:**
- `Description` is required and must not exceed 500 characters.

---

### SourceDocument
Represents a receipt, invoice, or other document that justifies a transaction.

| Property             | Type           | Description                                   |
|----------------------|----------------|-----------------------------------------------|
| Id                   | Guid           | Unique identifier                             |
| FileSubPath          | string         | Sub path to the document                      |
| FileHash             | string         | SHA256 hash of the file content               |
| FileNameDate         | DateOnly       | Date in the file name                         |
| Description          | string         | Description of the document                   |
| Amount               | decimal?       | Amount stated on the document                 |
| Status               | enum           | `New`, `Active`, `RemovedFromDisk`, `Removed` |
| FileCreatedDate      | DateTimeOffset | Creation date from the File metadata          |
| FileModifiedDate     | DateTimeOffset | Modified date from the File metadata          |


**Validation Rules:**
- `DocumentCreationDate` is required.
- `FileSubPath` is required.

---

## Enumerations

### TransactionStatus
```
Active              // Transaction is active
RemovedFromFile     // Removed from the Transactions file
Removed             // Confirmed Removed by the User
```

### SourceDocumentStatus
```
New                 // Newly found SourceDocument missing Amount
Active              // SourceDocument is active and have Amount
Changed             // SourceDocument has changed
RemovedFromDisk     // File has been removed from the SourceDocumentsDirectory
Removed             // Confirmed Removed by the User
```

---

## Business Rules



---

