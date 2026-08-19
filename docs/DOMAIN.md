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
| TransactionType | enum        | Classified transaction type             |
| Amount          | decimal     | Positive = credit, Negative = debit     |
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
| Ccy                  | enum?          | Currency for `CcyAmount` (`EUR`, `USD`)       |
| CcyAmount            | decimal?       | Amount stated in the selected currency        |
| AnnualType           | enum           | `NotAnnual`, `Annual`, `ExpiredAnnual`        |
| Status               | enum           | `New`, `Active`, `RemovedFromDisk`, `Removed` |
| IsFutureTransaction  | bool           | Marks the document as a future/pending transaction; defaults to `false` |
| Transactions         | List\<Transaction\> | Linked transactions                     |
| FileCreatedDate      | DateTimeOffset | Creation date from the File metadata          |
| FileModifiedDate     | DateTimeOffset | Modified date from the File metadata          |


**Validation Rules:**
- `DocumentCreationDate` is required.
- `FileSubPath` is required.
- If `CcyAmount` is set, `Ccy` is required.
- `Ccy` is limited to `EUR` and `USD`.
- `AnnualType` is required and defaults to `NotAnnual`.
- To transition a SourceDocument to `Active`, at least one of `Amount` or `CcyAmount` must be set.

---

## Enumerations

### TransactionStatus
```
Active              // Transaction is active
RemovedFromFile     // Removed from the Transactions file
Removed             // Confirmed Removed by the User
```

### TransactionType
```
Transfer            // Imported from "Överföring"
CardPurchase        // Imported from "Kortköp"
BankgiroDeposit     // Imported from "BG-insättning"
DirectDebit         // Imported from "Autogiro"
Payment             // Imported from "Betalning"
Deposit             // Imported from "Insättning"
AnnualFee           // Imported from "Årsavgift"
TaxRefund           // Imported from "Skatteåterbäring"
CashDeposit         // Imported from "Kontantinsättning"
```

### SourceDocumentStatus
```
New                 // Newly found SourceDocument missing both Amount and CcyAmount
Active              // SourceDocument is active and has at least Amount or CcyAmount
Changed             // SourceDocument has changed
RemovedFromDisk     // File has been removed from the SourceDocumentsDirectory
Removed             // Confirmed Removed by the User
Revived             // Previously removed SourceDocument matched by file hash and restored
```

### SourceDocumentCurrency
```
EUR
USD
```

### SourceDocumentAnnualType
```
NotAnnual           // Regular month-scoped document
Annual              // Yearly document still valid for current accounting period
ExpiredAnnual       // Yearly document from earlier period, kept for reference
```

---

## Business Rules

- A `SourceDocument` may be linked to multiple `Transaction` entries.
- Transactions are imported manually from pasted text; they are not discovered from a watched file.
- Transaction duplicate detection uses `AccountingDate`, `TransactionDate`, `TransactionType`, `Description`, and `Amount`.
- Currency conversion is out of scope; `Ccy` and `CcyAmount` are informational values shown to the user.
- `Annual` SourceDocuments are always visible in the Available SourceDocuments list, independent of selected month.
- `Annual` and `ExpiredAnnual` SourceDocuments must be visually flagged in the UI.
- `ExpiredAnnual` SourceDocuments follow normal month filtering and are not always visible in month-based view.
- Monthly coverage is complete when all Transactions and SourceDocuments in scope are either linked or explicitly marked as handled.
- Monthly SourceDocument coverage scope includes only month-filtered items; always-visible `Annual` items do not affect the `Month complete` indicator.
- SourceDocuments with `IsFutureTransaction == true` are excluded from monthly SourceDocument coverage totals but remain visible in the list and can still be linked to Transactions.
- `IsFutureTransaction` is a manual flag; it is not inferred automatically from transaction data. Clearing the flag is a manual user action.

---
