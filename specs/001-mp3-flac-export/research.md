# Research: Export to MP3 and FLAC

**Feature**: specs/001-mp3-flac-export | **Date**: 2026-10-11

Findings consolidated from codebase exploration (export pipeline, engine build,
ABI, smoke test) and external research (libFLAC/LAME build systems). Each
decision records rationale and rejected alternatives.

---

## D1 — Encoding lives native, behind a streaming C-ABI encoder

**Decision**: The managed export loop (render via `RenderOffline`, chunk 8192)
stays as the orchestrator. File writing for FLAC/MP3 goes through a new
stateless-per-instance streaming encoder in the engine, exposed as four pure C
functions in `nota_engine.h`: `nota_encoder_open_flac` / `nota_encoder_open_mp3`
(returns an opaque handle or null), `nota_encoder_write` (interleaved stereo
floats), `nota_encoder_close` (flush, finish, free), `nota_encoder_discard`
(free without finishing — used on failure so the managed side can delete the
partial file). Full signatures and semantics: `contracts/c-abi-encoder.md`.

**Rationale**: Mirrors the existing architecture exactly — the managed side
already owns render loops, progress sub-spans, stems and the two-pass normalize
(`WavAudioExporter.RenderToTemp`/`Transcode`, which is already an
encode-after-render transcode for WAV). A streaming encoder is the smallest ABI
surface that serves both the single-pass path (render → encode) and the
normalize path (temp float32 WAV → gain → encode). dr_libs already decode
FLAC/MP3 natively on import, so encode-native keeps all codec work in one place
and lets the smoke test exercise codecs through the same dylib.

**Alternatives considered**:
- *Whole-export-in-engine ABI* (`nota_engine_export(path, …)` doing render +
  encode natively): moves progress/stems/normalize logic into C++ for no user
  benefit; bigger blast radius. Rejected.
- *Managed NuGet encoders* (NAudio &c.): new managed deps with mixed licenses,
  split codec ownership (decode native / encode managed). Rejected in clarify.

---

## D2 — libFLAC vendored as a git submodule, built by its own CMake

**Decision**: Pin libFLAC 1.5.x (latest stable) as a git submodule at
`vendor/flac` (same pattern as `vendor/JUCE`), consumed with
`add_subdirectory(vendor/flac)` and options `BUILD_PROGRAMS=OFF`,
`BUILD_TESTING=OFF`, `BUILD_EXAMPLES=OFF`, `BUILD_DOCS=OFF`,
`INSTALL_MANPAGES=OFF`, `WITH_OGG=OFF` (native FLAC container only), linking
the static `FLAC::FLAC` target into `nota_engine`. FLAC row added to
`LICENSES/third-party.md` (BSD-3-Clause, ✅ in use).

**Rationale**: libFLAC 1.5's official build system is CMake (explicitly the
preferred MSVC path since 1.5.0); static is its default
(`BUILD_SHARED_LIBS=OFF`) and the exported `FLAC::FLAC` target makes
consumption one line. A submodule keeps the ~1k-file source tree out of the
repo while `release.yml` already checks out `submodules: recursive`. The
`nota_exports.txt` / `nota_exports.map` link scripts (export only `nota_*`)
keep libFLAC's symbols private inside the dylib; Windows exports nothing
without `__declspec(dllexport)`.

**Alternatives considered**:
- *Checked-in source snapshot* (dr_libs pattern): libFLAC is a full tree with a
  build system; snapshotting invites drift. Rejected.
- *Ogg FLAC*: no user need; dr_flac import expects native FLAC anyway. Off.
- *Multithreaded encode* (new in 1.5): enabled by default when threads are
  found; harmless for export-sized files, no option pinned.

---

## D3 — LAME vendored as a checked-in encoder-only snapshot + our CMakeLists

**Decision**: Vendor LAME 3.100 sources at `vendor/lame` as a checked-in
snapshot containing only what the encoder library needs: `libmp3lame/` sources,
`include/lame/`, the LGPL license text, plus a hand-written minimal `config.h`
and our own `CMakeLists.txt` that compiles the C-only encoder subset (no nasm
asm, no MMX/SSE intrinsics files, no `mpglib` decoder, no frontend) directly
into the `nota_engine` sources list — the exact pattern RtMidi uses
(`vendor/rtmidi/RtMidi.cpp` compiled straight into the target). LAME row added
to `LICENSES/third-party.md` (LGPL-2.0+, ⚠️/✅ accounted — see D6) with a note
covering dynamic-linking posture (LGPL code statically compiled into the AGPL
dylib; obligations met by shipping source + register row).

