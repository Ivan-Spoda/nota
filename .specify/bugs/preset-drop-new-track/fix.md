# Bug Fix: Dropping an instrument preset on an instrument track adds a new track

- **Slug**: preset-drop-new-track
- **Fixed**: 2026-10-11
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

An instrument preset dropped on a plain instrument track in the arrangement now loads into that
track's instrument (swapping the instrument when the preset is for another one) instead of
starting a new track. This works for factory presets and for preset files — user presets and
Nota Mosaic pack presets. Drops on empty space, audio / group / return tracks and racks still
start a track.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/managed/Nota.Presentation/DeviceInsertService.cs` | modified | `DropOnArrangement` applies presets with `InPlace: true`; `ApplyPresetCore` gains an in-place branch for preset files |
| `src/managed/Nota.Application/Ports/IPresetStore.cs` | modified | new `ApplyInstrumentInPlace(engine, path, trackId, out swapped)` |
| `src/managed/Nota.Infrastructure/Persistence/PresetStore.cs` | modified | implements it: swaps the built-in / plug-in instrument if needed, then `PresetService.ApplyInPlace` |
| `tests/Nota.SmokeTest/PaletteTests.cs` | added test | `PresetDrops()`; relabelled the existing "dropped one starts its own track" check |

## Diff Highlights

```csharp
// DropOnArrangement
if (item.Kind == BrowserItemKind.Preset) return ApplyPreset(item, new InsertTarget(trackId, Beat: beat, InPlace: true));

// ApplyPresetCore, after the factory in-place branch
if (fp is null && !target.NewTrack && target.InPlace && CanReplaceInstrument(t)
    && _presets?.ApplyInstrumentInPlace(_engine, item.Path, t, out bool fileSwapped) is { } fw)
    return new InsertResult { ..., InstrumentReplaced = fileSwapped, PresetLanded = (t, PresetChain.Instrument, -1, item.Name, "") };
```

`ApplyInstrumentInPlace` returns null for non-instrument files (effect presets), so those keep
going through `ApplyFromFile` unchanged. Kits are handled before this and are unaffected.

## Tests Added or Updated

- `PaletteTests.PresetDrops` — "a factory preset dropped on an instrument track loads there; a
  second one replaces it" (track count unchanged, kind switched Volt → Aurora, effects kept).
- `PaletteTests.PresetDrops` — "a preset file dropped on an instrument track swaps its instrument
  in place" (a user preset saved from a Volt, dropped on the Aurora track).
- `PaletteTests.PresetDrops` — "a preset dropped on empty space still starts a track".
- `PaletteTests.EngineChecks` — existing check relabelled "…while one not asked in place still
  starts its own track" (`ApplyPreset` without `InPlace` is unchanged).

## Local Verification

- `dotnet run --project tests/Nota.SmokeTest -c Release` → all new checks `[PASS]`.
  The run ended `SMOKE TEST FAILED (1)`: `Nota Consort stress … per 128-frame block (budget 2,90 ms)`
  — a DSP timing benchmark this change doesn't touch. It passed in an earlier run this session,
  and another run failed 4 timing checks, so it is load-dependent. Unrelated to this fix.
- The Mosaic pack path is not tested on its own here: a pack preset is a `builtin-instrument`
  `.notapreset` that goes through the same `PresetService.ApplyInPlace` (→ `LoadMosaicProgram`),
  which `MosaicTests` already checks ("a pack preset loads into a Mosaic").
- Not checked in the running app.

## Deviations from Assessment

- The assessment's open question (Session view / Devices panel drops) is not addressed: only the
  arrangement drop changed. The Devices panel (`Insert` → `InsertCore` → `ApplyPreset` without
  `InPlace`) still creates a track for an instrument preset.
- Swapping to Nota Rhythm through a preset doesn't load the default kit the way a bare
  instrument drop does (`TryReplaceInstrument`). Same as the palette's existing factory
  in-place path.

## Follow-ups

- Decide on Devices panel / Session view drops (pass `InPlace: true` there too).
- CHANGELOG `[Unreleased]` entry under Fixed: both bugs are user-visible.
