# Implementation Plan: MIDI Note Editing — Overwrite, In-Place Paste, Length & Velocity Keys

**Branch**: `007-midi-note-editing` (the spec directory; the work sits on `develop`) | **Date**: 2026-10-13 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/007-midi-note-editing/spec.md`

## Summary

A single pure rule, `NoteOverwrite` in `Nota.Application/Editing`, settles placed notes
against the rest of a clip ("placed material wins": same-pitch notes whose start is
covered go, notes running into a placed note are cut). The piano roll stops applying it
per edit. Instead it keeps a *pending placement* flag: moves, resizes, draws and pastes of
the selection set it, the notes underneath are only marked, and the rule runs once when
the selection is let go (selection change, focus loss, save, export). Paste always lands
on the copied notes' own beats. Shift+←/→ change the selected notes' length by one grid
step. Alt(+Shift)+↑/↓ change their velocity by ±10 (±1). Duplicate reuses the same rule.
The engine is not touched.

## Nota: Read Before Planning

- **UI work** → [`DESIGN.md`](../../DESIGN.md) (Ember) and the `nota-design` skill for
  the "will be overwritten" mark.
- **Anything** → [`CONTRIBUTING.md`](../../CONTRIBUTING.md): SPDX header on the new
  files, no new dependencies.
- **Shortcuts** → the `nota-shortcuts` skill (Settings → Shortcuts list), `nota-docs`
  for the piano-roll page, `nota-features` for FEATURES.md.

## Technical Context

**Language/Version**: C# / .NET 10 (managed app only)

**Primary Dependencies**: Avalonia (existing). No new dependency.

**Storage**: N/A. Notes are already persisted through the engine; the project format is
unchanged.

**Audio-Thread Impact**: None. This is a UI-only change. Edits reach the engine through the
existing `Commit` (`SetClipNotes` / `SetSessionNotes`) and `CommitLive` paths. While a
placement is pending, same-pitch notes may overlap in the engine, exactly as they can
today.

**Testing**: A new pure-logic section in `tests/Nota.SmokeTest` (`NoteEditingTests.cs`,
alone with `--notes`, also part of the full run) for the overwrite rule, length and
velocity steps. Interaction is checked in the real app (quickstart §2).

**Target Platform**: macOS, Windows, Linux desktop (⌘ = Ctrl and ⌥ = Alt off macOS).

**Project Type**: Desktop app (Avalonia).

**Performance Goals**: The marking is computed per frame for unselected notes against the
selected ones, O(n·s). This is negligible for clip sizes (hundreds of notes), and no
allocation happens in the render loop.

**Constraints**: Notes that already overlap in existing projects are not touched unless
the user places notes onto them (no settle without a pending placement). Undo granularity:
each key press is one step, and a settle is its own step.

**Scale/Scope**: 1 new file in `Nota.Application`, `PianoRollView.cs` (most of the
change), `MainWindow.Files.cs` (settle before save/export), 1 new test file + `Program.cs`,
Shortcuts list, docs.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design: still passes.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Real-time audio safety | ✅ Pass | No engine or audio-thread change |
| II. Boundary discipline | ✅ Pass | No new C ABI; existing `SetClipNotes`/`SetClipNotesLive` |
| III. Licensing hygiene | ✅ Pass | SPDX header on `NoteOverwrite.cs` and `NoteEditingTests.cs`; no dependency |
| IV. Verification gate | ✅ Pass | `scripts/build.sh` must pass; the smoke test gains a `NoteEditingTests` section |
| V. One logical change | ✅ Pass | One feature: piano-roll note editing. Four stories touch the same edit path |
| VI. Docs in sync | ✅ Planned | CHANGELOG, FEATURES.md, Shortcuts list, nota-docs piano-roll page (EN+RU) |
| VII. Spec-first | ✅ Pass | This spec, clarified 2026-10-13 |
| VIII. Rules not weakened | ✅ Pass | — |

## Design

See [research.md](research.md) for the decisions, [data-model.md](data-model.md) for the
rule and the pending state, and [contracts/piano-roll-keys.md](contracts/piano-roll-keys.md)
for the user-facing bindings.

### 1. `Nota.Application/Editing/NoteOverwrite.cs` (new)

Pure static, no Avalonia:

- `Fate(NotaNote note, IReadOnlyList<NotaNote> notes, IReadOnlyCollection<int> placed)` →
  `Kept` / `Cut(newLength)` / `Removed`, with the Duplicate rule (data-model §1).
- `Settle(IReadOnlyList<NotaNote> notes, IReadOnlyCollection<int> placed)` → the new note
  list and the new indices of the placed notes (so the selection can be remapped).
- `ResizeBy(...)` (length step, clamped to min length and clip end, start kept) and
  `VelocityBy(float v, int steps127)` (1–127 clamp, integer MIDI steps).

### 2. `PianoRollView.cs`

- **Pending placement**: `bool _placementPending`. It is set after a committed mouse
  move/resize (release with `_dragMoved`), `MoveSelection`, the new `ResizeSelection`, a
  double-click draw, and `PasteClipboard`. Velocity edits do not set it.
- **`public bool SettlePlacement()`**: if pending and the selection is non-empty, run
  `NoteOverwrite.Settle`, replace `_notes`, remap `_selection`, `CommitNotes()` (its own
  undo step), invalidate, and clear the flag. It is called:
  - before every selection change: pointer press that changes the selection (then
    re-hit-test, since indices may shift), Shift-click toggle, marquee start,
    double-click, Esc, ⌘A, paste, duplicate;
  - before whole-clip edits: Quantize, TransposeBy, remove out-of-scale, ApplyPreview;
  - on `NoteGrid` `LostFocus`;
  - by the host before save and export.
- `SetNotes` and `DeleteSelection` clear the flag without settling. A reload means the
  engine state wins (undo, recording), and deleting the placed notes leaves nothing to
  settle.
- **Paste**: settle, then anchor = `s_clipAnchor` always (the playhead branch is
  removed). Select the copies and set pending.
- **Duplicate**: settle first, then keep today's behaviour, with its inline loop
  replaced by `NoteOverwrite.Settle` over the copies.
- **Marking**: in `NoteGrid.Render`, when pending, unselected notes whose `Fate` is not
  `Kept` draw their doomed part (all of it for `Removed`, `[cut, end)` for `Cut`) faded,
  with a dashed outline in the track colour (research R4).
- **Keys** (`NoteGrid.OnKeyDown`, selection non-empty, no ⌘): Alt+↑/↓ → `NudgeVelocity(±10)`,
  Alt+Shift+↑/↓ → `±1`; Shift+←/→ → `ResizeSelection(±SnapStep)`. Plain ←/→ and
  (Shift+)↑/↓ are unchanged. Each press commits once (one undo step).

### 3. `MainWindow.Files.cs`

`_editorRoll?.SettlePlacement()` at the top of the project-save path (before
`_projects.Save`) and of `DoExportAsync`. Autosave does not settle (R5).

### 4. Tests — `tests/Nota.SmokeTest/NoteEditingTests.cs` (new) + `Program.cs`

A `--notes` flag and an entry in the full run, following the `SnapGridTests` pattern.
Cases: the spec's Story 1 scenarios, notes in the same selection never cut each other,
different pitches untouched, placed index remap, a copy exactly on its original
(original removed on settle only if the copy still covers its start), length step clamps,
velocity clamps 1/127 and integer steps.

### 5. Docs

CHANGELOG `[Unreleased]` (Changed: paste lands on the original, notes overwrite; Added:
length and velocity keys), FEATURES.md, Shortcuts list (nota-shortcuts), and the
nota-docs piano-roll page (EN+RU).

## Project Structure

### Documentation (this feature)

```text
specs/007-midi-note-editing/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/piano-roll-keys.md
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/managed/
├── Nota.Application/Editing/
│   └── NoteOverwrite.cs               # NEW — the overwrite rule, length/velocity steps
└── Nota.App/
    ├── PianoRollView.cs               # pending placement, settle, paste, keys, marking
    ├── MainWindow.Files.cs            # settle before save / export
    └── (Shortcuts list file — located by the nota-shortcuts skill)

tests/Nota.SmokeTest/
├── NoteEditingTests.cs                # NEW
└── Program.cs                         # --notes flag + full-run entry
```

**Structure Decision**: The rule goes in `Nota.Application`, so it is testable and free of
UI, next to `SnapGrid`. All interaction state stays in `PianoRollView`, which already owns
selection, drag and commit.

## Complexity Tracking

None. There are no constitution violations.
