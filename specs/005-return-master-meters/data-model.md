# Data Model: Return & Master Track Levels

No persisted data changes. The project file format is untouched: return volume is
already saved per track, master volume already in the project's transport state.

## UI state (ArrangementView, not persisted)

| Element | Per | Source of truth | Read | Write |
|---|---|---|---|---|
| Return meter (`MeterBar`, vertical) | return track id | engine track meter | `TryGetTrackMeter(id)` each UI tick (~30 Hz) | — |
| Master meter (`MeterBar`, vertical) | one | engine master meter | `MasterMeter()` each UI tick | — |
| Return fader (`MiniFader`, max 1.5, default 1.0) | return track id | engine track volume | `TrackInfo.Volume`, polled in `UpdateMeters` (skipped while dragging) | `SetTrackVolume(id, v)` |
| Master fader (`MiniFader`, max 1.5, default 1.0) | one | `TransportViewModel.MasterVolume` | `GetMasterVolume()` polled in `UpdateMeters` | `SetMasterVolume(v)` |
| dB read-out (`TextBlock`, Mono 9) | each fader | the fader's value | — | updated with the fader: `−∞` at ≤ 0.0011, else `0.0` dB |

## Lifetimes

- Footer widgets are recreated by `RebuildFooterHeaders` (refresh, selection change)
  and registered in footer-only dictionaries cleared there; track-header dictionaries
  (`_meters`, `_volFaders`, `_volDb`) are not touched by the footer.
- A rebuilt meter starts empty (peak hold cleared) — the same as track headers after a
  rebuild.

## Master volume flow (after the change)

```text
MST fader ─┐
Mixer ─────┤                                   ┌─► engine.SetMasterVolume
Master row ┼─► TransportViewModel.MasterVolume ┼─► project save (transport state)
MIDI-learn*┤                                   └─► polled by MST / Mixer / Master row
Remote ────┘
```

\* MIDI-learn writes the engine directly (`MidiLearnService`), unchanged by this
feature — noted as a known gap in research R5's spirit, not fixed here.
