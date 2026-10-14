# Quickstart: validating MIDI Note Editing

## 1. Automated

```bash
scripts/build.sh
```

The smoke test includes the `NoteEditingTests` section. To run it alone:

```bash
dotnet run --project tests/Nota.SmokeTest -- --notes
```

Expected: every `note editing` check passes (overwrite fates, index remap, length and
velocity clamps).

## 2. In the app (piano roll, 1/16 grid)

| # | Do | Expect |
|---|----|--------|
| 1 | Draw C4 0–2 and C4 3–4. Drag the second to start at 1, then click empty space | First note becomes 0–1; one attack per position on playback |
| 2 | Repeat 1 but stop before clicking empty space | First note is whole but its 1–2 part is drawn marked; dragging back clears the mark |
| 3 | Select a phrase, ⌘C, ⌘V with the transport stopped, then again while playing | Copies sit exactly on the originals both times; copies are selected |
| 4 | After ⌘V press → 16 times, then Esc | Copy is one bar to the right, originals whole |
| 5 | After ⌘V press → once, then Esc | The originals whose starts the copies cover are removed, the ones running in are cut |
| 6 | Select 3 notes, ⇧→ ×2, ⇧← ×1 | Each is one 1/16 longer, starts unchanged |
| 7 | Select notes vel 40/80/125, ⌥↑ | 50/90/127. ⌥⇧↓ → 49/89/126 |
| 8 | ⌘Z through steps 6–7 | Each press undone one at a time |
| 9 | Settings → Shortcuts | The rows from contracts/piano-roll-keys.md are listed |

Follow the project's UI verify recipe (memory: env-guarded hook + screencapture,
`NOTA_DATA_DIR` isolation) for screenshots of rows 2 and 9.
