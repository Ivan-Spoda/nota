# Bug Assessment: Long audio files — import stutter, BPM-edit freeze, unreadable clip-editor ruler and waveform

- **Slug**: long-audio-clip-editor
- **Created**: 2026-10-13
- **Source**: pasted text (+ two screenshots)
- **Verdict**: valid (items 2–4 confirmed in code; item 1 likely valid, needs a timing measurement to pin down)
- **Severity**: high

## Report (verbatim or summarized)

> А теперь давай поработаем с длинными файлами аудио:
> * когда я перемещаю большой flac файл на аранжировку через драг дроп, есть значительное подтормаживание
> * клип по умолчанию варпится (это нормально), но при попытке поменять BPM через clip editor, когда я кликаю на текстбокс редактирования BPM, происходит зависание
> * на длинных файлах clip editor выглядит страшно (приложил скриншот), текст накладывается друг на друга, нужно по-умному скрывать текст и перерисовывать сетку
> * второй скриншот: при сильном приближении в редакторе клипов временная диаграмма не перестраивается, не читабельно (нужно исправить, оптимизировано)

Screenshot 1: clip editor, long warped clip at fit-to-width — the ruler draws a label on every bar
(`1 2 3 … 10 11 12 …`), the digits overlap into an unreadable strip, and the beat grid through the
wave is a dense comb. Screenshot 2: clip editor zoomed in to ~1.2–6.4 bars — the waveform is a row of
wide flat blocks (one per peak bucket), with no detail inside them.

## Symptom

With a long (multi-minute) FLAC: (1) dropping it on the arrangement makes the UI stutter noticeably;
(2) clicking the segment BPM chip in the clip editor (to type a BPM) freezes the app; (3) the clip
editor's ruler labels collide and the grid is a solid comb; (4) zooming in the clip editor only
magnifies a fixed 1024-bucket overview, so the waveform turns into blocks. Expected: a smooth drop,
an instant BPM field, a ruler that thins labels/grid by zoom, and a waveform that re-resolves to
pixel detail as you zoom in.

## Reproduction

1. Drag a long FLAC (e.g. 5–10 min, 44.1/48 kHz stereo) from Finder or the browser onto an arrangement lane.
   → UI stutters during "Warping … %" (and possibly during decode) [NEEDS CLARIFICATION: which phase stutters — the "Processing… %" decode or the "Warping… %" stretch? Status bar text identifies it].
2. Open the clip in the clip editor (it is warped by default). Fit-to-width → ruler labels overlap (item 3).
3. Single- or double-click the BPM chip in the marker strip → app hangs for seconds (item 2).
4. Zoom in strongly (Ctrl+wheel) → waveform drawn as wide flat blocks (item 4).

## Suspected Code Paths

### (2) BPM click freeze — confirmed
- `src/managed/Nota.App/AudioClipEditorView.cs:810-823` — a press on a BPM chip starts a scrub
  (`_bpmScrubSeg = b.seg`), or on the 2nd click opens the text box.
- `src/managed/Nota.App/AudioClipEditorView.cs:1029` — on release, `if (_bpmScrubSeg >= 0) { …; Commit(); }`
  commits **unconditionally**, even when the pointer never moved. A double-click therefore commits
  twice (press 1 release + press 2 release) before the text box even appears, and Enter commits a third time.
- `AudioClipEditorView.cs:72-77` — `MarkersCommitted` → `_engine.SetClipWarpMarkers` → `Reload()` → `_onChanged()`.
- `src/native/nota.engine/src/Engine_Tracks.cpp:522-541` `Engine::setClipWarpMarkers` → `configureClipWarp`
  → `buildWarpCache` (`Engine_Tracks.cpp:162-191`) — runs the stretcher **synchronously over the whole
  played window** on the calling (UI) thread. For a multi-minute file that is seconds of work per
  commit. The deferred/sliced path (`SetDeferWarpBuild` + `WarpBuildStep`) is only used by import and
  project load, never by editor edits.
- Same synchronous rebuild hits every other warp edit on a long clip: marker drag release
  (`Commit()`), `setClipWarpTrim` (`Engine_Tracks.cpp:546-563`), pitch, warp mode, project-tempo change.

### (1) Drag-drop stutter — likely
- `src/managed/Nota.App/MainWindow.AudioImport.cs:100-113` — after a background decode, the clip is
  auto-warped with `SetDeferWarpBuild(true)`, then `BuildImportWarpAsync` (`:220-230`) builds the
  cache **on the UI thread** in `WarpBuildStep(48000)` slices, yielding at `Background` priority
  between slices. Each slice stretches ~1 s of device audio (3 × `kWarpBuildChunk` = 16384 frames);
  if a slice costs tens of ms, every frame during "Warping… %" is late → a visible stutter that lasts
  as long as the file (hundreds of slices for a long file).
