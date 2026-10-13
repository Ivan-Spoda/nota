# Bug Fix: Nota Reverb — sudden loud looped metallic burst

- **Slug**: reverb-metallic-burst
- **Fixed**: 2026-10-13
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`Reverb::Comb::process` occasionally read `buf[size]`, one float past the end of its delay
line. A fractional read position a hair past the write head wraps to `size − 1e-5`, which
rounds to exactly `size` in float. The stray heap word, sometimes a huge finite value,
recirculated in the comb as the loud looped ring. The read index is now wrapped back into
range. The same edge case is guarded in the other fractional delay reads of the same shape,
and a device-free self-test pins the case in the smoke test.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/native/nota.engine/src/Reverb.h` | modified | `Comb::process`: `if (i0 >= size) i0 -= size;` after the wrap (the fix). `readPre`: same guard (double precision, much rarer). New public static `combWrapSelfTest()`. |
| `src/native/nota.engine/src/Delay.h` | modified | `Delay::read`: same guard (double, same `rp += size; (int)rp` shape). |
| `src/native/nota.engine/src/WavetableSynth.h` | modified | Chorus `tap` lambda: same guard (scope expansion, see below). |
| `src/native/nota.engine/include/nota/nota_engine.h` | modified | New `nota_engine_reverb_wrap_selftest`. |
| `src/native/nota.engine/src/nota_engine_device.cpp` | modified | C ABI entry → `Reverb::combWrapSelfTest()`. |
| `src/managed/Nota.Infrastructure/Interop/NativeMethods.Devices.cs` | modified | P/Invoke `ReverbWrapSelfTest`. |
| `src/managed/Nota.Infrastructure/Interop/NotaEngine.Devices.cs` | modified | `NotaEngine.ReverbWrapSelfTest()`. |
| `tests/Nota.SmokeTest/Program.cs` | added test | Check in the "Nota Reverb" section. |
| `CHANGELOG.md` | modified | `[Unreleased] → Fixed` entry. |
| `src/native/nota.engine/src/ReverbTrace.h` | removed | Temporary flight recorder from the assessment (never committed), removed as the assessment required; `Reverb.h` hooks reverted. |

## Diff Highlights

```cpp
// Reverb::Comb::process
float rp = w - std::clamp(delay, 1.0f, (float)size - 2.0f);
if (rp < 0) rp += size;
int i0 = (int)rp; float frac = rp - i0;
if (i0 >= size) i0 -= size;          // rp + size can round up to exactly `size` in float
int i1 = i0 + 1; if (i1 >= size) i1 -= size;
```

`frac` is taken before the wrap. When `i0 == size`, `rp == size`, so `frac == 0` and the read
returns `buf[0]`, the sample the true position (`size − 1e-5`) is closest to.

## Tests Added or Updated

- `Reverb::combWrapSelfTest()` (via `nota_engine_reverb_wrap_selftest`, smoke check
  "Reverb comb read stays inside its delay line at the wrap edge (no metallic burst)"). It
  sweeps comb sizes 1000–4000 and write indices, and sets the delay to the next float above
  `w`, the exact case where `rp + size` rounds to `size`. It fills the line with zeros except
  `buf[0] = 1` and asserts the comb returns 1. It requires at least one size/index pair to
  actually hit the rounding case, so it can't pass vacuously.

## Local Verification

- Self-test against the unfixed code (guard line removed in a copy): **ASan
  heap-buffer-overflow**; without ASan **FAIL**. Against the fix: **PASS**, ASan clean.
- `scripts/build.sh` → native Release build, `dotnet build Nota.sln -c Release`, smoke test
  **SMOKE TEST PASSED**, including the new check and "every factory preset applies cleanly
  (429 instrument, 944 effect, 0 failed)".
- ASan soak of the fixed `Reverb::process` with the user's sample and Live Room params: 20 min /
  209 loops, clean, per-loop peak constant (0.592). Before the fix the same harness reported
  the overflow within minutes.
- ASan soak with random factory-preset switching every 1–8 s: 15 min, clean.
- `src/managed/Nota.App/bin/{Debug,Release}/net10.0/libnota_engine.dylib` refreshed with the
  fixed build (trace-free, exports `nota_engine_reverb_wrap_selftest`) so a Rider run picks it
  up without a Rebuild.

## Deviations from Assessment

- **Scope expansion**: `WavetableSynth.h` chorus `tap` has the same `while (rp < 0) rp += sz;
  (int)rp` shape (double) and was guarded too. Not listed in the assessment; found in the
  audit it asked for.
- **Audited, left unchanged**: Keys/Vintage/BeatRepeat (masked indices, safe); Flanger/Chorus
  (floor + mask, safe); Ceiling/Compressor/Prism/Shutter (integer delays, safe).
  Strata's loop position can round to `len` on reverse play. It reads `buf_[len]`, which sits
  inside the `maxLoop_` allocation, so it is not out of bounds; it only reads a stale sample at
  the loop seam. Listed as a follow-up.
- The test is a device-free C-ABI self-test (the engine's existing pattern) rather than a
  standalone unit test, because the engine has no native test runner.

## Follow-ups

- Strata: wrap `pos_` (or `p0`) the same way on reverse playback (seam sample, not a memory
  error).
- The gain-staging findings in the assessment are separate from this burst: the Freeverb
  "allpass" at g up to 0.75, comb gain not normalized against Decay, and a soft-clip ceiling
  above 0 dBFS. They cause predictable overs on long-decay presets (Infinite Pad, Send ·
  Plate ≈ 1.1). A candidate for its own change.
- `Engine::deviceParamDefault` builds a full throw-away device per call (21 Reverbs per
  preset apply). Harmless, but wasteful.
- Startup race: `Engine::start()` runs `propagateSampleRate()` after the audio callback is
  live (noted in the assessment, separate issue).
