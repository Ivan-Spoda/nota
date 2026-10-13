# Research: Return & Master Track Levels

All findings are from the code on `develop` (2026-10-13). No open unknowns remain.

## R1 — Why the rows show "−∞" / "0.0"

- **Finding**: `ArrangementView.BuildSlimRow` (`src/managed/Nota.App/ArrangementView.cs`)
  prints a constant: `masterSpine ? "0.0" : "−∞"`. No meter, no fader, no binding.
- **Decision**: Replace the constant with a real volume read-out and add a meter and a
  mini fader to the slim row.

## R2 — Source of the levels

- **Finding**: The engine already meters returns (render pass 2, `Track::setMeter`,
  `Engine_Render.cpp`) and the master (`masterPeakL_`…). `nota_engine_track_meter` works
  for return ids; the Mixer already reads them. For the master id (`kMasterTrackId`),
  `trackMeter` returns the master *effect-chain* track whose meter is never written —
  the real master level comes from `nota_engine_master_meter`.
- **Decision**: Return rows → `TryGetTrackMeter(returnId)`; Master row →
  `MasterMeter()`. No engine or C ABI change.
- **Alternatives**: Writing a meter into the master chain track in the engine — rejected,
  needless engine change.

## R3 — Meter widget and layout in a 40 px row

- **Finding**: Regular track headers use a vertical `MeterBar` (Width 8, margin
  0,6,3,6) docked right of the content; group headers use a horizontal 4 px `MeterBar`.
  `DESIGN.md` § meters: Signal → −6 dB Caution → 0 Alert, −60…+6, peak hold until
  clicked, no ballistics — all built into `MeterBar`.
- **Decision**: Same vertical `MeterBar` as track headers (Width 8, docked right,
  vertical margins 6 → 28 px tall in the 40 px row). Content: name + dB on the top line,
  `MiniFader` on the bottom line, as the track header's volume row (fader `*`, dB 30 px).
  The row keeps `FooterRowH = 40`.
- **Alternatives**: Horizontal 4 px rail like groups — rejected: the request is "the
  same meter as regular tracks", and a vertical bar reads level better at a glance.

## R4 — Return volume fader

- **Finding**: Returns are ordinary tracks for volume: `SetTrackVolume(id)` and
  `TrackInfo.Volume` work; `ArrangementView.UpdateMeters` already walks every track
  (returns included) and pulls `_volFaders[id]` toward the live value (automation).
  `MiniFader(value, max = 1.5)`, `Default = 1.0` gives double-click reset.
- **Decision**: Return row fader = `MiniFader(ti.Volume) { Default = 1.0 }`,
  `ValueChanged → SetTrackVolume`; read-out uses the same `ShowDb` formatting as
  track headers. Faders/read-outs live in dedicated footer dictionaries (see R6) and
  are synced in `UpdateMeters`.
- **Automation write**: Track faders call `BeginAutomationWrite` on gesture. Returns
  have no visible automation lane in the footer, so recording would create invisible
  envelopes. **Decision**: no automation write from footer faders (spec FR-006).

## R5 — Master volume: one value

- **Finding**: The master volume's owner is `TransportViewModel.MasterVolume`
  (`OnMasterVolumeChanged → engine.SetMasterVolume`, saved in the project's transport
  state, used by MST fader, SessionView mixer, Remote). There is no engine getter.
  `MixerView` keeps its own `_masterVolume = 1.0f` and calls `engine.SetMasterVolume`
  directly — it starts at 0 dB regardless of the project and its changes are never
  saved nor shown in MST (pre-existing bug, conflicts with FR-007).
- **Decision**: Give `ArrangementView` and `MixerView` the same pair of callbacks
  `SessionView` already has — `Func<float>? GetMasterVolume`, `Action<float>?
  SetMasterVolume` — wired in `MainWindow` to `vm.Transport.MasterVolume`. The footer
  master fader reads/writes through them and is polled in `UpdateMeters` (follows MST,
  Mixer, MIDI-learn, Remote, load). `MixerView` drops `_masterVolume` in favour of the
  callbacks.
- **Master automation**: the engine applies master-volume automation inside the
  graph without updating the VM, so a fader polling the VM cannot follow it (see R7).

## R6 — Rebuild lifetimes

- **Finding**: `RebuildHeaders` clears `_meters`, `_volFaders`, `_volDb`;
  `RebuildFooterHeaders` runs separately (on refresh and on every selection change).
- **Decision**: Footer rows register in their own `_footerMeters` /
  `_footerFaders` / `_footerDb` (cleared in `RebuildFooterHeaders`) plus a single
  `_masterMeterBar` / `_masterFader`, so a header rebuild never orphans footer
  widgets and vice versa. A fader drag must survive `RebuildFooterHeaders` triggered
  by the click-to-select on the same row: selection happens on `PointerPressed` of the
  card; the fader marks its press handled, so the row is not rebuilt mid-drag.

## R7 — Live master volume under automation

- **Finding**: `nota_engine.h` has no master-volume getter; master automation is
  graph-level (`GetMasterVolumeAutomation`).
- **Decision**: Out of scope to add an engine getter. US4-4 becomes: the master
  fader shows the master *set* volume (same as MST). Recorded as a spec adjustment.

## R8 — Verification

- No engine change → the smoke test is unchanged; `scripts/build.sh` must still pass.
- UI is verified with the headless Avalonia harness (see quickstart): MainWindow with
  its VM, the default template (Reverb/Delay returns), screenshots of the footer in
  Ember Graphite and Ember Paper; meters driven by real engine readings where the
  harness can pump audio, otherwise by checking that `UpdateMeters` pushes the
  engine's return/master readings into the footer bars.
- Fader behaviour checked with synthetic pointer drags on the footer `MiniFader`s:
  engine return volume / `Transport.MasterVolume` change, read-outs update, Mixer and
  MST follow.