- `MainWindow.AudioImport.cs:63-74,186-193` — decode ticks every 50 ms allocate a 2048-bucket peak array
  and repaint the timeline; cheap, but adds to the load during the decode phase.
- `src/managed/Nota.App/ArrangementView.cs:770-777` — every `Timeline.Refresh()` recomputes 2048 peaks
  for changed audio clips (fast via `SampleBuffer::peakTable`; unlikely to be the culprit).

### (3) Ruler label overlap / dense grid — confirmed
- `src/managed/Nota.App/AudioClipEditorView.cs:1328-1349` `DrawRuler` — `step` is either 1 beat
  (`beatW >= 30`) or 4 beats (one bar). There is no coarser step, so with ~1–10 px per bar every bar
  gets a label and a `FormattedText` allocation per bar per frame.
- `AudioClipEditorView.cs:1133-1144` — the beat grid through the wave draws **every beat** (no
  thinning by pixel density), producing the comb in screenshot 1 and thousands of lines per render.
- `AudioClipEditorView.cs:1351-1365` (unwarped seconds ruler) — has a step ladder but caps at 60 s;
  a very long unwarped file at fit-to-width can still collide.

### (4) Waveform doesn't re-resolve on zoom — confirmed
- `src/managed/Nota.App/AudioClipEditorView.cs:293-311` `Reload()` — fetches a fixed **1024**
  buckets for the whole clip (`GetClipWarpFullPeaks` / `GetClipSourcePeaks`) once, on reload only.
- `AudioClipEditorView.cs:1153-1175` — zoom just widens each bucket (`bw = _zoom * w / _count`);
  at `MaxZoom = 60` (`:666`) a 1024-bucket overview shows ~17 buckets per screen → blocks.
  One `FillRectangle` per bucket.
- Engine already has the building block: `warpPeaksRange(clip, out, n, beat0, beat1)`
  (`Engine_Tracks.cpp:808-836`) peak-picks any beat range; `getClipWarpFullPeaks` just calls it with
  `[0, warpBeats]`. There is no range variant for the unwarped source view (`getClipSourcePeaks`,
  `:889-906`, always whole file). `SampleBuffer::peakRange` (`SampleBuffer.h:83-94`) is
  block-table accelerated, so range queries are cheap.

## Root Cause Hypothesis

(2) **High confidence.** A plain click on the BPM chip is treated as a zero-length scrub and committed
on release; each commit synchronously re-stretches the entire long file on the UI thread
(`buildWarpCache`), and a double-click does this twice before the text field shows — hence a
multi-second "freeze when clicking the BPM box". (3) and (4) **High confidence**: the ruler/grid have
no density-based thinning beyond one bar, and the editor waveform is a fixed 1024-bucket overview
that is never re-queried for the visible window. (1) **Medium confidence**: the import's warp-cache
build runs on the UI thread in ~1 s-of-audio slices; on a long file that is hundreds of slices, each
long enough to drop frames.

## Proposed Remediation

**Preferred**:

1. *BPM chip* — track movement: only commit a scrub if the BPM actually changed (or pointer moved > ~2 px);
   a click without movement is a no-op; double-click opens the text box without any prior commit.
   Commit on Enter/LostFocus only if the typed value differs from the current one.
