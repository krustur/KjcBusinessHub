# UI Guidelines

This document establishes UI/UX standards and component guidelines for KjcBusinessHub. All UI work should follow these conventions for a consistent user experience.

---

## General Principles

- **Clarity over cleverness:** Labels, buttons, and messages should be plain and unambiguous.
- **Progressive disclosure:** Show only what the user needs at each step. Reveal advanced options on demand.
- **Feedback for every action:** Every user action must produce visible feedback (success, error, or loading state).
- **Power-user first:** Keep instructive or explanatory text to an absolute minimum. Assume the user knows the domain.
- **Tight layouts:** Lists and tables should be compact — not spacious SaaS-style, but with enough padding to remain readable. Aim for dense information density with clear row separation.

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

## Icons

- Use **one icon family** throughout the entire application (e.g., Lucide, Heroicons, or whatever is bundled with the chosen component library). Do not mix families.
- Icon usage by area:

| Area            | Guideline                                                        |
|-----------------|------------------------------------------------------------------|
| Navigation      | Icon + text label always                                         |
| Status badges   | Icon only — no text in the badge; tooltip shows label on hover   |
| Linking actions | Icon + text label (linking is a core workflow)                   |
| File actions    | Icon only where meaning is obvious (Open, Show in folder)        |
| Alerts/warnings | Consistent warning / info icons from the same family             |

- Retire all emoji-like characters, raw Unicode arrows, and ad-hoc checkmarks. Every symbol must come from the chosen icon family.

---

## Month Filtering

- Present month-filtering as a small, labeled **Filter Panel** — not as scattered checkboxes or buttons.
- The panel exposes:
  1. **View scope** — single-choice: `Current month` | `Adjacent months` | `All months`
  2. **Transactions month** — always visible month selector
  3. **Documents month** — visible only when sync is disabled (scope is not "All months")
- The sync / independent-month toggle is implicit in whether a Documents month selector appears; do not show it as a separate control.

---

## Status Badges

All statuses must use **one shared badge component** with no text inside the badge. The status label is shown as a **tooltip on hover**.

### Status → Icon & Color mapping

| Status                    | Icon (example)      | Color token      |
|---------------------------|---------------------|------------------|
| Linked                    | link / chain        | Green            |
| Unlinked                  | unlink / broken chain | Red            |
| Pending                   | clock               | Orange           |
| Annual                    | repeat / calendar loop | Blue          |
| Expired annual            | calendar-x          | Gray             |
| Handled without document  | check-circle        | Teal             |
| Month complete            | check-square        | Green (bold)     |

Rules:
- Do not use text, numbers, or emoji inside badges.
- Do not introduce new colors outside the defined color tokens.
- Every badge must have an `aria-label` equal to the status label text for accessibility.

---

## Context Menus

- Each row in a data list exposes a **context menu** (right-click or a `⋮` button at the row end) for low-frequency actions.
- Inline actions (high frequency or stateful) remain directly on the row.
- Transaction row context menu: `Mark as handled (no document)`
- SourceDocument row context menu: `Change amount` (when amount is already set), `Show in folder`, annual sub-menu (`Not annual` / `Annual` / `Expired annual`)
- SourceDocument inline action: `Set amount` — shown inline on the row only when neither `Amount` nor `CcyAmount` is set.

---

## Linking Panel

- When the user has a transaction selected **and** a document selected, display a **Linking Panel** that shows both items side-by-side with a prominent `Link ↔` action.
- The panel is hidden (or collapsed) when neither or only one side is selected.
- No instructional text is needed inside the panel — the layout itself communicates the workflow.

---



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

See the [Status Badges](#status-badges) section above for the canonical status → icon → color mapping. The old table below is superseded.

~~| Status               | Color  |~~
~~|----------------------|--------|~~
~~| Reconciled           | Green  |~~
~~| PartiallyReconciled  | Orange |~~
~~| Unreconciled         | Red    |~~

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
