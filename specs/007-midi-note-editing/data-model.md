# Data Model: MIDI Note Editing

No persisted data changes. `NotaNote` (`Nota.Application/Engine/EngineContracts.cs`):
`Pitch`, `StartBeat`, `LengthBeats`, `Velocity` (0..1 float), `ExprId`. A note is copied
whole (never rebuilt), so expression travels with it.

## 1. Overwrite rule (`NoteOverwrite`)

Inputs: the clip's note list and the set **P** of placed indices (the selection).

For each note **m** not in **P**, against every **c** in **P** with `c.Pitch == m.Pitch`:

| Condition (ε = 1e-9) | Fate of m |
|---|---|
| `c.Start − ε ≤ m.Start < c.End − ε` | **Removed** |
| `m.Start < c.Start` and `m.End > c.Start + ε` | **Cut** to `c.Start − m.Start` (the shortest over all such c) |
| otherwise | **Kept** |

Removal wins over a cut. Notes in **P** never change and never affect each other. Notes of
different pitch never interact. `Settle` returns the surviving notes in their original
order and the new indices of **P**.

This is exactly the rule `DuplicateSelection` applies today (copies = **P**).

## 2. Pending placement (in `PianoRollView`)

```text
          place (move/resize/draw/paste/Shift+←→)          place
 Idle ────────────────────────────────────────▶ Pending ◀────────┐
  ▲                                               │  └───────────┘
  │   settle: selection change, focus loss,       │
  │   whole-clip edit, save/export                │
  └──── NoteOverwrite.Settle + Commit ◀───────────┘
  ▲                                               │
  └──── discard: SetNotes (reload/undo), delete ◀─┘
```

- In **Pending**, rendering marks every unselected note whose fate is not Kept.
- Velocity edits and selection-preserving actions (clicking a selected note, zoom,
  scroll) leave the state unchanged.

## 3. Length step

For each selected note: `len' = clamp(len ± step, MinNoteLen, clipLength − start)`, with
`start` unchanged. `step = SnapStep` (snap on) or `MinNoteLen` (snap off). Notes already at
the clip end do not grow; the others still do. If no note changes, there is no commit.

## 4. Velocity step

`v' = clamp(round(v·127) + k, 1, 127) / 127`, with k ∈ {±10, ±1}. The same k applies to
every selected note. If no note changes (everything is at the clamp), there is no commit.

## 5. Note clipboard

Unchanged: notes normalized to beat 0 plus `s_clipAnchor` (the original clip-relative
start). Paste lands at `s_clipAnchor + offset` for each note. Notes starting at or past the
clip end are skipped, and overruns are trimmed to the clip end.
