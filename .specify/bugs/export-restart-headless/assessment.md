# Bug Assessment: Export and freeze restart audio that was never running

- **Slug**: export-restart-headless
- **Created**: 2026-10-14
- **Source**: pasted text; CI run https://github.com/nota-daw/nota/actions/runs/38546277984 (host: github.com — allowlisted; the run's failed-job log was read with `gh run view --log-failed`)
- **Verdict**: valid
- **Severity**: medium

## Report (verbatim or summarized)

> Export (and track freeze) crash when no audio device is available. After an offline
> render, AudioExporter.Restore and TrackFreezer.Restore unconditionally call
> engine.Start() to "resume live audio", even when the audio backend was not running
> before the export. On a machine without an audio output device (GitHub Actions Linux and
> Windows runners) Start() throws NotaEngineException: AudioDevice and the smoke test
> aborts. Expected: export/freeze restores the backend to the state it was in before
> (restart only if it was running), and never throws from Restore on a headless machine.

CI log, Linux and Windows jobs (identical):

```
Unhandled exception. Nota.Application.NotaEngineException: AudioDevice
   at Nota.Infrastructure.NotaEngine.Check(NotaResult result) in .../Interop/NotaEngine.cs:line 244
   at Nota.Infrastructure.NotaEngine.Start() in .../Interop/NotaEngine.cs:line 43
   at Nota.Infrastructure.AudioExporter.Restore(IAudioEngine engine, ExportRequest r) in .../Audio/AudioExporter.cs:line 339
   at Nota.Infrastructure.AudioExporter.ExportMaster(IAudioEngine engine, ExportRequest r, IProgress`1 progress) in .../Audio/AudioExporter.cs:line 53
   at Program.<Main>$(String[] args) in .../tests/Nota.SmokeTest/Program.cs:line 8460
