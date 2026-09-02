# Development Plan

This document tracks the development progress of KjcBusinessHub.

---

## Completed Priority — Year Calendar & Debitable Days (merged from `V_NEXT_DEV_PLAN.md`)

### 1. Domain + persistence for `OffDay` / `CalendarYear`

- [x] Add `OffDayType` enum for `PublicHoliday` and `Vacation`
- [x] Extend `OffDayType` with `BridgingDay` support
- [x] Add `OffDay` entity and `CalendarYear` aggregate with validation rules
- [x] Add `IOffDayRepository` + `OffDayRepository`
- [x] Add `OffDayConfiguration`, `OffDays` `DbSet`, DI registration, and EF migration

### 2. Dagsmart API importer

- [x] Add `ISwedishPublicHolidayImporter` and `PublicHolidayImportResult`
- [x] Add `DagsmartApiPublicHolidayImporter` using `HttpClient`
- [x] Register importer via `AddHttpClient` in DI

### 3. Calendar UI

- [x] Add `CalendarViewModel` with year navigation, off-day loading, import action, and day toggle
- [x] Add `CalendarView.axaml` with 12 mini-calendars and color-coded day cells
- [x] Add "Import red days for {year}" action
- [x] Add **Calendar** entry in main navigation
- [x] ~~Add explicit UI flow to create/edit custom off-day descriptions (currently only shown as tooltip text)~~ — removed from scope (simple vacation toggling is enough)

### 4. Debitable days calculation

- [x] Add `YearMonth`, `DebitableDaysQuery`, and `DebitableDaysResult`
- [x] Add `DebitableDaysCalculator`
- [x] Support optional deduction of vacation days and bridging days
- [x] Unit tests cover multi-year periods, all-off periods, and single-month periods

### 5. Debitable Days panel (UI)

- [x] Add embedded `DebitableDaysViewModel` with reactive recalculation on off-day changes
- [x] Add panel in Calendar view with fiscal-year start month selector, total, and per-month table
- [x] Show warning when one or more years in the range have no imported public holidays

### 6. Tests and docs sync

- [x] Unit tests for `CalendarYear` / `OffDay` domain rules
- [x] Repository integration tests for `OffDay` CRUD/upsert
- [x] ViewModel/UI tests for year navigation, day toggle, and import status
- [x] ViewModel tests for Debitable Days recalculation and warnings
- [ ] Add dedicated importer tests for `DagsmartApiPublicHolidayImporter`
- [ ] Align `DOMAIN.md` and `USE_CASES.md` with current implementation details (bridging-day support, fiscal-year panel behavior)
- [x] `DATABASE.md` includes `OffDays` table schema

---

## Next Priority — Transaction Import Redesign

### 1. Transaction model and persistence

- [x] Remove `Balance` from the `Transaction` entity, EF configuration, repositories, migrations, and any dependent tests
- [x] Add `TransactionType` to the domain model, persistence model, and database schema
- [x] Define the English `TransactionType` enum values and map them from the Swedish import labels
- [x] Update transaction duplicate matching to use `AccountingDate`, `TransactionDate`, `TransactionType`, `Description`, and `Amount`

### 2. Replace file-based transaction import

- [x] Remove the transaction file import from application startup
- [x] Remove the transaction file watcher while keeping source document import and source document watching intact
- [x] Replace file-based transaction import services/contracts with an explicit pasted-text import use case
- [x] Introduce import result models for `Error rows`, `New Transactions`, and `Duplicate Transactions`

### 3. Build the transaction import UI flow

- [x] Add a user-visible action to open Transaction import from the main UI
- [x] Add a split import surface with pasted input in the top area and structured parse results in the bottom area
- [x] Reparse input whenever the text changes
- [x] Highlight parse errors and require explicit acknowledgement before enabling import
- [x] Allow importing all `New Transactions` in one explicit confirmation step

### 4. Parsing, validation, and import behavior

- [x] Implement parsing for the new quoted semicolon-separated format: `AccountingDate`, `TransactionDate`, `TransactionType`, `Description`, `Amount`
- [x] Support Swedish-formatted decimal amounts and Swedish `TransactionType` labels
- [x] Preserve input order in the preview and import results
- [x] Ensure duplicate rows are shown to the user and require explicit keep/reject decisions before optional import
- [x] Ensure invalid rows remain visible as raw error items with line numbers

### 5. Tests and documentation

