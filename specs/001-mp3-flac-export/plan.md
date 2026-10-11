# Implementation Plan: Export to MP3 and FLAC

**Branch**: `feat/mp3-flac-export` | **Date**: 2026-10-11 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-mp3-flac-export/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Add FLAC and MP3 to the WAV-only export flow. Codecs are vendored native —
libFLAC 1.5.x (BSD, submodule, its own CMake, static) and LAME 3.100 (LGPL,
checked-in encoder-only snapshot, compiled by our CMake like RtMidi) — and
exposed through a four-function streaming encoder added to the pure C ABI
(`nota_encoder_open_flac/_open_mp3/_write/_close/_discard`). The managed export
orchestration (render loop, progress, stems, two-pass normalize) is unchanged;
`WavAudioExporter` becomes `AudioExporter`, dispatching per-request to
`WavWriter` (WAV, untouched) or a new `NativeEncoder` P/Invoke wrapper
(FLAC/MP3). The export dialog gains a format combo with adaptive option rows
(bit depth vs CBR quality, 96 kHz gated for MP3), format-aware estimates and
picker filters; the MCP export tools gain `format`/`bitrateKbps`. The smoke
test grows a FLAC/MP3 block that round-trips through the engine's existing
dr_flac/dr_mp3 decoders. Decisions and rationale: [research.md](research.md);
interfaces: [contracts/](contracts/).

## Nota: Read Before Planning

Consult the matching source of truth before designing:

