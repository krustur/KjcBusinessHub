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

The user should see a view of all Unlinked Transactions top left and all Unlinked source documents top right. When a transaction has been mapped to a source document these should be shown below side by side and with a visible link.





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
- The user should be able to enter an amount value for the document
- When an amount is set, the SourceDocument should change its state to `Active`

### UC-0304 Link Source Document to Transaction

**Pre-Conditions:**

- The application must have a configured SourceDocumentFolder
- At least one SourceDocument with state `Active` must exist
- At least one Transaction must exist

**Acceptance Criteria:**

- The user should be able to link an `Active` SourceDocument to a Transaction
- Only SourceDocuments with state `Active` should be allowed to be linked to a Transaction
- SourceDocuments that are not `Active` should not be selectable or available for linking

### UC-0305 Unlink Source Document from Transaction

**Pre-Conditions:**

- At least one SourceDocument must be linked to a Transaction

**Acceptance Criteria:**

- The user should be able to unlink a SourceDocument from a Transaction
- After unlinking, the SourceDocument and Transaction should no longer be associated

## Future / Planned




### View transactions 

Transactions should show: dates, account no, texts, amounts

Documents should show: dates and names. 

Default order: Transaction date, then document date, then transaction dates for all mapped documents.

Options: 
- See all, see month
- For see month, include neighboring months as a toggle option
- For see month, these navigations should be easily accessible: this month, next month and previous month

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