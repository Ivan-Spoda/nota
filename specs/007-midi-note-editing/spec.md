# Feature Specification: MIDI Note Editing — Overwrite, In-Place Paste, Length & Velocity Keys

**Feature Branch**: `007-midi-note-editing`

**Created**: 2026-10-13

**Status**: Draft

**Input**: User description: "MIDI editor (piano roll) editing improvements: (1) When a note is moved/resized/pasted onto other notes of the same pitch, the overlapped notes must be overwritten (trimmed/removed) instead of stacking on top of each other. (2) Ctrl+C / Ctrl+V on selected notes must always paste the copy at exactly the same position as the original (on top), selected, so the user then moves it with arrow keys or the mouse — currently the paste position is inconsistent (sometimes in place, sometimes shifted). (3) Keyboard shortcut to lengthen/shorten notes: Shift+Left/Right arrows, working on all selected notes. (4) Keyboard shortcuts to adjust velocity of the selected notes."

## Nota Context *(fill in — drives plan and tasks)*

**Feature area** *(pick the main one)*:

- [ ] Engine (C++, `src/native/nota.engine`) — real-time audio rules apply, see `ARCHITECTURE.md` AR table
- [x] Managed app (C#, `src/managed/*`) — domain, application, infrastructure, presentation
- [x] UI (Avalonia, `src/managed/Nota.Presentation`) — follow `DESIGN.md` (Ember)
- [ ] Plugin hosting (`pluginhost/`) — the only place JUCE is allowed
- [ ] Build / packaging / CI (`scripts/`, `.github/`) — see `BUILD.md`
- [ ] Docs only

The engine is not involved: these are note-list edits made in the piano roll before the
clip's notes are committed to the engine as today.

**Nota docs to keep in mind** *(links relative to `specs/<slug>/`, where this file lands)*:
[`ARCHITECTURE.md`](../../ARCHITECTURE.md) (AR rules),
[`CONTRIBUTING.md`](../../CONTRIBUTING.md) (licensing, dependency policy, PR
expectations), [`BUILD.md`](../../BUILD.md), [`DESIGN.md`](../../DESIGN.md),
[`FEATURES.md`](../../FEATURES.md).

**Current state (for reference)**:

- Duplicate (Cmd/Ctrl+D) already overwrites: copies land after the selection and cut or
  remove same-pitch notes they cover. Moving (mouse or arrows), resizing, drawing and
  pasting do not — notes of the same pitch can stack on top of each other.
- Paste places the clipboard at the playback cursor when the cursor is visible, otherwise
  at the copied notes' original position. Whether the cursor is visible depends on
  transport state, so the same Cmd+C / Cmd+V lands in different places.
- ←/→ nudge the selection by the snap step; ↑/↓ by a semitone, Shift+↑/↓ by an octave.
  Shift+←/→ currently behaves like plain ←/→. There is no keyboard control for note
  length or velocity; velocity is edited only in the velocity lane.

## Clarifications

### Session 2026-10-13

- Q: When is the overwrite applied while a pasted/moved selection is still being moved?
  → A: Deferred. While the selection is unchanged, notes underneath are only marked
  "will be overwritten"; the overwrite is applied once the selection is let go.
- Q: Which keys and step change the velocity of selected notes? → A: Alt+↑/↓ by ±10,
  Alt+Shift+↑/↓ by ±1 (Option on macOS).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Notes overwrite instead of stacking (Priority: P1)

A user drags a note onto another note of the same pitch. Instead of two notes sounding on
top of each other (doubled attack, hidden note that later surprises them), the note being
placed wins: the note underneath is cut where the placed note begins, or removed if the
placed note covers its start.

**Why this priority**: Stacked same-pitch notes are invisible bugs in the music (doubled
attacks, stuck-sounding notes, notes that cannot be clicked). Every other story in this
spec produces overlaps, so this rule has to exist first.

**Independent Test**: Place two notes of the same pitch, drag one so it partly covers the
other, click empty space to let the selection go, and check that only non-overlapping notes remain and playback has a
single attack at each position.

**Acceptance Scenarios**:

1. **Given** note A (C4, beats 0–2) and note B (C4, beats 3–4), **When** the user drags B
   to start at beat 1 and lets the selection go, **Then** A is cut to beats 0–1 and B occupies beats 1–2.
2. **Given** note A (C4, beats 1–2) and note B (C4, beats 3–5), **When** the user drags B
   to start at beat 0.5 (so B covers A's start) and lets the selection go, **Then** A is
   removed.
3. **Given** note A (C4, beats 2–3) and note B (C4, beats 0–1), **When** the user
   lengthens B to end at beat 2.5 and lets the selection go, **Then** A is removed.
4. **Given** note A (C4) and note B (D4) overlapping in time, **When** either is moved,
   **Then** nothing is cut — different pitches never overwrite each other.
5. **Given** B is still selected after the drag, **Then** A is drawn whole but marked as
   "will be overwritten" over the covered part; dragging B away clears the mark.
6. **Given** an overwrite was applied, **When** the user presses Undo once, **Then** the
   cut/removed notes are restored; a further Undo reverts the move itself.

---

### User Story 2 - Copy/paste always lands on the original (Priority: P1)

A user selects a phrase, presses Cmd/Ctrl+C then Cmd/Ctrl+V. The copy appears exactly on
top of the original, already selected, every time — regardless of whether the song is
playing or where the playback cursor is. The user then moves the copy where they want it
with the arrow keys or the mouse.

**Why this priority**: The reported inconsistency makes paste unpredictable; a fixed,
known landing spot is the core of the requested workflow.

**Independent Test**: Copy a selection, paste with the transport stopped, playing, and
with the cursor at various positions; in every case the pasted notes have the same
start, pitch and length as the originals and are the only selected notes.

**Acceptance Scenarios**:

1. **Given** a selection of notes at beats 4–6, **When** the user copies and pastes,
   **Then** identical copies appear at beats 4–6 and the copies (not the originals) are
   selected.
2. **Given** the transport is playing and the cursor is at beat 10, **When** the user
   pastes, **Then** the copies still land at the original position, not at beat 10.
3. **Given** a just-pasted copy (C4, beats 0–1) sitting on its original, **When** the
   user presses → four times with a 1/16 grid, **Then** the copy is at beats 1–2 and the
   original is still whole at beats 0–1 (while the copy partly covered it, the covered
   part was only marked, never cut).
4. **Given** a pasted copy moved to partly cover another C4 note, **When** the user
   clicks empty space (letting the selection go), **Then** the covered note is cut or
   removed per Story 1.
5. **Given** notes were copied from another MIDI clip, **When** pasted into this clip,
   **Then** they land at the same clip-relative beats they had in the source clip.

---

### User Story 3 - Stretch and shrink notes from the keyboard (Priority: P2)

With one or several notes selected, Shift+→ makes every selected note one grid step
longer and Shift+← one grid step shorter, keeping their start positions.

**Why this priority**: A frequent editing action that currently requires precise mouse
work on each note's edge; less critical than correctness of overlaps and paste.

**Independent Test**: Select three notes of different lengths, press Shift+→ twice and
Shift+← once; each note is exactly one grid step longer than at the start, and starts are
unchanged.

**Acceptance Scenarios**:

1. **Given** selected notes of lengths 1/4 and 1 beat and a 1/16 grid, **When** the user
   presses Shift+→, **Then** each note's length grows by 1/16 note and starts do not move.
2. **Given** a selected note one grid step long, **When** the user presses Shift+←,
   **Then** the note stays one grid step long (it is never shortened to zero or removed).
3. **Given** a selected note that ends at the clip's end, **When** the user presses
   Shift+→, **Then** that note does not grow past the clip end; other selected notes still
   grow.
4. **Given** a selected note lengthened into a following same-pitch note, **Then** the
   overwrite rule of Story 1 applies.

---

### User Story 4 - Adjust velocity from the keyboard (Priority: P2)

With notes selected, the user presses a velocity shortcut to make all selected notes
louder or softer in steps, and a modifier for fine steps. The velocity lane and note
shading update immediately, and the user can hear the change on playback.

**Why this priority**: Speeds up dynamics editing for multiple notes; the velocity lane
already allows it with the mouse.

**Independent Test**: Select notes with velocities 40, 80 and 120, press "velocity up"
once; they become 50, 90 and 127 (clamped). Press "velocity down" with the fine modifier;
each drops by 1.

**Acceptance Scenarios**:

1. **Given** selected notes with velocities 40 and 80, **When** the user presses Alt+↑,
   **Then** they become 50 and 90 (relative change; the differences between notes are
   kept).
2. **Given** a selected note at velocity 125, **When** the user presses Alt+↑, **Then**
   it becomes 127; at velocity 3, Alt+↓ makes it 1 (never 0 or below).
3. **Given** selected notes, **When** the user presses Alt+Shift+↑ or Alt+Shift+↓,
   **Then** each velocity changes by 1.
4. **Given** a burst of velocity key presses, **When** the user presses Undo, **Then**
   velocity edits are undone (each press is its own undo step, like arrow-key nudges).

### Edge Cases

- Pasting with an empty clipboard does nothing.
- Pasted notes whose original position lies beyond this clip's end are skipped; notes
  running past the end are trimmed to the clip end (as today).
- A selection moved onto several same-pitch notes cuts/removes each of them independently.
- Notes within the selection never overwrite each other (e.g. two selected notes moved
  together keep their relative layout even if they already overlapped).
- Moving a note away never restores a note cut in an earlier, already-settled placement
  (that is what Undo is for); only notes marked during the current, still-selected
  placement come back.
- Undo while a placement is unsettled undoes the last nudge/move as today; nothing has
  been overwritten yet, so there is nothing extra to restore.
- Notes marked "will be overwritten" still sound on playback until the placement
  settles; settling happens automatically before the clip is saved or exported, so a
  saved project never contains overlapping same-pitch notes.
- Shift+←/→ with snap off uses the same fallback minimum step as drawing with snap off.
- Shift+↑/↓ keeps its current meaning (move by an octave); Alt+←/→ is not assigned.
- Velocity shortcuts with no selection do nothing and are not consumed (the keys fall
  through to whatever else uses them).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: When notes are placed — moved (mouse or arrows), resized (mouse or
  keyboard), drawn or pasted — each placed note MUST overwrite unselected notes of the
  same pitch that it overlaps: a note whose start lies inside the placed note is removed;
  a note that starts before and runs into the placed note is cut to end where the placed
  note starts. The overwrite is applied when the placement is settled (FR-003).
- **FR-002**: Overwrite MUST NOT apply between notes that are part of the same selection,
  nor between notes of different pitches. Applying an overwrite MUST be an undoable step
  of its own.
- **FR-003**: Overwrite is deferred while the edited notes stay selected. As long as the
  selection is unchanged, further moves, resizes and arrow nudges re-evaluate against the
  notes as they were before the selection started moving — notes underneath are neither
  cut nor removed, only drawn marked as "will be overwritten", and a note the selection
  moves off is shown whole again. The placement settles, and the overwrite is applied,
  when the selection is let go: the selection changes (click on empty space or other
  notes, Esc, Select All, a new paste), the piano roll loses focus, the editor switches to
  another clip, or the project is saved or exported.
- **FR-003a**: Paste MUST place the copies exactly on top of the originals; with FR-003,
  the originals stay intact while the copies are selected and are overwritten only where
  the copies finally rest.
- **FR-004**: Paste MUST always place the clipboard notes at the beats they had when
  copied (clip-relative), independent of transport state and cursor position, and MUST
  select exactly the pasted notes.
- **FR-005**: Shift+→ MUST lengthen every selected note by one grid step and Shift+← MUST
  shorten every selected note by one grid step, keeping start positions; a note never
  becomes shorter than the minimum note length, and never extends past the clip end.
- **FR-006**: Alt+↑ / Alt+↓ MUST raise / lower the velocity of every selected note by 10;
  Alt+Shift+↑ / Alt+Shift+↓ by 1. Velocities MUST be clamped to 1–127, and the relative
  change MUST be the same for every note (no rescaling).
- **FR-007**: Each length or velocity key press MUST be a separate undoable edit, matching
  how arrow-key nudges behave today.
- **FR-008**: The Preferences → Shortcuts list and the user docs MUST list the new length
  and velocity shortcuts and describe the paste landing position.
- **FR-009**: Duplicate (Cmd/Ctrl+D) keeps its current behaviour (copies placed right
  after the selection, overwriting what they land on).

### Key Entities

- **Note**: pitch, start beat, length, velocity — within a MIDI clip of fixed length.
- **Selection**: the set of notes that the keyboard and mouse edits act on; after paste
  it is exactly the pasted copies.
- **Note clipboard**: notes copied with their clip-relative start beats, shared between
  MIDI clips.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Once a placement settles, and in every saved or exported project, a clip
  contains no two notes of the same pitch whose time ranges overlap.
- **SC-002**: Copy + paste places the copies at the original position in 100% of attempts,
  across stopped, playing and arbitrary-cursor states.
- **SC-003**: Changing the length of N selected notes by k grid steps takes k key presses
  regardless of N.
- **SC-004**: Changing the velocity of a selection by 30 takes 3 key presses.
- **SC-005**: Every overwrite, length and velocity edit is fully reverted by Undo, leaving
  the clip identical to its state before the edit.

## Assumptions

- "Grid step" means the piano roll's current snap step (the adaptive grid from spec 003
  when adaptive), the same step ←/→ already uses.
- Alt+↑/↓ and Alt+Shift+↑/↓ are not bound anywhere else in the app; the plain ↑/↓ and Shift+↑/↓ meanings stay. On macOS Alt is
  the Option key.
- Overwrite behaviour follows the existing Duplicate rule (and the arrangement's clip
  overwrite), so the app has one consistent "placed material wins" rule.
- The keyboard shortcuts act when the piano roll note grid has focus, like the existing
  arrow-key nudges.
- The velocity lane, expression lanes and audio clip editor are out of scope.

## Nota Compliance Checklist *(review before implementation; re-check in tasks.md)*

- [x] Every new source file will carry the AGPL-3.0-only SPDX header (two lines, see CONTRIBUTING.md)
- [x] No new dependency
- [x] Audio-thread impact considered: none — edits are made in the app and committed through the existing note-commit path
- [x] Managed/native crossings go through the pure C ABI in `nota_engine.h` only (AR-7); no new crossings
- [x] Smoke-test impact assessed: no engine behaviour change; covered by managed unit tests instead
- [x] User-visible change → `CHANGELOG.md` `[Unreleased]` entry planned, plus FEATURES.md, user docs and the Shortcuts pane (nota-features / nota-docs / nota-shortcuts)
- [x] UI change → no new visuals; existing piano roll rendering
