# Bug Fix: Long audio files — import stutter, BPM-edit freeze, unreadable clip-editor ruler and waveform

- **Slug**: long-audio-clip-editor
- **Fixed**: 2026-10-13
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

Warp-cache stretching moved off the UI thread: a new non-blocking `warpBuildPoll` stretches one clip
at a time on an engine worker thread and installs the result. This now drives imports, project load
and every warp edit in the clip editor. Until the new cache is ready the clip keeps playing its
previous (stale) cache, clipped to its new length. A plain click on the BPM tag no longer re-warps.
The clip-editor ruler and grid thin out by zoom level, and the waveform re-fetches peaks for the
visible slice. The zoom limit grows with the file's length so you can zoom down to single samples.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/native/nota.engine/src/Warp.h` | modified | `WarpInputs` (everything a cache depends on) stored in `WarpCache`; `WarpMarker ==` |
| `src/native/nota.engine/src/Engine.h` | modified | `warpBuildPoll`, `WarpJob` (worker state), `warpNeedsBuild`, `nextWarpBuildTarget`, `cancelWarpJob`, range-peak getters |
| `src/native/nota.engine/src/Engine_Tracks.cpp` | modified | `warpInputsOf` + shared `renderWarpWindow` (cancellable); staleness = inputs differ; deferred edits keep the old cache; `warpBuildPoll` worker build + install-if-unchanged; sync `warpBuildStep` reuses the scan and cancels a running job; `getClipWarpPeaksRange` / `getClipSourcePeaksRange` (≤ 8192 buckets) |
| `src/native/nota.engine/src/Engine_Render.cpp` | modified | a stale cache plays clipped to the clip's current played length |
| `src/native/nota.engine/src/Engine_Session.cpp` | modified | slot-from-clip rebuilds a stale cache too |
| `src/native/nota.engine/src/Engine.cpp` | modified | the destructor cancels and joins the worker |
| `src/native/nota.engine/include/nota/nota_engine.h`, `src/nota_engine_track.cpp` | added API | `nota_engine_warp_build_poll`, `nota_clip_get_warp_peaks_range`, `nota_clip_get_source_peaks_range` (additive, no ABI break) |
| `src/managed/Nota.Application/Engine/IAudioEngine.cs`, `Nota.Infrastructure/Interop/{NativeMethods,NotaEngine}.Tracks.cs` | added | `WarpBuildPoll`, `GetClipWarpPeaksRange`, `GetClipSourcePeaksRange` |
| `src/managed/Nota.App/MainWindow.AudioImport.cs` | modified | `PollWarpBuildAsync` (poll every 15 ms; several loops may share the work); import warp uses it |
| `src/managed/Nota.App/MainWindow.Files.cs` | modified | project-load warp build uses the poll loop |
| `src/managed/Nota.App/MainWindow.Detail.cs` | modified | wires the editor's `WarpBuildRequested` |
| `src/managed/Nota.App/AudioClipEditorView.cs` | modified | `WarpEdit` (defer + background build) around markers / trim / length / warp toggle & mode / auto- & beat-warp / pitch; BPM chip commits only after a real drag; BPM field commits only a changed value; zoom-aware detail peaks; density-based ruler (bars ×2ⁿ → beats → 16ths, ticks to 1/4096 beat) and wave grid; seconds ruler 0.5 ms … 1 h with `m:ss`; ruler clipped to its bounds; dynamic `MaxZoom` (~8 px per source frame) |
| `tests/Nota.SmokeTest/Program.cs` | added tests | range peaks + background warp build |
| `CHANGELOG.md` | modified | four `Fixed` entries under [Unreleased] |

## Diff Highlights

```cpp
// configureClipWarp: a deferred edit keeps the (now stale) cache playing
if (!c.warpEnabled) { c.warpCache = nullptr; return; }
if (deferWarpBuild_) return;   // stale (or no) cache until warpBuildStep / warpBuildPoll
```

```csharp
// BPM chip release: a click (no drag) must not re-warp the clip
if (_bpmScrubMoved) Commit();
```

## Tests Added or Updated

