# Contract: clear undo history

## C ABI — `src/native/nota.engine/include/nota/nota_engine.h` (Undo / redo section)

```c
/* Drop the undo and redo history without touching the session: the current state
 * becomes the baseline (e.g. a freshly built default project). Open undo groups are
 * closed. Message thread only. No-op for a null engine. */
NOTA_API void nota_engine_clear_undo_history(NotaEngine* engine);
```

- After the call: `nota_engine_can_undo` and `nota_engine_can_redo` return 0;
  `nota_engine_undo` / `nota_engine_redo` return `NOTA_ERR_UNKNOWN`.
- The session (tracks, clips, devices, params, transport) is unchanged; no graph is
  published.
- The next structural edit is undoable as usual (back to the cleared baseline).
- Audio thread: not involved. Graphs dropped from the stacks stay alive through the
  engine's retired list until the audio thread has moved past them.

## Managed — `IAudioEngine` (`src/managed/Nota.Application/Engine/IAudioEngine.cs`)

```csharp
/// <summary>Forget the undo/redo history; the current state becomes the baseline.</summary>
void ClearUndoHistory();
```

Implemented in `NotaEngine` over a `LibraryImport` of `nota_engine_clear_undo_history`.

## Managed — `ProjectTemplate` (`src/managed/Nota.Infrastructure/Persistence/ProjectTemplate.cs`)

```csharp
/// <summary>Builds the default new-project content on a freshly reset engine and makes it
/// the undo baseline. Returns the instrument track id (to select/arm).</summary>
public static int ApplyDefault(IAudioEngine engine);
```

Precondition: the engine was just reset (no tracks). Postconditions: see
[data-model.md](../data-model.md).
