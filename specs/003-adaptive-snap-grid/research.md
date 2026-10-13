# Research: Adaptive Snap Grid

Phase 0 of [plan.md](plan.md). Each entry resolves an open point from the spec's
Assumptions or the plan's Technical Context.

## R1 — Where the step rule lives

- **Decision**: A pure, UI-free `SnapGrid` module in `Nota.Application/Editing/`. It
  covers the ladder, step resolution, snapping a beat, labels and the shortcut
  transitions. Both editors call it, and the smoke test covers it directly.
- **Rationale**: The rule is shared by two views and is the part most likely to regress
  (FR-005, SC-001). `Nota.Application` already hosts small pure models with their own
  smoke-test sections (`TapTempo` → `TapTempoTests`, `--tap`). The smoke test does not
  reference `Nota.App`, so logic left in the views could not be tested.
- **Alternatives considered**: Keeping `GridStepBeats()` in `PianoRollView` and copying
  it into `ArrangementView` was rejected: two copies drift, and that drift is the bug
  being fixed. A shared static helper in `Nota.App` was rejected because it cannot be
  tested.

## R2 — Density thresholds (the "about 6 / 12 / 24 px" assumption)

- **Decision**: One set of thresholds for both editors. Each is the minimum on-screen
  spacing between the finest lines: Narrow 8 px, Medium 16 px, Wide 32 px. The thresholds
  double from one notch to the next, so each ⌘1/⌘2 press in Adaptive mode moves the step
  by exactly one ladder notch.
- **Rationale**: FR-003 requires 1/4 at the arrangement's default zoom (28 px per beat).
  With Medium at 16 px, 1/8 (14 px) fails and 1/4 (28 px) passes. The spec's 12 px would
  give 1/8, so it is rejected. The piano roll fits a clip to its width: a 1-bar clip in
  about 1000 px is about 250 px per beat, a 4-bar clip about 62 px per beat. These
  resolve to 1/32 and 1/8, bracketing the old fixed default 1/16 as the spec assumed.
  One set keeps "Medium" meaning the same thing in both editors.
- **Alternatives considered**: A larger base for the piano roll (24/48 px). Checked
  against the same examples, it gives the same steps (1/32 and 1/8), so it adds a second
  constant for no visible difference. A fixed 9 px, as in today's roll visuals, is denser
  and contradicts FR-003 in the arrangement.
- **Tuning**: The values are constants in `SnapGrid` and are checked against FR-003
  during implementation. If they change, update this entry.

## R3 — Ladder and triplet values

- **Decision**: The straight ladder, in beats (a beat is a quarter note): 1/32 = 0.125,
  1/16 = 0.25, 1/8 = 0.5, 1/4 = 1, 1/2 = 2, 1 bar, 2 bars, 4 bars. A triplet step is
  2/3 of the straight value: 1/32T = 1/12, 1/16T = 1/6, 1/8T = 1/3, 1/4T = 2/3,
  1/2T = 4/3. Bar steps have no triplet form. The piano roll's 1/64 is dropped (spec
  Assumptions). The roll's old "1/1" becomes "1 bar".
- **Rationale**: These are the musical values in FR-002. Beats-as-quarters matches the
  engine's beat unit and the existing `GridValues` array in `ClipPropsView`.
- **Alternatives considered**: Dotted values (out of scope), and 1/64 (finer than any
  zoom can draw at the arrangement's 240 px per beat maximum: 240/16 = 15 px, borderline,
  and unused).

## R4 — Bar anchoring and odd meters

- **Decision**: Grid positions are anchored to each bar start: within a bar, the positions
  are `barStart + n·step` for every n with `n·step < barLength`, plus the next bar start.
  `SnapBeat` returns the nearest of these. Bar steps (1, 2, 4 bars) are multiples of the
  bar length counted from beat 0.
- **Rationale**: In 3/4, a 1/2 step (2 beats) or a 1/2T step (4/3 beats) does not divide
  the bar. Anchoring each bar keeps every bar line a snap point and keeps lines and
  snapping identical (FR-005). In 4/4, every ladder step divides the bar, so anchoring
  changes nothing.
- **Alternatives considered**: A global lattice from beat 0 (bar lines stop being snap
  points in odd meters). Skipping steps that do not divide the bar (adaptive would jump
  over notches, which breaks the one-notch-per-press rule in R2).

## R5 — Time signature in the piano roll

- **Decision**: Pass beats-per-bar into `PianoRollView` (the same value
  `ArrangementView.BeatsPerBar` gets from the transport). Replace the hard-coded `4` in
  `GridPen` and `GridLabel`.
- **Rationale**: "1 bar" must mean the project's bar (spec Edge Cases). The roll
  currently assumes 4/4 for bar lines and ruler labels.
- **Alternatives considered**: Leaving the roll in 4/4 (wrong bar steps in 3/4 or 6/8).

## R6 — Fixed step too fine to draw (Clarification Q4)

- **Decision**: `Resolve` returns two steps. `Snap` is the step used for snapping.
  `Draw` is the finest drawn step. In Adaptive mode they are equal. In Fixed mode,
  `Draw` is the fixed step if its spacing meets the Medium threshold (16 px). Otherwise
  it is the finest ladder step at least as coarse as the fixed step that meets the
  threshold (same triplet family while sub-bar). `FinerThanVisible = Draw > Snap` drives
  the control's mark (FR-005a).
- **Rationale**: Readable grid and honest snapping, as clarified. Medium is used as the
  threshold for the thinning because density is an Adaptive-only setting.
