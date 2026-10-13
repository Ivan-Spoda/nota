# Research: Default Project Template

All findings come from reading the current code on `develop` (2026-10-13).

## R1 — Where "a new project" is created today

- **Decision**: Two entry points get the template: `MainWindow.OnMenuNew`
  (`src/managed/Nota.App/MainWindow.Files.cs`) — reached from File → New, the
  `file.new` shortcut and the welcome screen's "New project" — and
  `MainWindow.OnOpenedRecoveryCheck` (`MainWindow.Recovery.cs`), which marks the empty
  startup workspace clean before the welcome screen / recovery prompt.
- **Rationale**: These are the only places a project starts empty. Every other path —
  `OpenProject` (menu, browser, recent, recovery `ApplyPendingRecovery`, version switch
  in `MainWindow.History.cs`) — goes through `ProjectService.Apply`, which resets the
  engine and rebuilds from the document, so FR-009 holds without changes there.
- **Alternatives considered**: Building the template inside `Engine::reset()` — rejected:
  `reset` is also the first step of every project load, and the engine should not hold
  app-level content policy.

## R2 — Undo history after building the template (FR-007)

- **Finding**: Every structural edit (`AddInstrumentTrack`, `AddReturnTrack`,
  `AddBuiltinDevice`, `AddMidiClip`, `SetTrackName`) calls `Engine::publish` →
  `pushUndo()`. The only way to empty the undo/redo stacks today is
  `nota_engine_reset`, which also drops all tracks. An undo group would still leave one
  step that removes the whole template.
- **Decision**: Add `nota_engine_clear_undo_history(NotaEngine*)` to the C ABI
  (message thread only): clears `undoStack_` / `redoStack_` and the undo-group state,
  exactly the tail of `Engine::reset()`. Expose it as `IAudioEngine.ClearUndoHistory()`.
- **Rationale**: Smallest change that makes "the template is the baseline" true. Safe
  for the audio thread: the stacks hold old authoring graphs only; the live graph is
  `authoring_`, and every published graph is also kept alive by `retired_` until the
  audio thread's epoch passes it (the same reasoning that makes `reset()` safe). No
  allocation, lock or I/O happens on the audio thread (AR-6).
- **Alternatives considered**: (a) A managed-side "undo floor" that hides early steps —
  rejected: the engine would still undo past it via MCP / remote. (b) Building the
  template as raw publishes with no checkpoints through a dedicated ABI call
  (`nota_engine_load_default_template`) — rejected: puts content policy in the engine.
- **Note (out of scope)**: `ProjectService.Apply` has the same property — after opening
  a project, Undo removes loaded tracks one by one. The new call would fix that too,
  but that is a separate bug and a separate PR (Principle V).

## R3 — Making the return effects fully wet (FR-004)

- **Finding**: Both built-ins have a `Dry/Wet` param (index 12) and a `Wet Only` switch:
  Nota Reverb (built-in kind 2) param 19, Nota Delay (kind 3) param 22. `Wet Only`
  forces mix = 1 and dry gain = 0; the device cards show it as the WET ONLY switch
  (`ReverbDeviceBody.cs`).
- **Decision**: After `AddBuiltinDevice`, set `Wet Only` = 1 on each return effect via
  `DeviceSetParam`. Leave every other param at its default.
- **Rationale**: Explicit and visible to the user (the switch reads on), survives
  save/load like any param, and does not depend on the current `Dry/Wet` default.
- **Alternatives considered**: `Dry/Wet` = 1.0 — works for the wet path but leaves the
  `Dry Level` stage meaningful only through the mix law; `Wet Only` is the device's own
  "return use" switch.

## R4 — What the instrument and audio tracks look like

- **Decision**: Build them the way `MainWindow.AddTrack` does: `AddInstrumentTrack()`
  (Nota Synth) plus the seed 1-bar MIDI clip at beat 0 (`AddMidiClip(t, 0, 4)`), and
  `AddAudioTrack()`. Engine-assigned names and colours are kept. Returns get
  `SetTrackName("Reverb")` / `SetTrackName("Delay")` (FR-006).
- **Rationale**: FR-002 / FR-006 — a template track must be indistinguishable from one
  added by hand. Order of creation gives top-to-bottom order Instrument, Audio; returns
  always sit in their own section as A, B.
- **Alternatives considered**: No seed clip on the template instrument — rejected for
  consistency with the manual path; the user can delete it like any clip.

## R5 — Where the template code lives

- **Decision**: A small static `ProjectTemplate.ApplyDefault(IAudioEngine engine)` in
  `src/managed/Nota.Infrastructure/Persistence/` next to `ProjectService.Apply`. It
  assumes a freshly reset engine, builds the four tracks and two devices, and finishes
  with `engine.ClearUndoHistory()`. `MainWindow` calls it in both entry points, then
  `RefreshAfterLoad()` and `MarkProjectClean()` as it already does.
- **Rationale**: Engine-only logic with no UI dependency, so the smoke test can run it
  against the real engine. Mirrors where project-shaping code already lives.
- **Alternatives considered**: Inline in `MainWindow.Files.cs` — rejected: not testable
  from `tests/Nota.SmokeTest`.

## R6 — Dirty state (FR-007, SC-004)

- **Finding**: Unsaved-changes detection is fingerprint-based (`MarkProjectClean` stores
  `CurrentFingerprint()`; `HasUnsavedChanges` compares). Both entry points already call
  `MarkProjectClean()` at the end.
- **Decision**: Build the template before that call; no other change needed.

## R7 — Effects on other consumers

- MCP (`Nota.Mcp`) and the phone remote (`Nota.Remote`) have no "new project" command;
  they see the template tracks as ordinary tracks. No change.
- Smoke test: the engine still starts empty and `reset()` is unchanged, so existing
  tests are unaffected. New ABI surface → new smoke checks (Principle IV).
