# V-Next Development Plan

This document outlines the next major feature set planned for KjcBusinessHub: a **Year Calendar** with off-day management and a **Debitable Days** view for workday estimation.

---

## Feature 1 — Year Calendar

A full-year calendar view that lets the user track Swedish public holidays (red days) and their own planned vacation days for any given year.

### Domain additions

- Introduce a `CalendarYear` aggregate that owns a collection of `OffDay` entries for a given year.
- `OffDay` properties:
  - `Date` (`DateOnly`) — the calendar date.
  - `OffDayType` enum — `PublicHoliday` (Swedish red day) or `Vacation`.
  - `Description` (`string`) — optional label (e.g. "Midsommar", "Summer vacation week 1").
- Validation rules:
  - Each date within a year may appear at most once in the `OffDay` list; a day cannot be both a public holiday and a vacation simultaneously.
  - `Date` must belong to the year represented by the owning `CalendarYear`.

### Persistence

- Add an `OffDays` table (`Id`, `Year`, `Date`, `OffDayType`, `Description`).
- Add `IOffDayRepository` interface with methods to load all off days for a given year, add/update, and delete individual off days.
- Add EF Core migration for the new table.

### Swedish public holiday import (bonus)

- Use the free, public **[Dagar API](https://api.dagar.se)** (`https://api.dagar.se/v1/{year}`) which returns Swedish public holidays (röda dagar) as JSON — no authentication required.
- Introduce an `ISwedishPublicHolidayImporter` interface and a concrete `DagarApiPublicHolidayImporter` implementation in the Infrastructure layer.
- The importer fetches holidays for the requested year and upserts them as `PublicHoliday` off days into the repository; existing manually added `Vacation` off days are left untouched.
- Wrap the HTTP call in a try/catch; surface a user-visible warning if the import fails (network unavailable, etc.).
- Cache the imported holidays in the local database so the app works offline after the first successful import.

### Application use cases

- **UC-CAL-01 View year calendar** — open the calendar view for a selected year; display all 12 months in a grid; color-code cells: public holidays, vacation days, weekends, regular workdays.
- **UC-CAL-02 Navigate years** — previous-year / next-year navigation buttons; default to the current year on first open.
- **UC-CAL-03 Add vacation day** — click a date cell to toggle it as a vacation day; saves immediately.
- **UC-CAL-04 Remove vacation day** — click an existing vacation day to remove it.
- **UC-CAL-05 Import Swedish public holidays** — an explicit "Import red days" action for the current year; calls the Dagar API and persists results; shows a confirmation of how many days were added or updated.
- **UC-CAL-06 Add/edit off day description** — optional description field accessible from a day detail panel or tooltip.

### UI

- Add a **Calendar** entry to the main navigation.
- Year view: 12 monthly mini-calendars arranged in a 4×3 or 3×4 grid.
- Cell color legend:
  - Red — public holiday (red day).
  - Yellow / amber — vacation.
  - Light grey — weekend.
  - Default — regular workday.
- Clicking a day opens a small popover/panel to add or remove it as a vacation day and optionally set a description.
- "Import red days for {year}" button at the top of the view.
- Consistent with existing badge/icon system defined in `UI_GUIDELINES.md`.

### Tests

- Unit tests for `CalendarYear` / `OffDay` domain rules (duplicate date, year mismatch).
- Unit tests for `DagarApiPublicHolidayImporter` using a stubbed HTTP client.
- Repository tests for CRUD operations on `OffDay`.
- ViewModel / UI tests for year navigation, day toggle, and import confirmation state.

---

## Feature 2 — Debitable Days View

Given a start and end month (inclusive), calculate the number of workdays the user can potentially bill — excluding weekends, public holidays, and vacation days stored in the calendar.

This feature is built on top of Feature 1 and can be integrated as a panel within the Calendar view or as a standalone view tab.

### Domain additions

- Introduce a `DebitableDaysQuery` value object:
  - `StartMonth` (`YearMonth`) — first month of the period.
  - `EndMonth` (`YearMonth`) — last month of the period (must be ≥ `StartMonth`).
- Introduce a `DebitableDaysResult` value object:
  - `TotalDebitableDays` (`int`) — total workdays across the full period.
  - `PerMonth` (`IReadOnlyList<MonthDebitableDays>`) — ordered list with one entry per month.
- `MonthDebitableDays`:
  - `Month` (`YearMonth`).
  - `DebitableDays` (`int`).
- Calculation rules:
  - Iterate every calendar day in the period.
  - Exclude Saturdays and Sundays.
  - Exclude dates recorded as `PublicHoliday` in the off-day store.
  - Exclude dates recorded as `Vacation` in the off-day store.
  - Count the remaining days, grouped by month.

### Application use cases

- **UC-DEB-01 Calculate debitable days** — user selects a start month and end month; the application returns a `DebitableDaysResult` derived from the off-day data for all affected years.
- **UC-DEB-02 View per-month breakdown** — the result lists each month in the period with its debitable-day count.
- **UC-DEB-03 Recalculate on off-day change** — whenever the user adds or removes an off day in the Calendar view, any currently displayed Debitable Days result is automatically refreshed.

### UI

- Add a collapsible **Debitable Days** panel, either:
  - As a sidebar/footer panel within the Calendar view (preferred — keeps related data co-located), or
  - As a separate tab in the main navigation.
- Controls: **Start month** picker → **End month** picker → result updates reactively (no explicit submit button needed).
- Result area:
  - Prominent total: `Total debitable days: N`.
  - Table/list: one row per month with the month name and its debitable-day count.
- Months that span data from years not yet imported/configured should show a clear indication (e.g., "No holiday data for YYYY — import red days first").

### Tests

- Unit tests for the debitable-days calculation covering: period spanning multiple years, period with no off days, period where all days are holidays/vacation, single-month period.
- ViewModel tests for start/end month selection, reactive recalculation on off-day change, and missing-holiday-data warning.

---

## Suggested Implementation Order

1. **Domain + persistence** for `OffDay` / `CalendarYear` (Feature 1 core).
2. **Dagar API importer** and local caching (Feature 1 bonus).
3. **Calendar UI** — year view, day toggle, import action (Feature 1 UI).
4. **Debitable days calculation** domain logic (Feature 2 core).
5. **Debitable Days panel** integrated into the Calendar view (Feature 2 UI).
6. **Tests** at each layer, updated requirement documents (`DOMAIN.md`, `USE_CASES.md`, `DATABASE.md`).

---

## Documents to Update When Implementing

- `DOMAIN.md` — add `CalendarYear`, `OffDay`, `DebitableDaysQuery`, `DebitableDaysResult`, and related enumerations.
- `USE_CASES.md` — add UC-CAL-01 to UC-CAL-06 and UC-DEB-01 to UC-DEB-03.
- `DATABASE.md` — document the `OffDays` table schema.
- `DEV_PLAN.md` — add a new priority section referencing this plan and track checklist items.
