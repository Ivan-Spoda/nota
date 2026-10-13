# Implementation Plan: Hide the window title bar in fullscreen

**Branch**: `002-fullscreen-hide-titlebar` | **Date**: 2026-10-12 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-fullscreen-hide-titlebar/spec.md`

## Summary

When any app window with Nota's custom chrome enters the OS fullscreen state, its 36 px
title bar band and bottom hairline separator hide and the workspace extends to the top
screen edge; leaving fullscreen restores the bar. Visibility is derived purely from the
current window state (`bar visible ⇔ not Linux ∧ state ≠ FullScreen`). A new catalog
command `view.fullscreen` ("Toggle Full Screen", F11) in the View menu gives Windows —
which has no OS fullscreen gesture — an in-app entry point, reusing the one-definition
menu/keyboard/palette/Shortcuts pipeline. UI-only: no engine, ABI, storage, or
dependency changes. Research decisions: [research.md](research.md).

## Nota: Read Before Planning

Consult the matching source of truth before designing:

- **Engine work** → [`ARCHITECTURE.md`](../../ARCHITECTURE.md): the AR rules table is binding (AR-4 lock-free queue, AR-5 immutable snapshots, AR-6 audio-thread restrictions, AR-7 pure C ABI, AR-9 `Transport` is the only source of musical time).
- **UI work** → [`DESIGN.md`](../../DESIGN.md) (Ember design language) and the `nota-design` skill.
- **A built-in instrument / audio or MIDI effect** → the `nota-instrument` skill (DSP class, kind registration across C++ and C#, device-card UI).
- **Build / packaging** → [`BUILD.md`](../../BUILD.md).
- **Anything** → [`CONTRIBUTING.md`](../../CONTRIBUTING.md): licensing, dependency policy, PR expectations.

This feature is UI work only: no Ember palette changes (bars are hidden, not restyled),
no new surfaces.

## Technical Context

**Language/Version**: C# / .NET 10 (`src/managed/`) — no C++ changes.

**Primary Dependencies**: Avalonia 12.0.3 (existing; `WindowState`, `NativeMenu`,
`GetObservable`). No NEW dependency.

**Storage**: N/A.

**Audio-Thread Impact** *(required — state explicitly)*: **None: UI-only change.** No
engine call, no C ABI change, nothing crossing AR-4/AR-6; the window-state subscription
lives entirely on the UI thread.

**Testing**: `tests/Nota.SmokeTest` via `scripts/build.sh` — the palette/shortcuts
consistency assertions automatically cover the new `F11` gesture and the
every-catalog-id-bound check. Visual behaviour verified manually per
[quickstart.md](quickstart.md) (real window managers needed).

**Target Platform**: macOS, Windows, Linux desktops (all first-class here).

**Project Type**: desktop-app (Avalonia) over the Nota C++ engine.

**Performance Goals**: instantaneous chrome update on state change (one
`IsVisible` toggle); no layout thrash beyond the natural DockPanel reflow.

**Constraints**: maximized state must look exactly as today (app launches maximized);
traffic lights / caption buttons stay operable in fullscreen (OS-owned overlay).

**Scale/Scope**: `MainWindow` + `NotaWindow` base + command catalog/wiring + menu XAML +
Shortcuts pane text + docs. ~6 source files touched.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Real-Time Audio Safety | ✅ pass | UI-only; no audio-thread or queue changes (Audio-Thread Impact: none) |
| II. Boundary Discipline | ✅ pass | C ABI untouched; `nota_engine.h` unchanged |
| III. Licensing Hygiene | ✅ pass | No new files expected except spec docs; any new source file gets the standard SPDX header; no new dependency |
| IV. Verification Gate | ✅ pass | `scripts/build.sh` runs the smoke test; new gesture covered by palette/shortcuts assertions; manual matrix in quickstart.md |
| V. One Logical Change per PR | ✅ pass | Single purpose: fullscreen chrome hiding incl. its entry point |
| VI. Documentation in Sync | ✅ pass | Tasks include CHANGELOG `[Unreleased]`, `nota-features` (FEATURES.md), `nota-shortcuts` (Shortcuts pane + docs page), `nota-docs` (EN/RU manual) |
| VII. Spec-First | ✅ pass | Spec agreed (2026-10-12) before this plan |

**Post-Phase-1 re-check**: design adds no entity, contract, or dependency beyond
UI-internal state (see data-model.md, contracts/ui-contract.md) — all gates still pass.
No Complexity Tracking table needed (no violations).

## Project Structure

### Documentation (this feature)

```text
specs/002-fullscreen-hide-titlebar/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   └── ui-contract.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/managed/
├── Nota.App/
│   ├── MainWindow.axaml         # + NativeMenuItem "Toggle Full Screen" (View menu, Commands.Id)
│   ├── MainWindow.axaml.cs      # + WindowState subscription → TitleBar.IsVisible
│   ├── MainWindow.Commands.cs   # + B("view.fullscreen", ToggleFullscreen)
│   ├── Commands.cs              # ~ IsChord/wiring: allow bare F1–F12 as menu gestures
│   └── NotaWindow.cs            # + WindowState subscription → _chromeBar.IsVisible (non-Linux)
├── Nota.Application/
│   └── Palette/CommandCatalog.cs # + C("view.fullscreen", "Toggle Full Screen", "View", "F11", …)
tests/Nota.SmokeTest/            # (no new file — palette assertions cover the gesture)
```

**Structure Decision**: the feature folds into the existing partial-class layout of
`MainWindow` (state subscription near the existing Linux/extend-chrome ctor logic; the
toggle handler beside the other View commands in `MainWindow.Commands.cs`). The only
shared-code change is `NotaWindow`'s ctor subscription and the small `Commands.IsChord`
widening — both already central to every window / menu item. No new projects, files, or
folders beyond the spec docs above.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations — table intentionally empty.
