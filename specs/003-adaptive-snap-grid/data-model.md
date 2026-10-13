# Data Model: Adaptive Snap Grid

Phase 1 of [plan.md](plan.md). All types are managed-only and UI-free unless noted.
Nothing here crosses the C ABI or is written to project files.

## GridStep (value)

One rung of the ladder (research R3).

| Field | Type | Notes |
|-------|------|-------|
| `Index` | int 0..7 | 0 = 1/32 … 4 = 1/2, 5 = 1 bar, 6 = 2 bars, 7 = 4 bars |
| `Triplet` | bool | Only meaningful for `Index` 0..4; always false for bar steps |

Derived:

- `Beats(beatsPerBar)`: straight 0.125, 0.25, 0.5, 1, 2, then 1, 2 and 4 bars ×
  `beatsPerBar`. Triplet = straight × 2/3.
- `Label`: "1/32" … "1/2", "1 bar", "2 bars", "4 bars"; triplets add "T" ("1/8T").

Validation: `Index` is clamped to 0..7. `Triplet` is forced false when `Index ≥ 5`.

## GridDensity (enum)

`Narrow` | `Medium` | `Wide`. Thresholds: 8 / 16 / 32 px (research R2).

## GridSetting (persisted, one per editor)

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| `Adaptive` | bool | true | false = Fixed |
| `Density` | GridDensity | Medium | Remembered while Fixed (FR-013, ⌘5) |
| `FixedIndex` | int | 3 (1/4) for the arrangement, 1 (1/16) for the piano roll | Remembered while Adaptive |
| `Triplets` | bool | false | Applies in both modes |
| `SnapOn` | bool | true | Off = free positioning; lines still drawn (FR-010) |

Stored as `Settings.ArrangementGrid` and `Settings.PianoRollGrid` in `settings.json`.
Unknown or out-of-range values fall back to the per-field default (R7). The two
instances are fully independent (FR-001, Clarification Q3).

### Transitions (SnapGrid operations; each returns a new GridSetting)

| Operation | Adaptive | Fixed |
|-----------|----------|-------|
| `Narrower` (⌘1) | Density one notch finer: Wide → Medium → Narrow; stops at Narrow | `FixedIndex − 1`; stops at 0 |
| `Wider` (⌘2) | Density one notch coarser: Narrow → Medium → Wide; stops at Wide | `FixedIndex + 1`; stops at 7 |
| `ToggleTriplets` (⌘3) | Flip `Triplets` | Flip `Triplets` |
| `ToggleSnap` (⌘4) | Flip `SnapOn`; mode untouched | Flip `SnapOn`; mode untouched |
| `ToggleAdaptive` (⌘5) | → Fixed, keeping `FixedIndex` | → Adaptive, keeping `Density` |
| Menu pick "Adaptive · X" | `Adaptive = true`, `Density = X` | same |
| Menu pick a fixed step | `Adaptive = false`, `FixedIndex = i` | same |

Picking a grid value from the menu (Adaptive · X or a fixed step) also sets
`SnapOn = true`: choosing a value while snap is off means the user wants to snap to it.
The shortcuts other than ⌘4 leave `SnapOn` as it is.

## ResolvedGrid (derived, not stored)

`SnapGrid.Resolve(GridSetting, pixelsPerBeat, beatsPerBar)` returns:

| Field | Type | Meaning |
|-------|------|---------|
| `Snap` | GridStep | The step in effect: used by `SnapBeat`, nudges and paste |
| `Draw` | GridStep | The finest drawn line step (equal to `Snap` in Adaptive mode) |
| `FinerThanVisible` | bool | Fixed only: `Draw` is coarser than `Snap` (FR-005a) |
| `SnapOn` | bool | Copied from the setting |

Rules:

- **Adaptive**: the smallest `Index` (in the setting's triplet family) whose
  `Beats × pixelsPerBeat ≥ threshold(Density)`, clamped to 0..7. `Draw = Snap`.
- **Fixed**: `Snap = (FixedIndex, Triplets)`. `Draw` is the smallest `Index ≥ FixedIndex`
  meeting the Medium threshold (16 px), clamped to 7.
- In 4/4 at the arrangement's default 28 px per beat with default settings:
  `Snap = Draw = 1/4` (FR-003).

## SnapGrid.SnapBeat (behaviour)

`SnapBeat(beat, step, beatsPerBar)` returns the nearest grid position (research R4):

- Sub-bar steps: for the bar containing `beat`, the positions are
  `barStart + n·step` (n ≥ 0, `n·step < barLength − ε`) and the next bar start.
- Bar steps: multiples of `k·beatsPerBar` from beat 0.
- Never negative. Callers keep their existing `Math.Max(0, …)` and magnetic logic.

`GridLines(viewStart, viewEnd, step, beatsPerBar)` enumerates the same positions for
drawing, so lines and snap points come from one source (FR-005).

## Quantize step (persisted, piano roll only)

`Settings.QuantizeStep`: a `GridStep` restricted to sub-bar steps (`Index` 0..4, triplet
allowed). Default 1/16 straight. It is independent of `PianoRollGrid` (FR-008a). The roll
quantizes to `Beats(beatsPerBar)` of this value. Bar steps are not offered for Quantize.

## EditorGrids (Nota.App, live state; research R8)

| Member | Notes |
|--------|-------|
| `Arrangement`, `PianoRoll` | Current `GridSetting`s |
| `Quantize` | Current Quantize step |
| `Set(EditorKind, GridSetting)`, `SetQuantize(GridStep)` | Updates, saves `Settings`, raises `Changed` |
| `event Action<EditorKind> Changed` | Views redraw; snap controls relabel |

One instance per app: `EditorGrids.Shared`, on the registered `ISettingsService` (the
toolbar view comes from XAML and rolls are built in several places, so a shared instance
replaced passing one through `MainWindow`).
