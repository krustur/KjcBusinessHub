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

### CalendarYear
Aggregate that owns all tracked off-days for a single calendar year.

| Property | Type                  | Description                         |
|----------|-----------------------|-------------------------------------|
| Year     | int                   | The calendar year this aggregate represents |
| OffDays  | IReadOnlyList\<OffDay\> | All off-day entries for this year  |

**Validation Rules:**
- Each date may appear at most once across all `OffDay` entries.
- The `Date` of every `OffDay` must belong to the year represented by the aggregate.
- Each `OffDay` must be marked as a public holiday, an absence day, or both.

---

### OffDay
A single day marked as unavailable for billing purposes.

| Property                 | Type           | Description                                                   |
|--------------------------|----------------|---------------------------------------------------------------|
| Id                       | Guid           | Unique identifier                                             |
| Year                     | int            | Calendar year (denormalised for query convenience)            |
| Date                     | DateOnly       | The calendar date                                             |
| IsPublicHoliday          | bool           | Whether the date is a public holiday                          |
| PublicHolidayDescription | string         | Public-holiday label (for example "Midsommar")                |
| AbsenceType              | enum           | `None`, `Vacation`, or `SickLeave`                            |
| CreatedAt                | DateTimeOffset | When the record was created                                   |
| UpdatedAt                | DateTimeOffset? | When the record was last modified                            |

---

## Value Objects

### DebitableDaysQuery
Represents a request to calculate debitable workdays over a month range.

| Property            | Type      | Description                                              |
|---------------------|-----------|----------------------------------------------------------|
| StartMonth          | YearMonth | First month of the period (inclusive)                    |
| EndMonth            | YearMonth | Last month of the period (must be ≥ StartMonth)          |
| DeductAbsenceDays   | bool      | Whether user-marked absence days reduce the total        |

### DebitableDaysResult
Result of a debitable-days calculation.

| Property                   | Type                               | Description                                      |
|----------------------------|------------------------------------|--------------------------------------------------|
| TotalDebitableDays         | int                                | Total billable workdays across the full period   |
| PerMonth                   | IReadOnlyList\<MonthDebitableDays\> | One entry per month in the range                |
| YearsWithoutPublicHolidays | IReadOnlyList\<int\>               | Years in the selected range that have no imported public holidays |
| AbsenceDayCount            | int                                | Number of absence days found in the selected range |

### MonthDebitableDays

| Property       | Type      | Description                     |
|----------------|-----------|---------------------------------|
| Month          | YearMonth | The calendar month              |
| DebitableDays  | int       | Number of billable days in this month |

### Bridging Day
A bridging day is a derived weekday between non-working days. It is not stored in the database and is only shown as a calendar visual aid.

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
- Debitable days calculation: iterate every day in the requested period; exclude Saturdays and Sundays; exclude dates marked as `IsPublicHoliday`; exclude dates whose `AbsenceType` is not `None` only when absence deduction is enabled; count remaining days grouped by month.
- Calendar and debitable-days views always operate on a 12-month fiscal-year range anchored by the selected start month.
- Bridging days are derived from imported public holidays within the selected fiscal-year range and shown in the calendar UI without overwriting stored off-days.
- `CalendarYear` validates that each off-day's `Date` belongs to the owned year and that no two off-days share the same date.
- The `DagsmartApiPublicHolidayImporter` sets the public-holiday flag and description for the requested year while preserving any existing vacation flag on the same date.

---
