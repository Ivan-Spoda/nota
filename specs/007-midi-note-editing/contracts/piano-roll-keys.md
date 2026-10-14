# Contract: Piano-roll keyboard bindings

These apply when the note grid has focus. ⌘ = Ctrl and ⌥ = Alt off macOS. The Settings →
Shortcuts list must show exactly these rows (nota-shortcuts).

| Keys | With a selection | Status |
|---|---|---|
| ← / → | Move by one grid step | unchanged |
| ↑ / ↓ | Move by a semitone | unchanged |
| ⇧↑ / ⇧↓ | Move by an octave | unchanged |
| **⇧← / ⇧→** | **Shorten / lengthen by one grid step (start kept)** | new |
| **⌥↑ / ⌥↓** | **Velocity +10 / −10** | new |
| **⌥⇧↑ / ⌥⇧↓** | **Velocity +1 / −1** | new |
| ⌘C / ⌘X | Copy / cut | unchanged |
| **⌘V** | **Paste on top of the copied notes' original position; copies selected** | changed |
| ⌘D | Duplicate after the selection (overwrites what it lands on) | unchanged |
| Esc | Deselect (applies pending overwrites) | changed: also settles |
| ⌥ while dragging | Free positioning (no snap) | unchanged |

Without a selection, ⌥↑/↓ and ⇧←/→ are not handled; they fall through.

## Overwrite contract

Overwrites happen when a placement settles (data-model §2). Each settle is one undo step,
separate from the edits that led to it.
