# Research: Hide the window title bar in fullscreen

**Feature**: specs/002-fullscreen-hide-titlebar | **Date**: 2026-10-12

Codebase facts established by inspection (no external dependencies involved):

- `MainWindow` draws its custom chrome itself: `TitleBar` Border (36 px, hairline
  bottom separator) in `MainWindow.axaml`, with `ExtendClientAreaToDecorationsHint = true`
  and `ExtendClientAreaTitleBarHeightHint = 36` set in the ctor (macOS/Windows). On Linux
  the ctor already sets `TitleBar.IsVisible = false` and keeps the native frame
  (`MainWindow.axaml.cs:93-102`).
- Secondary windows share `NotaWindow`; the shared 36 px chrome bar exists only in the
  non-Linux branch (`_chromeBar`, `NotaWindow.cs:59-97`). `DeviceWindow` passes
  `ownTitleBarHeight > 0` and draws its own bar — which is a *functional* toolbar
  (preset picker, bypass, toggles), not pure chrome (`DeviceWindow.cs:168-195`).
- No `WindowState.FullScreen` handling exists anywhere today; the only state logic is the
  title-bar double-click Maximize toggle.
- Command plumbing: `CommandCatalog.Create()` (id/title/category/gesture/aliases) →
  `MainWindow.Commands.cs` binds handlers → `MainWindow.MenuKeys.cs` wires
  `NativeMenuItem`s (`app:Commands.Id`) and auto-attaches gestures — **but only for
  chords** (`Commands.IsChord`), because a bare typing key as a menu key equivalent would
  swallow text input. On Windows/Linux `TryMenuShortcut` dispatches wired gestures;
  on macOS AppKit fires them natively. `Commands.Parse` already parses "F11".
- The palette smoke test asserts every catalog gesture is listed in
  Settings → Shortcuts (`tests/Nota.SmokeTest/PaletteTests.cs:335`).
- Avalonia version: **12.0.3** (`Nota.App.csproj`).

## Decisions

### D1 — Trigger: `WindowState.FullScreen` observation

**Decision**: React to the window's `WindowState` property changing to/from
`FullScreen`: `TitleBar.IsVisible = !OperatingSystem.IsLinux() && WindowState != FullScreen`
(main window), `_chromeBar.IsVisible = WindowState != FullScreen` (NotaWindow). The
DockPanel layout fills the freed band automatically; no other layout surgery.

**Rationale**: The OS owns the fullscreen concept on every platform (macOS green button,
Linux WM, programmatic on Windows). Observing the state covers every entry path —
OS gesture or in-app command — and restore falls out for free. Repeated toggles converge
because visibility is a pure function of current state, not of transition history
(FR-006).

**Alternatives considered**:
- Listen to a "did enter fullscreen" event per platform — no such Avalonia abstraction;
  `WindowState` is the single cross-platform source of truth.
- Collapse the bar to height 0 instead of `IsVisible = false` — extra states for no gain;
  Linux already uses `IsVisible = false`.

### D2 — Windows/Linux entry point: `view.fullscreen` command + F11

**Decision**: One new catalog command
`C("view.fullscreen", "Toggle Full Screen", "View", "F11", "maximize immersive hide chrome")`,
a `NativeMenuItem` in the View menu (`app:Commands.Id="view.fullscreen"`), bound to a
handler that toggles `WindowState` between `FullScreen` and the remembered prior state
(Maximized or Normal — the app starts Maximized, so exiting fullscreen must return to
Maximized, not Normal). Allow bare function keys as menu gestures by widening
`Commands.IsChord` (or the wiring condition) to accept `F1`–`F12` — function keys cannot
swallow typing, which is the reason the chord-only rule exists. Then the existing
`TryMenuShortcut` path dispatches F11 on Windows/Linux with no new key-handling code.

**Rationale**: Reuses the established one-definition pipeline (menu + keyboard + palette
+ Shortcuts pane all read the catalog), so F11 shows in the menu automatically and the
smoke test's gesture-listing check keeps the Shortcuts pane honest. The prior-state
memory only lives in the command handler (a `WindowState?` field); macOS-native entry
(green button) restores natively and needs no bookkeeping.

**Alternatives considered**:
- Bare F11 handled ad-hoc in `HandleKeyDown` (like the bare "0" clip toggle) — works, but
  then the menu can't display the shortcut and the gesture rule stays chord-only forever.
- A ⌘/Ctrl-chord instead of F11 — F11 is the platform convention for fullscreen and needs
  no modifiers (safe: it types nothing).

### D3 — Secondary windows: shared chrome hides; functional toolbars stay

**Decision**: `NotaWindow` hides its shared `_chromeBar` on fullscreen (same state
observation, non-Linux branch only). `DeviceWindow`'s own bar **stays visible**: it hosts
live controls (preset picker, bypass, size toggles), so hiding it would remove
functionality, not chrome. Spec FR-008/Story 3 is amended accordingly (pure title-bar
chrome hides; functional toolbars remain) — a plan-level refinement, flagged to the user.

**Rationale**: FR-005's spirit ("controls remain reachable") extends to a device window's
own controls; its bar is a toolbar that happens to carry the device name, unlike
About/What's New/Save-changes windows whose bar is pure chrome.

**Alternatives considered**: Hide the device bar too — would hide the preset picker and
bypass in fullscreen, strictly worse; a "relocate controls" redesign is out of scope.

### D4 — macOS: no new entry surface beyond the shared menu item

**Decision**: The `view.fullscreen` menu item exists on all platforms (uniform menu), but
macOS keeps the green button / system gestures as the primary path, per the spec. The
handler toggling `WindowState.FullScreen` is identical everywhere.

**Rationale**: Spec decision (Q2). A uniform menu avoids platform-conditional XAML; plain
F11 rarely reaches apps on macOS (media-key default) so there is no conflict.

### D5 — Verification path

**Decision**: Visual verification on macOS via the in-app verify recipe (screencapture
before/after entering fullscreen — per the `nota-ui-verify` memory), Windows check via a
Windows build (F11 + menu), Linux check for no-regression (native frame path already
hides the bar). `scripts/build.sh` green incl. the palette smoke test, which
automatically asserts the new F11 gesture is listed in Settings → Shortcuts (update the
Shortcuts pane accordingly — `nota-shortcuts` skill). Manual/headless: `WindowState`
transitions are real-OS behaviour; no headless simulation — quickstart covers the manual
matrix.

**Rationale**: The change is pure UI chrome; the existing end-to-end gate plus the
catalog/shortcuts consistency assertion cover the automatable part; the visual outcome
needs eyes on each OS.

## Summary of resolved unknowns

No NEEDS CLARIFICATION markers remain in the plan. All decisions above are grounded in
the inspected code; no new dependencies, no engine/ABI changes, no storage.
