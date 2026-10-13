# Bug Verification: Nota Reverb — sudden loud looped metallic burst

- **Slug**: reverb-metallic-burst
- **Tested**: 2026-10-13
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

The root cause, an out-of-bounds comb read (`buf[size]`), reproduces deterministically under
AddressSanitizer on the user's own loop with the code before the fix. With the fix it no
longer occurs in 20 min of the same material, nor in random-automation soaks at 48 and 96 kHz.
The new self-test passes, and the full smoke suite passes with no regressions. **Partial**
only because the original in-app symptom (a burst after minutes of live playback) has not yet
been re-checked in the running app after the fix. That is the one reproduction the assessment
recorded that these checks don't exercise directly.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction, **pre-fix** (control) | ASan build of `Reverb::process` on `/tmp/revtest/s/loop.wav` (user's sample) + Live Room params, guard line removed (`/tmp/revtest/neg/Reverb.h`) | fail (expected) | `heap-buffer-overflow … 0 bytes after 9116-byte region` in `Comb::process` (`Reverb.h:173` in the copy), 2 s wall time |
| Reproduction, **post-fix** | Same harness against the working tree, 20 min of audio | pass | 209 loops, ASan clean, per-loop peak constant 0.592 |
| Random automation, 48 kHz | ASan, `t_sr` scenario 3 (Decay/Size/Diffusion/Algorithm/Freeze/… jumps every ~2 s), 10 min | pass | ASan clean; max 2.06 = soft-clip ceiling on extreme settings (gain-staging follow-up, not this bug) |
| Random automation, 96 kHz | Same, 10 min | pass | ASan clean; different comb sizes |
| Preset switching (from fix phase) | ASan, random factory-preset switching every 1–8 s, 15 min | pass | ASan clean |
| New test | `dotnet run --project tests/Nota.SmokeTest -c Release` | pass | `[PASS] Reverb comb read stays inside its delay line at the wrap edge (no metallic burst)`. The fix phase showed the same self-test **fails** on the unfixed code (ASan overflow / FAIL). |
| Regression suite | Same smoke run (drives the engine end-to-end; preceded by `scripts/build.sh` in the fix phase) | pass | `SMOKE TEST PASSED`, 3104 `[PASS]`, 0 `[FAIL]` |
| Lint / warnings | `c++ -std=c++20 -fsyntax-only -Wall -Wextra` on `Reverb.h`, `Delay.h`, `WavetableSynth.h` | pass | No warnings in the changed headers. The project has no separate linter. |
| In-app re-test after fix | Nota from Rider, looped sample + Reverb + preset switching, 15–20 min | not-run | Needs the user's session. The fixed `libnota_engine.dylib` is already in `bin/Debug` and `bin/Release`. |

## Output Excerpts

```
pre-fix:  ==44053==ERROR: AddressSanitizer: heap-buffer-overflow on address 0x62500000c49c
          READ of size 4 ... is located 0 bytes after 9116-byte region
          #0 nota::Reverb::process(float*, int) Reverb.h:173
post-fix: done: 209 loops, first-loop peak 0.592        rc=0
48 kHz:   scen 3 done, max=2.056                         rc=0
96 kHz:   scen 3 done, max=2.020                         rc=0
smoke:    [PASS] Reverb comb read stays inside its delay line at the wrap edge (no metallic burst)
          SMOKE TEST PASSED
```

## Residual Risks

- The in-app symptom has not been re-observed after the fix (see above). The trigger is rare
  (minutes) and the garbage value depends on heap layout, so even an in-app absence over
  20 min is supporting evidence, not proof. The ASan control/fix pair is the decisive check.
- The ASan soaks cover the standalone `Reverb`. Delay and the WavetableSynth chorus got the
  same guard, but they were only exercised by the smoke suite, not by a dedicated ASan soak.
  Their edge case is in double precision and ~10⁷× rarer.
- Unrelated to this bug and still open (see `fix.md` follow-ups): overs up to the soft-clip
  ceiling on extreme or long-decay settings, the Strata reverse-loop seam, and the startup
  `propagateSampleRate` race.

## Recommendation

Hold for one in-app confirmation: run Nota from Rider with the looped sample, Nota Reverb
(Live Room) and some preset switching for 15–20 min. If no burst occurs, close the bug as
verified. Everything automatable is green, and the decisive ASan control shows the
out-of-bounds read before the fix and none after. If a burst still occurs, re-enable the
flight recorder from the assessment phase and re-run `/speckit-bug-assess` with the new dump.
