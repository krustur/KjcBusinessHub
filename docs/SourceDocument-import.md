# Source Document import

## Logging

- Always include file path when logging.

- The Transcations file should always be ignored, log as Debug

- Files that are outside the correct path of a Source Document should be ignored, log as Warning.

- Incorrectly named subfolders should be logged as Error

- New Source Documents found should be logged as Information

- Deleted and Moved/Changed documents should be logged as Warning

## Folder structure

Every SourceDocument should be placed inside a subfolder of the SourceDocumentsFolder named "yyyy-mm", example: "2026-07". Subfolders deeper down should be included no matter their name.

The Transcations file are placed in the SourceDocumentsFolder root, this file should be ignored.

Incorrectly named folders should be ignored. Log as Error.

Example folder structure:
```
SourceDocumentsFolder/
├── 2024-12/
│   ├── 2024-12-02 Faktura FLY Pensionsplan.JPG
│   ├── 2024-12-09 Pyttemjuk G096932665_Invoice.pdf
│   └── 2024-12-12 Skatteverket - Betalningssammanstallning
├── 2025-02/
│   ├── Removed/
│   │   └── 2025-02-09 Pyttestor G096932665_Invoice.pdf
│   ├── 2025-02-12 Pyttemjuk G096932665_Invoice.pdf
│   └── 2025-02-23 Lönebesked.pdf
└── Consulting-Transactions.txt
```

## File name parsing

The file name begins with FileNameDate in the format "yyyy-mm-dd" followed by a space and then Description all the way to the file extension.

## File identification

Generate SHA256 hash of the file content for all files. This will be used to identify potentially moves of files while the app is offline.

## Matching by name

When checking for a match use FileSubPath. 

## Matching by hash

As a backup FileHash can be used when a new file is added to 'revive' SourceDocument that have been deleted (`RemovedFromDisk` or `Removed`)

## First time scan

Parse the SourceDocumentsFolder and add all identified SourceDocuments to the App:
- Id               = new guid
- FileSubPath      = sub path of the file relative to SourceDocumentsFolder
- FileHash         = calculate SHA256 hash from file content
- FileNameDate     = as parsed
- Description      = as parsed
- Amount           = null
- Ccy              = null
- CcyAmount        = null
- Status           = `New`
- FileCreatedDate  = from file metadata
- FileModifiedDate = from file metadata

## Second time scan

Parse the SourceDocumentsFolder and for every file that match by name an existing SourceDocument in the App:
1. note: As we get a match we know that FileNameDate and Description has not changed.
1. Match the files hash with the existing SourceDocument hash:
   - Match: Nothing has changed. If file Created- or Modified metadata has changed then update these
   - Not match: The file has changed, update:
      - FileHash     = new hash
      - Status       = `Changed`
      - If file Created- or Modified metadata has changed then update these

For every file that doesn't match by name any existing SourceDocument in the App, add SourceDocuments, see __First time scan__ above.

For every SourceDocument in the App that was not present during the scan:

1. The SourceDocument has been deleted from the disk or moved to outside of the SourceDocumentsFolder, update the SourceDocument:
- Status = `RemovedFromDisk`