**Rationale**: LAME 3.100 ships no CMake (autotools; MSVC projects only under
`dshow`), but `libmp3lame` is a compact C library that compiles with MSVC,
clang and gcc once a ~30-line `config.h` supplies the defines configure would
have found (standard integer types, `STDC_HEADERS`, no asm). Encoder-only is
sufficient — decoding already exists via dr_mp3. C-only (no takehiro asm /
vector routines) costs encode speed we do not need for offline export.

**Alternatives considered**:
- *vcpkg / package manager*: the project vendors everything (no vcpkg/Conan
  anywhere in the build); introducing one for a single dep changes the build
  contract for every contributor. Rejected.
- *ShiftMediaProject/lame fork* (native VS projects): unofficial fork, VS-only
  — no Linux/macOS story. Rejected.
- *System encoders* (CoreAudio on macOS, MediaFoundation on Windows): three
  platforms would need three implementations, Linux has none; breaks the
  one-smoke-test-everywhere property. Rejected.
- *Permissive-only MP3 encoder*: no qualifying encoder of usable quality
  exists (shine is LGPL too, and notably worse). Rejected — hence the LGPL
  acceptance recorded in the spec's Clarifications.

**Risk (tracked as the plan's only spike)**: the LAME `config.h` + file-subset
must compile on all three toolchains (Apple clang universal, gcc, MSVC/clang-cl
+ VS generator for CI packaging). If the snapshot route stalls, fallback is
building LAME via its configure/CMake ports in `scripts/` per platform —
decision point gated in tasks.

---

## D4 — Managed layer: format enum on the request, exporter dispatches writers

**Decision**: `Nota.Application`: add `ExportFormat { Wav, Flac, Mp3 }`;
`ExportRequest` gains `ExportFormat Format = Wav` and `int Mp3BitrateKbps = 320`
(CBR, per spec FR-002). `WavBitDepth` stays (WAV + FLAC use it; `Float32` is
WAV-only). `WavAudioExporter` is renamed `AudioExporter` (it is no longer
WAV-only; DI registration and the smoke-test instantiation are the only
referencing sites). Inside it:
- single-pass path: render loop writes through `WavWriter` (WAV, unchanged) or
  the new `NativeEncoder` wrapper (FLAC/MP3);
- normalize path: unchanged `RenderToTemp` (float32 WAV + `TruePeakScanner`),
  then `Transcode` reads the temp, applies make-up gain in managed code
  (multiply in the float buffer — keeps gain out of the ABI), and writes the
  final via `WavWriter` or `NativeEncoder`; temp deleted in `finally`.
`Nota.Infrastructure/Audio/NativeEncoder.cs` (new): same shape as `WavWriter`
(ctor opens, `WriteFrames(float[], int)`, `Dispose` closes/flushes; `Discard()`
for failure cleanup), P/Invoking the four ABI functions through a new
`NativeMethods.Encode.cs` partial (`LibraryImport`, same `Lib = "nota_engine"`
convention, `[Out]`-free plain float arrays in / `IntPtr` handle out).

**Rationale**: The existing two-pass normalize already proved the
render-to-temp + transcode shape; per-format writing is a strategy swap at two
call sites. Keeping the render loop managed preserves progress reporting,
stems and engine restore exactly as smoke-tested today. Gain-in-managed keeps
the ABI stateless (no gain parameter to thread through open/write).

**Alternatives considered**:
- *Encoder-side gain/dither parameters*: duplicates `WavWriter`'s conversion
  concerns into the ABI; only dither (16-bit FLAC/MP3 targets) stays native
  because the int conversion happens there. See contract.
- *Separate `IFlacExporter`/`IMp3Exporter` ports*: three ports for one flow
  fragments the seam. Rejected.

---

## D5 — UI: adaptive option rows in the existing ExportWindow

**Decision**: `ExportWindow` format combo becomes `{ "WAV", "FLAC", "MP3" }`
(default WAV). Rows adapt per format (matrix in
`contracts/ui-export-dialog.md`): FLAC swaps the depth list to 16/24 (no
float32); MP3 swaps the depth combo for a quality combo (320/256/192/128 kbps,
default 320) and disables 96 kHz in the rate combo; dither keeps its existing
rule — enabled only for 16-bit targets, force-off otherwise via the existing
`enabled:` predicate. Footer estimate becomes format-aware (WAV math today;
FLAC ≈ 0.65 × PCM bytes, labelled "≈"; MP3 = kbps × 125 bytes/s exact). Export
button and the save picker (filter, default extension, suggested name
"Nota Export.{ext}") follow the format; the stems path keeps its folder picker
and naming scheme with the format's extension. `ExportOptions` record gains
`ExportFormat Format` and `int BitrateKbps`. Ember styling: reuse existing
`Combo`/`WorkingToggle`/`Section` helpers — no new visual vocabulary; the
`nota-design` skill is consulted at implementation.

**Rationale**: Every adaptive behavior is expressible with the dialog's
existing helper set and the dither predicate pattern, which already
demonstrates grey-out + force-off. The estimate coefficient 0.65 is the honest
midpoint for typical full-range music at FLAC level 5; MP3 CBR math is exact.

**Alternatives considered**:
- *Silent 48 kHz resample for MP3@96k*: rejected in clarify (spec FR-003).
- *VBR presets*: rejected in clarify (CBR list, default 320).

---

## D6 — Licensing posture for the two codecs

**Decision**: Register both codecs in `LICENSES/third-party.md` before code
lands: libFLAC 1.5.x (BSD-3-Clause, ✅ in use) and LAME 3.100 (LGPL-2.0-or-
later, ⚠️ cleared with a condition — source shipped in-tree under
`vendor/lame`, license text preserved, note explaining static compilation into
the AGPL dylib with source availability). The spec's Clarifications record the
maintainer's call that an accounted LGPL dependency passes the policy's
"AGPL-compatible copyleft" bar (LGPL-2.1+ → LGPL-3 upgrade path is AGPLv3-
compatible). MP3 patents: core patents expired 2017 (existing note 3 governs
dr_mp3; extend it to cover encoding).

**Rationale**: The policy's gate is the register row + license check
(CONTRIBUTING.md § Adding a dependency); LGPL-2.0+ is one-way compatible with
AGPL-3 via the "or later" upgrade clause, satisfying "AGPL-compatible
copyleft" with the row as the accounting.

**Alternatives considered**: dropping MP3 (permissive-only) — rejected in
clarify; the register-with-condition route preserves the feature.

---

## D7 — Verification: smoke test drives the real exporter + engine codecs

**Decision**: Extend `tests/Nota.SmokeTest/Program.cs` (default suite, plain
`Check(...)` blocks like the M6-4 export tests) with a FLAC/MP3 block that uses
the real `AudioExporter` (the suite already references `Nota.Infrastructure`
and P/Invokes the same dylib):
- FLAC lossless: render a known audible pattern → export FLAC (24-bit) →
  `NotaEngine.DecodeMono` (dr_flac under the hood) → decoded RMS/peak ≈ the
  WAV-export reference within 24-bit quantization; file starts with `fLaC`.
- MP3 sanity: export at 320 CBR → decode (dr_mp3) audible, duration within
  codec padding tolerance (~±100 ms), file size within ±10% of
  bitrate × duration.
- Stems: FLAC stems export produces one decodable file per track.
- Normalize: FLAC export with normalize lands peak ≤ 0.8913 as today.
- MCP: `export_master(format: "flac")` writes a decodable file.
- ABI failure path: open with an invalid path returns null; discard leaves no
  locked handle (partial file deletion by caller succeeds).
UI-level guards (FR-003 matrix, estimates) are verified via the headless
Avalonia / screenshot recipe during implementation, not in SmokeTest
(ExportWindow lives in Nota.App, which the suite does not reference).

**Rationale**: The smoke test is the PR gate and already exercises export
end-to-end through the C ABI; codecs are in the dylib it loads, so no new
harness needed. dr_flac/dr_mp3 give independent decode verification — the
encoder's output checked by a different implementation than its writer.

**Alternatives considered**: golden-file bit-comparison — encoder versions
change bytes across platforms; semantic checks are stable. Rejected.

---

## D8 — Docs & changelog surface

**Decision**: `CHANGELOG.md` `[Unreleased]` Added entry; `FEATURES.md` export
section gains the formats (via `nota-features`); user manual export page EN+RU
re-shot (via `nota-docs`). Deferred to implementation phase per constitution VI.

---

## References

- libFLAC 1.5.0 announcement/changelog (CMake preferred on MSVC): xiph.org/flac/changelog.html
- xiph/flac CMake options (`BUILD_SHARED_LIBS=OFF` default, `WITH_OGG`): github.com/xiph/flac/blob/master/CMakeLists.txt
- libFLAC stream encoder API docs: xiph.org/flac/api/index.html
- LAME 3.100 (autotools; MSVC projects under `dshow`): lame.sourceforge.io, sourceforge guide
- ShiftMediaProject/lame (VS-only fork, evidence of MSVC compilability): github.com/ShiftMediaProject/lame
- Repo facts: engine CMake (`nota_engine` SHARED, sources list, visibility
  presets, export scripts `nota_exports.txt`/`.map`, RtMidi compiled from
  `vendor/`, dr_libs implementation defines in `src/AudioFile.cpp`),
  `NOTA_API` dllexport block in `nota_engine.h:19-23`, flat engine `src/`
  layout, `ExportWindow.cs` helpers and estimate block,
  `WavAudioExporter.RenderToTemp`/`Transcode`, `NativeMethods` LibraryImport
  pattern, SmokeTest harness (`Check` + banner blocks), `LICENSES/third-party.md`
  row format — verified in-tree 2026-10-11.
