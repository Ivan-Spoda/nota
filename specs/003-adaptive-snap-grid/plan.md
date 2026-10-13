# Implementation Plan: Adaptive Snap Grid

**Branch**: `003-adaptive-snap-grid` (the spec directory; the work currently sits on `develop`) | **Date**: 2026-10-13 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/003-adaptive-snap-grid/spec.md`

## Summary

The arrangement and the piano roll each get one grid setting: Adaptive (Narrow, Medium
or Wide) or Fixed, plus Triplets and Snap on/off. A single pure rule, `SnapGrid` in
`Nota.Application`, turns a setting and the current zoom into the step to snap to and the
step to draw. Both editors' drawing, snapping, nudging and paste go through it, so what
you see is what you snap to. A menu replaces forward cycling, ⌘1–⌘5 drive it from the
keyboard, and the piano roll's Quantize gets its own value. Settings persist app-wide in
`settings.json`. The engine is not touched.

## Nota: Read Before Planning

- **UI work** → [`DESIGN.md`](../../DESIGN.md) (Ember) and the `nota-design` skill for
  the snap chip, its menu, the dimmed and off label states, and the QUANTIZE section.
- **Anything** → [`CONTRIBUTING.md`](../../CONTRIBUTING.md): SPDX headers on the new
  files, no new dependencies.
- **Shortcuts** → the `nota-shortcuts` skill (Settings → Shortcuts list).

## Technical Context

**Language/Version**: C# / .NET 10 (managed app only)

**Primary Dependencies**: Avalonia (existing). No new dependency.

**Storage**: `settings.json` in `NotaPaths.DataDir` via `ISettingsService`. Three new
`Settings` properties (research R7). Project files are unchanged.

**Audio-Thread Impact**: None. This is a UI-only change. Snapping resolves positions
before the existing engine calls (`SetClipNotes`, clip move and resize), which are
unchanged.

**Testing**: A new pure-logic section in `tests/Nota.SmokeTest` (`SnapGridTests.cs`, run
alone with `--grid`, also part of the full run), the existing palette checks (ids bound,
shortcuts listed), and a headless Avalonia harness for screenshots (quickstart §3).

**Target Platform**: macOS, Windows, Linux desktop (⌘ = Ctrl off macOS).

**Project Type**: Desktop app (Avalonia).

**Performance Goals**: No visible snapping delay (SC-004). `Resolve` and `SnapBeat` are
O(ladder) = 8 steps, run per pointer move and per frame, allocation-free. Line
enumeration is bounded by the threshold (≤ view width / 8 lines).

**Constraints**: Must not change any stored clip or note position on load (SC-005).
Sections keep whole-bar snapping. ⌥ bypass and magnetic snap keep working.

**Scale/Scope**: About 9 touched files in `Nota.App`, 2 new and 1 touched in
`Nota.Application`, 1 new test file, docs.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design: still passes.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Real-time audio safety | ✅ Pass | No engine or audio-thread code touched |
| II. Boundary discipline | ✅ Pass | No new C ABI surface; no JUCE |
| III. Licensing hygiene | ✅ Pass | New files carry the two-line SPDX header; no dependency |
| IV. Verification gate | ✅ Pass | `scripts/build.sh` must pass; the smoke test gains the `SnapGridTests` section even though no engine behaviour changes |
| V. One logical change per PR | ✅ Pass | One feature, one PR. The piano-roll time-signature fix (R5) is part of FR-002/Edge Cases, not a separate change |
| VI. Docs stay in sync | ✅ Planned | CHANGELOG `[Unreleased]`, `nota-shortcuts`, `nota-features`, `nota-docs` (EN + RU) |
| VII. Spec-first | ✅ Pass | spec → clarify (4 Qs) → this plan |
| VIII. Safety rules not weakened | ✅ Pass | N/A |

No violations, so Complexity Tracking is empty.

## Design

### 1. The rule — `Nota.Application/Editing/SnapGrid.cs` (new)

Pure static functions and small value types as defined in [data-model.md](data-model.md):
`GridStep`, `GridDensity`, `GridSetting`, `ResolvedGrid`, plus

- `Resolve(setting, pixelsPerBeat, beatsPerBar)` → `ResolvedGrid` (R2, R6);
- `SnapBeat(beat, stepBeats, beatsPerBar)` and `GridLines(from, to, stepBeats,
  beatsPerBar)`, both bar-anchored and sharing one position rule (R4);
- `Narrower`, `Wider`, `ToggleTriplets`, `ToggleSnap`, `ToggleAdaptive`, `PickAdaptive`,
  `PickFixed`, each returning a new `GridSetting`;
- `Label(setting, resolved)` → "Auto · 1/8", "1/8T", "1 bar" (contract label table).

`Nota.Application/Settings.cs` gains `ArrangementGrid`, `PianoRollGrid` and
`QuantizeStep` with their FR-016 defaults.

### 2. Live state — `Nota.App/EditorGrids.cs` (new)

It holds both settings and the Quantize step, loads them from `ISettingsService` at
start-up, saves on change, and raises `Changed(EditorKind)` (R8). It is one app-wide
instance, `EditorGrids.Shared`, read by `ArrangementView`, every `PianoRollView`,
`ClipPropsView` and the snap chips. Views subscribe only while on screen.

### 3. Arrangement — `ArrangementView*.cs`

- Replace `_snapBeats` / `_snapEnabled` with a `Resolve` call cached per zoom or setting
  change (`_resolved`). The `SnapBeats` and `SnapEnabled` setters go away.
- `Snap()` → `SnapGrid.SnapBeat(beat, _resolved.Snap beats, _beatsPerBar)` when
  `SnapOn`. `SnapMaybe` and `SnapMagnetic` keep their ⌥ and magnet logic on top. All
  existing call sites (LaneControl, Controls, Automation, loop range, paste) inherit it
  (FR-007).
- The loop range's minimum length (`_loopS + _snapBeats`) uses the resolved step.
- The lane, footer and ruler grid loops (`LaneControl` around lines 1275–1285 and
  `Controls` around lines 189–200) draw from `GridLines(_resolved.Draw)` with the
  bar/beat/sub-beat pens. Today they draw only bars and beats (FR-005, FR-006).
- `SnapBar` for sections is unchanged.

### 4. Piano roll — `PianoRollView*.cs`, `ClipPropsView.cs`, `ClipEditorView.cs`, `MidiToolsView.cs`

- Remove `GridStepBeats()`, `GridLadder` and `_grid`. Add `_resolved` from
  `EditorGrids.PianoRoll`, refreshed on zoom (`SetZoom` and the fit in layout) and on
  `Changed`.
- Note snapping (`NoteGrid.Snap`), new note length, ← → nudges, paste anchor and the
  duplicate shift use `_resolved.Snap`. Drawing and the Expression lanes use
  `GridLines(_resolved.Draw)`.
- Add a `BeatsPerBar` property set from the transport at both build sites in
  `MainWindow.Detail.cs`. `GridPen` and `GridLabel` use it instead of the literal 4
  (R5).
- `Quantize(strength)` uses `EditorGrids.Quantize` (FR-008a). In `ClipPropsView`, the
  GRID section becomes QUANTIZE, with straight and triplet sub-bar values only.
  `MidiToolsView` passes the same Quantize step as `MidiToolContext.Grid` (R11).
- `ClipEditorView` header: a new `SnapChip` control before the Scale controls.

### 5. Shared snap chip — `Nota.App/SnapChip.cs` (new)

A small control used by both editors. It takes an `EditorKind` and `EditorGrids`, renders
the label states, and opens the menu from the contract (built with the app's existing
menu helpers; the same style as the `ClipPropsView` dropdowns). In the arrangement it
replaces the content and handler of the toolbar `SnapChip` cell
(`MainWindow.axaml:238`). `OnCycleSnap` and `SnapSteps` are deleted. `SnapToggle` binds
to `ArrangementGrid.SnapOn` in both directions.

### 6. Commands — `CommandCatalog.cs`, `MainWindow.Commands.cs`, `MainWindow.axaml`, `PreferencesWindow.cs`

- Five `grid.*` commands with ⌘1–⌘5 (contract). Each is bound to
  `EditorGrids.Set(target, SnapGrid.X(current))` plus the status line. The target comes
  from `_detailWasLastFocused` and whether a MIDI clip editor is showing (R9).
- Edit → Grid submenu in `MainWindow.axaml` (macOS key equivalents, plus the
  `TryMenuShortcut` path on Windows and Linux).
- A GRID group in Settings → Shortcuts. The palette smoke test enforces it.

### 7a. Audio clip editor — `AudioClipEditorView.cs` (US4, research R13)

The header's Snap switch is replaced by a `SnapChip(EditorKind.PianoRoll, …)` that reads
the drawn axis. `WaveformView` resolves the grid per axis. It replaces the static
`SnapBeat` (whole beats) and the private `BeatStep` grid thinning with `SnapGrid.SnapBeat`
and `SnapGrid.GridLines`, honours `SnapOn` and Alt for the trim brackets, the warp markers
(add and drag) and the envelope points (add and drag), and draws the grid on the envelope
axis in Envelope mode. `GridTarget()` and the status line cover it (FR-021).

### 7. Tests — `tests/Nota.SmokeTest/SnapGridTests.cs` (new) + `Program.cs`

A `--grid` flag and an entry in the full run, following the `TapTempoTests` pattern. The
cases are listed in quickstart §1.

## Project Structure

### Documentation (this feature)

```text
specs/003-adaptive-snap-grid/
├── spec.md
├── plan.md              # this file
├── research.md          # R1–R12
├── data-model.md        # GridStep, GridSetting, ResolvedGrid, transitions
├── quickstart.md        # validation guide
├── contracts/
│   └── grid-ui.md       # commands, shortcuts, snap chip states and menu
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks (not created yet)
```

### Source Code (repository root)

```text
src/managed/
├── Nota.Application/
│   ├── Editing/SnapGrid.cs            # NEW — the rule (pure)
│   ├── Settings.cs                    # + ArrangementGrid, PianoRollGrid, QuantizeStep
│   └── Palette/CommandCatalog.cs      # + grid.* commands (⌘1–⌘5)
└── Nota.App/
    ├── EditorGrids.cs                 # NEW — live state, persistence, Changed
    ├── SnapChip.cs                    # NEW — label + menu, used by both editors
    ├── ArrangementView.cs             # Snap()/loop via SnapGrid; _resolved cache
    ├── ArrangementView.LaneControl.cs # grid lines from GridLines
    ├── ArrangementView.Controls.cs    # ruler/footer grid lines
    ├── PianoRollView.cs               # _resolved, BeatsPerBar, nudges, quantize
    ├── PianoRollView.Expression.cs    # lane grid lines
    ├── AudioClipEditorView.cs         # US4: chip, grid per axis, snapping (R13)
    ├── MidiToolsView.cs               # MidiToolContext.Grid = Quantize step (R11)
    ├── ClipPropsView.cs               # GRID → QUANTIZE
    ├── ClipEditorView.cs              # header SnapChip
    ├── MainWindow.axaml(.cs)          # toolbar cell, SnapToggle binding, Edit → Grid
    ├── MainWindow.Commands.cs         # bind grid.*
    ├── MainWindow.Detail.cs           # pass EditorGrids + BeatsPerBar to rolls
    └── PreferencesWindow.cs           # Shortcuts: GRID group

tests/Nota.SmokeTest/
├── SnapGridTests.cs                   # NEW
└── Program.cs                         # --grid + full-run entry
```

**Structure Decision**: The rule goes in `Nota.Application`, so it is testable and free
of UI types. The live state and controls go in `Nota.App`, next to the views that already
own snapping. Nothing is added to `Nota.Presentation`, `Nota.Infrastructure` (the existing
`SettingsService` serialises the new properties unchanged) or the engine.

## Risks

- **Toolbar width**: "Auto · 1/16T" is wider than "1/4". Check it against
  `MainWindow.TransportFit.cs` hiding order at narrow widths, and fix the cell's width to
  the longest label so the toolbar does not jitter while zooming.
- **Redraw on zoom**: the snap chip label changes during wheel zoom. Update it only when
  the resolved step actually changes.
- **Behaviour change for existing users**: the default becomes Adaptive. At the default
  zoom it matches today's 1/4 (FR-003), and the CHANGELOG entry names ⌘5 for going back to
  a fixed grid.

## Complexity Tracking

None. The Constitution Check has no violations.
