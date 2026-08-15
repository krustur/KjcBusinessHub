# Development Plan

This document tracks the development progress of KjcBusinessHub.

---

## Phase 0 — Foundation

- [x] Define domain model (Transaction, SourceDocument, enumerations)
- [x] Define use cases (USE_CASES.md)
- [x] Set up Clean Architecture project structure (Application / Infrastructure / UI)
- [x] Configure EF Core with SQLite and initial migration
- [x] Implement ITransactionRepository and ISourceDocumentRepository interfaces
- [x] Implement SettingsService (persist SourceDocumentFolder)

---

## Phase 1 — File Import & File Watching

- [x] UC-0101 Transaction file import (parse Consulting Transactions CSV)
- [x] UC-0102 Source document file import (scan SourceDocumentFolder)
- [x] UC-0103 Transaction file watcher (re-import on file change)
- [x] UC-0104 Source document file watcher (re-scan on folder change)

---

## Phase 2 — App Startup & Navigation

- [x] UC-0001 App first-time start → open Settings screen, block navigation until folder is configured
- [x] UC-0002 App subsequent starts → open Main view, trigger file import & watchers
- [x] Settings screen: enter and validate SourceDocumentFolder
- [x] Navigate from Settings to Main view once configured

---

## Phase 3 — Main View (Transactions & Documents)

- [x] UC-0201 Show unlinked transactions (top-left) and unlinked source documents (top-right)
- [x] UC-0201 Show linked transaction–document pairs below, side-by-side
- [x] Display transaction details: accounting date, transaction date, account no, description, amount
- [x] Display source document details: file name date, description, amount
- [x] Default sort order: transaction date → document date → mapped-document dates
- [x] Filter: "See all" vs "See month" toggle
  - [x] "See month": include neighbouring months toggle
  - [x] "See month": quick-navigation buttons (this month, previous month, next month)

---

## Phase 4 — Linking

- [x] Link a source document to a transaction (drag-and-drop or button)
- [x] Unlink a source document from a transaction

---

## Phase 5 — Source Document Lifecycle

- [ ] Handle `New` status: prompt user to enter Amount for newly discovered documents
- [ ] Handle `RemovedFromDisk` status: show indication, allow user to confirm removal (`Removed`)
- [ ] Handle `Revived` status: restore previously removed document matched by file hash
- [ ] Handle `Changed` status: notify user that a tracked file has changed on disk

---

## Phase 6 — Transaction Lifecycle

- [ ] Handle `RemovedFromFile` status: show indication, allow user to confirm removal (`Removed`)
- [ ] Display deleted / removed transactions separately or with visual indicator

---

## Phase 7 — Quality of Life

- [ ] Easy visual differentiation of incoming (credit) vs outgoing (debit) transactions
- [ ] Filter transactions by incoming / outgoing
- [ ] "Frequently ignored" marker for transactions or documents
- [ ] Monthly to-do list with reminders
- [ ] Hints / links to common invoice sources

---

## Phase 8 — Data Safety & Maintenance

- [ ] Daily backup of the database to a human-readable text file
- [ ] Formal DB schema documentation (docs/DATABASE.md)
- [ ] DB migration strategy documented and tested
- [ ] Integration tests covering import → link → persistence round-trip
