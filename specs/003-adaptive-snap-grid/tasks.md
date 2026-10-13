---

description: "Task list for the adaptive snap grid (arrangement + piano roll, with triplets)"
---

# Tasks: Adaptive Snap Grid

**Input**: Design documents from `specs/003-adaptive-snap-grid/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/grid-ui.md](contracts/grid-ui.md), [quickstart.md](quickstart.md)

**Tests**: Included. The plan and quickstart §1 call for a pure-logic smoke-test section
(`SnapGridTests`, `--grid`). Write each test task before its implementation and see it
fail first.

**Organization**: By user story (spec.md): US1 snap follows zoom (P1), US2 menu and
shortcuts (P2), US3 triplets (P3).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1, US2 or US3

## Path Conventions

- Rule (pure): `src/managed/Nota.Application/Editing/`
- App and views: `src/managed/Nota.App/`
- Tests: `tests/Nota.SmokeTest/`
- Every new `.cs` file starts with the two-line SPDX header from CONTRIBUTING.md and a
  short comment header explaining why the file exists (match `TapTempo.cs`).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: The empty test section and its runner flag, so later tasks can add cases.

- [X] T001 Create `tests/Nota.SmokeTest/SnapGridTests.cs` with `internal static class SnapGridTests { public static IEnumerable<(bool Ok, string Label)> Run() { yield break; } }`, following the `TapTempoTests.cs` pattern (header comment, `using Nota.Application.Editing;`)
- [X] T002 Wire the section into `tests/Nota.SmokeTest/Program.cs`: a `--grid` early-exit block printing `GRID PASSED` / `GRID FAILED (n)` next to the `--tap` block (around line 210), and `Console.WriteLine("-- snap grid --"); foreach (...) Check(ok, label);` in the full run after the tap-tempo section (around line 14514)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The rule, its persistence and the live state. Every story builds on them.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Tests (write first, see them fail)

- [X] T003 Add the foundational cases to `tests/Nota.SmokeTest/SnapGridTests.cs`:
  - `GridStep(Index 0..7)` beats in 4/4 = 0.125, 0.25, 0.5, 1, 2, 4, 8, 16, and in 3/4 the bar steps are 3, 6, 12;
  - `Index` is clamped to 0..7;
  - labels "1/32" … "1/2", "1 bar", "2 bars", "4 bars";
  - default `GridSetting` resolves to 1/4 at 28 px per beat in 4/4 (FR-003);
  - at a fixed zoom, Narrow < Medium < Wide (strictly finer to coarser);
  - clamps to 1/32 at 240 px per beat (Narrow) and to 4 bars at 0.5 px per beat (Wide);
  - `SnapBeat` returns the nearest position and is never negative;
  - for many zoom and step combinations, every `GridLines` position snaps to itself and every `SnapBeat` result is in `GridLines` (SC-001);
  - `Label` gives "Auto · 1/4" for Adaptive and "1/8" for Fixed 1/8.

### Implementation

- [X] T004 Create `src/managed/Nota.Application/Editing/SnapGrid.cs` with the value types from data-model.md:
  - `GridStep` with `Index` (int, "clamped to 0..7") and `Triplet` (bool, "forced false when `Index ≥ 5`"), `Beats(int beatsPerBar)` and `Label`;
  - `enum GridDensity { Narrow, Medium, Wide }` with thresholds 8, 16 and 32 px (research R2);
  - `GridSetting` as a record with `Adaptive` = true, `Density` = Medium, `FixedIndex`, `Triplets` = false, `SnapOn` = true, and static `DefaultArrangement` (`FixedIndex` 3) and `DefaultPianoRoll` (`FixedIndex` 1);
  - `ResolvedGrid` (`Snap`, `Draw`, `FinerThanVisible`, `SnapOn`).
- [X] T005 In `src/managed/Nota.Application/Editing/SnapGrid.cs`, implement `static ResolvedGrid Resolve(GridSetting s, double pixelsPerBeat, int beatsPerBar)`:
  - Adaptive: the smallest Index in the setting's triplet family with `Beats × ppb ≥ threshold(Density)`, clamped, and `Draw = Snap`;
  - Fixed: `Snap = (FixedIndex, Triplets)`, and `Draw` is the smallest `Index ≥ FixedIndex` meeting the Medium threshold (16 px), clamped to 7;
  - `FinerThanVisible = Draw.Index > Snap.Index`.
  - Allocation-free; the zero and negative ppb guard returns 1/4.
- [X] T006 In `src/managed/Nota.Application/Editing/SnapGrid.cs`, implement `static double SnapBeat(double beat, double stepBeats, int beatsPerBar)` and `static IEnumerable<double> GridLines(double from, double to, double stepBeats, int beatsPerBar)` on one shared position rule, anchored per bar (research R4):
  - sub-bar steps: `barStart + n·step` for every n with `n·step < barLength − 1e-6`, plus the next bar start;
  - bar steps: multiples of `k·beatsPerBar` from beat 0;
  - results clamped at 0.
- [X] T007 In `src/managed/Nota.Application/Editing/SnapGrid.cs`, implement `static string Label(GridSetting s, ResolvedGrid r)`: "Auto · " + `r.Snap.Label` when Adaptive, else `r.Snap.Label`.
- [X] T008 [P] Add `public GridSetting ArrangementGrid { get; set; } = GridSetting.DefaultArrangement;`, `public GridSetting PianoRollGrid { get; set; } = GridSetting.DefaultPianoRoll;` and `public GridStep QuantizeStep { get; set; } = new(1, false);` to `src/managed/Nota.Application/Settings.cs`, with doc comments ("app-wide, never written to project files", FR-017). Make the types JSON-friendly (public get/set or init, parameterless construction) for `System.Text.Json` in `SettingsService`.
- [X] T009 Add settings cases to `tests/Nota.SmokeTest/SnapGridTests.cs`:
  - deserializing `{}` into `Settings` gives the FR-016 defaults;
  - a serialize, change and deserialize round trip keeps `ArrangementGrid`, `PianoRollGrid` and `QuantizeStep` independent;
  - an out-of-range `FixedIndex` (for example 42) is clamped on resolve.
- [X] T010 Create `src/managed/Nota.App/EditorGrids.cs` (research R8):
  - `enum EditorKind { Arrangement, PianoRoll }`;
  - `sealed class EditorGrids(ISettingsService settings)` exposing `Get(EditorKind)`, `Set(EditorKind, GridSetting)`, `Quantize`, `SetQuantize(GridStep)` and `event Action<EditorKind>? Changed`;
  - it loads from `settings.Current` and saves via `settings.Save()` on each change; a no-op when the value is unchanged.
- [X] T011 Create the `EditorGrids` instance in `src/managed/Nota.App/MainWindow.axaml.cs`, next to where `_vm` and services are set up, from `App.Services.GetRequiredService<ISettingsService>()`. Expose it as `internal EditorGrids Grids` so `DetailWindow` and the detail builders can reach it.

**Checkpoint**: `dotnet run --project tests/Nota.SmokeTest -- --grid` prints GRID PASSED.

---

## Phase 3: User Story 1 - Snap follows the zoom (Priority: P1) 🎯 MVP

**Goal**: In both editors the step to snap to equals the finest drawn line, chosen from
the zoom, with Adaptive · Medium as the default. The snap controls show the step in
effect.

**Independent Test**: With default settings, zoom each editor in and out and drag a
clip or note. It lands only on drawn lines, and the control reads "Auto · <step>" and
changes with zoom (quickstart §3 rows 1–3, 11).

### Arrangement

- [X] T012 [US1] In `src/managed/Nota.App/ArrangementView.cs`:
  - replace `_snapBeats` and `_snapEnabled` (lines 41–42) and the `SnapBeats` / `SnapEnabled` properties (lines 432 and 437) with a required `EditorGrids Grids` (init or constructor), a cached `ResolvedGrid _grid` and `internal ResolvedGrid Grid => _grid`;
  - recompute `_grid` in a `RefreshGrid()` that runs on zoom changes (both `_pixelsPerBeat` writes around lines 646 and 655, plus `ArrangementView.Overview.cs:95`), on `BeatsPerBar` set (line 430) and on `Grids.Changed(Arrangement)`;
  - raise `event Action? GridChanged` only when `_grid` actually changes.
- [X] T013 [US1] In `src/managed/Nota.App/ArrangementView.cs`, change `Snap(double)` (line 1051) to `_grid.SnapOn ? SnapGrid.SnapBeat(beat, _grid.Snap.Beats(_beatsPerBar), _beatsPerBar) : beat`. Keep `SnapMaybe` (Alt bypass) and `SnapMagnetic` (magnet) as they are. Replace the two `_snapBeats` uses in the loop range (lines 561 and 588) with the resolved step's beats.
- [X] T014 [P] [US1] In `src/managed/Nota.App/ArrangementView.LaneControl.cs` (vertical grid around lines 1275–1285), draw lines from `SnapGrid.GridLines(viewStart, viewEnd, _o.Grid.Draw.Beats(_o._beatsPerBar), _o._beatsPerBar)`: `BarPen` on bar starts, `BeatPen` on whole beats, and a new sub-beat pen (`NotaPalette.GridSubBeat`, as in `PianoRollView`) otherwise. Remove the `showBeats` threshold.
- [X] T015 [P] [US1] The same grid-line change in `src/managed/Nota.App/ArrangementView.Controls.cs` (footer grid around lines 189–200).
- [X] T016 [US1] In `src/managed/Nota.App/MainWindow.axaml.cs`:
  - delete `SnapSteps`, `_snapIndex` (lines 570–574) and `OnCycleSnap` (line 656);
  - pass `Grids` to `Timeline`;
  - bind `SnapToggle` both ways to `Grids.Get(Arrangement).SnapOn` (rewrite `OnToggleSnapEnabled` at line 721 to `Grids.Set(... with { SnapOn = on })`, and update `SnapToggle.IsChecked` on `Grids.Changed`).

### Snap chip (label only in this story)

- [X] T017 [US1] Create `src/managed/Nota.App/SnapChip.cs`, a control taking `(EditorKind kind, EditorGrids grids, Func<ResolvedGrid> current)`:
  - renders `SnapGrid.Label` in the Mono style of today's `SnapLabel` (`MainWindow.axaml:240`), with the tooltip from contracts/grid-ui.md;
  - exposes `Refresh()`, called on `Grids.Changed(kind)` and on the editor's `GridChanged`;
  - fixes its width to the widest label ("Auto · 1/16T") so the toolbar does not jitter (plan Risks).
  The menu comes in US2.
- [X] T018 [US1] In `src/managed/Nota.App/MainWindow.axaml` (lines 238–240), replace the `SnapChip` button's `Click="OnCycleSnap"` cell content with the `SnapChip` control, keeping the cell classes. Check that `MainWindow.TransportFit.cs` still hides and shows the cell correctly with the wider label.

### Piano roll

- [X] T019 [US1] In `src/managed/Nota.App/PianoRollView.cs`:
  - remove `_grid` (line 72), the `Grid` property (line 348), `GridLadder` and `GridStepBeats()` (lines 761–770);
  - add a required `EditorGrids Grids`, a `BeatsPerBar` property (default 4), a cached `ResolvedGrid _resolved` with `internal ResolvedGrid ResolvedGridState`, and `GridChanged`;
  - recompute `_resolved` on every `_pixelsPerBeat` write (lines 146, 166 and 367), on `BeatsPerBar` set and on `Grids.Changed(PianoRoll)`.
- [X] T020 [US1] In `src/managed/Nota.App/PianoRollView.cs`, switch every snap use from `_grid` to the resolved snap step through `SnapGrid.SnapBeat` (honouring `SnapOn` and the existing Alt handling):
  - `NoteGrid.Snap` (line 950);
  - new-note start and length (line 1120, length = one step);
  - the duplicate shift and caps (lines 427–428, 1184, 1203, 1215);
  - the paste anchor (line 646);
  - note length minimum (line 654);
  - ← → nudges (lines 1373–1374).
- [X] T021 [US1] In `src/managed/Nota.App/PianoRollView.cs`:
  - the note-grid line loop (around line 976) iterates `SnapGrid.GridLines(..., _resolved.Draw.Beats(BeatsPerBar), BeatsPerBar)`;
  - `GridPen(b)` and `GridLabel(b)` (lines 783–799) use `BeatsPerBar` instead of the literal 4 (research R5);
  - `LabelStepBeats()` stays.
- [X] T022 [P] [US1] In `src/managed/Nota.App/PianoRollView.Expression.cs` (line 251), replace `_o.GridStepBeats()` with the resolved draw step and iterate `SnapGrid.GridLines`.
- [X] T023 [US1] In `src/managed/Nota.App/PianoRollView.cs`, change `Quantize(strength)` (line 592) to use `Grids.Quantize.Beats(BeatsPerBar)` (FR-008a). In `src/managed/Nota.App/MidiToolsView.cs` (line 170), set `Grid = _roll.Grids.Quantize.Beats(_roll.BeatsPerBar)` (research R11).
- [X] T024 [US1] In `src/managed/Nota.App/ClipPropsView.cs` (lines 20–65):
  - rename the GRID section to QUANTIZE;
  - replace `GridValues`, `GridLabels` and `_gridIndex` with the straight sub-bar steps 1/32 … 1/2 (`Index` 0..4, "Bar steps are not offered for Quantize");
  - the dropdown calls `grids.SetQuantize(...)` and shows `grids.Quantize.Label`; default 1/16 comes from Settings;
  - the Quantize button and STRENGTH slider are unchanged.
  Triplet values are added in US3.
- [X] T025 [US1] In `src/managed/Nota.App/MainWindow.Detail.cs`, at both `new PianoRollView` sites (lines 394 and 595), pass `Grids = Grids` and `BeatsPerBar = Timeline.BeatsPerBar` (or the transport's numerator), and keep `BeatsPerBar` in sync when the time signature changes (where `Timeline.BeatsPerBar` is updated today). Pass `Grids` to `ClipPropsView` via `src/managed/Nota.App/ClipEditorView.cs` (line 48).
- [X] T026 [US1] In `src/managed/Nota.App/ClipEditorView.cs`, add a `SnapChip(EditorKind.PianoRoll, roll.Grids, () => roll.ResolvedGridState)` to the header's right group, before `ScaleControls()` (line 61), with a `HeaderDivider()` between them.
- [X] T027 [US1] Build and fix every remaining reference to the removed members: `dotnet build src/managed/Nota.App/Nota.App.csproj`. Check with `grep -rn "SnapBeats\|SnapEnabled\|GridStepBeats\|_roll.Grid\b\|\._grid\b" src/managed` (expect none).

**Checkpoint**: US1 works on its own: the default is Auto, lines and snapping agree in both
editors, the chips show the live step, and ⌥ and the magnet still work.

---

## Phase 4: User Story 2 - Pick a grid quickly from a menu or the keyboard (Priority: P2)

**Goal**: The chip opens a menu with every option; ⌘1, ⌘2, ⌘4 and ⌘5 drive the grid from
the keyboard; and Fixed mode marks a step that is finer than the drawn lines.

**Independent Test**: Quickstart §3 rows 4, 5, 8, 9 and 10, plus the palette checks in
`scripts/build.sh`.

### Tests (write first)

- [X] T028 [P] [US2] Add transition cases to `tests/Nota.SmokeTest/SnapGridTests.cs` per the data-model.md transitions table:
  - `Narrower` and `Wider` in Fixed mode step `FixedIndex` and stop at 0 and 7;
  - in Adaptive mode they step the density and stop at Narrow and Wide;
  - `ToggleAdaptive` twice restores both the density and the fixed step;
  - `ToggleSnap` flips only `SnapOn`;
  - `PickAdaptive` and `PickFixed` set `SnapOn = true`;
  - Fixed 1/32 at 4 px per beat gives `FinerThanVisible` true with a coarser `Draw`; at 240 px per beat it is false (FR-005a).

### Implementation

- [X] T029 [US2] In `src/managed/Nota.Application/Editing/SnapGrid.cs`, add pure transitions returning a new `GridSetting`: `Narrower`, `Wider`, `ToggleSnap`, `ToggleAdaptive`, `PickAdaptive(GridDensity)` and `PickFixed(int index)`. The exact rules are in the data-model.md table. "The shortcuts other than ⌘4 leave `SnapOn` as it is."
- [X] T030 [US2] In `src/managed/Nota.App/SnapChip.cs`:
  - add the click menu from contracts/grid-ui.md (Auto · Narrow/Medium/Wide, separator, the 8 fixed steps, separator, Snap to grid ⌘4), with a check mark on the active row; build it with the app's existing menu helpers (the `ShowMenu` style used by `ClipPropsView`);
  - add the label states: dimmed secondary colour plus the "finer than the visible grid" tooltip when `FinerThanVisible`, and struck through in the disabled colour when `!SnapOn`;
  - follow `DESIGN.md` Ember tokens (nota-design skill).
- [X] T031 [US2] In `src/managed/Nota.Application/Palette/CommandCatalog.cs`, add a `// ---- Grid` block with `grid.narrower` "Narrow Grid" ⌘1, `grid.wider` "Widen Grid" ⌘2, `grid.snap` "Toggle Snap to Grid" ⌘4 and `grid.adaptive` "Toggle Adaptive Grid" ⌘5, with aliases from contracts/grid-ui.md. `grid.triplets` comes in US3.
- [X] T032 [US2] In `src/managed/Nota.App/MainWindow.Commands.cs`:
  - bind the four commands to `Grids.Set(target, SnapGrid.X(Grids.Get(target)))`;
  - set the status line to `"<Arrangement|Piano roll> grid: <label>"` (FR-014);
  - choose the target with a helper `EditorKind GridTarget()`: PianoRoll when `_detailWasLastFocused` and the detail area shows a `ClipEditorView`, else Arrangement (research R9).
