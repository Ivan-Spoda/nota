# Bug Fix: Mosaic → Packs → a pack won't expand in the browser

- **Slug**: mosaic-packs-expand
- **Fixed**: 2026-10-11
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`BrowserViewModel.DeviceTabOf` now searches a device's whole subtree, so toggling a nested
folder (Nota Mosaic → Packs → a pack) re-flattens the Instruments list and the pack's presets
appear (and can be dragged).

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/managed/Nota.Presentation/ViewModels/BrowserViewModel.cs` | modified | `DeviceTabOf` uses a recursive `Holds(node, item)` instead of `d.Children.Contains(item)` |
| `tests/Nota.SmokeTest/BrowserTestSupport.cs` | added | `FakeMosaicPacks` — a fixed `IMosaicPacks` list for browser tests |
| `tests/Nota.SmokeTest/MosaicTests.cs` | added test | `BrowserPacks()` run from `MosaicTests.Run()` |

## Diff Highlights

```csharp
if (_instrTree.Any(d => Holds(d, item))) return 0;
...
private static bool Holds(BrowserItem node, BrowserItem item)
    => node.Children.Any(c => c == item || Holds(c, item));
```

## Tests Added or Updated

- `MosaicTests.BrowserPacks` — "a Mosaic pack expands to its presets in the browser": expands
  Nota Mosaic → Packs → pack via `ToggleExpand` and checks both preset rows are in `Instruments`.
- `MosaicTests.BrowserPacks` — "a Mosaic pack collapses again".

## Local Verification

- `dotnet run --project tests/Nota.SmokeTest -c Release` → `SMOKE TEST PASSED` (full run).
- With the old `DeviceTabOf` restored temporarily: `[FAIL] a Mosaic pack expands to its presets
  in the browser`, `SMOKE TEST FAILED` — the test pins the bug. Fix restored afterwards.
- Not checked in the running app (drag of the now-visible rows uses the generic preset drag path).

## Deviations from Assessment

None.

## Follow-ups

- `MosaicLibrary.Presets()` parses every pack preset on each browser `Rebuild()` — watch with
  many packs.
- Dropping a pack preset on an existing instrument track still adds a track — that is
  `preset-drop-new-track`.