2. *Warp edits off the UI thread* — make editor-driven warp edits (`SetClipWarpMarkers`, `SetClipWarpTrim`,
   pitch/mode toggles in the clip editor) use the deferred path: set `SetDeferWarpBuild(true)` around the
   edit and then build the cache with the same sliced builder used on import (factor
   `BuildImportWarpAsync` into a shared `WarpBuildScheduler` on `MainWindow`, coalescing concurrent
   requests). Better still (fixes item 1 too): move the stretch itself onto a worker thread — the
   builder only reads the immutable `SampleBuffer` and a copy of the markers, so the engine can expose
   "start building cache for clip X on a worker" + "install finished cache (republish) on the authoring
   thread", with a generation counter so a newer edit discards a stale build. Until the new cache is
   installed the clip keeps playing its previous cache (reporter's choice).
3. *Ruler/grid thinning* — pick the labelled step from a ladder of bars `{1,2,4,8,16,32,64,…}` (and beats
   only when a beat ≥ ~30 px), choosing the smallest step whose pixel width ≥ widest label + padding.
   Draw grid lines with a separate, finer ladder (bar lines when bar ≥ ~6 px, beat lines when beat ≥ ~6 px,
   sub-beat 16ths when very zoomed like screenshot 2). Extend the seconds ladder (120, 300, 600 s) for the
   unwarped ruler. Only iterate the visible range (compute first/last visible beat from scroll/zoom)
   instead of looping over all beats and `continue`-ing.
4. *Zoom-aware waveform* — keep the 1024-bucket overview as an instant fallback; add a "detail" peak
   buffer for the visible window: on zoom/scroll change (debounced ~30 ms, or on the next idle
   dispatch), request `~pixelWidth` buckets for `[visibleStart, visibleEnd]` (slightly padded) via new
   range APIs — `nota_clip_warp_peaks_range(track, clip, beat0, beat1, out, n)` (thin wrapper over
   `warpPeaksRange`) and `nota_clip_source_peaks_range(track, clip, frame0, frame1, out, n)`. Draw the
   detail buffer when it covers the view, else the overview. Draw peaks as one `StreamGeometry`
   (or per-pixel columns) instead of a `FillRectangle` per bucket. When buckets get narrower than a
   source frame (extreme zoom), fall back to drawing the sample line.

**Alternatives**:
- Simply raise the fixed overview to e.g. 65 536 buckets: trivial, but still blocky at high zoom for
  very long files, costs ~0.5 MB and a slower `Render` loop per reload; doesn't scale.
- Keep warp builds on the UI thread but shrink slices (e.g. 8192 frames) — smoother but slower overall and
  still blocks for one slice; a worker build is the real fix.

**Files likely to change**:
- `src/managed/Nota.App/AudioClipEditorView.cs` (BPM click, ruler/grid, detail peaks, render)
- `src/managed/Nota.App/MainWindow.AudioImport.cs` (shared warp-build scheduler / worker)
- `src/managed/Nota.App/MainWindow.DetailWindow.cs` / `MainWindow.Detail.cs` (host wiring of the editor's change callbacks, if the async build is owned by MainWindow)
- `src/managed/Nota.Application/Engine/IAudioEngine.cs`, `src/managed/Nota.Infrastructure/Interop/NotaEngine.Tracks.cs`, `NativeMethods` (range-peak APIs; async warp build if chosen)
- `src/native/nota.engine/include/nota/nota_engine.h`, `src/native/nota.engine/src/nota_engine_track.cpp`, `Engine.h`, `Engine_Tracks.cpp` (range peaks; off-thread warp build + install)
- tests: engine smoke / unit tests under `tests/`

**Tests to add or update**:
- Engine: range-peak APIs return the same min/max as the full query over the same span (warped and unwarped), and handle reversed clips, out-of-range spans and `n` larger than frames.
- Engine: async/deferred warp build after `setClipWarpMarkers` produces a cache identical to the synchronous build; a superseding edit discards the stale build.
- Managed (headless Avalonia): BPM chip click without movement does not call `SetClipWarpMarkers`; double-click opens the editor with zero commits.
- Managed: ruler label positions for a 700-bar clip at fit-to-width never overlap (label rects disjoint) and grid line count is bounded by ~w/6.
- Manual/perf: drop a 10-min FLAC — UI frame time stays < ~20 ms during "Warping…" (log the per-slice duration).

## Risks & Considerations

- Async warp build: playback of the edited clip during the rebuild (silence vs. stale cache vs. realtime stretch) must be decided; the snapshot/republish model (`republishWithTrack`) must install the cache only onto the matching clip generation; undo must not race an in-flight build.
- Thread safety: the worker may only read the immutable `SampleBuffer` and its own copies of markers/settings; no authoring-state access off the authoring thread (constitution: audio-thread restrictions + authoring-thread ownership).
- Reversed clips: detail peaks must go through the same mirror logic as the overview (the editor mirrors file-order peaks itself; keep the range API in file order).
- New C ABI functions: additive only (no ABI break), AGPL headers on any new file.
- The arrangement lane has the same fixed-2048-bucket limitation for very long clips at deep timeline zoom — out of scope here, but worth a follow-up.

## Clarifications (2026-10-13, from the reporter)

- Item 1 stutters during **"Processing… %"** (the decode phase), not "Warping… %". The decode itself
  already runs on a worker (`DecodeImport` via `Task.Run`), so the UI-thread cost has to be found by
  measurement: candidates are the 50 ms progress ticks (`Progress<ImportTick>` → lane repaint of the
  growing placeholder, `PendingClipsFor` VM churn), the `AddPendingImport → Refresh()` path, the final
  hand-off (`AddImportedAudioClip` + `Timeline.Refresh`, still labelled "Processing…"), and GC /
  memory pressure from the multi-hundred-MB sample buffer. `/speckit-bug-fix` must time the UI-thread
  calls during a long-FLAC import first and fix what the numbers show; the off-thread warp build stays
  in scope because it is needed for item 2 anyway.
- During an async warp rebuild the clip **keeps playing the old warp** until the new cache is installed.
- **Yes** — `MaxZoom` scales with clip length so a long file can zoom down to sample level.
