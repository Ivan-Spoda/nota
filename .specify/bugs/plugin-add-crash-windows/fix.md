# Bug Fix: hosted plugin crashes when its bus layout grows past Nota's process buffer

- **Slug**: plugin-add-crash-windows
- **Fixed**: 2026-10-13
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`PluginInstrument::render` and `PluginEffect::process` now never hand a plugin a buffer narrower than its current
layout. Channels past `storage_` point at one zeroed per-instance scratch channel, so a VST3 plugin that widens its
buses via `restartComponent(kIoChanged)` (Reaktor's Play/Patch/Build switch) no longer gets null channel pointers on
the audio thread. Instrument `storage_` is also sized by the wider of inputs/outputs.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/native/nota.engine/pluginhost/PluginHost.cpp` | modified | new `pluginChannelPtrs()` helper + `kNoAllocChans`; used by `PluginInstrument::render` and `PluginEffect::process`; `spare_` scratch channel (sized in `prepare()` / `setSampleRate()`); instrument `storage_` = `max(2, in, out)` |

## Diff Highlights

```cpp
int pluginChannelPtrs(const juce::AudioProcessor& p, juce::AudioBuffer<float>& storage,
                      float* spare, int frames, float* (&ptrs)[kMaxChans]) {
    const int have = juce::jmin(storage.getNumChannels(), kMaxChans);
    const int want = juce::jmin(kMaxChans, juce::jmax(kNoAllocChans, have,
                                                      p.getTotalNumInputChannels(), p.getTotalNumOutputChannels()));
    if (want > have) juce::FloatVectorOperations::clear(spare, frames);
    for (int c = 0; c < want; ++c) ptrs[c] = c < have ? storage.getWritePointer(c) : spare;
    return want;
}
```

The buffer always spans at least 31 channels — the most a referring `juce::AudioBuffer` holds without allocating —
which also covers a layout change that lands between reading the channel counts and JUCE taking its process lock.

## Tests Added or Updated

None in the repo: the smoke test has no hosted plugin that changes its layout at runtime (see Follow-ups).

## Local Verification

- Deterministic repro with the real Reaktor 6 VST3 (installed on this Mac), harness `/tmp/widenharness` (Python ctypes
  over `libnota_engine.dylib`, `NOTA_DATA_DIR` isolated, copy of the user's `plugins.xml`). A temporary, since removed
  `TEMP-VERIFY` hook enabled all of Reaktor's buses and re-prepared it like JUCE's `restartComponentOnMessageThread`
  does, leaving `storage_` as is: layout went `in=0 out=2` → `in=2 out=16` with `storage=2`.
  - old buffer width (toggle) → **SIGSEGV during render** (exit 139)
  - fixed → 200 × 512-frame blocks rendered, exit 0
  - fixed, no hook → 200 blocks rendered, exit 0
- `scripts/build.sh` → native + managed build OK, **SMOKE TEST PASSED**.
- Not done: clicking Reaktor's mode buttons in the GUI app (no WindowServer in the sandbox), and no Windows /
  DecentSampler run.

## Deviations from Assessment

- The assessment proposed passing `kMaxChans` (64) channels every block. That would make JUCE's referring
  `AudioBuffer` allocate on the audio thread (its inline space is 32 pointers), so the buffer instead spans
  `max(31, storage, plugin in, plugin out)`, capped at 64. A plugin with more than 31 channels still makes the
  `AudioBuffer` allocate, as it already did before the fix.

## Follow-ups

- Confirm by hand in the app: Reaktor 6 VST3 → open GUI → Play/Patch/Build (`/speckit-bug-test`).
- Ask the Windows reporter to retest with DecentSampler on the build with this fix.
- `audioProcessorChanged` could resize `storage_` when the layout grows, so the extra channels become audible rather
  than discarded. That needs a lock-free buffer swap; out of scope for the hotfix.
- CHANGELOG `[Unreleased]` → Fixed entry.