- [X] T033 [US2] In `src/managed/Nota.App/MainWindow.axaml`, add an Edit → Grid submenu with `NativeMenuItem`s carrying `app:Commands.Id="grid.narrower"`, `"grid.wider"`, `"grid.snap"` and `"grid.adaptive"` (the gestures come from the catalog via `MainWindow.MenuKeys.cs`).
- [X] T034 [US2] In `src/managed/Nota.App/PreferencesWindow.cs` (the shortcuts table around lines 1000–1080), add the GRID group with the rows from contracts/grid-ui.md (⌘1 ⌘2, ⌘4, ⌘5, ⌥ while dragging). Use the nota-shortcuts skill to check that the list matches the handlers.
- [X] T035 [US2] Run `dotnet run --project tests/Nota.SmokeTest -- --grid` and the palette checks (`scripts/build.sh`). Confirm "every catalog shortcut is listed in Settings → Shortcuts" and that every id is bound.

**Checkpoint**: US1 and US2 both work. The grid is fully controllable without cycling.

---

## Phase 5: User Story 3 - Triplet grid (Priority: P3)

**Goal**: Triplet lines and snapping in both editors and both modes, ⌘3 to toggle, and
triplet Quantize values.

**Independent Test**: Quickstart §3 rows 6 and 7; the triplet cases in `--grid`.

