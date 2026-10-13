# Bug Verification: Clicks on fast Freq / Morph moves in Auto Filter, EQ-8 and Dynamic EQ-8

- **Slug**: filter-param-zipper
- **Tested**: 2026-10-13
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

Measured in both the standalone DSP harness and the real engine, the click on a sudden knob jump is gone:
- **Standalone harness**: the transient over the settled sound drops from ×22 – ×17 000 (HEAD) to ×1.3 – ×75 (fix).
- **Real engine**: the new smoke test fails 4 of 7 cases on a pre-fix engine and passes 7 of 7 with the fix.
- **Static renders** are unchanged.
- **Full smoke suite** passes with 0 failures.

The result is **partial**, not verified, for two reasons. The assessment's reproduction is a by-ear check in the running app, which was not done. Automation playback was not exercised either.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction, pre-fix vs post-fix (DSP) | `/tmp/fpz/harness.cpp` built twice: against `git show HEAD:` copies of the three headers, and against the working tree | pass | HEAD clicks: Auto Filter Freq ×17 472, Morph ×10 028; EQ-8 high cut ×6 324, 48 dB low cut ×725, bell 1k→4k ×438; Dynamic EQ-8 high cut ×4 148. Fix: ×44, ×7, ×75, ×9, ×4, ×45. Low bells ×22–44 → ×1.3. |
| Static output unchanged | same harness, no knob move | pass | RMS identical to 6 digits for Auto Filter, EQ-8, Dynamic EQ-8 and a ducking dynamic band |
| New test fails without the fix | Engine copy in `/tmp/fpz/eng` (working tree with the three headers reverted to HEAD), smoke binary run against it: `dotnet /tmp/fpz/binbase/Nota.SmokeTest.dll` | pass (fails as expected) | 4/7 FAIL: Auto Filter Freq ×3 329, Morph ×4 816; EQ-8 high cut ×518; Dynamic EQ-8 high cut ×296. 3/7 pass even pre-fix, see Residual Risks. The other 9 FAILs in that run are "repo root located", an artefact of running the binary from `/tmp`. |
| New test passes with the fix | `dotnet run --project tests/Nota.SmokeTest -c Release` (filter knob glide section) | pass | ×20.2, ×3.8, ×13.3, ×1.7, ×1.0, ×7.7, ×1.1, all ≤ ×150 |
| Regression suite | `cmake --build src/native/nota.engine/build` + `dotnet build tests/Nota.SmokeTest -c Release` + full smoke run | pass | 3182 PASS, 0 FAIL, `SMOKE TEST PASSED`. This ran on a tree that also holds another session's uncommitted work (`specs/004-default-project-template`). |
| Full `scripts/build.sh` | — | not-run | Only the Release native build and the smoke test project were built. The universal `lipo` build and the whole `Nota.sln` build were not run. |
| By-ear check in the app | manual knob sweeps and an automation ramp | not-run | The reproduction steps in the assessment are aural. Not performed in this session. |
| Lint / type-check | `dotnet build` (warnings) | pass | No new warnings. The existing CS8602/CS0219 warnings are in untouched files. |

## Output Excerpts

```
-- filter knob glide --            (pre-fix engine)
  [FAIL] Auto Filter Freq jump glides without a click (jump ×3329,2 the settled sound, limit ×150)
  [FAIL] Auto Filter Morph jump glides without a click (jump ×4815,6 the settled sound, limit ×150)
  [FAIL] EQ-8 high cut Freq jump glides without a click (jump ×518,0 the settled sound, limit ×150)
  [FAIL] Dynamic EQ-8 high cut Freq jump glides without a click (jump ×296,4 the settled sound, limit ×150)
```
```
-- filter knob glide --            (fixed engine)
  [PASS] … ×20,2 · ×3,8 · ×13,3 · ×1,7 · ×1,0 · ×7,7 · ×1,1 (limit ×150)
SMOKE TEST PASSED
```

## Residual Risks

- **Three test cases don't guard the fix.** EQ-8 bell Freq (×33.8 → ×1.7), EQ-8 48 dB low cut (×15.8 → ×1.0)
  and Dynamic EQ-8 bell Gain (×16.6 → ×1.1) pass under the single ×150 limit even without the fix. A regression
  there would go unnoticed. Per-case limits (e.g. ×10 for these, ×50 for the resonant sweeps) would close the gap.
  That is a test change, outside this command.
- Not heard in the app. Automation playback was not exercised, though it writes the same param atomics the test uses.
- The residual ×44 – ×75 on resonant cuts in the harness is attributed to the resonance sweeping across the
  test tone, plus 16-sample coefficient steps. That attribution is reasoned, not proven by ear.
- Extreme settings (Q 18, very fast sweeps), other sample rates (only 44.1/48 kHz were checked) and EQ-8
  Type/Slope/Channel switches were not covered. The last one is out of scope.

## Recommendation

Hold before closing:
1. Sweep Freq/Morph by hand and play a steep automation ramp on each device. This turns the result into **verified**.
2. Optionally tighten the smoke-test limits per case so all seven cases guard the fix.
3. Run `scripts/build.sh` before the PR.

The DSP evidence is strong and nothing regressed, so no re-assessment is needed.
