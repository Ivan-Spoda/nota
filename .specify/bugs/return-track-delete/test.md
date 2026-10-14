# Bug Verification: Return track can't be deleted in the arrangement

- **Slug**: return-track-delete
- **Tested**: 2026-10-13
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The reproduction from the assessment no longer reproduces: in the real MainWindow with the
default template, a Return is deleted by Backspace, by Delete and by the context menu's
"Delete track", and undo brings it back. The engine keeps the surviving Return sounding with
its sends intact. Full `scripts/build.sh` (universal engine + solution + smoke test) passes with
no failures.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | headless harness `/tmp/returnharness` (`NOTA_DATA_DIR=/tmp/returnharness/data dotnet run`): real MainWindow + template, synthetic pointer clicks on the footer rows, KeyDown Back/Delete on the window, right-click → flyout item Click | pass | 10/10 checks; Master menu has no Delete |
| New tests | `scripts/build.sh` → smoke section `-- return delete --` | pass | 10/10 (`ReturnDeleteTests`) |
| Regression suite | `scripts/build.sh` (full smoke test) | pass | exit 0, `SMOKE TEST PASSED`, 0 `[FAIL]` lines |
| Build / type-check | `scripts/build.sh` (cmake universal + `dotnet build Nota.sln -c Release`) | pass | 0 errors; dylib arm64 + x86_64 |

## Output Excerpts

```
-- return delete --
  [PASS] returns: the survivor moves to bus 0 (index 0)
  [PASS] returns: the send follows its bus (0,70, 0,00)
  [PASS] returns: the surviving return still sounds (peak=0,030)
  [PASS] returns: undo restores both buses and both sends
  [PASS] returns: save/load keeps each send on its return
SMOKE TEST PASSED
```

```
[PASS] Backspace on the selected Reverb row deletes it
[PASS] Delete on the selected Delay row deletes only Delay
menu items found: Rename…, Color, Delete track
[PASS] the menu item deletes Reverb
master menu: Rename…, Color
[PASS] the master row's menu has no Delete
```

## Residual Risks

- UI input was synthetic (headless Avalonia), not a physical mouse/keyboard on the running
  macOS app: the native Edit-menu accelerator path (`OnMenuDelete`, same `_trackHeadersFocused`
  gate) was not exercised directly. A 10-second manual check in the app would close this.
- The pre-fix failure of the new engine test was not demonstrated (it would require reverting
  source); the pre-fix behaviour is established by code reading in the assessment.
- Session view / mixer still offer no Return deletion (out of scope, open question).

## Recommendation

Close the bug — verified end-to-end through the real window in headless mode and by the
engine regression tests; optionally confirm once by hand in the running app before release.
