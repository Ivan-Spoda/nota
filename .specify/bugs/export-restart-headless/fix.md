# Bug Fix: Export and freeze restart audio that was never running

- **Slug**: export-restart-headless
- **Fixed**: 2026-10-14
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

The engine now reports whether its audio backend is running (`nota_engine_is_running` →
`IAudioEngine.IsRunning`). `AudioExporter` and `TrackFreezer` read it before stopping the
backend for an offline render and restart it afterwards only if it was running, so an engine
that was never started — headless CI runners, a machine without an output device — stays
stopped and the export no longer throws.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/native/nota.engine/include/nota/nota_engine.h` | modified | new `nota_engine_is_running` (additive ABI) |
| `src/native/nota.engine/src/Engine.h` | modified | `Engine::isRunning()` → `backend_->isRunning()` |
| `src/native/nota.engine/src/nota_engine_core.cpp` | modified | C entry point; 0 for a null engine |
| `src/managed/Nota.Infrastructure/Interop/NativeMethods.cs` | modified | `IsRunning` import |
| `src/managed/Nota.Application/Engine/IAudioEngine.cs` | modified | `bool IsRunning { get; }` |
| `src/managed/Nota.Infrastructure/Interop/NotaEngine.cs` | modified | `IsRunning` property |
| `src/managed/Nota.Infrastructure/Audio/AudioExporter.cs` | modified | `ExportMaster` / `ExportStems` remember the state; `Restore` starts only if it was running |
| `src/managed/Nota.Infrastructure/Audio/TrackFreezer.cs` | modified | same for `Freeze` / `BounceRange` (6 `Restore` call sites) |
| `tests/Nota.SmokeTest/Program.cs` | added tests | four headless checks; stale comment on the normalize test corrected |

## Diff Highlights

```csharp
bool wasRunning = engine.IsRunning; // restart afterwards only what was running
engine.Stop();               // halt the audio backend: no audio thread during render
...
if (wasRunning) engine.Start();
```

## Tests Added or Updated

- `Program.cs` "export leaves a backend that was never started stopped" — `ExportMaster` on a
  fresh engine (the call that crashed on the Linux/Windows runners).
- `Program.cs` "stem export leaves a backend that was never started stopped" — `ExportStems`.
- `Program.cs` "bounce leaves a backend that was never started stopped" — `TrackFreezer.BounceRange`.
- `Program.cs` "freeze leaves a backend that was never started stopped" — `TrackFreezer.Freeze`.

The device-present case (a running engine restarts after export) is exercised by the app
itself; the opt-in `--audiocheck` was not extended — see Deviations.

## Local Verification

- Commands run: `scripts/build.sh` on macOS (worktree on `fix/ci-green` from `origin/develop`)
  → native + managed build OK; smoke test: the four new checks PASS; one unrelated failure,
  `bg warp: plays identically to the synchronous rebuild (max diff 5.4E-001)`.
- Re-ran the smoke test twice without rebuilding → `SMOKE TEST PASSED` both times; the
  `bg warp` check is intermittent and does not touch export/freeze (see Follow-ups).
- Not verified locally: the headless Linux/Windows path itself (this Mac has an output
  device) — that is what the CI run on the pull request checks.

## Deviations from Assessment

- The assessment suggested also extending the opt-in `--audiocheck` with "a started engine is
  running again after an export". Not done: the check needs audio hardware and never runs in
  CI, and the restart path is the pre-existing behaviour, unchanged for a running engine.

## Follow-ups

- `bg warp: plays identically to the synchronous rebuild` failed once in three local runs —
  a likely race between the background warp worker and the comparison render; worth its own
  `/speckit-bug-assess`.
- The doc comments on `nota_engine_sample_rate` / `NotaEngine.SampleRate` say "0 if not
  started", but the backends keep the last rate after `stop()`.
- `MainWindowViewModel` still calls `Engine.Start()` unguarded, so the app cannot open
  without an output device (assessment, Open Questions).
- `CHANGELOG.md` `[Unreleased]`: add a Fixed entry when this lands.
