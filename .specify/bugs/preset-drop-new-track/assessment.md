# Bug Assessment: Dropping an instrument preset on an instrument track adds a new track

- **Slug**: preset-drop-new-track
- **Created**: 2026-10-11
- **Source**: pasted text
- **Verdict**: valid (behaviour as coded; the user expects in-place replacement)
- **Severity**: medium

## Report (verbatim or summarized)

> перемещаешь пресет из браузера на аранжировку, добавляется инструмент с пресетом. Потом
> перещаешь другой пресет на ту же дорожку, добавляется новая дорожка (а хотелось бы чтобы на
> старой обновлялся)

## Symptom

Dragging an instrument preset onto an existing instrument track in the arrangement always
creates a new track. Expected: the preset loads into that track's instrument (swapping the
instrument if it is a different kind), as dropping a bare instrument already does.

## Reproduction

1. Drag any instrument preset (e.g. a Nota Mosaic factory preset) to empty arrangement space →
   a new track with that preset.
2. Drag another instrument preset onto that same track's lane.
3. A second track is created; the first is unchanged.

## Suspected Code Paths

- `src/managed/Nota.Presentation/DeviceInsertService.cs:176` `DropOnArrangement()` — bare
  instruments try `TryReplaceInstrument` first; presets go to `RouteToTrack`.
- `src/managed/Nota.Presentation/DeviceInsertService.cs:307` `RouteToTrack()` — calls
  `ApplyPreset(item, new InsertTarget(trackId))`, i.e. `InPlace = false`.
- `src/managed/Nota.Presentation/DeviceInsertService.cs:358` `ApplyPresetCore()` — the in-place
  branch exists but only for **factory** presets with `target.InPlace` (the command palette,
  `PaletteProviders.cs:196`). Otherwise `_factory.Apply` / `_presets.ApplyFromFile` →
  `PresetService.Apply` (`Nota.Infrastructure/Persistence/PresetService.cs:172`), whose
  `builtin-instrument` / `plugin-instrument` cases always add a track.
- File-based instrument presets (user presets, Mosaic pack presets) have no in-place path in the
  insert service at all; `IPresetStore` only has `ApplyFromFile`.
  `PresetService.ApplyInPlace` (`:298`) and `IMosaicPacks.ApplyInPlace` exist and are used by the
  device card's preset picker.

## Root Cause Hypothesis

Design gap rather than a regression: the arrangement drop never asks for in-place application,
and only factory presets could do it anyway. Confidence: high.

## Proposed Remediation

**Preferred**:
1. In `DropOnArrangement`, route instrument presets with `InPlace: true` when the drop target is
   a plain instrument track (`CanReplaceInstrument`); otherwise keep today's new-track behaviour
   (empty space, audio / group / return tracks, racks).
2. Extend the in-place branch of `ApplyPresetCore` to file presets: add
   `IPresetStore.ApplyInPlaceFromFile(engine, path, trackId)` (in `PresetStore`) that loads the
   document and, for `builtin-instrument`, swaps the track's built-in instrument if the kind
   differs then calls `PresetService.ApplyInPlace`; for `plugin-instrument`, sets the track's
   instrument plugin then restores its state. It reports whether the instrument was swapped so the
   result carries `InstrumentReplaced` (card state reset). Effect presets and kits are unchanged.
3. Keep `PresetLanded` populated so the card's preset label updates.

**Alternatives**:
- Only fix factory presets (one-line `InPlace: true`) — leaves Mosaic pack and user presets
  still spawning tracks, which is exactly the pack workflow from `mosaic-packs-expand`.
- A modifier key (e.g. ⌥-drop) to force a new track — could be added later; the default should
  match bare-instrument drops.

**Files likely to change**:
- `src/managed/Nota.Presentation/DeviceInsertService.cs`
- `src/managed/Nota.Application/Ports/IPresetStore.cs`
- `src/managed/Nota.Infrastructure/Persistence/PresetStore.cs` (+ `PresetService.cs` helper)
- `tests/Nota.SmokeTest/` (insertion test)

**Tests to add or update**:
- Factory instrument preset dropped on an instrument track of the same kind → track count
  unchanged, preset params applied.
- Preset of a different built-in kind → same track, `TrackInstrumentKind` switched.
- Mosaic pack / user file preset → same track, Mosaic program set.
- Preset dropped on empty space / an audio track → still creates a new track.

## Risks & Considerations

- Behaviour change: users who relied on "drop preset = new track" lose it on instrument lanes;
  dropping below the last track still creates one.
- Swapping the instrument kind discards the old instrument's state — same as a bare-instrument
  drop today, and one undo step (`Group`).
- Session view drops (`OnSessionDrop`) and Devices-panel drops route separately; decide whether
  they should match (Devices panel probably yes).
- Racks (kinds 3/4) excluded by `CanReplaceInstrument` — a preset there keeps making a new track.

## Open Questions

- Should the Session view and Devices panel drops get the same in-place behaviour?
  [NEEDS CLARIFICATION]
