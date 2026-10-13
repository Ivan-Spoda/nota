# Quickstart: validating the default project template

## Automated

```bash
scripts/build.sh
```

Must exit green. The smoke test covers, on a real engine:

1. `nota_engine_clear_undo_history` — after a structural edit, clearing leaves
   `CanUndo`/`CanRedo` false, keeps the session intact, and the next edit is undoable.
2. `ProjectTemplate.ApplyDefault` on a reset engine — one instrument track (Synth, one
   MIDI clip), one audio track, two returns named `Reverb` / `Delay` holding exactly
   Nota Reverb / Nota Delay with Wet Only on; `CanUndo == false`.
3. Save → load round-trip of the template yields the same tracks, devices and params.

## Manual (in the app; isolate data with `NOTA_DATA_DIR`)

| Step | Expected |
|------|----------|
| Launch the app with the welcome screen off | Arrangement shows Instrument, Audio, and returns Reverb, Delay |
| File → New from a project with content | Same four tracks; nothing from the old project remains |
| Play the computer keyboard on the instrument track | Synth sounds, dry |
| Raise the instrument track's send A | Reverb tail added; dry level unchanged |
| Raise send B | Echoes added |
| Press Undo on the untouched new project | Nothing happens |
| Quit right after File → New | No "save changes?" prompt |
| Delete the audio track, then Undo | Track comes back |
| Open an existing project | It loads exactly as saved — no extra tracks |
| Save the new project, reopen it | Same four tracks, Wet Only still on |
