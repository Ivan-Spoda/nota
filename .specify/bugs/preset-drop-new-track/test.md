# Bug Verification: Dropping an instrument preset on an instrument track adds a new track

- **Slug**: preset-drop-new-track
- **Tested**: 2026-10-11
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The reproduction from the assessment (drop a preset → a track; drop another on that track) was run
through `MainWindow`'s real arrangement drop handler. The second drop now loads into the existing
track, with no new track, across pack, factory and cross-instrument presets. A drop on empty space
still creates a track.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | `/tmp/packsharness`, `MainWindow.OnArrangementDrop` (reflected) | pass | Pack preset "Grand" on empty space → Mosaic track; "Upright" on that track → same track count, program now "Upright" |
| Cross-instrument | same harness | pass | Factory Volt preset on the Mosaic track → Volt in place; pack preset → back to Mosaic in place |
| New tests | `dotnet run --project tests/Nota.SmokeTest -c Release` | pass | `PaletteTests.PresetDrops`: factory twice, user preset file of another kind, empty space |
| Regression suite | full smoke test | pass (1 unrelated) | Only `Nota Consort stress` timing failed; unrelated DSP benchmark, varied between runs |
| Lint / type-check | `dotnet build` (via the smoke run) | pass | |

## Output Excerpts

```
[PASS] a pack preset dropped on empty space makes a Mosaic track (status: Applied preset Grand)
[PASS] a second pack preset dropped on that track adds no track (status: Applied preset Upright)
[PASS] …and the track's Mosaic program is now 'Upright'
[PASS] a factory Volt preset dropped on it swaps to Volt in place (status: Applied preset Warm Pad)
[PASS] and a pack preset swaps it back to Mosaic (status: Applied preset Grand)
HARNESS PASSED
```

## Residual Risks

- The native drag gesture is not exercised headless (see `mosaic-packs-expand/test.md`).
- Devices panel and Session view drops still create a track for an instrument preset. The fix
  deliberately left them out, and the assessment's open question is still open.
- A plug-in instrument preset dropped in place was not exercised (no plug-ins in the isolated data
  dir). The code path is `SetTrackInstrumentPlugin` + `ApplyState`.

## Recommendation

Close the bug for the arrangement. Decide separately whether the Devices panel and Session view
drops should behave the same way.
