# UI Contract: Snap Grid Controls and Commands

What the user and the command palette can rely on. The palette smoke test checks the
catalog side of this contract: every id is bound and every shortcut is listed in
Settings → Shortcuts.

## Catalog commands (`CommandCatalog`, category "Grid")

| Id | Title | Gesture | Aliases |
|----|-------|---------|---------|
| `grid.narrower` | Narrow Grid | ⌘1 | finer smaller snap |
| `grid.wider` | Widen Grid | ⌘2 | coarser larger snap |
| `grid.triplets` | Toggle Triplet Grid | ⌘3 | triplet 1/8T swing |
| `grid.snap` | Toggle Snap to Grid | ⌘4 | magnet free |
| `grid.adaptive` | Toggle Adaptive Grid | ⌘5 | auto zoom fixed |

- ⌘ means Cmd on macOS and Ctrl on Windows and Linux (`Commands.Parse`).
- Target editor (research R9): the piano roll if the detail area was used last and a
  MIDI clip editor is showing, otherwise the arrangement.
- Each command sets the status line to `"<Editor> grid: <control label>"`, for example
  "Arrangement grid: Auto · 1/4" or "Piano roll grid: Snap off" (FR-014).
- The commands are always available; the palette never greys them out.
- Menu: Edit → Grid submenu with the five items, in this order.

## Settings → Shortcuts

A new group `GRID (ARRANGEMENT OR PIANO ROLL — WHICHEVER YOU USED LAST)`:

| Keys | Description |
|------|-------------|
| ⌘1   ⌘2 | Narrow / widen the grid · in Auto, change the density |
| ⌘3 | Triplet grid on / off |
| ⌘4 | Snap to grid on / off |
| ⌘5 | Switch between the adaptive (Auto) and a fixed grid |
| ⌥ while dragging | Move freely, ignoring the grid |

The existing piano-roll row "← → Move notes by the grid" stays and now means the step in
effect.

## Snap control (one per editor)

| Editor | Location | Replaces |
|--------|----------|----------|
| Arrangement | The existing toolbar cell `SnapChip` / `SnapLabel` | Forward-only cycling (`OnCycleSnap`) |
| Piano roll | A new chip in the clip-editor header's right group, before Scale | The GRID dropdown's role as the snap step |

### Label states

The piano-roll chip reads the full label on one line. The arrangement's toolbar cell keeps
its two-line layout to stay narrow: the step on top ("1/8") and the caption **AUTO GRID**
(Adaptive) or **GRID** (Fixed) below it.

| State | Label | Notes |
|-------|-------|-------|
| Adaptive | `Auto · 1/8` | The step updates live while zooming |
| Adaptive, triplets | `Auto · 1/8T` | |
| Fixed | `1/8` | |
| Fixed, triplets | `1/8T` | |
| Fixed, finer than drawn | `1/32` in the dimmed (secondary) text colour | Tooltip "1/32 — finer than the visible grid · zoom in to see it" (FR-005a) |
| Snap off | Current label struck through in the disabled colour | Tooltip "Snap off — items move freely (⌘4)" |

The default tooltip lists the shortcuts: "Snap grid · ⌘1 ⌘2 narrow/widen · ⌘3 triplets ·
⌘4 snap · ⌘5 auto". Colours and type come from the Ember tokens (`DESIGN.md`).

### Menu (opens on click)

```text
✓ Auto · Narrow
  Auto · Medium
  Auto · Wide
  ─────────────
  1/32
  1/16
  1/8
  1/4
  1/2
  1 bar
  2 bars
  4 bars
  ─────────────
  Triplets                ⌘3
  Snap to grid            ⌘4
```

- One check mark on the active mode row (an Auto row or a fixed step). Triplets and
  Snap to grid show their own on/off check.
- With triplets on, the fixed rows read 1/32T … 1/2T; the bar rows stay unchanged.
- Picking an Auto row or a step applies immediately and turns snap on (data-model.md).

### Arrangement toolbar latch

`SnapToggle` (the magnet) stays. It mirrors and sets `ArrangementGrid.SnapOn`, so it, ⌘4
and the menu's "Snap to grid" are the same switch.

## Piano roll inspector

The clip inspector's GRID section becomes QUANTIZE: a dropdown with 1/32 … 1/2 and
1/32T … 1/2T (default 1/16), the Quantize button, and the existing STRENGTH slider.
The dropdown sets `Settings.QuantizeStep` (FR-008a) and nothing else.