- **Alternatives considered**: Thinning at the remembered density (an invisible setting
  would change Fixed-mode visuals).

## R7 — Persistence (Clarification Q1)

- **Decision**: Three new properties on `Nota.Application.Settings`:
  `ArrangementGrid`, `PianoRollGrid` (both `GridSetting`), and `QuantizeStep`. They are
  saved through `ISettingsService.Save()` into `settings.json` in `NotaPaths.DataDir`.
  Missing properties deserialize to the defaults (FR-016). A value that does not parse
  falls back to the default per field.
- **Rationale**: This is the existing app-wide store. It already tolerates older or
  missing fields (`catch → new Settings()`), it is isolated in tests by `NOTA_DATA_DIR`,
  and project files are untouched (FR-017).
- **Alternatives considered**: Per-project sidecars like `sections.json` (rejected in Q1).

## R8 — Live state shared by rebuilt piano rolls and the pop-out window

- **Decision**: A small app-level `EditorGrids` object in `Nota.App` holds the two live
  `GridSetting`s and the Quantize step. It loads them from `Settings` at start-up, saves
  on every change, and raises `Changed(EditorKind)`. `ArrangementView` and every
  `PianoRollView` read from it and redraw on `Changed`. Each snap control rebuilds its
  label on `Changed`.
- **Rationale**: `MainWindow.Detail.cs` builds a new `PianoRollView` per clip (arrangement
  clips and session slots) and the editor can float in `DetailWindow`. Settings kept as
  view fields would reset on every clip switch, which is how today's GRID dropdown
  behaves (`_gridIndex = 4` per `ClipPropsView`).
- **Alternatives considered**: Pushing setters into each view from `MainWindow`, as
  `Timeline.SnapBeats` is set today (every new roll would have to be re-seeded, and the
  floating window would need its own wiring).

## R9 — Which editor a shortcut targets

- **Decision**: The shortcut acts on the piano roll when the detail panel or the detail
  window was the last area used (`_detailWasLastFocused`, already maintained for Tab) and
  a MIDI clip editor is showing. Otherwise it acts on the arrangement. The status line
  names the target: "Piano roll grid: Auto · 1/16".
- **Rationale**: This matches the spec's focus rule and its "last worked in" fallback,
  and reuses the existing signal instead of adding focus tracking.
- **Alternatives considered**: Only the keyboard-focused control (fails after a toolbar
  click, which the spec rules out).

## R10 — Shortcut keys

- **Decision**: ⌘1 narrower, ⌘2 wider, ⌘3 triplets, ⌘4 snap, ⌘5 adaptive (Ctrl on
  Windows and Linux). They are declared in `CommandCatalog` under a new "Grid" category
  and bound in `MainWindow.Commands.cs`. They appear in a new Edit → Grid submenu, which
  gives native key equivalents on macOS and the `TryMenuShortcut` path on Windows and
  Linux. They are also listed in Preferences → Shortcuts under a new GRID group.
- **Rationale**: No catalog command, menu item or key handler uses ⌘/Ctrl + 1–5 today
  (only bare 0 is bound). The palette smoke test already enforces that every catalog
  shortcut is listed in Settings → Shortcuts. These are also the keys Ableton users
  expect.
- **Alternatives considered**: ⌥-based keys (⌥ is the snap-bypass modifier while
  dragging; overloading it is confusing).

## R11 — The MIDI clip tools' grid

- **Decision**: The clip-tools panel (`MidiToolsView`) passes the Quantize step, not the
  snap step, as `MidiToolContext.Grid`. This value is used by the Warp "Snap to Grid"
  shape and a generator's fallback gap.
- **Rationale**: Like Quantize (Clarification Q2), a tool rewrites notes. Its result must
  not change with the zoom. Today both read the same `_roll.Grid`, so keeping them
  together preserves current behaviour.
- **Alternatives considered**: The snap step in effect (zoom-dependent output).

## R13 — The audio clip editor (US4, Clarifications Q6–Q7)

- **Decision**: `AudioClipEditorView` reads the piano roll's setting
  (`EditorKind.PianoRoll`, used for "the clip editor") and gets the same `SnapChip` in
  place of its Snap switch. It has two beat axes:
  - **warp beats** (`BeatToX`, mirrored when reversed) for the grid lines, the trim
    brackets and the warp markers, at `ppb = zoom · width / TotalBeats`;
  - **played clip beats** (`EnvBeatToX`, never mirrored) for the envelope, at
    `ppb = zoom · width / clipBeats`.
  Each axis resolves its own `ResolvedGrid`. In Envelope mode the lines are drawn on the
  envelope axis, otherwise on the warp axis. The header chip shows the axis that is
  drawn. Bars are 4 beats, as everywhere in the app (the editor's `BeatsPerBar` constant).
- **Rationale**: Lines and snapping must agree (FR-005) on whichever timeline the user is
  editing. A trimmed clip's played beats are offset from its warp beats, so one shared
  axis would put envelope points between the lines.
- **Alternatives considered**: Warp axis for everything (envelope points off the drawn
  lines on trimmed clips). A third, separate setting (rejected in Q6).
- **Shortcut target**: `GridTarget()` returns the clip editor when the detail area was used
  last and either a MIDI or an audio clip editor is open. The status line reads "Clip
  editor grid: …" for both, since they share the setting.

## R12 — Out-of-scope neighbours noted during research

- Song sections keep `SnapBar` (whole bars), per the spec.
- MCP tools and Nota Remote do not expose the snap grid; unchanged.
