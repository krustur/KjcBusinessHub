# Domain Model

This document defines the core domain entities, value objects, and validation rules for KjcBusinessHub.

---

## Entities

### Transaction
Represents a single entry on a bank statement.

| Property       | Type        | Description                                 |
|----------------|-------------|---------------------------------------------|
| Id             | Guid        | Unique identifier                           |
| Date           | DateOnly    | Date the transaction occurred               |
| Amount         | decimal     | Positive = credit, Negative = debit         |
| Description    | string      | Bank-provided transaction description       |
| Reference      | string?     | Bank reference number (optional)            |
| Status         | enum        | `Unreconciled`, `PartiallyReconciled`, `Reconciled` |
| SourceDocuments| List\<SourceDocument\> | Linked source documents          |

**Validation Rules:**
- `Amount` must not be zero.
- `Date` must not be in the future.
- `Description` is required and must not exceed 500 characters.

---

### SourceDocument
Represents a receipt, invoice, or other document that justifies a transaction.

| Property    | Type     | Description                                    |
|-------------|----------|------------------------------------------------|
| Id          | Guid     | Unique identifier                              |
| Date        | DateOnly | Date on the document                           |
| Amount      | decimal  | Amount stated on the document                  |
| DocumentType| enum     | `Receipt`, `Invoice`, `MileageLog`, `WellnessReceipt`, `Other` |
| Description | string   | Short description                              |
| FilePath    | string?  | Optional path to uploaded document image/PDF  |

**Validation Rules:**
- `Amount` must be greater than zero.
- `DocumentType` must be a valid enum value.
- `Date` is required.

---

### ExpenseCategory
Categorizes expenses for reporting purposes.

| Property | Type   | Description         |
|----------|--------|---------------------|
| Id       | Guid   | Unique identifier   |
| Name     | string | Category name       |
| Code     | string | Short code (e.g. `TRAVEL`, `WELLNESS`) |

**Validation Rules:**
- `Name` must be unique and not exceed 100 characters.
- `Code` must be uppercase alphanumeric, max 20 characters.

---

### MileageExpense
A specific expense type for mileage/travel allowances.

| Property      | Type     | Description                      |
|---------------|----------|----------------------------------|
| Id            | Guid     | Unique identifier                |
| Date          | DateOnly | Date of travel                   |
| FromLocation  | string   | Starting location                |
| ToLocation    | string   | Destination                      |
| Kilometers    | decimal  | Distance in km                   |
| RatePerKm     | decimal  | Allowance rate per km            |
| CalculatedAmount | decimal | `Kilometers * RatePerKm`      |
| TransactionId | Guid?    | Linked bank transaction (optional) |

**Validation Rules:**
- `Kilometers` must be greater than zero.
- `RatePerKm` must be greater than zero.
- `CalculatedAmount` is always derived — it must not be set directly.

---

### Skill
Represents a professional skill or completed course for CV generation.

| Property        | Type     | Description                         |
|-----------------|----------|-------------------------------------|
| Id              | Guid     | Unique identifier                   |
| Name            | string   | Skill or course name                |
| Category        | string   | E.g. `Technical`, `Management`      |
| CompletedOn     | DateOnly | Date skill was acquired/course done |
| CertificateUrl  | string?  | Optional link to certificate        |

---

## Enumerations

### TransactionStatus
```
Unreconciled         // No source documents linked
PartiallyReconciled  // Some but not all amount is covered
Reconciled           // Fully covered by source documents
```

### DocumentType
```
Receipt
Invoice
MileageLog
WellnessReceipt
Other
```

---

## Business Rules

1. **Reconciliation completeness:** A `Transaction` is `Reconciled` when the sum of all linked `SourceDocument.Amount` values equals `Transaction.Amount`.
2. **Wellness allowance cap:** The total of all `WellnessReceipt` expenses in a calendar year must not exceed the configured annual limit (stored in application settings).
3. **Mileage rate:** The `RatePerKm` for `MileageExpense` defaults to the current government-approved rate but can be overridden per entry.

---

## Future / Planned

> _Placeholder: Additional entities and rules will be added here._

- `Invoice` entity (outgoing invoices to clients)
- `Client` entity
- Multi-currency support with exchange rate tracking
