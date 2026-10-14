# Bug Assessment: Return track can't be deleted in the arrangement

- **Slug**: return-track-delete
- **Created**: 2026-10-13
- **Source**: pasted text (user report)
- **Verdict**: valid
- **Severity**: medium

## Report (verbatim or summarized)

> Заметил проблему, что нельзя через контекстное меню или delete / backspace (на мак) удалить return дорожку на аранжировке

## Symptom

A return (send/aux) track shown in the arrangement's footer rows can't be deleted: its context
menu has only Rename + Colour, and Delete / Backspace with the return selected does nothing.
Expected: the same "Delete track" path regular tracks have (menu item + key, one undo step).

## Reproduction

1. New project (template creates Reverb + Delay returns).
2. Arrangement → right-click the "Reverb" row in the footer → no "Delete track" item.
3. Left-click the return row (it gets selected) → press Delete / Backspace → nothing happens.

## Suspected Code Paths

- `src/managed/Nota.App/ArrangementView.cs:2513` `ShowFooterMenu` — return/master menu builds
  only Rename + Colour; no Delete item (comment says returns "have no clips to copy/duplicate",
  delete was simply left out).
- `src/managed/Nota.App/ArrangementView.Tracks.cs:25` `SelectedTrackIds()` — enumerates `_tracks`
  only; returns live in `_returns`, so `DeleteSelectedTracks()` gets an empty list → returns false.
- `src/managed/Nota.App/ArrangementView.Tracks.cs:30` `IsInTrackHeaders` — requires the event to
  be inside `_scroller`; the footer (`_footer`, `ArrangementView.cs:379`) is docked outside it,
  so clicking a return row never sets `_trackHeadersFocused` (`MainWindow.axaml.cs:425`), and the
  key path in `MainWindow.Input.cs:168/360` / `MainWindow.Menu.cs:181` is never reached.
- **Engine** `src/native/nota.engine/src/Engine_Tracks.cpp:2700 removeTrack`, `:2919 removeTracks`
  — drop the track but do not touch return-bus numbering. Return buses are addressed by
  `Track::returnIndex_` (assigned as `returnTrackCount()` in `addReturnTrack`, `:875`) and every
  track's `send_[bus]` is indexed by that slot.
- `src/native/nota.engine/src/Engine_Render.cpp:1093–1098, 1376` — renderer clears
  `numReturns = count(returns)` buses and skips a return whose `returnIndex >= numReturns`.

## Root Cause Hypothesis

Two layers (confidence: high).

1. **UI**: delete was never wired for footer rows — no menu item, selection helpers ignore
   `_returns`, and the footer is outside the area that routes track keys.
2. **Engine (latent, exposed once UI allows it — and already reachable via MCP `remove_track`)**:
   removing a return leaves a gap in `returnIndex`. With returns {0: Reverb, 1: Delay}, deleting
   Reverb leaves Delay at index 1 while `numReturns` becomes 1 → the render pass skips Delay
   (`bus >= numReturns`): **the remaining return goes silent** and its meter reads 0. A later
   `addReturnTrack` assigns index `count()==1` → collides with Delay. Sends on other tracks to the
   deleted bus keep their level and now (after collision) feed the wrong return. Save/load re-adds
   returns sequentially (`ProjectService.cs:461`) while sends are saved by bus index (`:101/:593`),
   so a gap also misroutes sends after reload.

## Proposed Remediation

**Preferred**:

*Engine* — in `removeTrack`/`removeTracks`, when the dropped set contains returns, compact the
remaining returns: renumber their `returnIndex` to 0..n-1 in order, and for every surviving
non-return track remap `send_[old] → send_[new]` (dropping the deleted buses' levels, zeroing
the tail). Because `Track` objects are shared with undo snapshots, renumbered/re-sent tracks must
be cloned (`cloneTrack`) into the new `Graph`, not mutated in place, so undo restores the
original numbering and send levels exactly. Factor this into one helper used by both functions.
The master can never be removed (already not in `authoring_->tracks`; keep a guard).

*UI* —
- `ShowFooterMenu`: add a separator + "Delete track" (`GlyphKind.Trash`, `MenuKit.DeleteKey`) for
  returns only (not the master), calling `DeleteTracks(new[]{trackId})`.
- `SelectedTrackIds()`: also include a selected return (`_returns` where `IsTrackMultiSelected`).
- `IsInTrackHeaders`: also return true when the event lands in the footer header panel
  (`_footerHeaders`), so Delete/Backspace (and Cmd+Backspace via Edit ▸ Delete) route to
  `DeleteSelectedTracks()`. Copy/Cut/Duplicate/Paste already skip returns in the engine
  (`insertTrackClones`), so including returns in the selection is safe for those, but Cut should
  not delete a return it can't paste back — filter returns out in `CutTracks`/`CopySelectedTracks`.

**Alternatives**:
- UI-only fix (no engine compaction): smaller, but deleting the first return silences the others
  and corrupts sends — not acceptable.
- Make the renderer clear/iterate all `kMaxReturns` buses and keep gaps: fixes the silence, but
  `addReturnTrack` collision and save/load misrouting remain; the "Return N" naming would show gaps.

**Files likely to change**:
- `src/native/nota.engine/src/Engine_Tracks.cpp` (+ `Engine.h` for the helper)
- `src/managed/Nota.App/ArrangementView.cs` (`ShowFooterMenu`)
- `src/managed/Nota.App/ArrangementView.Tracks.cs` (`SelectedTrackIds`, `IsInTrackHeaders`, `CutTracks`)
- engine tests (C++), smoke test if it covers returns
- `CHANGELOG.md` [Unreleased] → Fixed

**Tests to add or update**:
- Engine: two returns, track A sends 0.5 → R0 and 0.7 → R1; remove R0 → remaining return has
  index 0, A's send[0]==0.7, send[1]==0; rendering a tone through A's send yields signal on the
  remaining return. Undo → both returns back with original indices and sends.
- Engine: remove R0, then `addReturnTrack` → new return gets index 1, no collision.
- Save → load round-trip after deleting a return keeps sends on the right return.

## Risks & Considerations

- Undo snapshot sharing: in-place mutation of `send_`/`returnIndex_` would corrupt history — clone.
- RT safety: all work happens on the message thread when building the new `Graph`; the render
  path is unchanged.
- Device-chain / mixer / session views cache "Return N" labels and send-knob counts — they must
  refresh after the delete (`AfterTrackEdit()` + existing track-list refresh should cover it; verify).
- Sidechain / route slots keyed by track id (`recomputeRouting`) are unaffected by renumbering.
- Shortcuts pane: no new binding (Delete already listed) — `nota-shortcuts` not needed; `nota-docs`
  may mention deleting returns.

## Open Questions

- Should deleting a return that other tracks still send to ask for confirmation? (Proposed: no —
  it's one undo step, like regular tracks.)
- Session view / mixer: should returns be deletable there too? Out of scope for this bug unless
  the same gap exists — [NEEDS CLARIFICATION].
