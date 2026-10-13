# Bug Assessment: Clicks on fast Freq / Morph moves in Auto Filter, EQ-8 and Dynamic EQ-8

- **Slug**: filter-param-zipper
- **Created**: 2026-10-13
- **Source**: pasted text (user report + a preliminary code read in the same session)
- **Verdict**: valid
- **Severity**: medium

## Report (verbatim or summarized)

> В Autofilter, eq-8, dynamic eq могут возникать щелчки при резком изменении freq или morph

The preliminary read found these causes:
1. Freq, Morph, Gain and Q are read once per audio block and not smoothed.
2. The EQs use RBJ biquads, so a step in the coefficients produces a transient.
3. Auto Filter's Morph is an instant crossfade between SVF outputs.
4. Auto Filter's Mod Smooth smooths only the env/LFO modulation, not the Freq knob.

## Symptom

A fast move of the **Freq** knob (all three devices) or **Morph** (Auto Filter) makes an
audible click or zipper noise. The same happens with **Gain** and **Q** in both EQs. The
expected behaviour is that a fast knob move or a steep automation ramp sounds smooth, as in
other DAWs.

## Reproduction

1. Put a sustained, bright or low-heavy source on a track, e.g. a saw pad or a bass with a lot of low end.
2. Insert Nota Auto Filter (LP, Res around 0.5) and press Play.
3. Flick Freq quickly from about 200 Hz to about 5 kHz, or drag Morph quickly from 0 to 1. Clicks are heard.
4. Repeat with Nota EQ-8 and Nota Dynamic EQ-8 using a Bell band at +12 dB, Q 4. Sweep its
   Freq quickly through 60–300 Hz, or step its Gain. Clicks are heard.
5. The same should happen with a steep automation ramp on these parameters.
   [NEEDS CLARIFICATION: not verified by ear; it follows from the code, since automation writes the same parameter atomics.]

## Suspected Code Paths

- `src/native/nota.engine/src/AutoFilter.h:187-191` — `baseHz`, `resBase` and `morph` are read
  once per `process()` call (one audio block) and held constant for the whole block. A knob
  move therefore reaches the DSP as a step at the block boundary.
- `src/native/nota.engine/src/AutoFilter.h:269-277` — at the 16-sample control rate only the
  *modulation* (`smOct_`, `smRes_`) is slewed by `smCoef`, and only when Mod Smooth > 0.
  `baseHz` is applied unsmoothed.
- `src/native/nota.engine/src/AutoFilter.h:482` — `y = y*(1-morph) + outs[next]*morph`.
  A Morph step means an instant switch between two different signals (for example LP→BP), which is a waveform discontinuity.
- `src/native/nota.engine/src/Eq.h:135-145` — the per-band biquad cascades are built once per
  block from the raw Freq, Gain and Q (×Scale). The comment at :142 confirms that a frequency
  move keeps the filter state and just swaps the coefficients.
- `src/native/nota.engine/src/Eq.h:349-357` — `State::run` is a transposed direct form II biquad.
  When its coefficients jump, the old `z1`/`z2` no longer match the new filter, so the output
  jumps. This is worst at low f0, high Q and large gain, because the states there are large.
- `src/native/nota.engine/src/DynamicEq.h:155-161, 183-185` — the same pattern: `f0`, `gStat`
  and `q` are read once per block. The apply coefficients are refreshed every `kCtrl` = 16
  samples (:215-220), but only for the dynamic gain term `dynDb_`, which is smoothed by
  attack/release. The static Freq, Gain and Q still step at the block boundary.
- `src/native/nota.engine/src/DynamicEq.h:174` — the detector band-pass `det[b]` also jumps
  with Freq. That moment is inaudible, but it can make the dynamic gain twitch for a moment.
- Precedent in the same code base: `Eq.h:160-162` already glides Output and Auto Gain
  ("~20 ms, click-free"). The idea exists in the code; it just isn't applied to the filter parameters.

Dynamic EQ-8 has no Morph parameter. There the report applies to Freq (and Gain/Q).

## Root Cause Hypothesis

**Confidence: high.** None of the three devices smooths its filter-shaping parameters on the
audio thread. The UI/automation value is sampled once per block, so the DSP sees a step function.
- **EQ-8 and Dynamic EQ-8**: a step in TDF-II biquad coefficients with the old state values
  produces a transient whose size grows with resonance, gain and how low the frequency is.
  This is the classic "zipper/click" of unsmoothed biquads.
- **Auto Filter**: the TPT SVF itself is robust to coefficient changes, so a Freq step there
  causes a brief tonal jump rather than a blow-up, but it is still audible at high
  resonance. The Morph step is a hard crossfade jump, and this is the sharper click.
- **Auto Filter's Mod Smooth** does not help, by design: it smooths only the env/LFO path.

## Proposed Remediation

