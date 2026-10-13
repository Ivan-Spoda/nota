# Quickstart: Validate the Adaptive Snap Grid

How to prove the feature works after implementation. Rules and values are in
[data-model.md](data-model.md). Controls and shortcuts are in
[contracts/grid-ui.md](contracts/grid-ui.md).

## Prerequisites

- A working Nota build (BUILD.md § prerequisites).
- For the app checks: a scratch data folder, so the grid preference does not touch the
  real `settings.json`: `export NOTA_DATA_DIR=/tmp/nota-grid`.

## 1. Rule tests (fast loop)

```bash
dotnet run --project tests/Nota.SmokeTest -- --grid
```

Expected: `GRID PASSED`. The section covers:

- default settings at 28 px per beat in 4/4 resolve to 1/4 (FR-003);
- Narrow, Medium and Wide at a fixed zoom give strictly finer-to-coarser steps;
- clamping at 1/32 (or 1/32T) and 4 bars at the zoom extremes;
- triplet steps are 2/3 of straight ones, and bar steps ignore triplets;
- 3/4: every bar start is a snap point for 1/2 and 1/2T (research R4);
- `GridLines` and `SnapBeat` agree: every enumerated line snaps to itself and every snap
  result is an enumerated line (SC-001);
- Fixed 1/32 at 4 px per beat: `Draw` is coarser and `FinerThanVisible` is true; at
  240 px per beat it is false (FR-005a);
- ⌘1, ⌘2, ⌘3, ⌘4 and ⌘5 transitions, including stops at the ends and remembered
  density and step (data-model.md table);
- labels: "Auto · 1/8", "1/8T", "1 bar", "4 bars";
- `Settings` without the new fields loads the FR-016 defaults; a round trip keeps both
  editors' settings and the Quantize step separate.

## 2. Full gate

```bash
scripts/build.sh
```

Expected: green, including the existing palette checks. Every `grid.*` id is bound and
every ⌘1–⌘5 shortcut is listed in Settings → Shortcuts.

## 3. In the app (headless harness or a manual run)

With `NOTA_DATA_DIR` set and a project with one MIDI track and clip:

| # | Action | Expected |
|---|--------|----------|
| 1 | Default zoom, look at the arrangement snap cell | "Auto · 1/4"; beat lines drawn |
| 2 | Zoom in until eighths appear, drag a clip | The cell reads "Auto · 1/8"; the clip lands on a drawn eighth line |
| 3 | Zoom out to the whole song, drag a clip | The cell reads "Auto · 1 bar" or coarser; the clip lands on a bar line |
| 4 | Click the cell, pick 1/32, zoom far out | The label "1/32" is dimmed with the "finer than the visible grid" tooltip; the lines are thinned |
| 5 | ⌘5, then ⌘5 again | Auto with the previous density, then back to 1/32 |
| 6 | Open the clip, press ⌘3 in the piano roll | The roll chip reads "Auto · …T"; triplet lines; notes snap to them; the arrangement cell is unchanged (Q3) |
| 7 | Set QUANTIZE to 1/8T, zoom the roll in and out, press Quantize | Notes land on 1/8T regardless of zoom (Q2) |
| 8 | Click a toolbar button, press ⌘2 | It acts on the editor used last; the status line names it (R9) |
| 9 | ⌘4 in the arrangement | The magnet latch turns off with it; clips move freely; lines stay |
| 10 | Quit and relaunch | Both editors' settings and the Quantize step are as left (Q1); opening another project does not change them |
| 11 | Hold ⌥ while dragging | Free positioning in both editors |

Take screenshots of rows 1, 4 and 6 in both Ember variants for the PR (the nota-ui-verify
recipe in the agent memory describes the headless harness).

## 4. Docs sync

- `CHANGELOG.md` `[Unreleased]` → Added and Changed entries.
- The `nota-shortcuts`, `nota-features` and `nota-docs` skills (EN + RU).
