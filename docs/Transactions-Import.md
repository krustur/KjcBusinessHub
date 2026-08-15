# Transactions import

This file is generally named the Transcations file in documentation.

## Logging

- Always include line number from file when logging.

- All unparseable rows should be logged as Errors.

- All added transactions should be logged as Information.

- All removed transactions should be logged as Warning.

- All Comments should be logged as Debug

- All parsed rows that are skipped because they already exist in the App should be logged as Debug

## File format spec

### General

- File name: `Consulting-Transactions.txt`

- File location: In the root of  SourceDocumentFolder

- File type: Text file with a single entry per row

- Wikndows (CR LF) line endings

- The last line must not include a line feed

- Empty lines can be ignored. All whitespaces is considered empty.

- Order of transactions in file is relevant. The user should be able to see the Transactions in the same order as given in the file.

### Comments

- '#' begins a comment. This can be positioned anywhere on a line. The '#' and the remaining characters of that line should be ignored.

### Line format

- Trim leading and trailing whitespaces

- Parse the rest of the line until the end of the line or the beginning of a comment. Parse in the following order:

  1. Skip: A line can begin with a pair of square brackets with zero or a single character of any kind inside. Examples: "[]", "[ ]", "[X]", "[-]", "[1]". Skip this part. Skip following whitespaces.
  1. All remaining fields are separated by one or more tab characters (`\t`). The expected field order is:
     - AccountingDate: `yyyy-mm-dd`. Example: "2026-07-31".
     - TransactionDate: `yyyy-mm-dd`. Example: "2026-06-08".
     - Description: string. Trim trailing whitespaces.
     - Amount: decimal with optional negative sign, space as thousands separator, comma as decimal separator, 2 decimals. Examples: "-499,00", "99 897,12".
     - Balance: decimal with optional negative sign, space as thousands separator, comma as decimal separator, 2 decimals. Examples: "-226,00", "45 050,18".

## Matching

When checking for a match include all fields that have been parsed: AccountingDate, TransactionDate, Description, Amount, Balance

## First time scan

Parse the Transactions file and add all Transactions to the App:
- Id              = new guid
- AccountingDate  = as parsed
- TransactionDate = as parsed
- Amount          = as parsed
- Balance         = as parsed
- Description     = as parsed
- Status          = `Active`
- SourceDocuments = empty

## Second time scan and notifications from _UC-0103 Transaction file watcher_

Parse the Transactions file and for every Transaction in file:

1. If an exact matching Transaction already exists in the App: 
   1. If the Transaction was `RemovedFromFile` or `Removed`, update the Transaction:
      - Status = `Active`. 
   1. Else ignore this Transaction

2. If no exact matching Transaction already exists in the App: Add a new Transaction, see __First time scan__ above.

For every Transaction in the App that was not present in the Transaction file:

1. The Transaction has been deleted from the File, update the Transaction:
- Status = `RemovedFromFile`


