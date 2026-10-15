# Implementation Plan: Device categories in the browser

**Branch**: `013-browser-categories` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/013-browser-categories/spec.md`

## Summary

> **Revised (R9):** the first build used foldable category headers. After seeing it, the
> user chose Ableton-style **category chips** instead. This summary describes the chips
> design; R3–R6 in research.md are kept for history.

The Instruments, Audio effects and MIDI effects tabs get a row of category chips above the
list. Picking a chip narrows the list to that category; it is on by default. The work
splits five ways:

1. **Categories** (unchanged from the first build): a pure `DeviceCategories` class in
   `Nota.Application` holds:
   - the confirmed lists (Instruments 5, Audio effects 8, MIDI effects 2) with their
     built-in members;
   - the `Other` fallback;
   - the plug-in mapping (VST3 sub-categories; AU type names map to Other) (R1, R2).

   Each device row carries its category.
2. **Chips (US1)**: `BrowserViewModel` gains a per-tab category filter applied in the
   existing device loop, next to the favourite/tag filter, plus `CategoriesOn(tab)`.
   `BrowserView` gains a wrapping chip row between the search row and the Favorites/tags
   row, built with the existing `MakeChip`. One pick per tab: click again to clear (R9).
3. **Off switch (US2)**: "Category chips" in ⋮, persisted as
   `Settings.BrowserCategoryChips` (default true). Turning it off clears the picks.
4. **Combining (US3) and plug-ins (US4)**: the chip stacks with search, Favorites and tags
   for free (all of them filter the same loop). Plug-ins carry their mapped category, and
   the Other chip appears only when something maps there.
5. **Guard and docs**:
   - `DesignTokenCheck.RunBrowser` (unchanged);
   - behavioural checks in `BrowserCategoryTests` (`--browser`);
   - CHANGELOG, FEATURES.md, DESIGN.md and the nota-instrument skill.

It is UI and settings only: no engine, C ABI, project-format or shortcut change.

## Nota: Read Before Planning

- **UI work** → [`DESIGN.md`](../../DESIGN.md) (Studio) and the `nota-design` skill.
  The chips reuse `MakeChip`, the filter-chip style of the Favorites / tags and the Files
  sample chips; there are no new controls.
- **A built-in device** → the `nota-instrument` skill. Its registration checklist gains
  "add the device to `DeviceCategories`".
- **Anything** → [`CONTRIBUTING.md`](../../CONTRIBUTING.md): SPDX headers on new files,
  no new dependency.

## Technical Context

**Language/Version**: C# / .NET 10 (managed app only).

**Primary Dependencies**: Avalonia 12.0.3 (existing). No new dependency.

**Storage**: `settings.json` gains `BrowserCategoryChips` (bool, default true). The picked
chip is not saved. No project-format change.

**Audio-Thread Impact**: None. This is a UI-thread list projection; nothing crosses the
C ABI.

**Testing**: `tests/Nota.SmokeTest`:
- **New**: `DesignTokenCheck.Browser.cs` (`RunBrowser`, wired into `Program.cs`).
- **Behavioural checks** in `BrowserCategoryTests.cs`, run in sequence and alone with
  `--browser`. They use `FakePluginCatalog` and `FakeBrowserLibrary` from
  `BrowserTestSupport.cs`.
- **Screenshots**: the device tabs under Xvfb in all four themes.

**Target Platform**: macOS 13+, Windows 10+, Linux, as today.

**Project Type**: Desktop app (Avalonia).

**Performance Goals**: no measurable change. Ordering at most a few hundred device rows
with a multi-key sort is negligible next to today's rebuild. The palette is unaffected
because it reads the unchanged trees.

**Constraints**:
- With no chip picked, the list must be today's sequence exactly (SC-003).
- No new colour, radius or icon style (FR-008).
- Device rows, preset expansion, drag, preview, favourites and tags are unchanged
  (FR-007, FR-010).

**Scale/Scope**:
- 50 built-in devices across 15 categories, plus any number of plug-ins;
- about 6 source files touched and 2 new (one app, one test).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Note |
|---|---|---|
| I. Real-time audio safety | ✅ N/A | UI-only. |
| II. Boundary discipline | ✅ | No C ABI change. The plug-in category string already crosses today, in the plug-in description. No JUCE outside `pluginhost/`. |
| III. Licensing hygiene | ✅ | The two new files carry the SPDX header. No dependency. |
| IV. Verification gate | ✅ | The smoke test gains `RunBrowser` and behavioural browser checks. `scripts/build-linux.sh` must pass. |
| V. One logical change per PR | ✅ | One feature: browser categories. |
| VI. Docs in sync | ✅ | CHANGELOG `[Unreleased]`; FEATURES.md (nota-features); DESIGN.md browser bullet; nota-instrument skill checklist. nota-docs (sibling repo) is noted as a follow-up because it isn't available here. |
| VII. Spec-first | ✅ | Spec plus 5 clarifications; plan now, tasks next. |
| VIII. Safety rules not weakened | ✅ | Checks only get stricter (a new guard). |

**Post-design re-check**: unchanged. The chips design adds one settings key and one
in-memory filter, and leaves the list code path as it was when no chip is picked.

## Project Structure

### Documentation (this feature)

```text
specs/013-browser-categories/
├── plan.md              # this file
├── research.md          # R1–R8
├── data-model.md        # DeviceCategory, BrowserItem additions, view-model state, settings
├── quickstart.md        # automated + manual verification
├── contracts/
│   └── ui-contract.md   # C1–C6 observable behaviour
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks (not created here)
```

### Source Code (repository root)

```text
src/managed/
├── Nota.Application/
│   ├── Palette/DeviceCategories.cs        # NEW: category lists, For(), ForPlugin(), Order() (R1, R2)
│   └── Settings.cs                        # + BrowserCategoryView, BrowserFoldedCategories (R5)
├── Nota.Presentation/ViewModels/
│   └── BrowserViewModel.cs                # BrowserItem.DeviceCategory; per-tab category filter,
│                                          # CategoriesOn (R9)
└── Nota.App/
    └── BrowserView.cs                     # category chip row (WrapPanel, MakeChip);
                                           # ⋮ "Category chips"; settings load/save (R9)

tests/Nota.SmokeTest/
├── DesignTokenCheck.Browser.cs            # NEW: RunBrowser (R7)
├── BrowserCategoryTests.cs                # NEW: chip filter behaviour (--browser)
├── BrowserTestSupport.cs                  # + FakePluginCatalog, FakeBrowserLibrary
└── Program.cs                             # wire RunBrowser and BrowserCategoryTests

DESIGN.md · CHANGELOG.md · FEATURES.md · .claude/skills/nota-instrument/SKILL.md
```

**Structure Decision**: the existing three-layer split is kept.
- Pure category data and mapping go in `Nota.Application`, next to
  `BuiltinDeviceCatalog`, so both the view model and the smoke test can reach them.
- List projection goes in the `Nota.Presentation` view model, which already owns
  grouping and search.
- Header drawing and settings I/O go in `Nota.App/BrowserView.cs`, which already owns
  the ⋮ menu and the group row.

## Complexity Tracking

No constitution violations, so nothing to justify.
