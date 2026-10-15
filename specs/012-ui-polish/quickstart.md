# Quickstart: verifying the UI polish pass

## 1. Gate

```bash
scripts/build-linux.sh      # or scripts/build.sh on macOS
```

Expect these smoke-test sections to pass: `-- design: labels --` and `-- design: icons --`
(new), plus the extended numbers and geometry checks, `PaletteTests`, and every existing
design, theme and appearance check. The only allowed failure is the known environmental
one ("Consort stress"), and it must also fail on the base branch.

## 2. Screenshot harness (Xvfb, 1200 × 800)

- **Tour**: re-run the critique tour (`tour.sh <theme>`) in all four themes and check
  every finding F1–F8 and F10–F12 against contracts/ui-contract.md (SC-001).
- **Cards**: before and after every built-in card, with an edge-overlay diff:
  `convert … -edge 1` for each, then compare. The control positions must match within
  1 px (P4.2).
- **Defect runs**:
  - add a track while the tracks fill the view, and check that the new one is fully
    visible above a seam (P1.1);
  - open Session at 1200 × 800 (P1.2);
  - open a project, then switch to Modular with nothing selected (P1.3).

## 3. Manual on macOS (window chrome)

1. Open the mixer with 2, 5 and 12 tracks. The window fits its strips. Press Escape and
   it closes.
2. Open Export audio. The title bar reads "Export audio". Press Escape and it cancels.
3. Hover the three view keys. Each tooltip shows the view name.

## 4. Regression

- Every FEATURES.md path and shortcut still works (SC-006). In particular, view switching
  through the menu and the palette is unchanged.
- Projects from before this feature open unchanged.