**Preferred**: add per-parameter smoothing on the audio thread in all three devices and
recompute coefficients from the *smoothed* values at the existing control rate (`kCtrl = 16`).

- Smooth each parameter with a one-pole smoother of about 20 ms, matching the existing Output glide in `Eq.h`:
  - **Freq** in log2 domain (octaves), so a sweep sounds even across the spectrum;
  - **Gain** in dB, plus the EQ-8 **Scale**, applied to the scaled gain;
  - **Q**, **Res** in log domain (or linear for Res, 0..1);
  - **Morph**: linear (one-pole is fine) — evaluated **per sample** or per control block, since it is a cheap crossfade.
- Smoother state is snapped to the target on first block, on `prepare`/sample-rate change and
  on preset load. Apply the same `primed_` pattern as `Eq.h` so a freshly inserted
  device doesn't sweep from a default value.
- **Auto Filter**: smooth `baseHz` (octaves), `resBase` and `morph`. Fold the smoothed base into
  the per-16-sample `fcL`/`fcR` computation, which already exists. No change to Mod Smooth semantics.
- **EQ-8**: the cascade is currently built once per block. Move the build into a control-rate
  refresh inside the sample loop, but only for bands whose smoothed values are still moving.
  Skip the work once `|current − target|` is below an epsilon, so a static EQ costs the same as today.
- **Dynamic EQ-8**: smooth `f0`, `gStat` and `q`, and include them in the existing 16-sample
  refresh (it currently runs for dynamic bands only; extend it to any band that is gliding).
  Rebuild the detector `det[b]` from the smoothed f0 as well.

**Alternatives**:
- **Switch both EQs from TDF-II biquads to a TPT SVF (Simper/Cytomic) formulation**, with the
  same RBJ-equivalent responses for bell, shelf, HP, LP and notch. The SVF tolerates coefficient changes far better,
  so it removes most of the residual zipper even under fast modulation. Trade-offs: a larger
  rewrite of `coeffs()`/`cascade()`, including the 24/48 dB cascades and `averageDb`/curve drawing that read
  `Coeffs`, plus re-verifying the EQ curves against the UI graph. It is worth doing if smoothing alone
  still leaves audible artefacts at high Q and low f0. It can be a follow-up.
- **Per-sample coefficient interpolation instead of control-rate recompute.** It is smoother still, but
  costs more CPU (8 bands × up to 4 sections × 2 channels), and 16-sample steps of an already-smoothed
  parameter are inaudible in practice. Not recommended.

**Files likely to change**:
- `src/native/nota.engine/src/AutoFilter.h`
- `src/native/nota.engine/src/Eq.h`
- `src/native/nota.engine/src/DynamicEq.h`
- possibly a small shared helper (e.g. a `ParamSmoother` header next to them) if one does not already exist in the engine
- `CHANGELOG.md` ([Unreleased] → Fixed)

**Tests to add or update**:
- A smoke test (`tests/Nota.SmokeTest`) per device: render a steady sine or saw through the device,
  step Freq (and Morph for Auto Filter; Gain for the EQs) mid-render, and measure the
  maximum sample-to-sample difference (or high-passed energy) around the step against a
  no-step baseline. Assert that it stays within a small tolerance. A deterministic source is needed,
  because synth notes drift (see the smoke-level-tests note): use a generated WAV or a sine.
- A regression check that a *static* setting renders the same as before the change (level
  within ±0.1 dB), so the smoothing does not change the steady-state response.
- A check that a newly inserted device or a preset load does not audibly sweep (first block at target).

## Risks & Considerations

- **Feel / latency of controls**: a 20 ms glide is imperceptible as lag, but it slightly softens
  intentional hard steps, e.g. stepped automation used as an effect. This is acceptable; other DAWs do the same.
- **CPU**: recomputing EQ cascades every 16 samples while a band moves costs more than once per block.
  It is bounded by the "only while gliding" gate, and the static cost is unchanged.
- **Real-time safety**: smoother state must live in the device and be touched only on the
  audio thread. No allocations or locks (CONTRIBUTING audio-thread rules).
- **Type / Slope / Channel changes** in EQ-8 still reset the band state (`Eq.h:144`) and can
  click. These are discrete switches and out of scope here, but worth noting. A short crossfade on type
  change would be a separate improvement.
- **UI curve and analyzer** read the parameters directly, not the smoothed values. This is fine (the UI
  should show the target), but don't route the smoothed values back into the UI.
- **Auto Gain in EQ-8** is re-measured from raw params. It already has its own 20 ms glide, so this needs no change.

## Open Questions

- [NEEDS CLARIFICATION: is the click also heard on *automation* playback, or only on manual knob moves? The code says both; worth confirming by ear after the fix.]
- [NEEDS CLARIFICATION: should the SVF rewrite of the EQs be part of this fix, or a follow-up if smoothing alone is not enough? The preferred plan assumes a follow-up.]
