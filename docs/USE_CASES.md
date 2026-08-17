# Use Cases

This document describes use cases for the KjcBusinessHub application. 

## Application start (00)

### UC-0001 App First time start

**Pre-Conditions:**

- The application is started without a configured SourceDocumentFolder

**Acceptance Criteria:**

- The user enters the App in the Settings screen. 
- The user are not able to navigate to the Main view
- The user are expected to configure a SourceDocumentFolder
- When a SourceDocumentFolder has been configured:
    1. The user should be able to navigate to the Main view
    1. UC-0101 should be activated
    1. UC-0102 should be activated
    1. UC-0103 should be activated
    1. UC-0104 should be activated

### UC-0002 App Subsequent starts

**Pre-Conditions:**

- The application is started with a configured SourceDocumentFolder

**Acceptance Criteria:**

- The user enters the App in the Main view. 
- UC-0101 should be activated
- UC-0102 should be activated
- UC-0103 should be activated
- UC-0104 should be activated

## File watcher (01)

### UC-0101 Transaction file import

**Pre-Conditions:**

- The application must have a configured SourceDocumentFolder

**Acceptance Criteria:**

- The application reads the Consulting Transactions file located in the SourceDocumentFolder and parses it's content according to: [Transactions-Import.md](Transactions-Import.md)

### UC-0102 Source document file import

**Pre-Conditions:**

- The application must have a configured SourceDocumentFolder

**Acceptance Criteria:**

- The application scans the SourceDocumentFolder for Source Documents according to: [SourceDocument-import.md](SourceDocument-import.md)

### UC-0103 Transaction file watcher

**Pre-Conditions:**

- The application must have a configured SourceDocumentFolder
- _UC-0101 Transaction file import_ should be finished

**Acceptance Criteria:**

- The app should watch for changes to the Consulting Transactions file located in the SourceDocumentFolder and when a change occurs it should parse it's content according to:[Transactions-Import.md](Transactions-Import.md)

### UC-0104 Source document file watcher

**Pre-Conditions:**

- The application must have a configured SourceDocumentFolder
- _UC-0102 Source document file import_ should be finished

**Acceptance Criteria:**

- The app should watch for changes in the SourceDocumentFolder and when a change occur it should handle it according to: [SourceDocument-import.md](SourceDocument-import.md)

## Main view (02)

### UC-0201 View transactions 

The user should see a view of all available Transactions top left and all available source documents top right. Linked items should remain visible in those lists, display a visible link indicator, and show the linked count when linked more than once. In both lists, unlinked items should appear before linked items. Linked pairs should be shown below, grouped by Transaction so that each Transaction is listed once with its linked source documents beneath it.

**Acceptance Criteria:**

- Default view mode is month-based (`See month`)
- A checkbox `Show all months` toggles between month-based and all-month views
- A checkbox `Sync transaction and source document month` controls whether both lists use one shared month selector
- When sync is disabled, the SourceDocument month selector keeps the currently shown month and can then diverge from the Transaction month
- When sync is disabled and the SourceDocument month differs from the Transaction month, a `Sync with Transaction` button is enabled to realign the SourceDocument month
- SourceDocuments marked as `Annual` must always be visible in `Available Source Documents`, even in month-based view
- SourceDocuments marked as `Annual` or `OldAnnual` must be visually flagged in the list
- SourceDocuments marked as `OldAnnual` follow normal month filtering unless `Show all months` is enabled
- Only `Annual` has always-visible behavior; `OldAnnual` must not be forced visible in month-based view
- SourceDocuments marked `IsFutureTransaction` must always be visible in `Available Source Documents` regardless of which month is selected, and must be visually flagged with a `Pending` badge
- The user should be able to see monthly coverage for the selected month:
  - Transactions: handled count vs total count
  - SourceDocuments: handled count vs total count (SourceDocuments with `IsFutureTransaction == true` are excluded from these totals)
  - A clear `Month complete` indicator when both counts are fully handled

### UC-0202 Mark transaction as handled without SourceDocument

**Pre-Conditions:**

- At least one Transaction must exist

**Acceptance Criteria:**

- The user should be able to mark a Transaction as handled without linking a SourceDocument
- Marked Transactions should be visibly distinguishable from linked and unhandled Transactions
- Marked Transactions should be included as handled in monthly coverage calculations
- The user should be able to revert the handled-without-document marking





## Source Document Actions (03)

### UC-0301 Open Document