- [x] Replace existing transaction import tests with coverage for the new parser, duplicate detection, and manual import flow
- [x] Update view-model/UI tests for the new import action, preview state, acknowledgement rule, and import confirmation
- [x] Update repository tests affected by the schema change from `Balance` to `TransactionType`
- [x] Update requirement documents (`USE_CASES.md`, `Transactions-Import.md`, `DOMAIN.md`, `DATABASE.md`) to reflect the redesign

---

## Next Priority — UX Improvements

### 1. Month Filtering Panel

- [x] Replace the current mix of "Show all months", "Include neighbouring months", and sync controls with a single compact, labeled **Filter Panel**
  - [x] Add a **View scope** selector with three options: `Current month` / `Adjacent months` / `All months`
  - [x] Show **Transactions month** selector (always visible)
  - [x] Show **Documents month** selector only when sync is disabled
- [x] Remove the old individual controls once the panel is in place

### 2. Unified Status Badge System

- [x] Define a single badge component used for all statuses across the app
  - Statuses to cover: `Linked`, `Unlinked`, `Pending`, `Annual`, `Expired annual`, `Handled without document`, `Month complete`
  - [x] No text inside badges — icon only
  - [x] Show status label as tooltip on hover
  - [x] One color scale mapped to all statuses (see UI_GUIDELINES.md)
  - [x] One icon per status drawn from the chosen icon family
- [x] Replace all existing ad-hoc badges, colored text, and emoji with the new badge component

### 3. Consistent Icon Set

- [x] Choose one icon family for the entire application (Material Icons via Material.Icons.Avalonia)
- [x] Audit all current symbols: arrows, checkmarks, emoji-like characters, and mixed text icons
- [x] Replace every icon with the chosen family
  - [x] Navigation icons
  - [x] Linking / unlinking icons
  - [x] Status icons
  - [x] File action icons (open, show in folder)
  - [x] Alert / warning icons
- [x] Keep text labels on important buttons; use icon-only for controls where meaning is unambiguous

### 4. Context Menus per Row

- [x] **Transaction row** — context menu with:
  - [x] Mark as handled (no document)
- [x] **SourceDocument row** — context menu with:
  - [x] Change amount (only when `Amount` or `CcyAmount` is already set)
  - [x] Show in folder
  - [x] Annual submenu: `Not annual` / `Annual` / `Expired annual`
- [x] **SourceDocument row** — inline (direct) action:
  - [x] Set amount — rendered inline in the row when the document has neither `Amount` nor `CcyAmount`
- [x] Remove any toolbar or standalone buttons that have been fully moved to the context menu

### 5. Explicit Linking Panel

- [x] Add a **Linking Panel / Summary Area** that shows the currently selected transaction and currently selected document side-by-side
- [x] Display the "Link ↔" action prominently inside this panel
- [x] Panel is hidden (or collapsed) when nothing is selected on either side
- [x] Panel makes the "selected transaction + selected document → link" workflow self-evident without instructional text

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

- [x] UC-0102 Source document file import (scan SourceDocumentFolder)
- [x] UC-0104 Source document file watcher (re-scan on folder change)

---

## Phase 2 — App Startup & Navigation

- [x] UC-0001 App first-time start → open Settings screen, block navigation until folder is configured
- [x] UC-0002 App subsequent starts → open Main view, trigger source document import & watcher
- [x] Settings screen: enter and validate SourceDocumentFolder
- [x] Navigate from Settings to Main view once configured

---

## Phase 3 — Main View (Transactions & Documents)

- [x] UC-0201 Show unlinked transactions (top-left) and unlinked source documents (top-right)
- [x] UC-0201 Show linked transaction–document pairs below, side-by-side
- [x] Display transaction details: accounting date, transaction date, transaction type, description, amount
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

## Phase 8 — Quality of Life

- [ ] Easy visual differentiation of incoming (credit) vs outgoing (debit) transactions
- [ ] Filter transactions by incoming / outgoing
- [ ] "Frequently ignored" marker for transactions or documents
- [ ] Monthly to-do list with reminders
- [ ] Hints / links to common invoice sources

---

## Phase 9 — Data Safety & Maintenance

- [ ] Daily backup of the database to a human-readable text file
- [x] Formal DB schema documentation (docs/DATABASE.md)
- [ ] DB migration strategy documented and tested
- [ ] Integration tests covering import → link → persistence round-trip