- `tests/Nota.SmokeTest/Program.cs` (import section):
  - `range peaks: whole warp range == full-material peaks`
  - `range peaks: source sub-range matches a full scan`
  - `range peaks: sub-frame buckets read single frames`
  - `bg warp: the deferred edit leaves a stale cache to rebuild`
  - `bg warp: the clip keeps playing its old cache meanwhile`
  - `bg warp: worker build installed`
  - `bg warp: plays identically to the synchronous rebuild` (max diff 0)
  - `bg warp: a superseded build is dropped, the latest edit wins`

## Local Verification

- Commands run: `scripts/build.sh` → native + managed build OK, **SMOKE TEST PASSED**.
- Headless harness (`/tmp/longaudioharness`, real `MainWindow`, 8-min 48 kHz FLAC dropped through
  `ImportAudioInBackgroundAsync`):
  - Before: one **3.2–3.4 s** UI-thread block at the end of "Processing…". Measured as ~480 sliced
    `WarpBuildStep(48000)` calls of 5–18 ms each; decode itself cost ~1 ms per UI tick.
  - After: longest UI-thread block **~21–27 ms** during the whole import; cache fully built.
  - Clip editor: double-click on the BPM chip **4.8 ms**, warp untouched, field opens. Typing 125 + Enter
    **5.3 ms**; the background rebuild takes ~3.1 s with the longest UI block **4.7 ms**; warp beats
    960 → 1000.
  - Zoom 80× fetches 3727 detail buckets for the visible slice. Max zoom is 148 406× on this file.
    Unwarped zoom fetches detail too.
- Manual checks (screenshots): at fit, bar labels every 8 bars with no overlap. At 80×, the waveform
  is sharp and labelled `73 · 73.2 · 73.3 …`. At 2000×, single cycles of the 110 Hz tone are
  visible. The unwarped ruler reads `0 s · 30 s …` at fit and `192.0005 s …` at maximum zoom. A
  half-off-screen ruler label no longer bleeds into the inspector.

## Deviations from Assessment

- Item 1: after the reporter clarified that the stutter happens during "Processing…", the
  measurement showed decode is fine. The "Processing…" phase on the track includes the warp build,
  and that build was the stall. So the off-thread warp build fixes item 1 directly; no decode-side
  change was needed.
- The synchronous `warpBuildStep` API is kept (the smoke tests and possible external callers rely on
  it). The worker path is a new additive `warp_build_poll` instead of a change to `warpBuildStep`.
- Also wrapped in `WarpEdit`: warp toggle/mode, auto-warp, beat-warp and pitch changes in the clip
  editor. They hit the same synchronous rebuild.
- An unrelated pre-existing style-lint failure ("plain space before a unit" in the warp status text)
  was fixed in passing, because the moved line was flagged.

## Follow-ups

- A project **tempo** change still rebuilds every warp cache synchronously
  (`reconfigureAllWarpStreams`), so a long warped file freezes there too. It should go through
  defer + `warpBuildPoll` as well.
- `AutoWarpClip` / `BeatWarpClip` still run tempo or transient detection on the UI thread. That is
  quick for loops but can be slow on very long files.
- The arrangement lane still draws long clips from a fixed 2048-bucket overview. Deep timeline zoom
  on a long clip is blocky there; the same range-peaks API would fix it.
- Arrangement edits outside the clip editor (lane trims, split, tempo via the transport) still
  build caches synchronously.

## Follow-up (2026-10-13, reporter feedback)

- **Zoom flashed the old wide blocks:** each zoom step drew the coarse overview for one frame
  until the background detail fetch landed. Now the previous detail keeps drawing wherever it
  reaches, and the overview fills only outside it, until the fresh fetch arrives. The detail is
  used whenever it's finer than the overview.
- **Blocks and the playhead spilled outside the waveform:** at zoom, buckets, markers and the
  playhead could have x < 0 or > width and were drawn over the inspector and the neighbouring
  panel. `Render` now clips everything to the editor's bounds.
- Verified headless: frames captured right after zoom steps 80→96× and 96→140×, before the
  background fetch ran, show the detailed waveform (no blocks). A playhead scrolled past the left
  edge doesn't appear over the inspector. The harness's other checks still pass.
