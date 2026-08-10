# Use Cases

This document describes user stories and use cases for the KjcBusinessHub application. Use cases are organized by feature area and follow the format: _As a [user], I want to [goal] so that [reason]._

---

## Bank Reconciliation

### UC-001: Import Bank Transactions
**As a** freelancer,  
**I want to** import my bank transactions from a CSV or OFX file,  
**so that** I can reconcile them against my source documents without manual data entry.

**Acceptance Criteria:**
- User can upload a CSV or OFX file exported from their bank.
- The system parses and stores each transaction (date, amount, description, reference).
- Duplicate transactions are detected and flagged.

---

### UC-002: Match Transaction to Source Document
**As a** freelancer,  
**I want to** link a bank transaction to a receipt or invoice,  
**so that** I can prove every transaction is valid and covered by a source document.

**Acceptance Criteria:**
- User can search for an existing source document by amount, date, or description.
- A transaction can be linked to one or more source documents.
- Matched transactions are marked as reconciled.

---

### UC-003: View Unreconciled Transactions
**As a** freelancer,  
**I want to** see a list of transactions that have not yet been matched to a source document,  
**so that** I know which transactions still need attention.

**Acceptance Criteria:**
- Dashboard shows count and list of unreconciled transactions.
- User can filter by date range and amount.

---

## Expense Tracking

### UC-010: Register Travel Expense
**As a** freelancer,  
**I want to** register a mileage allowance expense,  
**so that** I can claim it as a business expense and attach it to the correct bank transaction.

**Acceptance Criteria:**
- User can enter trip details (date, from, to, kilometers, rate per km).
- The system calculates the allowance amount.
- The expense can be linked to a bank transaction.

---

### UC-011: Register Wellness Allowance
**As a** freelancer,  
**I want to** register a wellness allowance expense with a receipt,  
**so that** I can track wellness spending within the annual limit.

**Acceptance Criteria:**
- User can upload a receipt image and enter the amount.
- The system warns if the annual wellness allowance limit is approached or exceeded.

---

## Skills & Professional Development

### UC-020: Add Skill or Course
**As a** freelancer,  
**I want to** record a completed course or acquired skill,  
**so that** I can track my professional development and use the data to generate my CV.

**Acceptance Criteria:**
- User can enter skill name, category, completion date, and optional certificate.
- Skills are listed under a professional profile section.

---

## Future / Planned

> _Placeholder: Additional use cases will be added here as new features are planned._

- UC-030: Generate reconciliation report (PDF/Excel export)
- UC-031: Multi-currency transaction support
- UC-040: Generate CV from skills and experience data
