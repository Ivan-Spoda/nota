# Implementation Plan: Default Project Template

**Branch**: `004-default-project-template` | **Date**: 2026-10-13 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/004-default-project-template/spec.md`

## Summary

Every new project — File → New, the welcome screen's "New project", and the empty
workspace on launch — starts with an instrument track (Nota Synth + seed clip), an
audio track, and two returns: "Reverb" (Nota Reverb, Wet Only) and "Delay" (Nota
Delay, Wet Only). A new `ProjectTemplate.ApplyDefault` builds it from existing engine
operations on a freshly reset engine, then calls a new C ABI function
`nota_engine_clear_undo_history` so the template is the undo baseline; the app then
marks the project clean as it already does. See [research.md](research.md).

## Nota: Read Before Planning

- **Engine work** → [`ARCHITECTURE.md`](../../ARCHITECTURE.md): one new message-thread
  ABI call; AR-6 and AR-7 apply (see Constitution Check).
- **Anything** → [`CONTRIBUTING.md`](../../CONTRIBUTING.md): licensing, PR expectations.

## Technical Context

**Language/Version**: C++20 (one engine function + ABI wrapper), C# / .NET 10 (interop,
template builder, two `MainWindow` call sites)

**Primary Dependencies**: none new

**Storage**: N/A — template content is saved by the existing project format; no format change

**Audio-Thread Impact**: None. `clear_undo_history` runs on the message thread and only
clears the undo/redo vectors of old authoring graphs; graphs are kept alive by the
engine's `retired_` list until the audio thread's epoch passes them (same as
`Engine::reset`). Template building uses the existing structural-edit path (immutable
snapshot publish, AR-5) and `DeviceSetParam` (atomic params).

**Testing**: `tests/Nota.SmokeTest` via `scripts/build.sh` — new checks for the ABI call
and for `ProjectTemplate.ApplyDefault` on a real engine (incl. save/load round-trip)

**Target Platform**: macOS, Windows, Linux desktop (unchanged)

**Project Type**: desktop app (native engine + managed UI)

**Performance Goals**: new project feels instant — 4 tracks + 2 devices, well under 100 ms

**Constraints**: no project-format change; no behaviour change for opening/recovering/
switching versions

**Scale/Scope**: ~1 engine function, 1 ABI function, 1 interop method, 1 new managed
file (~40 lines), 2 call sites, smoke tests, CHANGELOG/FEATURES/docs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Note |
|-----------|--------|------|
| I. Real-time audio safety | PASS | Message-thread only; no audio-thread alloc/lock/I/O; graphs retired via `retired_` |
| II. Boundary discipline | PASS | New call is a plain C function in `nota_engine.h`; no JUCE |
| III. Licensing hygiene | PASS | New `ProjectTemplate.cs` gets the two-line SPDX header; no dependency |
| IV. Verification gate | PASS (planned) | New ABI surface covered by the smoke test |
| V. One logical change per PR | PASS | Template + the undo-clear it needs. The pre-existing "Undo after Open removes loaded tracks" gap is **not** fixed here (separate PR) |
| VI. Docs in sync | PLANNED | CHANGELOG `[Unreleased]` (Changed), FEATURES.md via `nota-features`, user manual via `nota-docs` |
| VII. Spec-first | PASS | spec → plan → tasks |
| VIII. Safety rules not weakened | PASS | — |

**Post-design re-check**: unchanged — all PASS.

## Project Structure

### Documentation (this feature)

```text
specs/004-default-project-template/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/engine-abi.md
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/
├── native/nota.engine/
│   ├── include/nota/nota_engine.h        # + nota_engine_clear_undo_history (Undo/redo section)
│   └── src/
│       ├── Engine.h / Engine.cpp         # + Engine::clearUndoHistory() (reset() reuses it)
│       └── nota_engine_core.cpp          # + C wrapper
└── managed/
    ├── Nota.Application/Engine/IAudioEngine.cs        # + ClearUndoHistory()
    ├── Nota.Infrastructure/Interop/NativeMethods.cs   # + LibraryImport
    ├── Nota.Infrastructure/Interop/NotaEngine*.cs     # + implementation
    ├── Nota.Infrastructure/Persistence/ProjectTemplate.cs   # NEW: ApplyDefault(engine)
    └── Nota.App/
        ├── MainWindow.Files.cs           # OnMenuNew: ApplyDefault after Reset, before RefreshAfterLoad
        └── MainWindow.Recovery.cs        # OnOpenedRecoveryCheck: ApplyDefault before MarkProjectClean

tests/Nota.SmokeTest/
└── ProjectTemplateTests.cs               # NEW: ABI clear-undo + template shape + round-trip (wired from Program.cs)

CHANGELOG.md, FEATURES.md                 # user-visible change
```

**Structure Decision**: Template logic lives in Infrastructure/Persistence beside
`ProjectService.Apply` (engine-only, testable from the smoke test); `MainWindow` only
calls it. The engine gets a policy-free primitive (clear history), not the template.

## Complexity Tracking

No constitution violations.