### Tests (write first)

- [X] T036 [P] [US3] Add triplet cases to `tests/Nota.SmokeTest/SnapGridTests.cs`:
  - triplet beats are 2/3 of straight ones (1/8T = 1/3, 1/4T = 2/3, 1/2T = 4/3);
  - bar steps ignore `Triplet`;
  - labels "1/8T" and "Auto · 1/16T";
  - Adaptive with triplets moves through the triplet family as the zoom changes;
  - `ToggleTriplets` flips only `Triplets`;
  - in 3/4, 1/2 and 1/2T keep every bar start as a snap point and in `GridLines`;
  - line and snap agreement (SC-001) holds for all triplet steps.

### Implementation

- [X] T037 [US3] In `src/managed/Nota.Application/Editing/SnapGrid.cs`:
  - make sure `Resolve` keeps the triplet family for sub-bar steps and switches to straight bar steps once the Index reaches 5;
  - add `ToggleTriplets`;
  - `GridStep.Label` appends "T".
- [X] T038 [P] [US3] Add triplet rows to the chip menu in `src/managed/Nota.App/SnapChip.cs`: a "Triplets ⌘3" check row, with the fixed rows showing 1/32T … 1/2T when triplets are on (contract menu).
- [X] T039 [US3] Add `grid.triplets` "Toggle Triplet Grid" ⌘3 to `src/managed/Nota.Application/Palette/CommandCatalog.cs`, bind it in `src/managed/Nota.App/MainWindow.Commands.cs` the same way as T032, add it to the Edit → Grid submenu in `src/managed/Nota.App/MainWindow.axaml` (after Widen Grid), and add the ⌘3 row to the GRID group in `src/managed/Nota.App/PreferencesWindow.cs`.
- [X] T040 [P] [US3] Grid pens for triplet lines in `src/managed/Nota.App/PianoRollView.cs` (`GridPen`) and in the arrangement grid loops (`ArrangementView.LaneControl.cs` and `ArrangementView.Controls.cs`): the bar and beat tests use a tolerance, so 2/3-beat positions draw with the sub-beat pen and whole beats keep `BeatPen` (FR-006). `GridLabel` stays 1/16-based. Ruler labels never show triplet indices (`LabelStepBeats` limits them).
- [X] T041 [US3] Add the triplet Quantize values 1/32T … 1/2T (`Index` 0..4, `Triplet` true) to the QUANTIZE dropdown in `src/managed/Nota.App/ClipPropsView.cs`, after the straight values, as today's list does.

