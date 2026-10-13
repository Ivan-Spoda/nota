# Data Model: Default Project Template

No new persisted data and no project-file format change: the template is ordinary
project content, saved and loaded like anything the user creates.

## Default project template (code-defined, not stored)

| Order | Track kind  | Name              | Devices                         | Other                          |
|-------|-------------|-------------------|---------------------------------|--------------------------------|
| 1     | Instrument  | engine default    | Nota Synth (default instrument) | seed empty MIDI clip, beats 0–4 |
| 2     | Audio       | engine default    | none                            | —                              |
| A     | Return      | `Reverb`          | Nota Reverb (kind 2), Wet Only = on (param 19) | —               |
| B     | Return      | `Delay`           | Nota Delay (kind 3), Wet Only = on (param 22)  | —               |

**Invariants after `ProjectTemplate.ApplyDefault` on a freshly reset engine**:

- Exactly 2 non-return tracks and 2 return tracks.
- All sends from tracks 1–2 to returns A–B at their default (off) level.
- `CanUndo == false`, `CanRedo == false`.
- Transport, master and scenes untouched (the caller resets them, as today).

**State transitions**: none — it is a one-shot build applied to an empty engine. Any
later edit is a normal, undoable edit.
