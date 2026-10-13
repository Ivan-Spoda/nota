# Quickstart: validating Return & Master levels

## Automated

```bash
scripts/build.sh
```

Must exit green. No engine change, so the smoke test is unchanged.

## Headless UI harness (isolate data with `NOTA_DATA_DIR`)

Recipe: memory "Nota UI verify" — throwaway console project referencing Nota.App,
`Avalonia.Headless` + Skia, `MainWindow` with its `MainWindowViewModel`, default new
project (Instrument, Audio, Reverb, Delay returns).

| Check | Expected |
|---|---|
| Footer screenshot, Ember Graphite and Ember Paper | Each return and the master row: name, `0.0`, mini fader, vertical meter at the right; row height 40 |
| Set return volume via engine (`SetTrackVolume(ret, 0.5)`), tick `UpdateMeters` | Return fader moves, read-out `-6.0` |
| Synthetic drag on a return's fader | Engine return volume changes; Mixer strip shows the same after refresh |
| Synthetic drag on the master fader | `Transport.MasterVolume` changes; MST fader + dB follow |
| Change master in `MixerView`, save project, reload | Master row, MST and Mixer all show the saved value |
| Double-click a footer fader | Back to `0.0` |
| Add a third return, delete the first | Remaining rows' meters/faders still address their own return |
| Select a return row repeatedly | Rows rebuild; meters keep updating; no exceptions |

## Manual (when a display is available)

| Step | Expected |
|---|---|
| New project, play the instrument track, raise send A | Reverb row meter moves; matches the Mixer's Reverb strip |
| Mute the Reverb return | Its meter drops to empty |
| Stop playback | Return/master meters show the tail decaying, then empty |
| Push the master above 0 dBFS | Master meter's top segment shows Alert; peak hold stays until clicked |
