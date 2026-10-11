---
description: "Task list for MP3/FLAC export implementation"
---

# Tasks: Export to MP3 and FLAC

**Input**: Design documents from `/specs/001-mp3-flac-export/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ — all present.

**Tests**: Included — the spec's compliance checklist and constitution IV mandate smoke-test coverage; test tasks are the story acceptance gates.

**Organization**: Tasks grouped by user story (US1 FLAC master → US2 MP3 master → US3 stems → US4 MCP) so each story is independently deliverable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to
- Exact file paths in every description

## Path Conventions

- **Engine (C++)**: `src/native/nota.engine/` (C ABI: `include/nota/nota_engine.h`)
- **Managed app (C#)**: `src/managed/<Project>/`
- **Tests**: `tests/Nota.SmokeTest/` — the end-to-end gate every PR must pass

---

## Phase 1: Setup (vendor the codecs, clear the license gate)

**Purpose**: New native dependencies in place and registered — nothing else can build without them.

- [x] T001 Add libFLAC and LAME rows to `LICENSES/third-party.md` following the existing table format (`| Dependency | Version | Purpose | License | Status |`): libFLAC 1.5.x (BSD-3-Clause, "✅ in use"), LAME 3.100 (LGPL-2.0-or-later, "⚠️ cleared with a condition" — condition: source + license text preserved in-tree under `vendor/lame/`, static compile into the AGPL dylib); extend note 3 (MP3 patents) to cover encoding, not just dr_mp3 decoding. THE POLICY GATE — must land before any codec code.
- [x] T002 [P] Vendor libFLAC as git submodule at `src/native/nota.engine/vendor/flac` (pin latest stable 1.5.x) and wire into `src/native/nota.engine/CMakeLists.txt`: `add_subdirectory(vendor/flac)` with `BUILD_PROGRAMS=OFF BUILD_TESTING=OFF BUILD_EXAMPLES=OFF BUILD_DOCS=OFF INSTALL_MANPAGES=OFF WITH_OGG=OFF`, link static `FLAC::FLAC` into `nota_engine` (research D2)
- [x] T003 [P] Vendor LAME 3.100 as checked-in snapshot at `src/native/nota.engine/vendor/lame/`: `libmp3lame/` encoder sources only (no nasm/asm, no MMX/SSE intrinsics files, no mpglib decoder, no frontend), `include/lame/`, LGPL license text, hand-written `config.h` (integer types, `STDC_HEADERS`, no-asm defines per research D3) + minimal `CMakeLists.txt` compiling the subset into the `nota_engine` sources list (RtMidi pattern)
- [x] T004 Verify `scripts/build.sh` is green with both codecs vendored and linked (no encoder ABI yet — engine must load and all existing smoke tests pass unchanged)

**Checkpoint**: Dependencies registered and building on macOS; symbol-export scripts (`nota_exports.txt`/`nota_exports.map`) untouched (nota_* glob already covers the new prefix).

---

## Phase 2: Foundational (encoder ABI + managed seam)

**Purpose**: The streaming-encoder seam every story consumes. BLOCKS all user stories.

- [x] T005 Add the "Audio file encoders (export)" section to `src/native/nota.engine/include/nota/nota_engine.h` exactly per `specs/001-mp3-flac-export/contracts/c-abi-encoder.md`: opaque `NotaEncoder`, `nota_encoder_open_flac(path, sampleRate, bitsPerSample, dither)`, `nota_encoder_open_mp3(path, sampleRate, bitrateKbps)`, `nota_encoder_write`, `nota_encoder_close`, `nota_encoder_discard` — all `NOTA_API` `extern "C"`
- [x] T006 Implement the encoder in new `src/native/nota.engine/src/nota_encoder.cpp` + `nota_encoder.h` (AGPL SPDX headers, `namespace nota`, flat-layout naming): per-handle state (FLAC stream encoder level 5 / LAME instance), float→int with TPDF dither ONLY when `dither` flag set AND bits == 16, `write` buffering interleaved stereo, sticky-error then no-op writes, `close` = flush + finish (FLAC MD5/seektable finalization; LAME flush + Xing/LAME tag) + free, `discard` = free without finishing, open returns null on failure (bad path / unsupported bits / unsupported rate), MP3 sample rates restricted to 44100/48000 per contract
- [x] T007 [P] Add `ExportFormat { Wav, Flac, Mp3 }` enum and `Format = ExportFormat.Wav`, `Mp3BitrateKbps = 320` fields to `ExportRequest` in `src/managed/Nota.Application/Ports/IAudioExporter.cs` (constraint: `Mp3`×`Float32` invalid, `Mp3BitrateKbps ∈ {128,192,256,320}`, defaults per spec FR-002); rename `WavAudioExporter` → `AudioExporter` (file `src/managed/Nota.Infrastructure/Audio/AudioExporter.cs`), update DI in `src/managed/Nota.App/App.axaml.cs` and the instantiation in `tests/Nota.SmokeTest/Program.cs` — WAV behaviour byte-identical
- [x] T008 [P] Add `src/managed/Nota.Infrastructure/Interop/NativeMethods.Encode.cs` partial (LibraryImport, `Lib = "nota_engine"`, UTF-8 path marshalling: `EncoderOpenFlac/EncoderOpenMp3 → IntPtr`, `EncoderWrite(IntPtr, float[], int) → NotaResult`, `EncoderClose`, `EncoderDiscard`) and `src/managed/Nota.Infrastructure/Audio/NativeEncoder.cs` wrapper with the `WavWriter` shape (ctor throws on null handle, `WriteFrames(float[] interleaved, int frames)`, `Dispose` = close, `Discard()` for failure cleanup — exactly-once close/discard enforced)
- [x] T009 Native smoke hook: extend `tests/Nota.SmokeTest/Program.cs` with an ABI-level check (banner block style, `Check` calls): `EncoderOpenFlac` on an invalid path returns null-ptr; open-write-discard on a valid temp path leaves a deletable file

**Checkpoint**: Native encoder + managed wrapper exist and pass ABI-level smoke; WAV export still byte-identical (existing M6-4 tests green).

---

## Phase 3: User Story 1 — FLAC master export (Priority: P1) 🎯 MVP

**Goal**: A user exports a lossless FLAC master through the existing dialog; decoded output matches the WAV render at the same bit depth.

**Independent Test**: Export a project as FLAC 24-bit; re-import into Nota (`DecodeMono`) and compare against the equivalent WAV export — matches within 24-bit quantization (smoke block T012 proves it headlessly).

### Implementation for User Story 1

- [x] T010 [US1] FLAC branch in `src/managed/Nota.Infrastructure/Audio/AudioExporter.cs`: single-pass path renders through `NativeEncoder` (open_flac with request.Depth∈{Pcm16,Pcm24}, dither = request.Dither && Pcm16); normalize path keeps `RenderToTemp` then `Transcode` reads temp, applies make-up gain in the managed float buffer, writes final FLAC via `NativeEncoder`; temp deleted in `finally`; progress sub-spans unchanged
- [x] T011 [US1] Dialog in `src/managed/Nota.App/ExportWindow.cs`: format combo `{WAV, FLAC, MP3}` default WAV; FLAC adapts depth list to 16/24 (no float32 row), dither pill enabled ⇔ 16-bit (existing `enabled:` predicate pattern), footer estimate `0.65 × sec × sr × 2 × (2|3)` with `≈`, button label "Export FLAC"; `ExportOptions` record gains `Format` + `BitrateKbps`; picker in `src/managed/Nota.App/MainWindow.Files.cs` for master FLAC: filter `("FLAC audio", "*.flac")`, `DefaultExtension = "flac"`, suggested name `Nota Export.flac` (matrix: `contracts/ui-export-dialog.md`)
- [x] T012 [US1] Smoke tests in `tests/Nota.SmokeTest/Program.cs`: (a) FLAC 24-bit round-trip — render known audible pattern, export via `AudioExporter`, decode via `NotaEngine.DecodeMono` (dr_flac), RMS/peak ≈ WAV reference within quantization, file magic `fLaC`; (b) normalize → FLAC peak ≤ 0.8913; (c) `scripts/build.sh` green

**Checkpoint**: US1 complete — FLAC master exportable from the dialog and proven lossless headlessly.

---

## Phase 4: User Story 2 — MP3 master export (Priority: P2)

**Goal**: A user exports a shareable MP3 (CBR, default 320 kbps); invalid combinations (96 kHz, dither) are unreachable.

**Independent Test**: Export MP3 at 320 — decodes audible in Nota import and a common OS player; duration within codec padding tolerance; size ≈ bitrate × duration.

### Implementation for User Story 2

- [x] T013 [US2] MP3 branch in `src/managed/Nota.Infrastructure/Audio/AudioExporter.cs`: single-pass + normalize-transcode paths write through `NativeEncoder` (open_mp3, `Mp3BitrateKbps`, dither never applied — lossy); FLAC-branch structure reused
- [x] T014 [US2] Dialog MP3 options in `src/managed/Nota.App/ExportWindow.cs`: quality combo `320 / 256 / 192 / 128 kbps` default 320 replaces depth combo (depth hidden), sample rate 96 000 disabled/greyed while MP3 selected, dither pill greyed ("lossy format" hint), estimate `bitrateKbps × 125 × sec` bytes, button "Export MP3", picker filter `*.mp3` / `DefaultExtension = "mp3"` / `Nota Export.mp3` in `src/managed/Nota.App/MainWindow.Files.cs`
- [x] T015 [US2] Smoke tests in `tests/Nota.SmokeTest/Program.cs`: MP3 320 CBR export — decodes audible (DecodeMono/dr_mp3), duration within ±100 ms of the render, file size within ±10 % of `bitrate×duration`, ID3/Xing header present at file start

**Checkpoint**: US1 + US2 both independently working; no invalid MP3 combination reachable in the dialog.

---

## Phase 5: User Story 3 — Stems in the chosen format (Priority: P3)

**Goal**: Stems export produces one decodable file per non-return track in the selected format, shared normalize gain preserved.

**Independent Test**: Export stems with FLAC (and spot-check MP3): every file decodes; naming scheme carries the format extension; normalized stems still sum to the target.

### Implementation for User Story 3

- [x] T016 [US3] `ExportStems` format-aware in `src/managed/Nota.Infrastructure/Audio/AudioExporter.cs`: per-track sink selection (WavWriter/NativeEncoder) with the format's extension in the existing `$"{k+1:00} {Inst|Audio} {trackId}"` naming; shared make-up gain from the full-mix peak unchanged; stems path in `src/managed/Nota.App/MainWindow.Files.cs` passes format through (folder picker untouched)
- [x] T017 [US3] Smoke test in `tests/Nota.SmokeTest/Program.cs`: FLAC stems on a multi-track project — one file per non-return track, each decodes (dr_flac) with the per-track isolation property the WAV stem test asserts today (mirror the 8439–8466 block)

**Checkpoint**: All three delivery formats work for master and stems.

---

## Phase 6: User Story 4 — MCP export tools (Priority: P4)

**Goal**: `export_master`/`export_stems` accept `format` (`wav` default | `flac` | `mp3`) and `bitrateKbps` (default 320), keeping programmatic/interactive parity.

**Independent Test**: MCP `export_master(format: "flac")` writes a decodable FLAC; existing calls without new params behave byte-identically.

### Implementation for User Story 4

- [x] T018 [US4] Parameters in `src/managed/Nota.Mcp/Tools/ExportTools.cs` per `contracts/mcp-export-tools.md`: `format`/`bitrateKbps` on both tools, `ParseFormat` in the existing `ParseDepth` style, float32→pcm24 fallback for FLAC, engine rate >48 kHz clamped to 48 kHz with a note in the return string, updated tool descriptions (schema-visible), error-string convention for bad tokens
- [x] T019 [US4] Smoke test in `tests/Nota.SmokeTest/Program.cs` next to the existing MCP export test (≈13510): `export_master(format: "flac")` → decodable file; default call (no new args) unchanged WAV

**Checkpoint**: Full parity — dialog, exporter API, MCP.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Multi-story verification, docs, platforms.

- [x] T020 Visual verification of the dialog matrix per the UI verify recipe (env-guarded hook + `screencapture`, `NOTA_DATA_DIR` isolation): screenshots of WAV/FLAC/MP3 option states + 96 kHz disabled under MP3; sanity-check Ember conformance (`nota-design` skill) — adaptive rows reuse existing `Combo`/`WorkingToggle` vocabulary
- [ ] T021 Cross-platform builds: `scripts/build-linux.sh` green (gcc/ALSA path); `scripts/build-win.ps1` in VS dev shell + CI package job green — LAME-on-MSVC is the flagged risk: if the C-only subset stalls on MSVC, apply the research D3 fallback (build LAME per-platform in `scripts/`) and record the deviation in `specs/001-mp3-flac-export/research.md`
- [x] T022 Run `specs/001-mp3-flac-export/quickstart.md` end-to-end on macOS (build → UI matrix walk → smoke block) and record results

### Nota closing tasks (always include — these are the PR gates)

- [x] T023 Run `scripts/build.sh` (or the Linux/Windows siblings) until native + managed + smoke test are green
- [x] T024 AGPL-3.0-only SPDX two-line header on every new source file (`nota_encoder.cpp/.h`, `NativeEncoder.cs`, `NativeMethods.Encode.cs`, `AudioExporter.cs` rename) — see CONTRIBUTING.md
- [x] T025 New dependency: license checked + rows added to `LICENSES/third-party.md` (done in T001 — verify final state matches shipped versions)
- [x] T026 User-visible change → `CHANGELOG.md` `[Unreleased]` entry (formats, dialog options, MCP params)
- [x] T027 User-visible capability change → FEATURES.md via the `nota-features` skill; user docs pages (export, EN+RU, re-shot) via the `nota-docs` skill; `nota-shortcuts` N/A (no new bindings)
- [x] T028 Engine behaviour change → `tests/Nota.SmokeTest` extended (T009/T012/T015/T017/T019 — verify coverage of the compliance-checklist list: FLAC lossless round-trip, MP3 validity/size, failure paths)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: T001 first (license gate); T002 ∥ T003 (different vendor trees); T004 last in phase.
- **Phase 2 (Foundational)**: BLOCKS all stories. T006 needs T002+T003+T005; T007 ∥ T008 (different projects); T009 needs T006+T008.
- **Phase 3 (US1)**: needs Phase 2 complete. T010 → T011 → T012.
- **Phase 4 (US2)**: needs Phase 2 (T013 reuses US1's branch structure but no hard dep — can run after T010 for pattern reuse).
- **Phase 5 (US3)**: needs T010 or T013 (per-format sink exists); T016 → T017.
- **Phase 6 (US4)**: needs T007 (request fields) + any one story's encoder path.
- **Phase 7**: after all stories to be shipped.

### User Story Dependencies

- **US1 (P1)**: after Foundational — no story deps. **This is the MVP.**
- **US2 (P2)**: after Foundational; soft-depends on US1's exporter pattern (T010).
- **US3 (P3)**: after US1 or US2 (needs at least one compressed sink).
- **US4 (P4)**: after Foundational + ≥1 story.

### Parallel Opportunities

- T002 ∥ T003 (vendor trees are disjoint)
- T007 ∥ T008 (Application/Infrastructure files are disjoint)
- T020 ∥ T021 (UI verification vs cross-platform builds)
- Story smoke tests (T012/T015/T017/T019) touch one file — serialize them, but their *writing* can be drafted in parallel

---

## Parallel Example: Phase 2

```bash
# After T005 lands, launch together (different files):
Task: "T007 ExportFormat + ExportRequest fields + rename (Nota.Application / DI / SmokeTest refs)"
Task: "T008 NativeMethods.Encode.cs + NativeEncoder.cs (Nota.Infrastructure)"
# Meanwhile T006 (engine encoder) proceeds independently against T005's header.
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 (codecs vendored, licenses registered) → 2. Phase 2 (ABI + seam) → 3. Phase 3 (FLAC end-to-end) → **STOP and VALIDATE** against `quickstart.md` steps 1–3.
2. Ship/demo FLAC-only is viable: every later phase is additive.

### Incremental Delivery

1. Foundation → 2. +US1 FLAC (MVP) → 3. +US2 MP3 → 4. +US3 stems → 5. +US4 MCP → 6. Polish (docs, platforms, visual verification).

### Solo-developer order

Strictly sequential T001→T028; the [P] marks matter only if parallelizing.

---

## Notes

- Implementation notes (2026-10-11): T013's exporter branch landed together with
  T010 (one format-dispatch change in `AudioExporter` serves both codecs); T014's dialog
  work landed as one adaptive-UI rewrite with T011. T006 ships without a separate
  `nota_encoder.h` — the TU is self-contained (opaque handle completes in the .cpp).
  T021 (Linux/Windows builds) cannot run on this Mac — CI (`release.yml`) must confirm;
  macOS is fully green including the universal dylib (lipo arm64+x86_64).

- [P] tasks = different files, no dependencies on incomplete tasks
- Smoke tests are acceptance gates per story (run via `scripts/build.sh`); they are written after each story's implementation because they exercise the real exporter end-to-end
- Commits are left to the maintainer — do not auto-commit after tasks
- Risk register: LAME-on-MSVC (T003/T021, fallback in research D3); libFLAC submodule pin (T002) must match `release.yml`'s `submodules: recursive` checkout
