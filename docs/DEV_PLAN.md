# Development Plan

This document tracks the development progress of KjcBusinessHub.

---

## MVP Priority — Next Features

- [x] Make month-based workflow the default reconciliation mode
  - [x] Set month view as default for Transactions and SourceDocuments
  - [x] Replace "See all / See month" buttons with checkbox `Show all months`
  - [x] Replace `Use separate month for Source Documents` with checkbox `Sync transaction and source document month`
  - [x] Keep quick month navigation accessible (this month, previous month, next month)
- [ ] Add monthly coverage visibility
  - [ ] Show handled-vs-total for Transactions per selected month
  - [ ] Show handled-vs-total for SourceDocuments per selected month
  - [ ] Show clear `Month complete` indicator when both sides are fully handled
- [ ] Add explicit handling for Transactions that do not require a SourceDocument
  - [ ] Add a user action to mark a Transaction as handled without linked document
  - [ ] Ensure handled-without-document Transactions count as covered in monthly completion
- [ ] Add SourceDocument annual classification
  - [ ] Add `AnnualType` enum values: `NotAnnual`, `Annual`, `OldAnnual`
  - [ ] Visually flag `Annual` and `OldAnnual` SourceDocuments in the UI
  - [ ] Always include `Annual` SourceDocuments in `Available Source Documents` regardless of selected month
- [x] Prioritize "See month" split-month filtering before other planned MVP items
  - [x] Add toggle: `Use separate month for Source Documents`
  - [x] When enabled, Source Documents use their own month selector
  - [x] When disabled, Source Documents follow the Transactions month selector
- [x] Support linking one SourceDocument to multiple Transactions
  - [x] Domain and persistence: keep many-to-many links for repeated receipts on one paper
  - [x] UI linking flow: allow adding additional links without replacing existing ones
- [x] Extend SourceDocument with currency display fields
  - [x] Add `Ccy` with allowed values `EUR` and `USD`
  - [x] Add `CcyAmount` as optional amount in selected currency
  - [x] No currency conversion; values are only shown to the user
- [x] Update Set Amount validation rules
  - [x] Allow setting `Amount`, `CcyAmount`, or both
  - [x] Require at least one of `Amount` or `CcyAmount`
  - [x] Require `Ccy` whenever `CcyAmount` is set

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
  - [x] "See month": optional separate month selector for Source Documents

---

## Phase 4 — Linking

- [x] Link a source document to a transaction (drag-and-drop or button)
- [x] Unlink a source document from a transaction

---

## Phase 5 — Source Document Actions

- [x] UC-0301 Open Document: open a source document using the default OS application
- [x] UC-0302 Show in Explorer: open the file explorer and highlight the document in its folder
- [x] UC-0303 Set Amount: allow user to enter an amount for a document; transitions status to `Active`
- [x] UC-0304 Link Source Document: enforce that only `Active` documents can be linked to a transaction

---

## Phase 6 — Source Document Lifecycle

- [ ] Handle `New` status: prompt user to enter Amount for newly discovered documents
- [ ] Handle `RemovedFromDisk` status: show indication, allow user to confirm removal (`Removed`)
- [ ] Handle `Revived` status: restore previously removed document matched by file hash
- [ ] Handle `Changed` status: notify user that a tracked file has changed on disk

---

## Phase 7 — Transaction Lifecycle

- [ ] Handle `RemovedFromFile` status: show indication, allow user to confirm removal (`Removed`)
- [ ] Display deleted / removed transactions separately or with visual indicator

---

## Phase 8 — Quality of Life

- [ ] Easy visual differentiation of incoming (credit) vs outgoing (debit) transactions
- [ ] Filter transactions by incoming / outgoing
- [ ] "Frequently ignored" marker for transactions or documents
- [ ] Monthly to-do list with reminders
- [ ] Hints / links to common invoice sources

---

## Phase 9 — Data Safety & Maintenance

- [ ] Daily backup of the database to a human-readable text file
- [ ] Formal DB schema documentation (docs/DATABASE.md)
- [ ] DB migration strategy documented and tested
- [ ] Integration tests covering import → link → persistence round-trip
