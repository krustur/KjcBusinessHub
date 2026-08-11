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

- The application reads the file `Consulting-Transactions.txt` located in the SourceDocumentFolder and parses it's content and adds new Transactions to the Application, see [Transactions-Import.md](Transactions-Import.md)

### UC-0102 Source document file import

**Pre-Conditions:**

- The application scans the SourceDocumentFolder for Source Documents and adds new SourceDocuments to the Application, see [SourceDocument-import.md](SourceDocument-import.md)

### UC-0103 Transaction file watcher

**Acceptance Criteria:**

- The app should keep track of files and add deletes and renames of these while the session is active.

### UC-0104 Source document file watcher

**Acceptance Criteria:**

## Main view







## Future / Planned


### View transactions 
The user should see a view of all transactions to the left and all source documents to the right. When a transaction has been mapped to a source document these should be shown side by side and with a visible link. Unmapped transactions should be listed on top in the left pane. Unmapped documents should be listed top of the right pane. 

Transactions should show: dates, account no, texts, amounts

Documents should show: dates and names. 

Default order: Transaction date, then document date, then transaction dates for all mapped documents.

Options: 
- See all, see month
- For see month, include neighboring months as a toggle option
- For see month, these navigations should be easily accessible: this month, next month and previous month

### Link source document to transaction
### Unlink source document from transaction

unsorted
=======
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