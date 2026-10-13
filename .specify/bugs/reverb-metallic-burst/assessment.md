# Bug Assessment: Nota Reverb — sudden loud metallic burst after a while

- **Slug**: reverb-metallic-burst
- **Created**: 2026-10-13
- **Source**: pasted text
- **Verdict**: valid
- **Severity**: high

## ROOT CAUSE — CONFIRMED (2026-10-13, supersedes the hypothesis below)

**An out-of-bounds read in `Reverb::Comb::process` (`Reverb.h:375-377`).**

```cpp
float rp = w - std::clamp(delay, 1.0f, (float)size - 2.0f);
if (rp < 0) rp += size;          // rp = -7.6e-6 → size - 7.6e-6 rounds to exactly `size` in float
int i0 = (int)rp;                // i0 == size  → buf[size] is one past the end
```

The read position is fractional, because the comb length is `tuning·srScale·span_ ± LFO`.
When it lands a hair *above* the integer write index (`w − delay ∈ (−ulp/2, 0)`, ulp ≈ 1.2–2.4e-4
for sizes of 1000–4000), `rp + size` rounds to `size` and `buf[size]` reads the heap word right
after the comb buffer. Whatever is there (allocator metadata, a neighbour's data) enters the
feedback loop as a sample. Usually it is small and goes unnoticed. Sometimes it is a huge
finite float, or a NaN — which is what the earlier "NaN guard" fix was really catching. A huge
value then recirculates at the comb's pitch and is held at the soft clipper's ceiling: a loud,
**looped, metallic** ring.
It's a rare per-sample coincidence (16 combs × ~27 loop cycles/s × a ~1e-4 window), hence
"after a while", seemingly random, and independent of what is played.

Evidence:
- **In-app flight recorder dump** (`~/nota-reverb-trace/20261013-122155-rv1-4.*`, Live Room
  settings, 252 s in): params unchanged, input 0.46, comb input 0.0165, and in that same block
  the **comb sum is 6.2e34**. The value didn't come from the input; it appeared inside a
  comb. Afterwards the allpasses hold 6.2e34, the raw wet is 1.5e34, and the output is pinned
  at ≈ 2.5 (soft-clip ceiling × Output).
- **AddressSanitizer** on the standalone harness (user's sample + Live Room params) reports
  `heap-buffer-overflow ... READ of size 4 ... 0 bytes after 9116-byte region` in
  `Reverb::process` → `Comb::process`, with the buffer allocated by `Comb::init` in
  `setSampleRate`. It triggers within the first minutes of the run.
- Minimal repro of the arithmetic: `size=2500, w=100, delay=100.00001f` gives `rp=2500, i0=2500`.

Why earlier fixes didn't help: the NaN/Inf guards only reject *non-finite* garbage. The soft
clipper only bounds the level. Size smoothing addressed a different click. None of them stopped
the out-of-bounds read.

### Remediation (replaces "Proposed Remediation" below as the primary fix)

1. `Comb::process`: make the wrap index-safe. After wrapping, clamp the index:
   `int i0 = (int)rp; if (i0 >= size) i0 -= size;` (keep `i1` as is). Better still, compute
   the read position in `double`, or as integer + fraction, and wrap the *integer* part.
2. Same class, rarer (double precision): `Reverb::readPre` (`Reverb.h:347-353`) and
   `Delay::read` (`Delay.h:318-324`) — apply the same `i0 >= n → i0 -= n` guard. Audit other
   fractional delay reads (Chamber's algorithmic tail, Chorus/Flanger/Phaser) for the same
   `if (rp < 0) rp += size; (int)rp` shape. Masked reads (`& (N-1)`, e.g. Keys, Vintage) are
   safe.
3. Regression test: a deterministic unit-level check that drives `Comb`-style reads with
   `w − delay = −ulp/4` at several sizes and asserts the index stays in range. Plus a short ASan
   soak of `Reverb::process` on noise with modulation on (CI or a dev script).
4. Remove the temporary `ReverbTrace.h` instrumentation before merging.
5. Optional follow-ups, separate from this bug: the resonance gain-staging issue described
   below (Freeverb allpass at g up to 0.75, unnormalized comb gain, soft clipper above 0 dBFS).
   Real, but it produces predictable overs on long presets, not this burst.

## Report (verbatim or summarized)

> Сейчас когда добавляю эффект Reverb в цепочку устройств, через какой-то время может
> "выстрелить" резкий громкий металлический звук. Я не могу сказать четко, когда он
> возникает, но проявляется это почти всегда спустя время. Этот баг уже пытались фиксить
> несколько раз, но до сих пор он есть.

Previous fix attempts (CHANGELOG):
- an earlier release — NaN/Inf guards in the comb/allpass loops and tone filters
  ("periodic metallic overdriven roar");
- 0.34.0 — delay-length smoothing on Size/Algorithm change (`kSpanGlide`) + soft
  saturator instead of a hard clip on the wet output.

Neither touched the gain structure of the tail, which is where the burst comes from.

## Symptom

With Nota Reverb (builtin kind 2) on a track, a loud metallic ringing occasionally builds
up and overloads the output (above 0 dBFS, rounded off by the wet soft clipper). It shows up
"after a while" and seemingly at random. In practice it happens when a held note or a
sustained partial lands on one of the tail's sharp resonances. Expected: a reverb tail with
no ringing modes that sit tens of dB above the average response, and that never drives the
output past full scale on normal-level input.

## Reproduction

Reproduced offline with a standalone harness (`/tmp/revtest/`, not in repo) that drives
`nota::Reverb::process()` directly at 48 kHz, 512-frame blocks:

1. Default Reverb, Dry/Wet 1.0 (or Decay ≈ 0.8 / Diffusion ≈ 0.8, Mix 0.5).
2. Feed held saw notes at −12 dBFS (0.25 peak), random pitch MIDI 36–72, 2 s each, 300 notes.
3. Watch the output peak per note.

| Settings (decay / diffusion / mix) | Notes > 0 dBFS | Worst peak |
|---|---|---|
| 0.55 / 0.60 / 0.30 (defaults) | 0 / 300 | −7.2 dBFS |
| 0.55 / 0.60 / 1.00 | 2 / 300 | +0.4 dBFS |
| 0.80 / 0.80 / 0.50 | 2 / 300 | +0.4 dBFS |
| 0.90 / 1.00 / 0.50 | **128 / 300** | **+5.6 dBFS** |

The same run with the output allpasses swapped for true (unity-magnitude) Schroeder
allpasses: 0 / 300 in every row, worst ≈ −10 dBFS.

Wet impulse-response spectrum (20 Hz–8 kHz, Early Refl off, Mod Depth 0):

| decay / diff / damp | current peak gain | current peak / median | true-allpass peak / median |
|---|---|---|---|
| 0.55 / 0.6 / 0.5 | 5.4× (+14.6 dB) | 16 dB | 16 dB |
| 0.55 / 1.0 / 0.5 | 10.3× (+20.2 dB) | 19 dB | 16 dB |
| 0.90 / 1.0 / 0.5 | 40.5× (+32.1 dB) | 29 dB | 23 dB |
| 1.00 / 1.0 / 0.0 | 63.1× (+36.0 dB) | 32 dB | 27 dB |

Random parameter automation (Decay, Size, Diffusion, Algorithm, Freeze…) over 20 simulated
minutes gave peaks up to ≈ 2.1–2.3 (the soft-clip ceiling), but no NaN or unbounded growth.
The loops are stable; the problem is gain, not instability.

[NEEDS CLARIFICATION: which source material and Reverb settings the user had when they heard
it — sustained synth/bass, long Decay, high Diffusion? Confirms the trigger but not the
diagnosis.]

## Suspected Code Paths

- `src/native/nota.engine/src/Reverb.h:367-378` (`Reverb::Allpass::process`): this is the
  Freeverb "allpass" (`out = -in + buf; buf = in + buf·g`), which is **not** allpass. Its
  magnitude swings between `g/(1-g)` and `(2+g)/(1+g)`. Freeverb keeps g = 0.5, which is
  close to flat. Here `diff = 0.4 + Diffusion·0.35` pushes g to 0.75, so each stage peaks at
  ×3 (+9.5 dB). Four stages in series reach up to ×81 (+38 dB) where their peaks line up,
  always including DC/low frequencies. This is the main extra coloration and gain.
- `src/native/nota.engine/src/Reverb.h:133-137` (comb feedback): an honest RT60 gives
  `fb` up to 0.995 (12 s). The comb resonance peak is `1/(1-fb)` (×200), but the input gain is
  a constant `0.015`. Steady-state gain on a comb mode therefore grows with Decay and is never
  normalized.
- `src/native/nota.engine/src/Reverb.h:204-208, 274-278` (`softClip`): the last safety net
  is transparent up to 1.2 and saturates toward ±2.2, *then* gets multiplied by `outG` (up to
  ×2). So a resonance can reach the bus at ≈ +7…+13 dBFS. The tanh knee on a narrow ringing
  mode is exactly the "loud metallic" sound.
- `src/native/nota.engine/src/FluxSynth.h:451`: the same Freeverb allpass pattern (not part
  of this bug; noted for consistency).

## Root Cause Hypothesis (initial — superseded, see ROOT CAUSE above)

The tail has extremely peaky steady-state gain. The output diffuser's Freeverb-style
"allpass" stages, run at g up to 0.75 instead of Freeverb's 0.5, stack up to +38 dB of
coloration. On top of that, the eight high-feedback combs have peak gain ∝ 1/(1-fb), which
is not normalized against Decay. When a held note or partial hits one of these modes, it
builds over roughly one RT60 into a narrow, loud ring. That explains "after a while" and
"random": it depends on which pitch is held. The ring then drives the wet soft clipper,
whose ceiling sits above full scale and is boosted again by Output. Previous fixes
addressed NaN latching and pointer jumps, which are real but different mechanisms; the gain
structure was never touched. Confidence: **high** for the mechanism (measured, and fixed in
a harness by swapping the allpass). **Medium** that this is the exact trigger the user hits.
The secondary race below could also produce a sudden burst, but only around engine start.

## Proposed Remediation

**Preferred**:
1. Replace `Reverb::Allpass::process` with a true Schroeder allpass, the same form as
   `DiffAp`: `v = in + g·buf; out = buf − g·v; buf = v`. Unity magnitude, same diffusion.
   Keep the Diffusion → g mapping.
2. Re-calibrate the wet level with a fixed makeup gain so the default preset's tail loudness
   (wet RMS of the IR at default params) stays close to the current one. The true allpass
   chain has a much lower mean gain (the median IR bin drops ~16 dB at defaults), so without
   makeup the reverb would suddenly sound much quieter.
3. Partially normalize the comb input against Decay, e.g. `inGain ∝ sqrt(1 − fb²)` blended
   or clamped so long tails still get louder but the mode peak can't run away. Calibrate so
   Decay 0.9–1.0 with Diffusion 1.0 keeps held −12 dBFS notes well under 0 dBFS at Mix 0.5.
4. Make the wet safety net an actual ceiling: knee and limit at or below 0 dBFS *after*
   `outG` (or apply `outG` before the soft clip). It stays a last resort, not the mechanism
   that shapes the sound.

**Alternatives**:
- Keep the Freeverb allpass but cap g at 0.5 (original Freeverb). Smallest diff and keeps
  the character, but Diffusion loses most of its range, and long-decay comb peaks
  (point 3) remain.
- Only lower the soft-clip ceiling. Removes the overload but not the metallic ringing. This
  is the same symptomatic class as the previous attempts — not recommended alone.

**Files likely to change**:
- `src/native/nota.engine/src/Reverb.h`
- `tests/Nota.SmokeTest/Program.cs` (or a new `ReverbTests.cs` in the smoke project)
- `CHANGELOG.md` (`[Unreleased]` → Fixed)

**Tests to add or update**:
- Smoke regression: instrument track playing held notes across a pitch range at moderate
  velocity → Nota Reverb with Decay 0.9, Diffusion 1.0, Mix 0.5 → render offline. Assert
  master peak < 1.0 (0 dBFS) for every note. Use a deterministic source (see the smoke-level
  memo: synth levels drift ±2 dB; a fixed WAV clip or a fixed-seed saw is safer).
- Level-preservation check: wet RMS of a noise burst at default settings within ±1.5 dB of
  the pre-fix value, so the recalibration doesn't change the default sound's loudness.
- Keep the existing 20-minute random-automation soak idea as a dev harness. Optionally
  promote a short version (a few seconds of random param jumps) to the smoke test: no NaN,
  peak bounded.

## Risks & Considerations

- **Sound change**: a true allpass and decay normalization change the reverb's colour and
  level. Existing projects will sound different (cleaner, possibly quieter at long Decay).
  Calibrate carefully and mention it in the CHANGELOG. Param layout is untouched, so there is
  no persistence or migration impact.
- **Realtime**: all changes are arithmetic inside `process()`; no allocation, no locks.
  Audio-thread rules hold.
- **Secondary race (separate issue)**: `Engine::start()` (`Engine.cpp:62-65`) starts the
  backend callback and *then* calls `propagateSampleRate()`. That runs
  `Reverb::setSampleRate()` and reallocates the comb/allpass/pre-delay vectors on the
  calling thread while the audio thread may already be in `process()`. This is a
  use-after-free window that can inject garbage into the loops, and it affects every built-in
  device with buffers. It only fires around engine start or device restart, not "after a
  while", so it is unlikely to be this report — but it should be tracked separately.
- `FluxSynth.h:451` uses the same non-allpass structure; out of scope here.

## Open Questions

- [NEEDS CLARIFICATION: typical Reverb settings and source when the burst happens (long
  Decay? high Diffusion? sustained synth/pad/bass?)]
- [NEEDS CLARIFICATION: how much loudness change at long Decay is acceptable after
  normalization — keep "long = louder" partially, or flatten fully?]

## Update 2026-10-13 — user reproduction (preset `Reverb_Bug01`)

User reproduced it: one looped audio sample (warp mode 3, 168 BPM, 16-beat loop), Nota Reverb
with `Reverb_Bug01`, which is exactly factory "Live Room" (Decay 0.44, Room, Mix 0.24). It fired
5–10 min into looped playback. The Reverb card was open and the user was **switching presets**
while playing. They describe the sound as possibly **looped** ("зацикленный"), not only metallic.
With the Reverb removed from the chain, everything is fine.

Harness results (all in `/tmp/revtest/`, not in repo) — **none reproduced the burst**:

| Run | Duration | Result |
|---|---|---|
| `Reverb::process` alone, user's sample + exact params, 512 blocks | 30 min / 314 loops | per-loop peak constant (0.592) |
| Real engine via C ABI, offline render, track + warp clip + Reverb, transport loop | 20 min, ± Reverb | peak constant (0.39–0.41) |
| Same, block sizes 470/471, random 1–1024, 441 with 17s mixed in | 3 × 20 min | peak constant |
| Real engine, **live CoreAudio backend** (master vol 0), polling track meter + Reverb scope every 2 ms | 25 min, ± Reverb | no anomaly, 0 xruns, peak ≤ 0.43 |
| `Reverb::process` + random factory-preset switching every 1–8 s (applied like `PresetService.ApplyInPlace`: named value or default per param) | 15 min | worst 1.07 (Infinite Pad), 1.12 (Send · Plate); no burst/loop |

Conclusions so far:
- With "Live Room" settings the comb/allpass resonance gain (main hypothesis above) is too small
  to explain a *sudden loud* event on this material. It still explains the >0 dBFS overs on
  long-decay presets.
- The deterministic engine path (offline and live, without the app UI) is clean. The trigger is
  something only the running app does: preset switching from the card, the browser's preset
  audition (`PresetAudition` → engine preview, which can loop — `nota_engine_set_preview_loop`),
  or another UI-thread interaction.
- Code read so far: `Engine::deviceSetParam` (atomic store + PDC check), `deviceParamDefault`
  (builds a throw-away `Reverb` per call — wasteful, but no shared state), autosave/Capture
  (read-only), Reverb card refresh (`scopeRead`, atomics only) — none mutates live DSP state.
- Factory presets "Shimmer Freeze" and "Stone Hold" turn **Freeze** on. Stepping through
  presets with Next/Prev passes through them and holds whatever tail is ringing as an endless
  loop until a non-freeze preset is applied. A preset-switching session would therefore produce
  a "looped" sound by design — confirm whether that is what was heard.

Next step proposed: in-app instrumentation (env-guarded, e.g. `NOTA_REVERB_TRACE`). The Reverb
keeps a ring buffer of its last ~3 s of input/output plus a per-block snapshot of params, `fb`,
`span_`, comb-buffer max and freeze state. When the wet peak jumps >12 dB above its running
level, it dumps the ring to a file. The user then reproduces with the normal workflow, and the
dump tells us whether the input was already bad (source/preview), a param/state change caused
it, or the loop ran away.

### Instrumentation added (temporary, uncommitted)

- `src/native/nota.engine/src/ReverbTrace.h` (new) + hooks in `Reverb.h` (`setSampleRate`,
  `process`, member `trace_`). Inert unless `NOTA_REVERB_TRACE` is set (`1` → temp dir, or a path).
- Triggers: raw wet ≥ 12 dB over its ~2 s running level, raw wet > 1.2 (soft-clip knee),
  non-finite internal value, or **Kill tail** (manual save, bypasses the 15 s cooldown). At most
  12 dumps per instance.
- Each dump: `<time>-rv<id>-<n>.wav` (4 ch float32: in L/R, out L/R; 8 s before + 1 s after),
  `.csv` (per block: params, comb feedback, size glide, peaks at input / comb input / comb sum /
  after allpasses / raw wet / output), `.txt` (reason, times).
- Verified in a harness (auto, soft-clip and manual triggers) and on the real engine (2 min of the
  user's loop: one instance, no false triggers). Remove before the fix lands.
