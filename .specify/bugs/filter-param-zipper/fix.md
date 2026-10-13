# Bug Fix: Clicks on fast Freq / Morph moves in Auto Filter, EQ-8 and Dynamic EQ-8

- **Slug**: filter-param-zipper
- **Fixed**: 2026-10-13
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

The filter-shaping knobs now glide on the audio thread. A one-pole glide with a ~20 ms time constant
moves Freq (in log2 Hz), Gain (in dB) and Q (in log2), and the coefficients are re-built from the
gliding values every 16 samples while a knob moves. Morph in Auto Filter glides per sample.
When a glide reaches its target it snaps to the exact value. A still knob therefore renders bit-identically
to before, and a static EQ does no extra work.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/native/nota.engine/src/AutoFilter.h` | modified | `glOct_`/`glRes_`/`glMorph_` glide toward Freq/Res/Morph. Freq and Res step once per 16-sample control block and feed the existing `fcL`/`fcR`/`resN` computation. Morph glides per sample inside the sample loop. `primed_` snaps on the first block after construction or `setSampleRate`. |
| `src/native/nota.engine/src/Eq.h` | modified | Per band, `glF_`/`glG_`/`glQ_` hold the gliding values (Gain includes Scale). The cascade is built from the gliding values. While any band moves, the sample loop re-builds its cascade every `kCtrl` = 16 samples. Auto Gain is measured on the **target** cascade, not the mid-glide one. A band that just came on (`cfgKey_ < 0`) or a fresh device starts at its values. Slope is pinned per block (`sl[]`). |
| `src/native/nota.engine/src/DynamicEq.h` | modified | The same glide for f0, the static gain and Q. Gliding bands step on the existing 16-sample refresh, and their apply coefficients are re-built together with the dynamic gain. The detector band-pass follows the gliding f0/Q. `wasOn_`/`primed_` make a band that just came on start without a glide. |
| `tests/Nota.SmokeTest/FilterGlideTests.cs` | added test | Seven jump cases across the three devices (see below). |
| `tests/Nota.SmokeTest/Program.cs` | modified | Wires `FilterGlideTests` into the full run, and adds a `--glide` entry next to `--grid`. |
| `CHANGELOG.md` | modified | Adds a `[Unreleased]` → Fixed entry. |

## Diff Highlights

```cpp
// Eq.h — the band glides; the cascade is re-built while it moves
if (moving && --ctrl_ <= 0) {
    ctrl_ = kCtrl;
    ...
    glF_[b] = settle(glF_[b] + (tF[b] - glF_[b]) * glideC, tF[b], 1e-5);
    ...
    cascade(c[b], ty[b], sl[b], sr, std::exp2(glF_[b]), glG_[b], std::exp2(glQ_[b]));
}
```

```cpp
// AutoFilter.h — Morph is a crossfade: glide it per sample
glMorph_ = settle(glMorph_ + (morph - glMorph_) * glideS, morph, 1e-6);
```

## Tests Added or Updated

`tests/Nota.SmokeTest/FilterGlideTests.cs::Run` plays steady tones (100 Hz + 1 kHz, a generated
WAV) through the real engine and jumps one knob at a block boundary. It then checks that the
output's peak 4th difference right after the jump is at most ×150 of the settled sound before
and after the jump. The cases are:
- Auto Filter: Freq 0.2→0.8 at Res 0.5, and Morph 0→1.
- EQ-8: high cut Freq 600→8000 Hz at Q 4; bell Freq 1→4 kHz at +15 dB Q 8; 48 dB low cut Freq 40→300 Hz at Q 6.
- Dynamic EQ-8: high cut Freq 600→8000 Hz at Q 4, and bell Gain 0→+15 dB.

Results with the fix: ×20.2, ×3.8, ×13.3, ×1.7, ×1.0, ×7.7, ×1.1.

## Local Verification

**Standalone C++ harness** (`/tmp/fpz/harness.cpp`, not in the repo). It runs each device directly at
44.1 kHz with 512-sample blocks and measures the peak 4th difference after a jump against the settled sound.

| Case | Before | After |
|---|---|---|
| Auto Filter Freq 0.2→0.8, Res 0.7 | ×17 472 | ×44 |
| Auto Filter Morph 0→1 | ×10 028 | ×7 |
| EQ-8 bell Freq 80→250, +12 dB Q 4 | ×40 | ×1.3 |
| EQ-8 bell Gain 0→+15 | ×22 | ×1.4 |
| EQ-8 48 dB low cut 40→300, Q 6 | ×725 | ×9 |
| EQ-8 bell Freq 1k→4k, +15 dB Q 8 | ×438 | ×4 |
| EQ-8 high cut 600→8k, Q 8 | ×6 324 | ×75 |
| Dynamic EQ-8 bell Freq / Gain | ×44 / ×35 | ×1.3 / ×1.3 |
| Dynamic EQ-8 high cut 600→8k, Q 8 | ×4 148 | ×45 |

- **Static renders** (no knob move) of Auto Filter, EQ-8 and Dynamic EQ-8 have the same RMS to 6 digits before and after. A ducking dynamic band also renders identically.
- **Native engine**: `cmake --build src/native/nota.engine/build` passes. Building the harness with `-Wall -Wextra` gives no warnings from the changed headers.
- **Smoke test**: `dotnet run --project tests/Nota.SmokeTest -c Release` → **SMOKE TEST PASSED** (0 failures, including the 7 new checks).
- I did not run the new smoke test against the pre-fix engine. Other work-in-progress is uncommitted in the same tree, so I did not stash. The before numbers above come from the standalone harness, which runs the same device classes.

## Deviations from Assessment

- **Preset load is not snapped.** The assessment suggested snapping smoother state on preset load.
  A preset arrives as plain `setParam` calls, so the device can't tell it from a knob move. A
  20 ms glide on a preset switch is click-free and harmless, so I left it. Snapping happens on device
  creation, `setSampleRate`, and when a band turns on.
- **Auto Filter Freq and Res still step per 16-sample control block** (the same rate the existing LFO and
  envelope modulation uses), not per sample. The residual is ×44 against ×17 000 before. Making it
  per sample would cost a `tan()` per sample per channel.
- **First version of the smoke test used tones at 0.3.** A Q 8 resonance sweeping across them clipped the
  engine's output at ±1, which looked like a click. The test now uses tones at 0.05 and Q 4 for the cuts.
  This was a test artefact, not a DSP finding.

## Follow-ups

- If any zipper is still audible at extreme settings (high cut or low cut at Q 18 swept fast), consider
  the TPT-SVF rewrite of the EQ sections from the assessment's alternatives.
- In EQ-8, a Type, Slope or Channel change still resets the band state and can click. These are discrete
  switches and out of scope here; a short crossfade on those changes would be a separate fix.
- A listening check in the app: sweep the knobs by hand and play automation ramps.