**Pre-Conditions:**

- The application must have a configured SourceDocumentFolder
- At least one SourceDocument must exist

**Acceptance Criteria:**

- The user should be able to trigger "Open Document" for any SourceDocument
- The document should be opened using the default application associated with its file type

### UC-0302 Show in Explorer

**Pre-Conditions:**

- The application must have a configured SourceDocumentFolder
- At least one SourceDocument must exist

**Acceptance Criteria:**

- The user should be able to trigger "Show in Explorer" for any SourceDocument
- The file explorer should open and highlight the selected document in its containing folder

### UC-0303 Set Amount

**Pre-Conditions:**

- The application must have a configured SourceDocumentFolder
- At least one SourceDocument must exist

**Acceptance Criteria:**

- The user should be able to trigger "Set Amount" for any SourceDocument
- The user should be able to set `Amount`, `CcyAmount`, or both for the document
- At least one of `Amount` or `CcyAmount` must be set before the action can be saved
- If `CcyAmount` is set, `Ccy` must also be set
- `Ccy` is limited to `EUR` and `USD`
- When at least one of `Amount` or `CcyAmount` is set and validations pass, the SourceDocument should change its state to `Active`

### UC-0304 Link Source Document to Transaction

**Pre-Conditions:**

- The application must have a configured SourceDocumentFolder
- At least one SourceDocument with state `Active` must exist
- At least one Transaction must exist

**Acceptance Criteria:**

- The user should be able to link an `Active` SourceDocument to one or more Transactions
- Linking a SourceDocument to an additional Transaction should keep existing links intact
- Linked Transactions and linked SourceDocuments should remain visible in their available lists
- Linked items in the available lists should display a visible link indicator, and when linked more than once the number of links should also be shown
- Only SourceDocuments with state `Active` should be allowed to be linked to a Transaction
- SourceDocuments that are not `Active` should not be selectable or available for linking
- If a SourceDocument is marked `Pending` (`IsFutureTransaction = true`), linking it to a Transaction automatically clears the `Pending` mark

### UC-0305 Unlink Source Document from Transaction

**Pre-Conditions:**

- At least one SourceDocument must be linked to a Transaction

**Acceptance Criteria:**

- The user should be able to unlink a SourceDocument from a Transaction
- After unlinking, the SourceDocument and Transaction should no longer be associated

### UC-0306 Mark SourceDocument as Future transaction

**Pre-Conditions:**

- The application must have a configured SourceDocumentFolder
- At least one SourceDocument must exist

**Acceptance Criteria:**

- The user can mark a SourceDocument as `Future` / `Pending` via a `Mark as Pending` action
- The mark is persisted (`IsFutureTransaction = true`)
- The document stays in its invoice-date month but is always visible in `Available Source Documents` regardless of which month is selected
- The document is clearly distinguishable from normal SourceDocuments via a `Pending` badge
- A future-marked document is excluded from SourceDocument monthly coverage totals
- A future-marked document may still be linked to a Transaction normally; doing so automatically clears the `Pending` mark

### UC-0307 Remove Future transaction mark

**Pre-Conditions:**

- At least one SourceDocument is marked `Future` / `Pending`

**Acceptance Criteria:**

- The user can clear the `Future` / `Pending` mark via a `Remove Pending` action
- The mark is removed (`IsFutureTransaction = false`)
- Once cleared, the document participates in monthly coverage again

## Future / Planned




### View transactions 

Transactions should show: dates, account no, texts, amounts

Documents should show: dates and names. 

Default order: Transaction date, then document date, then transaction dates for all mapped documents.

Options: 
- Default to month-based view with quick navigation: this month, previous month, next month
- Checkbox `Show all months` to include all months in one view
- Checkbox `Sync transaction and source document month` to keep both month selectors in lockstep
- When separate month selection is shown for SourceDocuments, keep its current month when leaving sync mode and offer a `Sync with Transaction` action when the two month selections differ
- For month view, include neighboring months as a toggle option
- SourceDocuments marked `Annual` should always be shown in `Available Source Documents`
- SourceDocuments marked `Annual` or `OldAnnual` should have a visual flag

### Backlog / Unsorted

- back-up-db-to-human-readable txt file daily.
- db schema
- db migrations
- deleted transactions
- deleted documents
- "frequently ignored"
- easy to differntiate incoming/outgoing transactions
- also filter for it
- monthly todo-list with reminder
- hints/links to where invoices are normally collected from