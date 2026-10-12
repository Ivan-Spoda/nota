# Quickstart: Validate "hide the title bar in fullscreen"

**Feature**: specs/002-fullscreen-hide-titlebar | **Date**: 2026-10-12

Runnable validation for the feature end-to-end. The visual assertions need a real OS
desktop (fullscreen is window-manager behaviour); the automated gate is `scripts/build.sh`.

## Prerequisites

- Repo checked out on the feature branch; macOS build host (Linux/Windows siblings exist
  for their checks).
- Build once: `scripts/build.sh` (builds native + managed, runs the smoke test).

## Automated gate (all platforms)

```sh
scripts/build.sh            # macOS
# scripts/build-linux.sh    # Linux
# scripts/build-win.ps1     # Windows
```

**Expected**: green, including the palette smoke test which now asserts that the new
`F11` gesture for "Toggle Full Screen" is listed in Settings → Shortcuts (it fails with
"shortcuts missing from Settings → Shortcuts: F11 (…)" if the pane was not updated).

## Manual matrix (the real proof)

### macOS (primary visual check)

1. Launch the app (`scripts/build.sh` output or `dotnet run --project src/managed/Nota.App`).
2. **Baseline**: window maximized (default) → 36 px title bar with centered project name
   and hairline separator visible; transport sits below it.
3. Click the green traffic light (or menu View → Toggle Full Screen):
   - title bar band and separator are gone; workspace content reaches the top screen
     edge; no leftover strip or second hairline;
   - move the pointer to the top edge → traffic lights reveal and click (no dead zone).
4. Leave fullscreen (green button / menu / ⌃⌘F as the OS provides):
   - title bar and separator are back, title centered, bar draggable, double-click on
     the bar still toggles maximize.
5. Toggle rapidly 10× → bar visibility always matches the current state (no stuck
   states).
6. Fullscreen a secondary chrome window (e.g. About) → its bar hides; exit → restores.

### Windows

1. Run the app; press **F11** → window covers the whole screen including the taskbar,
   no title bar/separator.
2. Press **F11** again → returns to the *previous* state (maximized if it was maximized —
   the app's default), title bar visible.
3. Menu View → Toggle Full Screen does the same as F11; the menu shows the F11 gesture.

### Linux

1. Run the app → native frame as today (no custom bar), everything as before.
2. If the WM provides fullscreen (e.g. via its own shortcut), enter/leave → no Nota
   regression; F11/menu path also toggles.

## Screenshot evidence (optional but recommended)

Use the project's UI verify recipe (env-guarded in-app hook + `screencapture`) to capture
before/after shots of the fullscreen transition for the PR description.

## Success

All scenarios above pass and match [ui-contract.md](contracts/ui-contract.md); spec
acceptance scenarios 1–3 (Story 1) and Story 2/3 checks hold.
