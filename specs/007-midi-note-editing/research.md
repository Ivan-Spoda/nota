# Research: MIDI Note Editing

No open technical unknowns after clarification. Decisions taken while reading the current
code (`src/managed/Nota.App/PianoRollView.cs`, `MainWindow.Detail.cs`, `MainWindow.Files.cs`):

## R1 — Why paste is inconsistent

- **Finding**: `PasteClipboard` anchors at `SnapBeat(_playheadBeat)` whenever the host shows
  the playhead (transport playing and inside the clip), otherwise at `s_clipAnchor`. That
  is why the landing spot depends on transport state.
- **Decision**: Always anchor at `s_clipAnchor`, the copied notes' clip-relative origin.
- **Alternatives**: Paste at the playhead with a modifier. Rejected because it is not
  requested, and Duplicate already covers "place after".

## R2 — Where the overwrite rule lives

- **Decision**: In `Nota.Application/Editing/NoteOverwrite.cs`, a pure function shared by
  Duplicate and settle, tested in the smoke test.
- **Rationale**: Duplicate already has the rule inline, so extracting it keeps one
  definition. Pure code needs no headless UI to test.
- **Alternatives**: Keep the logic inside `PianoRollView`. Rejected because it cannot be
  tested without the UI, and the rule would exist twice.

## R3 — How deferral works without touching the originals

- **Decision**: While pending, `_notes` holds the placed notes *and* the untouched notes
  underneath (overlapping). Nothing is cut until `SettlePlacement`. "Shown whole again when
  moved off" therefore needs no restore logic: the marking is recomputed from the current
  positions on every render.
- **Consequence**: The engine plays the overlap until settle (accepted in the spec's edge
  cases). Undo during pending needs nothing extra: the engine undo reloads the roll via
  `SetNotes`, which clears the flag.
- **Alternatives**: Keep a shadow "pre-placement" list and restore cut notes as the
  selection moves. Rejected because it duplicates state and holds no information the
  live list lacks.

## R4 — The "will be overwritten" mark

- **Decision**: Draw the doomed span of an unselected note at about 25% of its normal
  alpha, with a 1 px dashed outline in the track colour. For a cut, that is the part from
  the cut to the end; for a removal, the whole note. Kept notes draw as today. Verify
  against DESIGN.md / nota-design (no new tokens expected; ghost/preview styling already
  exists for tool previews and must stay distinguishable — a preview is a brass outline).
- **Revised in implementation**: The faint fill sits under the covering note, so the doomed span's dashed edge is drawn in a second pass on top of all notes. Otherwise a note hidden under a pasted copy would show no mark at all.
- **Alternatives**: Hide doomed notes entirely. Rejected because the user would lose sight
  of what will be removed.

## R5 — When to settle

- **Decision**: Settle on selection change, grid focus loss, whole-clip edits (Quantize,
  Transpose, remove out-of-scale, applying a tool preview), and explicit save or export.
  Do not settle on autosave or recovery writes, so an in-flight edit is never altered
  behind the user's back; a recovered project may contain the pending overlap, which is
  harmless.
- **Focus loss also covers clip switching**: each clip gets a fresh `PianoRollView`, and
  the old one loses focus before it is dropped, while its `Commit` closure still targets
  its own clip.
- **Only with a pending placement**: Selecting and deselecting old overlapping notes
  (legacy projects) never mutates them.

## R6 — Key bindings

- **Finding**: Alt+↑/↓ and Alt+Shift+↑/↓ are bound nowhere in `Nota.App`. Shift+←/→
  currently behave as plain ←/→ (the switch ignores Shift for horizontal keys).
- **Decision**: Alt is checked before Shift, so Alt+Shift+↑ is the fine velocity step, not
  an octave move. Velocity steps are in MIDI units: round(v·127) ± k, clamped to 1–127, then
  /127. That keeps velocities on integer MIDI values and makes the scenarios exact.

## R7 — Length step with snap off

- **Decision**: Use `SnapStep` when snap is on and `MinNoteLen` (1/32 note = 0.125 beat)
  when it is off, the same fallback that drawing uses. The minimum length is `MinNoteLen`.
