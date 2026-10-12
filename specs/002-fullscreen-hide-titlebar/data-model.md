# Data Model: Hide the window title bar in fullscreen

**Feature**: specs/002-fullscreen-hide-titlebar | **Date**: 2026-10-12

No persistent data is introduced — the feature is pure window-chrome presentation. This
document records the transient state model that drives it.

## Entities (transient, UI-layer only)

### Window chrome state (derived — no new storage)

A pure function of the window's current display state, re-evaluated on every
`WindowState` change:

| Window state (OS-owned)  | Custom title bar + separator | Notes |
|--------------------------|------------------------------|-------|
| Normal                   | visible                      | unchanged behaviour |
| Maximized                | visible                      | app starts here; explicitly out of fullscreen scope (spec Q1) |
| FullScreen (macOS/Win)   | hidden                       | content fills to the top edge |
| Any state on Linux       | hidden (already, native frame) | pre-existing behaviour; no regression |

Derived rule: `bar visible ⇔ (not Linux) ∧ (state ≠ FullScreen)`.

**Validation**: FR-006 (rapid toggles converge) holds because visibility is derived from
current state, never from transition history.

### Pre-fullscreen window state (one field, main window only)

- **Purpose**: the F11/menu exit path on Windows/Linux must return to the state the
  window had before entering fullscreen (Maximized or Normal) — the app launches
  Maximized, so restoring Normal would un-maximize unexpectedly.
- **Type**: remembered `WindowState` (Normal/Maximized) or "unset"; lives only while
  fullscreen, cleared on exit.
- **Lifecycle**: set by the toggle command when entering fullscreen; consumed and cleared
  when leaving. macOS-native entry/exit (green button) restores natively and never reads
  this field.

### Command catalog entry (existing catalog, one new row)

| Field   | Value |
|---------|-------|
| Id      | `view.fullscreen` |
| Title   | Toggle Full Screen |
| Category| View |
| Gesture | `F11` (bare function key — no modifiers) |
| Aliases | maximize, immersive, hide chrome |
| Palette | listed (not hidden) |

**Validation**: the palette smoke test requires the gesture to appear in
Settings → Shortcuts; every catalog id must be bound in `MainWindow.Commands.cs`
(missing binding throws at startup).

## Relationships

- Command entry (above) → drives `WindowState` → drives derived chrome state.
- No relationships to engine, project data, settings, or persistence.
