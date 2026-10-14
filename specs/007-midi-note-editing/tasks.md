---

description: "Task list for piano-roll note editing: overwrite, in-place paste, length and velocity keys"
---

# Tasks: MIDI Note Editing — Overwrite, In-Place Paste, Length & Velocity Keys

**Input**: Design documents from `specs/007-midi-note-editing/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/piano-roll-keys.md](contracts/piano-roll-keys.md), [quickstart.md](quickstart.md)

**Tests**: Included. The plan and quickstart §1 call for a pure-logic smoke-test section
(`NoteEditingTests`, `--notes`). Write each test task before its implementation and see it
fail first (a compile failure on the missing type counts).

**Organization**: By user story (spec.md): US1 notes overwrite (P1), US2 paste on the
original (P1), US3 length keys (P2), US4 velocity keys (P2).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1–US4

## Path Conventions

- Rule (pure): `src/managed/Nota.Application/Editing/`
- App and views: `src/managed/Nota.App/`
- Tests: `tests/Nota.SmokeTest/`
- Every new `.cs` file starts with the two-line SPDX header from CONTRIBUTING.md and a
  short comment header explaining why the file exists (match `SnapGrid.cs`).
- Line numbers are as of `develop` at 648d7ab and drift as you edit, so search by member
  name.

---

## Phase 1: Setup

- [X] T001 Create `tests/Nota.SmokeTest/NoteEditingTests.cs` with the SPDX header, a comment header ("Piano-roll note editing (specs/007-midi-note-editing): the overwrite rule, length and velocity steps. Also runnable alone: `dotnet run --project tests/Nota.SmokeTest -- --notes`."), and `internal static class NoteEditingTests { public static IEnumerable<(bool Ok, string Label)> Run() { yield break; } }`, following `SnapGridTests.cs`.
- [X] T002 Wire it into `tests/Nota.SmokeTest/Program.cs`:
  - add a `--notes` early-exit block printing `-- note editing --` and `NOTES PASSED` / `NOTES FAILED (n)`, next to the `--grid` block (around line 227);
  - in the full run, add `Console.WriteLine("-- note editing --"); foreach (var (ok, label) in NoteEditingTests.Run()) Check(ok, label);` right after the snap-grid section (around line 14548).

---

## Phase 2: Foundational (blocks every story)

- [X] T003 Create `src/managed/Nota.Application/Editing/NoteOverwrite.cs` (namespace `Nota.Application.Editing`, SPDX header, comment header citing the spec), with type stubs only: `enum NoteFateKind { Kept, Cut, Removed }`, `readonly record struct NoteFate(NoteFateKind Kind, double CutLength)`, and a `static class NoteOverwrite` whose methods throw `NotImplementedException`. US1, US3 and US4 fill in the bodies.

**Checkpoint**: `dotnet build src/managed/Nota.App/Nota.App.csproj` and the test project build.

---

## Phase 3: User Story 1 — Notes overwrite instead of stacking (P1) 🎯 MVP

**Goal**: Placed notes overwrite same-pitch notes once the selection is let go; until then
the notes underneath are only marked.

**Independent Test**: quickstart §2 rows 1 and 2, plus `--notes`.

### Tests (write first)

- [X] T004 [P] [US1] In `tests/Nota.SmokeTest/NoteEditingTests.cs`, add overwrite cases, each yielding one labelled check:
  - the spec's Story 1 scenarios 1–3 through `NoteOverwrite.Settle` (A cut to 0–1; A removed; A removed by a lengthened B);
  - different pitches untouched (scenario 4);
  - two placed notes overlapping each other are both kept unchanged;
  - an unplaced note fully inside a placed one is removed;
  - an unplaced note run into by two placed notes is cut at the earliest placed start;
  - `placedOut` indices point at the same notes after removals;
  - a copy exactly on its original (same start) removes the original;
  - `Fate` agrees with `Settle` for every unplaced note in these fixtures;
  - ε handling: an unplaced note ending exactly at a placed start (within 1e-9) is Kept.

### Implementation

- [X] T005 [US1] In `src/managed/Nota.Application/Editing/NoteOverwrite.cs`, implement the data-model.md §1 rule verbatim:
  - `public static NoteFate Fate(in NotaNote m, IReadOnlyList<NotaNote> notes, IReadOnlyCollection<int> placed)` — "`c.Start − ε ≤ m.Start < c.End − ε` → Removed; `m.Start < c.Start` and `m.End > c.Start + ε` → Cut to `c.Start − m.Start` (shortest over all such c); otherwise Kept"; "Removal wins over a cut"; same pitch only; ε = 1e-9. Allocation-free (it is called per frame from Render).
  - `public static void Settle(IReadOnlyList<NotaNote> notes, IReadOnlyCollection<int> placed, List<NotaNote> output, List<int> placedOut)` — placed notes unchanged, survivors in original order, `placedOut` the new indices of the placed notes.
  Run `dotnet run --project tests/Nota.SmokeTest -- --notes`; the T004 checks must pass.
- [X] T006 [US1] In `src/managed/Nota.App/PianoRollView.cs` `DuplicateSelection` (around line 461), replace the inline overwrite loop with `NoteOverwrite.Settle` over `_notes + copies`, with the copies as `placed`, and keep the behaviour identical (FR-009). Select the copies via `placedOut`.
- [X] T007 [US1] In `src/managed/Nota.App/PianoRollView.cs`, add the pending placement:
  - a `private bool _placementPending` field;
  - `public bool SettlePlacement()`: return false unless pending; clear the flag; if the selection is empty, return false; otherwise `NoteOverwrite.Settle(_notes, _selection, …)`. If nothing changed (same count and no cut), return false without committing. Otherwise replace `_notes`, rebuild `_selection` from `placedOut`, `Invalidate(); CommitNotes(); Changed?.Invoke();` (one undo step, FR-002) and return true;
  - a `private void MarkPlaced() => _placementPending = true;` helper.
- [X] T008 [US1] In `src/managed/Nota.App/PianoRollView.cs`, set pending after placements:
  - `NoteGrid.OnPointerReleased` when `was != Mode.Marquee && _dragMoved` (move, resize, resize-left);
  - `MoveSelection` after a successful move;
  - the double-click draw branch in `NoteGrid.OnPointerPressed` (the new note is selected).
- [X] T009 [US1] In `src/managed/Nota.App/PianoRollView.cs`, settle before every selection change and whole-clip edit:
  - In `NoteGrid.OnPointerPressed`, when the press will change the selection (double-click; Shift-click toggle; hit not in the selection; empty-space marquee), call `_o.SettlePlacement()` first and, if it returned true, re-run `HitTest(p, out edge)`, because indices may have shifted. A press on an already-selected note without Shift must not settle.
  - Also settle on Esc and ⌘A in `NoteGrid.OnKeyDown`, and at the start of `DuplicateSelection`, `Quantize`, `TransposeBy` and `RemoveOutOfScaleNotes`. *(Implemented: `ShowPreview`/`ApplyPreview` clear the flag instead of settling — the tool computed its result from the unsettled notes and its captured indices would go stale; focus loss on clicking the tools rail settles first anyway.)*
- [X] T010 [US1] In `src/managed/Nota.App/PianoRollView.cs`, clear `_placementPending` without settling in `SetNotes` (a reload or undo means the engine wins) and in `DeleteSelection`.
- [X] T011 [US1] In `src/managed/Nota.App/PianoRollView.cs` `NoteGrid`, override `OnLostFocus` to call `_o.SettlePlacement()` (research R5: this also covers switching clips, since the old roll loses focus while its `Commit` still targets its own clip).
- [X] T012 [US1] In `src/managed/Nota.App/MainWindow.Files.cs`, call `_editorRoll?.SettlePlacement();`:
  - before `_projects.Save(` (around line 306), and before the save-as picker so a cancelled dialog has still settled;
  - at the top of `DoExportAsync` (around line 51).
  Autosave (`MainWindow.Recovery.cs`) is left alone (R5). Check that project-version saves (`MainWindow.History.cs`) go through the same save method; if not, add the call there too.
- [X] T013 [US1] In `src/managed/Nota.App/PianoRollView.cs` `NoteGrid.Render` (the note loop around line 1034), when `_o._placementPending` and the note is not selected and not a ghost, compute `NoteOverwrite.Fate(n, notes, _o._selection)`:
  - `Removed`: draw the whole note at ~25% of its normal alpha with a 1 px dashed outline in the track colour;
  - `Cut`: draw `[start, start+cutLen)` normally and the rest faded and dashed.
  Use static pens and brushes cached per track colour (no per-frame allocation beyond what the loop already does), keep it distinguishable from the brass tool-preview ghost, and check against `DESIGN.md` with the nota-design skill (research R4).

**Checkpoint**: quickstart rows 1–2 behave; Duplicate is unchanged; `--notes` passes.

---

## Phase 4: User Story 2 — Copy/paste lands on the original (P1)

**Goal**: ⌘V always lands on the copied notes' own beats, with the copies selected and
pending.

**Independent Test**: quickstart §2 rows 3–5.

- [X] T014 [US2] In `src/managed/Nota.App/PianoRollView.cs` `PasteClipboard` (around line 685):
  - call `SettlePlacement()` first;
  - replace `double anchor = _playheadBeat >= 0 ? SnapBeat(_playheadBeat) : s_clipAnchor;` with `double anchor = s_clipAnchor;`;
  - after selecting the pasted notes, call `MarkPlaced()`, so the paste itself overwrites nothing (FR-003a);
  - update the doc comment ("on top of the copied notes' original position").
  Skip and trim at the clip end as today (data-model §5).
- [X] T015 [US2] In `src/managed/Nota.App/MainWindow.Menu.cs` (the paste handler around line 162), confirm the Edit-menu paste reaches the same `PasteClipboard`. No change is expected; record the result in the task. *(Confirmed: `OnMenuPaste` → `ActiveRoll().PasteClipboard()`.)*

**Checkpoint**: US1 and US2 together give the requested "paste on top, then move" workflow.

---

## Phase 5: User Story 3 — Stretch and shrink from the keyboard (P2)

**Goal**: ⇧←/⇧→ shorten or lengthen every selected note by one grid step.

**Independent Test**: quickstart §2 row 6, plus `--notes`.

### Tests (write first)

- [X] T016 [P] [US3] In `tests/Nota.SmokeTest/NoteEditingTests.cs`, add `NoteOverwrite.ResizeBy` cases:
  - +step grows every note with starts unchanged;
  - −step on a note of length `minLen` leaves it at `minLen`;
  - a note ending at the clip end does not grow while the others do;
  - it returns false when nothing changed.

### Implementation

- [X] T017 [US3] In `src/managed/Nota.Application/Editing/NoteOverwrite.cs`, implement `public static bool ResizeBy(List<NotaNote> notes, IEnumerable<int> indices, double delta, double minLen, double clipLength)` per data-model §3: "`len' = clamp(len ± step, MinNoteLen, clipLength − start)`, with `start` unchanged".
- [X] T018 [US3] In `src/managed/Nota.App/PianoRollView.cs`, add `public bool ResizeSelection(int dir)`:
  - step = `SnapGridState.SnapOn ? SnapStep : MinNoteLen` (R7);
  - call `ResizeBy(_notes, _selection, dir * step, MinNoteLen, _lengthBeats)`;
  - on change, `Invalidate(); CommitNotes(); Changed?.Invoke(); MarkPlaced();`.
- [X] T019 [US3] In `src/managed/Nota.App/PianoRollView.cs` `NoteGrid.OnKeyDown` (around line 1417), inside the `!meta && selection` block, handle `Key.Left` and `Key.Right` with `shift` (and no Alt) as `_o.ResizeSelection(-1 / +1)` before the plain nudge cases. Plain ←/→ stay as they are.

---

## Phase 6: User Story 4 — Velocity from the keyboard (P2)

**Goal**: ⌥↑/⌥↓ change velocity by ±10, and ⌥⇧↑/⌥⇧↓ by ±1.

**Independent Test**: quickstart §2 rows 7–8, plus `--notes`.

### Tests (write first)

- [X] T020 [P] [US4] In `tests/Nota.SmokeTest/NoteEditingTests.cs`, add `NoteOverwrite.VelocityBy` cases: 40/127 + 10 → 50/127; 125/127 + 10 → 127/127; 3/127 − 10 → 1/127; 0.8f + 1 → (round(0.8·127)+1)/127; the result is always on an integer MIDI step.

### Implementation

- [X] T021 [US4] In `src/managed/Nota.Application/Editing/NoteOverwrite.cs`, implement `public static float VelocityBy(float v, int steps127)` per data-model §4: "`v' = clamp(round(v·127) + k, 1, 127) / 127`".
- [X] T022 [US4] In `src/managed/Nota.App/PianoRollView.cs`, add `public bool NudgeVelocity(int steps127)`. It applies `VelocityBy` to every selected note; if any changed, `Invalidate(); CommitNotes(); Changed?.Invoke();`. It does **not** touch `_placementPending`. The velocity lane and the `SelectionInfo` text refresh through `Changed`/`Invalidate`.
- [X] T023 [US4] In `src/managed/Nota.App/PianoRollView.cs` `NoteGrid.OnKeyDown`, read `bool alt = (e.KeyModifiers & KeyModifiers.Alt) != 0;`. In the selection block, check Alt **before** Shift: `Key.Up`/`Key.Down` with Alt → `_o.NudgeVelocity(shift ? ±1 : ±10)`. Leave Alt+←/→ unhandled. Without a selection, the keys fall through (contract).

---

## Phase 7: Polish & Cross-Cutting

- [X] T024 [P] In `src/managed/Nota.App/PreferencesWindow.cs` (the "PIANO ROLL" group around line 1074), add rows per contracts/piano-roll-keys.md:
  - `("⇧←  ⇧→", "Shorten / lengthen the notes by the grid")`;
  - `("⌥↑  ⌥↓", "Velocity +10 / −10 · ⌥⇧ by 1")`;
  - change ⌘C ⌘X ⌘V to "Copy / cut / paste notes · paste lands on the originals";
  - add `("Esc", "Deselect · applies the overwrite of moved notes")`.
  Run the nota-shortcuts skill to confirm the list matches the handlers.
- [X] T025 [P] Add `CHANGELOG.md` `[Unreleased]` entries:
  - Changed: "Notes moved, stretched, drawn or pasted onto notes of the same pitch now replace them instead of stacking — applied when you deselect; until then the notes underneath are marked." and "⌘V in the piano roll always pastes on top of the copied notes."
  - Added: "⇧←/⇧→ shorten or lengthen the selected notes by the grid; ⌥↑/⌥↓ change their velocity by 10 (⌥⇧ by 1)."
- [X] T026 [P] Update `FEATURES.md` with the nota-features skill.
- [X] T027 [P] Update the piano-roll page in `../nota-docs` (EN + RU) and regenerate the shortcuts page with the nota-docs skill.
- [X] T028 Run `scripts/build.sh` (builds native + managed and runs the full smoke test, including `-- note editing --`); it must exit green.
- [X] T029 Walk quickstart §2 rows 1–9 in the real app (the memory's UI verify recipe, `NOTA_DATA_DIR` isolation) and take screenshots of row 2 (the mark) and row 9 (Shortcuts).
- [X] T030 Recheck the spec's Nota Compliance Checklist: SPDX headers on `NoteOverwrite.cs` and `NoteEditingTests.cs`, no new dependency, CHANGELOG present.

---

## Dependencies & Execution Order

- **Setup (T001–T002)** → **Foundational (T003)** → stories.
- **US1 (T004–T013)** comes first: US2 and US3 call `MarkPlaced`/`SettlePlacement`.
- **US2 (T014–T015)** depends on T007.
- **US3 (T016–T019)** depends on T003 and T007 (`MarkPlaced`). Its rule work (T016–T017) can run alongside US1.
- **US4 (T020–T023)** depends only on T003, so it is independent of US1–US3 at the rule level. T023 touches the same `OnKeyDown` as T019, so do them one after the other.
- **Polish** comes after the stories it documents.

Inside `PianoRollView.cs` everything is sequential (same file).

## Parallel Examples

```text
# After T003:
T004 (overwrite tests)   T016 (resize tests)   T020 (velocity tests)   — same test file: write in one sitting, or one after another
T017 (ResizeBy)          T021 (VelocityBy)                              — same file as T005: one after another
# Polish:
T024  T025  T026  T027   — different files
```

## Implementation Strategy

1. **MVP = US1 + US2** (both P1): overwrite with deferred settle, and paste on the
   original. This is the user's core workflow ("paste on top, move with arrows").
2. Then US3 (length keys), then US4 (velocity keys). Each is a small key handler over a
   tested pure step.
3. Polish: shortcuts list, CHANGELOG, FEATURES, docs, `scripts/build.sh`, app walkthrough.

---

## Phase 8: Convergence

- [X] T031 Settle the pending placement before the velocity lane and the MPE expression lanes change the selection. Without this, a stem click swaps the selection while `_placementPending` stays true, and the next settle treats the clicked note as placed. In `src/managed/Nota.App/PianoRollView.cs`, `VelocityLane.OnPointerPressed`: call `_o.SettlePlacement()` before `_selection` changes (and re-run `Nearest(p)` if it returned true). Do the same in `src/managed/Nota.App/PianoRollView.Expression.cs` `ExpressionLane.TargetsAt` before it clears or adds to `_selection`. Neither lane is focusable, so the grid's `LostFocus` does not fire. Add a `/tmp/noteharness` case: paste, press →, then click another note's stem — that note must not overwrite anything — per FR-003 (contradicts)
- [X] T032 Settle instead of silently dropping a pending placement when the roll is reloaded or replaced for any reason other than undo/redo. In `src/managed/Nota.App/MainWindow.Detail.cs`, `ReloadEditorNotes` (reached from `TracksChanged`, `RecordingTick` and pattern-view edits) and both `_editorRoll = roll;` sites (lines ~437 and ~616, where a clip editor that never had focus can be replaced): call `_editorRoll?.SettlePlacement()` first. Keep `RefreshAfterUndoRedo` (`MainWindow.Menu.cs`) discarding, because there the engine state wins. That way a later save never stores the stacked notes, per SC-001 / FR-003 (partial)
- [X] T033 Reconcile the clip-tool preview with plan §2: `src/managed/Nota.App/MidiToolsView.cs` (line ~160) captures `_roll.SourceNotes` / `SelectionIndices` without settling, and `ShowPreview` / `ApplyPreview` drop the pending flag (the tasks.md T009 note). Call `_roll.SettlePlacement()` right before that capture, so a tool never builds on stacked notes even when the grid kept focus, per plan: settle before whole-clip edits (contradicts)
  *(Implemented in part: the two `_editorRoll = roll;` sites now settle the outgoing roll. `ReloadEditorNotes` is deliberately left discarding. Every caller (pattern-view edit, recording tick, `TracksChanged`) means the engine changed underneath, so settling there would commit the roll's stale note list over that external change. That is the plan's R5 / §2 rule "a reload means the engine state wins". SC-001 holds for edits made in the roll; a placement interrupted by an external reload keeps the engine's notes as they are.)*
  *(T033 implemented at `MidiToolsView.Preview()`, not at `Resync()`: `Resync` runs after every roll edit and would settle immediately.)*
