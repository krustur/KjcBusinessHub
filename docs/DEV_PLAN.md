# Development Plan

This document tracks the development progress of KjcBusinessHub.

---

## Next Priority — UX Improvements

### 1. Month Filtering Panel

- [ ] Replace the current mix of "Show all months", "Include neighbouring months", and sync controls with a single compact, labeled **Filter Panel**
  - [ ] Add a **View scope** selector with three options: `Current month` / `Adjacent months` / `All months`
  - [ ] Show **Transactions month** selector (always visible)
  - [ ] Show **Documents month** selector only when sync is disabled
- [ ] Remove the old individual controls once the panel is in place

### 2. Unified Status Badge System

- [ ] Define a single badge component used for all statuses across the app
  - Statuses to cover: `Linked`, `Unlinked`, `Pending`, `Annual`, `Expired annual`, `Handled without document`, `Month complete`
  - [ ] No text inside badges — icon only
  - [ ] Show status label as tooltip on hover
  - [ ] One color scale mapped to all statuses (see UI_GUIDELINES.md)
  - [ ] One icon per status drawn from the chosen icon family
- [ ] Replace all existing ad-hoc badges, colored text, and emoji with the new badge component

### 3. Consistent Icon Set

- [ ] Choose one icon family for the entire application (e.g., Lucide, Heroicons, or the icon set bundled with the component library)
- [ ] Audit all current symbols: arrows, checkmarks, emoji-like characters, and mixed text icons
- [ ] Replace every icon with the chosen family
  - Navigation icons
  - Linking / unlinking icons
  - Status icons
  - File action icons (open, show in folder)
  - Alert / warning icons
- [ ] Keep text labels on important buttons; use icon-only for controls where meaning is unambiguous

### 4. Context Menus per Row

- [ ] **Transaction row** — context menu with:
  - [ ] Mark as handled (no document)
- [ ] **SourceDocument row** — context menu with:
  - [ ] Change amount (only when `Amount` or `CcyAmount` is already set)
  - [ ] Show in folder
  - [ ] Annual submenu: `Not annual` / `Annual` / `Expired annual`
- [ ] **SourceDocument row** — inline (direct) action:
  - [ ] Set amount — rendered inline in the row when the document has neither `Amount` nor `CcyAmount`
- [ ] Remove any toolbar or standalone buttons that have been fully moved to the context menu

### 5. Explicit Linking Panel

- [ ] Add a **Linking Panel / Summary Area** that shows the currently selected transaction and currently selected document side-by-side
- [ ] Display the "Link ↔" action prominently inside this panel
- [ ] Panel is hidden (or collapsed) when nothing is selected on either side
- [ ] Panel makes the "selected transaction + selected document → link" workflow self-evident without instructional text

---

## MVP Priority — Next Features

- [x] Make month-based workflow the default reconciliation mode
  - [x] Set month view as default for Transactions and SourceDocuments
  - [x] Replace "See all / See month" buttons with checkbox `Show all months`
  - [x] Replace `Use separate month for Source Documents` with checkbox `Sync transaction and source document month`
  - [x] Keep quick month navigation accessible (this month, previous month, next month)
- [x] Add monthly coverage visibility
  - [x] Show handled-vs-total for Transactions per selected month
  - [x] Show handled-vs-total for SourceDocuments per selected month
  - [x] Show clear `Month complete` indicator when both sides are fully handled
- [x] Add explicit handling for Transactions that do not require a SourceDocument
  - [x] Add a user action to mark a Transaction as handled without linked document
  - [x] Ensure handled-without-document Transactions count as covered in monthly completion
- [x] Add SourceDocument annual classification
  - [x] Add `AnnualType` enum values: `NotAnnual`, `Annual`, `ExpiredAnnual`
  - [x] Visually flag `Annual` and `ExpiredAnnual` SourceDocuments in the UI
  - [x] Always include `Annual` SourceDocuments in `Available Source Documents` regardless of selected month
- [x] Add Future / Pending flag for SourceDocuments (UC-0306 / UC-0307)
  - [x] Add `IsFutureTransaction` bool on `SourceDocument` (default `false`)
  - [x] Add EF Core migration for `IsFutureTransaction`
  - [x] Mark/unmark action buttons and `Pending` badge in the UI
  - [x] Exclude future-marked documents from monthly SourceDocument coverage totals
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
