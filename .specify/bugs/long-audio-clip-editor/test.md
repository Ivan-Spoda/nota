# Bug Verification: Long audio files — import stutter, BPM-edit freeze, unreadable clip-editor ruler and waveform

- **Slug**: long-audio-clip-editor
- **Tested**: 2026-10-13
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

None of the four symptoms reproduces any more, judging by the automated equivalent of the
assessment's reproduction (real `MainWindow`, headless, an 8-minute FLAC dropped through the real
import path):
- the import no longer blocks the UI for seconds;
- a click on the BPM tag is instant;
- the ruler labels don't collide;
- the waveform re-resolves on zoom, with no flash of old blocks and nothing drawn past the
  editor's edges.

The full build and smoke test pass, including the 8 new engine checks. No regressions were found.
The reporter confirmed the first fix in the real app ("Все отлично"); the follow-up (zoom flash,
clipping) is verified headless only.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction 1 — drop a long FLAC | `/tmp/longaudioharness` (`NOTA_DATA_DIR=… dotnet run`): `ImportAudioInBackgroundAsync` on an 8-min 48 kHz FLAC, UI thread timed every iteration | pass | longest UI-thread block 22.6 ms (before the fix: one 3.2–3.4 s block) |
| Reproduction 2 — BPM click | synthetic double-click on the BPM chip; type 125 + Enter | pass | click 5.2 ms, warp untouched, field opens; commit 5.7 ms, background rebuild 3.1 s with a 6.7 ms max UI block; clip 960 → 1000 beats |
| Reproduction 3 — ruler on a long clip | screenshots at fit, 12×, 80×, 2000×, max zoom; warped + unwarped | pass | labels every 8 bars at fit, no overlap; beats / 16ths up close; seconds ruler `0 s…450 s` → `192.0005 s` |
| Reproduction 4 — waveform at zoom | detail fetch at 80× (3727 buckets) and unwarped; frames captured right after zoom steps 80→96× and 96→140×, before the fetch ran | pass | detailed waveform, no block flash; sample-level at 2000×; max zoom 148 406× |
| Follow-up — drawing past the edges | playhead scrolled past the left edge at 96× | pass | nothing drawn over the inspector |
| New engine tests | `scripts/build.sh` (smoke test) | pass | 3 range-peaks checks + 5 background-warp checks, all PASS (bg build 126 ms; identical to the sync build, max diff 0) |
| Regression suite | `scripts/build.sh` — native universal build, `dotnet build Nota.sln -c Release`, full smoke test | pass | `SMOKE TEST PASSED`, 3112 checks PASS, 0 errors |
| Lint / style | the smoke test's source-style checks | pass | the "plain space before a unit" check passes after the fix |
| Real app with a display / the reporter's 96 kHz file | — | not-run | headless only from this session; the reporter checked the main fix in the real app |

## Output Excerpts

```
import done in 3483 ms … max RunJobs 22.6 ms, max render 23.6 ms
[PASS] double-click on the BPM chip returns in 5.2 ms
[PASS] background rebuild finished in 3107 ms, max UI block 6.7 ms
[PASS] bg warp: plays identically to the synchronous rebuild (max diff 0,0E+000)
[PASS] bg warp: a superseded build is dropped, the latest edit wins (max diff 0,0E+000)
SMOKE TEST PASSED
```

## Residual Risks

- Measured on a generated 48 kHz file headless. A 96 kHz / 24-bit FLAC (like the reporter's)
  stretches about twice the source data per clip. That work is now on the worker thread, so the
  UI cost shouldn't scale with it, but this exact case wasn't run.
- While a rebuild runs, the clip plays its previous warp, clipped to its new length. That is the
  reporter's choice, so the first second or so after an edit sounds like the old timing.
- Not covered by this fix (listed in fix.md follow-ups):
  - a project tempo change still rebuilds every warp cache synchronously;
  - auto- and beat-warp detection runs on the UI thread;
  - arrangement-lane trims, splits and tempo edits outside the clip editor still build caches synchronously;
  - the arrangement lane still draws long clips from a fixed 2048-bucket overview.

## Recommendation

Close the bug: verified through the automated end-to-end reproduction and the full smoke test,
with the reporter's confirmation of the main fix in the real app. Worth a final in-app look at the
zoom-flash follow-up with the original 96 kHz FLAC. Open separate issues for the follow-ups,
starting with the synchronous rebuild on a project tempo change.
