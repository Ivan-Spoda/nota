# Bug Verification: Mosaic → Packs → a pack won't expand in the browser

- **Slug**: mosaic-packs-expand
- **Tested**: 2026-10-11
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The reproduction no longer reproduces in the real `MainWindow` (headless Avalonia, real app
services, a Mosaic pack on disk). Clicking the pack's disclosure triangle shows its presets, and
clicking it again hides them. Those presets drop onto the arrangement through the window's real
drop handler. No regressions from the change.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | `/tmp/packsharness` (`NOTA_DATA_DIR=/tmp/packsharness/data dotnet run`) | pass | Real `MainWindow` + `BrowserView`; two presets saved via `IMosaicPacks.Save` into "Test Pack"; synthetic `PointerPressed` on the disclosure triangles of Nota Mosaic → Packs → Test Pack |
| Presets are draggable payload | same harness, `MainWindow.OnArrangementDrop` (reflected) with the pack rows | pass | A pack preset on empty space makes a Mosaic track |
| New tests | `dotnet run --project tests/Nota.SmokeTest -c Release` | pass | "a Mosaic pack expands to its presets in the browser", "a Mosaic pack collapses again" |
| Test pins the bug | same, with the old `DeviceTabOf` (during `/speckit-bug-fix`) | pass | The expand check failed on the old code |
| Regression suite | full smoke test | pass (1 unrelated) | Only `Nota Consort stress` timing failed; it is a DSP benchmark this fix doesn't touch, and it varied between runs |
| Lint / type-check | `dotnet build` (via the smoke run) | pass | No new warnings in changed files |

## Output Excerpts

```
packs root: /tmp/packsharness/data/presets/Mosaic Packs
[PASS] Nota Mosaic → Packs row is shown
[PASS] Packs → Test Pack row is shown
[PASS] clicking the pack's triangle shows its presets (Grand, Upright)
[PASS] clicking it again collapses the pack
[PASS] a pack preset dropped on empty space makes a Mosaic track (status: Applied preset Grand)
HARNESS PASSED
```

Screenshot: `/tmp/packsharness/shots/packs-expanded.png`. It shows Nota Mosaic → Packs → Test Pack
→ Grand, Upright.

## Residual Risks

- The OS drag gesture itself (`DragDrop.DoDragDropAsync`) can't run headless. The drop was fed to
  `OnArrangementDrop` directly. The drag start in `BrowserView.OnRowPressed` is generic for
  `Preset` rows and unchanged.
- Not clicked by hand in the running app on a display.
- The harness used a generated pack, not a real downloaded pack. The tree built from it is the
  same.

## Recommendation

Close the bug. The fix is verified in the real window, and the remaining gap (the native drag
gesture) is code this fix doesn't touch. A quick manual expand-and-drag in the app before the
release is still worth doing.