**Checkpoint**: All three stories work independently.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T042 [P] Add an `[Unreleased]` entry to `CHANGELOG.md`:
  - Added: the adaptive snap grid in the arrangement and piano roll, with Narrow/Medium/Wide, triplets, the grid menu, ⌘1–⌘5 and a separate Quantize value;
  - Changed: the snap cell opens a menu instead of cycling, and the piano roll's GRID becomes QUANTIZE; mention ⌘5 to get a fixed grid back.
- [X] T043 [P] Update `FEATURES.md` via the `nota-features` skill.
- [X] T044 Update the user docs (`../nota-docs`, EN + RU) via the `nota-docs` skill: the arrangement grid, the piano roll grid and Quantize pages, the regenerated shortcuts page, and re-shot screenshots of the chip and menu.
- [X] T045 Headless UI verification per quickstart §3 (the nota-ui-verify recipe, `NOTA_DATA_DIR=/tmp/nota-grid`): screenshots of the arrangement chip at default zoom, Fixed 1/32 zoomed out (dimmed), and the piano roll with triplets, in Ember Graphite and Ember Paper. Check the toolbar at narrow widths (`MainWindow.TransportFit.cs`).
- [X] T046 Check SC-004 and SC-005:
  - no allocation in `Resolve` or `SnapBeat` per pointer move (review the code; the label updates only when the step changes);
  - open an existing project and confirm no clip or note positions change.
