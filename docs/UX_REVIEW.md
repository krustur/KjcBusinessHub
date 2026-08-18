# UX Review — Set Amount Interaction

_Reviewed: 2026-08-18_

---

## Problem

When the user clicks **"Set amount"** on a source document, the editing form (labelled _"Set amount for: …"_) was rendered outside the current visible area of the app. The form lived in a dedicated card either below or above the source-document list, and because the list itself can be long, the card was frequently off-screen. The user had to scroll to find the form, which made the interaction feel broken and unintuitive.

---

## Root Cause

The form was a single card that lived at a fixed row in the outer `Grid` of the _Source Documents_ panel. Its position in the layout had no relationship to the specific document row that triggered the action. When many documents were visible, the list pushed the card below the viewport, or the card appeared far above the document the user had just interacted with.

Previous attempt (same session): the card was moved to _above_ the list, but this still did not place it next to the specific item being edited. If the list itself was taller than the viewport, the user was still required to scroll up to find the form.

---

## Chosen Solution — Inline Editing Inside the List Item

The form is now embedded **directly inside the DataTemplate** of each source-document list item. When the user clicks "Set amount" on a document:

1. The action buttons for that row are hidden.
2. The inline editing form appears immediately below the document details, within the same list item card.
3. All other rows continue to show their action buttons normally.

This means the form always appears next to the document the user is editing, with no scrolling required.

### Why inline over a modal dialog

| Criterion | Inline form | Modal dialog |
|-----------|------------|--------------|
| Context preservation | The user can still see the document's details (date, description, existing amount) while typing. | Document details are hidden behind the overlay. |
| Scroll position | No change; the editing area is already visible. | Window focus shifts; user must re-orient after dismissal. |
| Implementation simplicity | A `Border` toggled by `IsVisible` inside the existing DataTemplate. | Requires a new `Window` or `Popup`, a ViewModel event, and glue code in code-behind. |
| Consistency with existing patterns | Matches the "inline action" style already used for _Mark handled_ on transactions. | Introduces a new interaction pattern for a single action. |

A modal would be preferable only if the form were complex or required context unavailable on the current screen. For a two-field amount entry this is unnecessary overhead.

---

## Technical Implementation

A `FuncMultiValueConverter` (`Converters.AreReferenceEqual` / `Converters.AreReferenceNotEqual`) is used in a `MultiBinding` to compare the current list item (the `SourceDocument` bound by the `DataTemplate`) to the `AppViewModel.DocumentBeingAmounted` property. This lets the XAML toggle visibility per-row without introducing a display-specific property on the domain entity.

```
MultiBinding
  ├── Binding Path="."                           → current SourceDocument
  └── Binding RelativeSource=FindAncestor(UserControl)
              Path="((AppViewModel)DataContext).DocumentBeingAmounted"
                                                 → the document being edited
```

The converter returns `true` only when both values are the same non-null reference, driving the inline form's `IsVisible`.

---

## Remaining Considerations

- **Keyboard focus**: After the form appears, focus is not automatically moved to the first `TextBox`. A future improvement could call `Focus()` on the amount field in the `BeginSetAmount` handler.
- **Cancel on outside click**: There is no automatic cancellation when the user clicks elsewhere. This matches the current pattern (explicit Cancel button). A future improvement could cancel editing when the selected source document changes.
- **Scroll to reveal**: If the document being edited is partially off-screen (e.g., near the bottom of the list), the list does not auto-scroll. A future improvement could use `BringIntoView` via a behavior or code-behind.
