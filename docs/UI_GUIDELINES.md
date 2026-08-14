# UI Guidelines

This document establishes UI/UX standards and component guidelines for KjcBusinessHub. All UI work should follow these conventions for a consistent user experience.

---

## General Principles

- **Clarity over cleverness:** Labels, buttons, and messages should be plain and unambiguous.
- **Progressive disclosure:** Show only what the user needs at each step. Reveal advanced options on demand.
- **Feedback for every action:** Every user action must produce visible feedback (success, error, or loading state).

---

## Forms

### Dropdowns / Select Fields
- **All dropdowns must include a search/filter field** when they are expected to contain more than 5 options.
- Use a component that supports type-to-filter (e.g., a combobox or searchable select).
- Placeholder text should describe what to search for, e.g., _"Search expense category..."_.

**Example — Expense Category Dropdown:**
```html
<SearchableSelect
  label="Expense Category"
  placeholder="Search category..."
  options="@expenseCategories"
  @bind-Value="selectedCategory" />
```

### Text Inputs
- Always show a label above the input (never placeholder-only labels).
- Show inline validation errors below the field immediately on blur.
- Required fields are marked with an asterisk (`*`).

### Date Pickers
- Use a consistent date picker component throughout the app.
- Default to today's date where appropriate.
- Display dates in the format `DD.MM.YYYY` (locale-aware).

### Currency / Amount Fields
- Display amounts with two decimal places and the currency symbol.
- Align amount columns right in tables.
- Show a warning indicator when an amount is zero.

---

## Buttons

| Variant  | Use case                                          |
|----------|---------------------------------------------------|
| Primary  | The main action on a page (e.g., Save, Import)    |
| Secondary| Alternative actions (e.g., Cancel, Back)          |
| Danger   | Destructive actions (e.g., Delete) — require confirmation dialog |
| Ghost    | Low-emphasis actions in tables or toolbars        |

- Every form must have a clearly labeled **primary action button**.
- Destructive actions (delete, discard) must be followed by a **confirmation dialog**.

---

## Tables

- All data tables must support **sorting** on at least the date and amount columns.
- Include a **status badge** column for entities that have a status (e.g., `Reconciled`, `Unreconciled`).
- Support **row selection** for bulk actions.
- Show an **empty state** message when there is no data (with a call-to-action if relevant).

**Example — Transaction Table Columns:**

| Date       | Description          | Amount  | Status       | Actions |
|------------|----------------------|---------|--------------|---------|
| 10.08.2026 | Client Payment ABC   | 5000.00 | Reconciled   | View    |
| 09.08.2026 | Office Supplies      | -250.00 | Unreconciled | Match   |

---

## Status Badges

| Status               | Color  |
|----------------------|--------|
| Reconciled           | Green  |
| PartiallyReconciled  | Orange |
| Unreconciled         | Red    |

---

## Navigation

- Main navigation should be a sidebar (desktop) or bottom navigation bar (mobile).
- Active section is highlighted.
- Breadcrumbs are shown on detail pages.

---

## Notifications & Toasts

- **Success:** Green toast, auto-dismiss after 4 seconds.
- **Error:** Red toast, stays until dismissed, includes a short error description.
- **Warning:** Orange toast, auto-dismiss after 6 seconds.
- **Info:** Blue toast, auto-dismiss after 4 seconds.

---

## Accessibility

- All interactive elements must be keyboard-navigable.
- Images and icons must have `alt` text or `aria-label`.
- Color alone must not be the only indicator of status — pair color with text or icon.

---

## Future / Planned

> _Placeholder: Add component library choice (e.g., MudBlazor, Radzen, Fluent UI) once decided._

- Dark mode support
- Mobile-responsive breakpoints specification
- Loading skeleton screens for async data