- **Engine work** → [`ARCHITECTURE.md`](../../ARCHITECTURE.md): the AR rules table is binding (AR-4 lock-free queue, AR-5 immutable snapshots, AR-6 audio-thread restrictions, AR-7 pure C ABI, AR-9 `Transport` is the only source of musical time).
- **UI work** → [`DESIGN.md`](../../DESIGN.md) (Ember design language) and the `nota-design` skill.
- **A built-in instrument / audio or MIDI effect** → the `nota-instrument` skill (DSP class, kind registration across C++ and C#, device-card UI).
- **Build / packaging** → [`BUILD.md`](../../BUILD.md).
- **Anything** → [`CONTRIBUTING.md`](../../CONTRIBUTING.md): licensing, dependency policy, PR expectations.

## Technical Context

**Language/Version**: C++20 engine (C code compiled alongside; C11 for libFLAC/LAME), C# / .NET 10 managed app; touches engine + Infrastructure + App UI + Mcp.

**Primary Dependencies**: existing — dr_libs (decode), miniaudio, Avalonia, MCP SDK. **NEW**: libFLAC 1.5.x (BSD-3-Clause, git submodule `vendor/flac`, static via its CMake) and LAME 3.100 (LGPL-2.0+, checked-in encoder-only snapshot `vendor/lame` + our CMakeLists). Both need `LICENSES/third-party.md` rows before code lands (research D6).

**Storage**: N/A — writes user-chosen export files only (`.flac`/`.mp3`); no settings persistence changes.

**Audio-Thread Impact** *(required — state explicitly)*: None. Encoding runs on the managed export background thread (audio backend stopped, same as today's `RenderOffline` export loop); the encoders are offline-only code paths, never reachable from `processBlock`. AR-6 untouched.

**Testing**: `tests/Nota.SmokeTest` (default suite, `Check` blocks) — new FLAC/MP3 export block per research D7; UI matrix verified via the headless-Avalonia screenshot recipe during implementation.

**Target Platform**: macOS 13+ (universal arm64+x86_64, Apple clang), Windows 10+ x64/arm64 (MSVC, VS generator in CI), Linux x64 (gcc). All three must compile both codecs — LAME-on-MSVC is the flagged risk (research D3).

**Project Type**: desktop app (real-time audio engine + Avalonia UI).

**Performance Goals**: offline export only — encode at ≥ render speed in practice (FLAC level 5 multithreaded comfortably exceeds; LAME C-only at 320 kbps ≈ 5–15× real-time on a modern core, acceptable for the export dialog's progress flow).

**Constraints**: no vcpkg/Conan (vendor everything, per project build contract); `scripts/build.sh` + siblings must stay green (PR gate); managed/native cross only via `nota_engine.h` C ABI; LGPL sources preserved in-tree with license text.

**Scale/Scope**: ~5 native files changed/added, ~8 managed files, 1 header section, vendor additions; songs minutes long, stereo, ≤ 96 kHz.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Verdict | How |
|---|---|---|
| I. Real-time audio safety | ✅ | Encoding is offline-only, background thread, backend stopped (same discipline as `render_offline_at`); no audio-thread code |
| II. Boundary discipline | ✅ | New surface is four `extern "C"` `NOTA_API` functions in `nota_engine.h` (contract: `contracts/c-abi-encoder.md`); no C++ types cross; no JUCE involved |
| III. Licensing hygiene | ✅ with condition | libFLAC BSD row; LAME LGPL row registered as accounted copyleft (maintainer decision, spec Clarifications) with source + license text in-tree; SPDX headers on every new file |
| IV. Verification gate | ✅ | Smoke test extended (round-trips via dr_flac/dr_mp3); `build.sh` gate covers it |
| V. One logical change per PR | ✅ | Single feature: codecs + encoder ABI + exporter dispatch + dialog + MCP + tests travel together (inseparable slice) |
| VI. Documentation in sync | ✅ planned | CHANGELOG `[Unreleased]`, FEATURES.md (`nota-features`), user docs EN+RU (`nota-docs`) |
| VII. Spec-first | ✅ | This spec/plan/tasks run |
| VIII. Safety rules never weakened | ✅ | No rule bent; LGPL acceptance is a policy decision recorded by the maintainer, not a rule change |

Post-Phase-1 re-check: design (research D1–D8, contracts, data model) introduces
no new crossing points, no audio-thread contact, no unregistered dependency —
gate still passes.

## Project Structure

### Documentation (this feature)

```text
specs/001-mp3-flac-export/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   ├── c-abi-encoder.md
│   ├── mcp-export-tools.md
│   └── ui-export-dialog.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/native/nota.engine/
├── include/nota/nota_engine.h          # + "Audio file encoders" section (AR-7)
├── src/nota_encoder.cpp/.h             # NEW: libFLAC/LAME streaming encoder
├── vendor/flac/                        # NEW: libFLAC submodule (add_subdirectory, static)
├── vendor/lame/                        # NEW: LAME encoder-only snapshot + our CMakeLists + config.h
├── CMakeLists.txt                      # vendor wiring, sources list, defines
└── nota_exports.txt / nota_exports.map # no edit needed (nota_* glob covers nota_encoder_*)

src/managed/
├── Nota.Application/Ports/IAudioExporter.cs   # + ExportFormat, request fields (WavAudioExporter renames away)
├── Nota.Infrastructure/
│   ├── Audio/AudioExporter.cs                 # renamed+extended from WavAudioExporter (dispatch per format)
│   ├── Audio/NativeEncoder.cs                 # NEW: P/Invoke wrapper, WavWriter-shaped
│   ├── Audio/WavWriter.cs                     # untouched
│   └── Interop/NativeMethods.Encode.cs        # NEW partial: nota_encoder_* LibraryImports
├── Nota.App/ExportWindow.cs                   # format combo, adaptive rows, estimates
├── Nota.App/MainWindow.Files.cs               # picker filters/extensions per format
└── Nota.Mcp/Tools/ExportTools.cs              # format/bitrateKbps params

tests/Nota.SmokeTest/Program.cs                # FLAC/MP3 export block (round-trip via DecodeMono)

LICENSES/third-party.md                        # libFLAC + LAME rows, MP3-patent note extension
```

**Structure Decision**: flat engine `src/` layout kept (new `nota_encoder.*`
pair matches the per-domain `nota_engine_*.cpp` naming for ABI impls);
codecs vendored under `vendor/` following the two existing patterns (submodule
for a full upstream tree like JUCE, checked-in snapshot for a curated subset
like dr_libs — LAME's snapshot needs our thin CMakeLists because upstream has
none). Managed changes stay inside the existing exporter seam — no new
projects, no new NuGet packages.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

None — no violations; the LGPL dependency is an accounted policy decision
(Constitution III, spec Clarifications), not a gate bypass.
