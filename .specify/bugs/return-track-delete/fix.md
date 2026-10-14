# Bug Fix: Return track can't be deleted in the arrangement

- **Slug**: return-track-delete
- **Fixed**: 2026-10-13
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

The engine now compacts return buses when a return is removed (survivors renumbered 0..n-1,
every track's sends remapped, changed tracks cloned so undo is exact), and the arrangement
lets you delete a Return from its footer row — "Delete track" in the context menu, and
Delete / Backspace once the row is selected.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/native/nota.engine/src/Engine.h` | modified | declares `compactReturns(Graph&)` |
| `src/native/nota.engine/src/Engine_Tracks.cpp` | modified | `compactReturns`; called from `removeTrack` / `removeTracks` only when a return is dropped |
| `src/managed/Nota.App/ArrangementView.cs` | modified | `ShowFooterMenu`: separator + "Delete track" for returns (not the master) |
| `src/managed/Nota.App/ArrangementView.Tracks.cs` | modified | `IsInTrackHeaders` counts the footer headers; `DeleteSelectedTracks` takes the selected return |
| `tests/Nota.SmokeTest/ReturnDeleteTests.cs` | added test | engine-level regression (compaction, sound, undo, new return, save/load) |
| `tests/Nota.SmokeTest/Program.cs` | modified | runs the new tests in the full smoke run + `--returns` |
| `CHANGELOG.md` | modified | [Unreleased] → Fixed |

## Diff Highlights

```cpp
// removeTracks: …
if (anyReturn) compactReturns(*g);
publish(std::move(g));
```

```csharp
public bool DeleteSelectedTracks()
{
    var ids = SelectedTrackIds();
    if (ids.Count == 0 && _returns.Any(r => r.Id == SelTrackId)) ids.Add(SelTrackId);
    return DeleteTracks(ids);
}
```

## Tests Added or Updated

- `ReturnDeleteTests.Run` — two returns + sends 0.5/0.7: deleting return 0 moves the survivor to
  bus 0, the 0.7 send follows it, the survivor still has signal; undo restores indices + sends;
  `RemoveTrack` compacts too; a new return then takes bus 1 unfed; save/load keeps each send on
  its return.

## Local Verification

- `cmake --build src/native/nota.engine/build` → OK
- `dotnet build Nota.sln -c Release` → 0 errors
- `dotnet run --project tests/Nota.SmokeTest -c Release` → SMOKE TEST PASSED (10/10 new checks pass)
- Headless UI harness (`/tmp/returnharness`, real MainWindow + default template, synthetic
  pointer/key events): Backspace and Delete on a selected Return delete it; footer row disappears,
  status "Deleted track(s)"; undo restores it; Reverb context menu shows Rename…, Color, Delete
  track and the item deletes it; Master menu has no Delete. All checks PASS.

## Deviations from Assessment

- `SelectedTrackIds()` was left rows-only and Cut/Copy were not touched: a Return is only ever
  selected alone (the footer click replaces the selection), so including it only in
  `DeleteSelectedTracks` is enough and keeps Copy/Cut/Duplicate from ever seeing a return.
- `compactReturns` runs only when a return is among the deleted tracks, so deleting a regular
  track never rewrites anyone's sends.

## Follow-ups

- Session view / mixer: returns still can't be deleted there (open question in the assessment).
- `dotnet run … --no-build -- --template|--returns` falls through to the full run (pre-existing,
  affects all the solo flags in that invocation form).
- nota-docs: the tracks page could mention deleting Returns.