```

Preceded on Linux by ALSA `cannot find card '0'` / `Unknown PCM default`. The macOS job does
not hit it: the runner has a virtual CoreAudio output, so the restart succeeds.

## Symptom

Exporting (master or stems) or freezing/bouncing a track on an engine whose audio backend is
not running ends by *starting* the backend. With no usable output device that start fails
and `Restore` throws after the file was already written; the export reports failure (app) or
the process dies (smoke test, MCP). Expected: the backend ends in the state it was in before
the operation, and a missing device never turns a successful offline render into an error.

## Reproduction

1. On a machine with no audio output device (e.g. `ubuntu-latest` GitHub runner, no ALSA card),
   build and run `scripts/build-linux.sh`.
2. The smoke test reaches the export section (`exporter.ExportMaster(ne, req)`,
   `tests/Nota.SmokeTest/Program.cs:8486` in the current tree); `ne` was never `Start()`ed.
3. `ExportMaster` renders the file, then `Restore` → `engine.Start()` → `NOTA_ERR_AUDIO_DEVICE`
   → unhandled `NotaEngineException`, exit code 134 (Linux) / 0xE0434352 (Windows).

In the app the same path is reachable when the output device disappears while Nota runs
(unplugged USB interface, backend stopped) and the user then exports or freezes —
[NEEDS CLARIFICATION: not reproduced in the app; the app currently needs a device to start
at all, `MainWindowViewModel` calls `Engine.Start()` unguarded].

## Suspected Code Paths

- `src/managed/Nota.Infrastructure/Audio/AudioExporter.cs:333` `Restore()` — calls `engine.Start()`
  unconditionally; reached from `ExportMaster` (:53) and `ExportStems` (:103).
- `src/managed/Nota.Infrastructure/Audio/AudioExporter.cs:26`, `:72` — `engine.Stop()` before
  rendering; the backend state at this point is not recorded.
- `src/managed/Nota.Infrastructure/Audio/TrackFreezer.cs:120` `Restore()` — same unconditional
  `engine.Start()`; reached from `Freeze` (:31, :59, :63) and `BounceRange` (:85, :111, :115),
  stops at :25 and :79.
- `src/native/nota.engine/src/nota_engine_core.cpp:32` `nota_engine_start` — maps a failed
  backend start to `NOTA_ERR_AUDIO_DEVICE` (correct; the bug is in the caller).
- `src/native/nota.engine/include/nota/nota_engine.h` — no way to ask whether the backend is
  running. `nota_engine_sample_rate` is not a substitute: `CoreAudioBackend` and
  `MiniaudioBackend` keep `sampleRate_` after `stop()`, so it stays non-zero.
- `src/native/nota.engine/src/AudioBackend.h:67` `isRunning()` — the state exists natively,
  it is just not exposed.

## Root Cause Hypothesis

`AudioExporter` and `TrackFreezer` assume the backend is always running when an offline
render begins, so "restore" is implemented as "start". That holds in the normal app session
but not for an engine that was never started (smoke test, a future headless/CLI export) or
whose device went away. Because the managed layer cannot query the running state through
the C ABI, it cannot remember and restore it. Confidence: high — the stack trace points at
the exact call, and the code path is unconditional.

## Proposed Remediation

**Preferred**: Expose the running state and restore it faithfully.

1. C ABI: add `int32_t nota_engine_is_running(const NotaEngine*)` to `nota_engine.h`
   (returns `backend_->isRunning()`; 0 for a null engine), implemented in
   `nota_engine_core.cpp` via a new `Engine::isRunning() const`.
2. Managed: `NativeMethods.IsRunning`, `IAudioEngine.IsRunning { get; }`,
   `NotaEngine.IsRunning`.
3. `AudioExporter.ExportMaster/ExportStems` and `TrackFreezer.Freeze/BounceRange`: read
   `bool wasRunning = engine.IsRunning` before `engine.Stop()`, pass it to `Restore`, and call
   `engine.Start()` only when it was running.

A device that vanished *during* the render (was running, start now fails) still throws —
that is a real device error, and the callers in `MainWindow.Files.cs` / `MainWindow.Freeze.cs`
already surface export failures; this fix does not change that path.

**Alternatives**:
- Catch `NotaEngineException` in `Restore` and swallow it. Smallest change, no ABI addition,
  but it still starts audio on an engine that was deliberately idle (the macOS smoke run
  currently opens the real device mid-test) and hides genuine device loss after a render.
- Infer the state from `SampleRate > 0`. Rejected: backends keep the last rate after `stop()`.

**Files likely to change**:
- `src/native/nota.engine/include/nota/nota_engine.h`
- `src/native/nota.engine/src/nota_engine_core.cpp`
- `src/native/nota.engine/src/Engine.h`
- `src/managed/Nota.Application/Engine/IAudioEngine.cs`
- `src/managed/Nota.Infrastructure/Interop/NativeMethods.cs`
- `src/managed/Nota.Infrastructure/Interop/NotaEngine.cs`
- `src/managed/Nota.Infrastructure/Audio/AudioExporter.cs`
- `src/managed/Nota.Infrastructure/Audio/TrackFreezer.cs`
- `tests/Nota.SmokeTest/Program.cs`

**Tests to add or update**:
- Smoke: a fresh engine reports `IsRunning == false`; after `ExportMaster`, `ExportStems`,
  `TrackFreezer.Freeze` and `BounceRange` on that engine it is still `false` and no exception
  is thrown (this is what fails on the headless CI runners today).
- Smoke (device present only, behind the existing opt-in `--audiocheck`): a started engine is
  running again after an export.

## Risks & Considerations

- Additive C ABI change: the constitution requires new API surface to be reflected in
  `nota_engine.h` and the managed bindings together; the engine version bumps with the next
  release as usual (minor/patch per the release rules).
- Several files in the fix (`nota_engine.h`, `nota_engine_core.cpp`, `Engine.h`,
  `IAudioEngine.cs`, `NativeMethods.cs`, `NotaEngine.cs`, `Program.cs`) carry uncommitted
  pan-law / stereo-map work in the current working tree; the fix must land on its own branch
  from `origin/develop` so the two do not mix.
- `isRunning()` is read on the UI/worker thread while the audio thread may be running; the
  backends' `running_` is a plain `bool` written only by start/stop on the control thread, so
  the read is safe from the same thread that calls `Stop()` (the exporter's worker thread
  calls both).

## Open Questions

- [NEEDS CLARIFICATION: should the app itself tolerate starting with no output device (offline
  mode) — out of scope for this fix, but the same unguarded `Start()` sits in
  `MainWindowViewModel`.]
