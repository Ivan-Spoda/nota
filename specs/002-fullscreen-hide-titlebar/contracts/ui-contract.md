# UI Contract: Hide the window title bar in fullscreen

**Feature**: specs/002-fullscreen-hide-titlebar | **Date**: 2026-10-12

Nota is a desktop app; its user-facing contract surface is (a) the command system shared
by the menu / keyboard / command palette / Shortcuts pane, and (b) window chrome
behaviour. This feature extends both.

## Command contract: `view.fullscreen`

| Aspect      | Contract |
|-------------|----------|
| Id          | `view.fullscreen` |
| Title (menu/palette) | "Toggle Full Screen" |
| Category    | View |
| Gesture     | F11, no modifiers |
| Menu        | View menu of the main window, alongside the other View toggles |
| Palette     | Searchable by title/aliases ("maximize", "immersive", "hide chrome") |
| Effect      | Main window: enter the OS fullscreen state, or leave it |

**Behaviour**:

- Enter: main window `WindowState → FullScreen`; the pre-fullscreen state
  (Maximized/Normal) is remembered for the exit path.
- Exit: `WindowState → remembered state` (Maximized if the window was maximized before,
  Normal otherwise; never a surprising un-maximize).
- Idempotent per invocation (it is a toggle); firing it twice returns to the starting
  state.
- Works on every OS. On macOS the OS gestures (green button, system menu) remain the
  primary fullscreen path and produce the same chrome behaviour.

## Chrome visibility contract

For every app window with Nota's custom chrome (main window + `NotaWindow` dialog family
on macOS/Windows):

| State              | Title bar band + separator |
|--------------------|---------------------------|
| Normal / Maximized | visible (unchanged styling, drag, centered title) |
| FullScreen         | hidden; workspace content extends to the top screen edge |

- Windows whose top band is a functional toolbar (the device pop-out window's preset
  bar) keep that band in fullscreen — it carries live controls, not just chrome.
- On Linux (native OS frame) the app draws no custom bar in any state; the OS frame's
  own fullscreen behaviour applies unchanged.

## Out of contract (unchanged)

- Audio engine, C ABI, playback, project data — untouched (FR-009).
- Maximized-state chrome (bar stays), title-bar double-click → maximize toggle,
  traffic-light/caption-button placement and operability in fullscreen (OS-owned).
