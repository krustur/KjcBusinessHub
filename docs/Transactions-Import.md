# Transactions import

This document defines the manual Transaction import flow and pasted-text format.

## Import workflow

- The import is started explicitly by the user from the UI.
- The import UI opens in a separate window for better overview.
- The import UI contains:
  - a text area for pasted input
  - a result area showing parse results
- Every text change triggers a full reparse of the current input.
- Parsed rows are grouped into:
  - **Error rows** — rows that could not be parsed, shown with line number and original row text
  - **New Transactions** — valid rows that do not already exist in the application
  - **Duplicate Transactions** — valid rows that already exist in the application
- Error rows are never imported automatically.
- Duplicate Transactions must each be explicitly marked as either **Keep transaction** or **Reject transaction** before import is enabled.
- Duplicate Transactions marked as **Keep transaction** are imported as additional Transactions even when an exact match already exists.
- Duplicate Transactions marked as **Reject transaction** are excluded from import.
- New Transactions are persisted together with any kept duplicate Transactions when the user explicitly confirms import.
- If any Error rows exist, the user must acknowledge that before import is enabled.

## Logging

- Always include the pasted input line number when logging parse results.
- Unparseable rows should be logged as Errors.
- Imported Transactions should be logged as Information.
- Duplicate Transactions should be logged as Debug.

## Input format spec

### General

- Input source: pasted plain text
- Each Transaction is represented by one row
- Empty lines can be ignored. All-whitespace rows are considered empty.
- Order of Transactions in the pasted text is relevant. The user should be able to preview Transactions in the same order as given in the input.

### Line format

- Each row is a semicolon-separated list of five quoted fields:
  1. `AccountingDate` — `yyyy-MM-dd`
  2. `TransactionDate` — `yyyy-MM-dd`
  3. `TransactionType` — Swedish label from the allowed set below
  4. `Description` — string
  5. `Amount` — decimal with optional negative sign, space as thousands separator, comma as decimal separator, and two decimals

Example:

```text
"2026-08-16";"2026-08-16";"Överföring";"9060.42.850.51";"-82 000,00"
"2026-08-09";"2026-08-08";"Kortköp";"MAXI ICA STORMARKNAD U,OREBRO,SE";"-34,95"
"2026-08-08";"2026-08-07";"Kortköp";"GITHUB, INC.,SAN FRANCISCO,US";"-966,11"
"2026-08-06";"2026-08-06";"Kortköp";"GITHUB, INC.,SAN FRANCISCO,US";"-592,66"
```

### Allowed TransactionType import values

The pasted input uses Swedish labels. They must be mapped to the application's English `TransactionType` enum values.

- `Överföring` → `Transfer`
- `Kortköp` → `CardPurchase`
- `BG-insättning` → `BankgiroDeposit`
- `Autogiro` → `DirectDebit`
- `Betalning` → `Payment`
- `Insättning` → `Deposit`
- `Årsavgift` → `AnnualFee`
- `Skatteåterbäring` → `TaxRefund`
- `Kontantinsättning` → `CashDeposit`

## Matching

When checking whether a parsed Transaction already exists in the application, match on:

- `AccountingDate`
- `TransactionDate`
- `TransactionType`
- `Description`
- `Amount`

`Balance` is not part of the model or import anymore.

## Import result handling

- **New Transactions** are eligible for import.
- **Duplicate Transactions** are shown to the user and require an explicit keep/reject decision with no default selection.
- **Error rows** stay as raw row text with a parse error message and line number.

## Persisted Transaction data

For each imported new Transaction, create:

- `Id` = new guid
- `AccountingDate` = as parsed
- `TransactionDate` = as parsed
- `TransactionType` = mapped enum value
- `Amount` = as parsed
- `Description` = as parsed
- `Status` = `Active`
- `SourceDocuments` = empty