- [X] T047 Run quickstart.md end to end.

### Nota closing tasks *(PR gates)*

- [X] T048 Run `scripts/build.sh` until native, managed and the smoke test (including `-- snap grid --`) are green
- [X] T049 Check that the AGPL-3.0-only SPDX two-line header is on `SnapGrid.cs`, `EditorGrids.cs`, `SnapChip.cs` and `SnapGridTests.cs`
- [X] T050 New dependency: N/A (none added)
- [X] T051 CHANGELOG entry: covered by T042
- [X] T052 FEATURES.md, docs and shortcuts: covered by T043, T044 and T034/T039 (nota-shortcuts)
- [X] T053 Engine behaviour change: N/A (UI only); the smoke test still gains the SnapGrid section (T001–T003, T009, T028, T036)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (T001–T002)**: none.
- **Foundational (T003–T011)**: after Setup; blocks every story. Inside the phase: T003 → T004 → T005, T006, T007 (same file, sequential) → T008 [P with T004–T007] → T009 → T010 → T011.
- **US1 (T012–T027)**: after Foundational.
- **US2 (T028–T035)**: after Foundational. It touches `SnapChip.cs` from T017, so do it after US1's T017–T018 in practice.
- **US3 (T036–T041)**: after Foundational. The menu rows (T038) need T030, and the command (T039) mirrors T031–T034, so do it after US2.
- **Polish (T042–T053)**: after the stories you ship.

### Within US1

T012 → T013 → (T014 ∥ T015) → T016 → T017 → T018; then T019 → T020 → T021 → (T022 ∥ T023) → T024 → T025 → T026 → T027.

### Parallel Opportunities

- T008 (Settings.cs) alongside T004–T007 (SnapGrid.cs).
- T014 and T015 (different arrangement partials).
- T022 (Expression lanes) alongside T023 (Quantize and MidiTools).
- Test tasks T028 and T036 alongside their stories' early implementation in other files.
- T040 alongside T038; T042 and T043.

## Parallel Example: User Story 1

```bash
# After T013 lands, the two arrangement grid loops are independent:
Task: "T014 grid lines from SnapGrid.GridLines in ArrangementView.LaneControl.cs"
Task: "T015 grid lines from SnapGrid.GridLines in ArrangementView.Controls.cs"

# After T021, the Expression lanes and Quantize are independent:
Task: "T022 Expression lanes use the resolved draw step in PianoRollView.Expression.cs"
Task: "T023 Quantize + MidiToolsView use Grids.Quantize"
```

---

## Implementation Strategy

### MVP first (US1)

1. Phases 1–2: the rule, its tests, persistence, `EditorGrids`.
2. Phase 3: both editors snap and draw from the rule, and the chips show "Auto · step".
3. **Stop and validate**: quickstart rows 1–3 and 11, plus `--grid`. This alone fixes the
   reported pain.

### Incremental delivery

1. + US2: the menu, ⌘1/⌘2/⌘4/⌘5 and the finer-than-visible mark.
2. + US3: triplets everywhere and ⌘3.
3. Polish: docs, screenshots, the gates. One PR for the whole feature (Constitution V).

## Notes

- Keep `SnapBar` (sections) and `AudioClipEditorView`'s own snap switch unchanged (research R12).
- Do not commit. The user reviews and commits the changes.

---

## Phase 7: Convergence

- [ ] T054 Make ⌘1–⌘5 work while the floating detail window is active on macOS: that window has no native menu, so the key equivalents never fire there. In `src/managed/Nota.App/MainWindow.Input.cs` `HandleFloatingKeyDown`, match ⌘/Ctrl + 1–5 (no Alt/Shift) and run `grid.narrower` / `grid.wider` / `grid.triplets` / `grid.snap` / `grid.adaptive` through `_commands.Run`, then mark the key handled. Keep Windows/Linux from running it twice (their `TryMenuShortcut` path). Per FR-013 and plan R9 (partial)
- [ ] T055 Snap the dragged note edge's absolute position in the piano roll, not only the drag delta. In `src/managed/Nota.App/PianoRollView.cs` (`Mode.Resize` / `Mode.ResizeLeft`, `_resizeAnchor`), the end (or start) of the grabbed note should land on `SnapBeat(XToBeat(p.X))`, and the other selected notes should move by the same delta. Today a note whose end is off the grid keeps that offset when you resize it. Per US1/AC3 and FR-008 (partial)
- [ ] T056 Decide and reconcile the arrangement toolbar label with FR-011, which requires "Auto · <step>" in Adaptive mode. The toolbar cell shows the step plus an "AUTO GRID" caption (`src/managed/Nota.App/SnapChip.cs`, `MainWindow.axaml` SnapChip cell). Either render "Auto · 1/8" in the cell, or amend FR-011 in spec.md to allow the two-line toolbar variant that is already documented in contracts/grid-ui.md. Per FR-011 (contradicts)
- [ ] T057 Ignore grid shortcuts while a text field has keyboard focus: on macOS the native menu fires ⌘1–⌘5 even in a TextBox. Add a guard at the top of `StepGrid` in `src/managed/Nota.App/MainWindow.Commands.cs`: return if the focused element is inside a `TextBox` or `NumericUpDown`. Per spec Edge Cases "Text entry" (partial)
- [ ] T058 Fix the snap controls' width to their widest label so they never shift while zooming. The toolbar value `MinWidth="46"` (`src/managed/Nota.App/MainWindow.axaml`) fits "1/16T" but not "2 bars" / "4 bars". The piano-roll chip's `MinWidth = 82` (`src/managed/Nota.App/SnapChip.cs`) is narrower than "Auto · 4 bars". Measure the widest label in the mono font, or raise the minimums. Per plan Risks (toolbar width) (partial)
