---

description: "Task list for device categories in the browser"
---

# Tasks: Device categories in the browser

**Input**: Design documents from `/specs/013-browser-categories/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-contract.md, quickstart.md

**Tests**: The spec requires a guard (FR-009) and measurable checks (SC-001, SC-003, SC-004),
so the check tasks are part of the work. They extend the existing gate in
`tests/Nota.SmokeTest`; there is no new test project.

**Organization**: by user story. Path shorthands:

| Shorthand | Path |
|---|---|
| `App/` | `src/managed/Nota.App/` |
| `Application/` | `src/managed/Nota.Application/` |
| `VM` | `src/managed/Nota.Presentation/ViewModels/BrowserViewModel.cs` |
| `Smoke/` | `tests/Nota.SmokeTest/` |

The Xvfb harness and capture scripts from 012 live in `/tmp/claude-0/`, outside the repo.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story the task belongs to (US1–US4)

---

## Phase 1: Setup

**Purpose**: a known-good build and a visual baseline of the three device tabs.

- [X] T001 Build `013-browser-categories` with `scripts/build-linux.sh` (Xvfb, PulseAudio null sink) and record the smoke-test result in `/tmp`, not committed. Only the known "Consort stress" failure is allowed; the palette keystroke budget may also fail under load and passes when re-run quietly.
- [X] T002 [P] Capture the **baseline** Instruments, Audio effects and MIDI effects tabs (browser at its default width, fresh `HOME`) in all four themes into `/tmp/claude-0/shots/browser-before/`. Use a small `/tmp/claude-0/scripts/browser-tabs.sh <theme> <outdir>` that clicks the three rail tabs. Also capture the Audio effects tab with ⋮ open.

---

## Phase 2: Foundational (blocking)

**Purpose**: category data, the guard, settings and row fields. Every story builds on these.

- [X] T003 Create `Application/Palette/DeviceCategories.cs`:
  - the two-line SPDX header and a why-comment;
  - a `public const string Other = "Other"`;
  - per `BuiltinDeviceType`, an ordered list of `(string Name, string[] Builtins)` written exactly as the spec's Assumptions list them:
    - Instruments: Synths, Keys & physical, Drums, Samplers, Racks;
    - Audio effects: EQ & filter, Dynamics, Reverb & delay, Modulation, Distortion, Pitch & time, Utility, Racks;
    - MIDI effects: Generate, Shape.

    Use the full catalogue names (`"Nota Reverb"`).
  - `For(BuiltinDevice)` returns the category, or `Other`;
  - `Order(BuiltinDeviceType)` returns the names in tab order;
  - `Position(BuiltinDeviceType, string)` returns the index, or `int.MaxValue` for `Other` or an unknown name;
  - `ForPlugin(bool instrument, string reported)` returns `Other` for now (US4 fills it in).

  See data-model.md § DeviceCategory.
- [X] T004 [P] Create `Smoke/DesignTokenCheck.Browser.cs`, a `partial class DesignTokenCheck` with the SPDX header and `RunBrowser()`. Per research R7 it yields:
  - every `BuiltinDeviceCatalog.All` entry is in exactly one category of its own type (none `Other`);
  - every listed name exists in the catalogue with that type (a stale name fails, naming it);
  - no category has an empty `Builtins`;
  - `NotaText.Label(name) == name` for every category name;
  - the per-tab totals are 18 / 26 / 6.

  Each failure names the device or category.
- [X] T005 Wire `RunBrowser()` into `Smoke/Program.cs` as a `-- design: browser --` section, next to the other `DesignTokenCheck.Run*` calls (around line 63–81).
- [X] T006 [P] In `Application/Settings.cs`, add these next to `BrowserGroupBySource`, each with an XML doc line:
  - `public bool BrowserCategoryView { get; set; } = true;`
  - `public List<string> BrowserFoldedCategories { get; set; } = new();`

  See data-model.md § Settings.
- [X] T007 In `VM` `BrowserItem`, add:
  - `public string DeviceCategory { get; init; } = "";`
  - `public bool IsFoldable { get; init; }`
  - `public bool IsFolded { get; init; }`

  In `Rebuild()`, set `DeviceCategory` on each built-in (look up the `BuiltinDevice` in `BuiltinDeviceCatalog.All` by `Type` and `Kind`, then `DeviceCategories.For`). Set it to `DeviceCategories.Other` on plug-ins for now.
- [X] T008 Build and run the smoke test. `-- design: browser --` must be green, and the rest unchanged from T001.

  *Result*: T001 baseline — only "Consort stress" fails. Design checks 152/152 (134 + 18 new browser checks). `ForPlugin` was written in full with T003 and plug-in rows get it in T007, so T026–T027 land here.

**Checkpoint**: categories exist and are guarded. Nothing visible has changed yet.

---

## Phase 3: User Story 1 — Browse devices by what they do (Priority: P1) 🎯 MVP

**Goal**: the device tabs show built-in devices grouped by category by default (contract C1, research R3).

**Independent test**: on a fresh profile, each device tab lists its categories in the fixed order, and every built-in appears exactly once, alphabetically within its category.

- [X] T009 [US1] In `VM`, add the `CategoryView` property: a `bool _categoryView = true`, and a setter that rebuilds the device tabs, following the `GroupBySource` pattern. Doc comment: "Group the device tabs under category headers."
- [X] T010 [US1] In `VM` `RebuildVisible(tab)`, branch on `_categoryView`, leaving the existing flat path **byte-for-byte unchanged**. In the category branch:
  1. Filter devices with today's library-filter and query rules.
  2. Order them by these keys:
     - `DeviceCategories.Position(type, DeviceCategory)`;
     - then, if `_groupBySource`, `IsPlugin`;
     - then, if `_favoritesFirst && _library != null`, not-favourite;
     - then `ShortName` (`StringComparer.OrdinalIgnoreCase`).
  3. For each run of the same category, emit a `Group` row:
     - `Name` = the category;
     - `GroupCount` = the devices in that run;
     - `Path = $"cat:{TabKey(tab)}:{name}"`, where `TabKey` is `instr`, `fx` or `midi`;
     - `IsFoldable = true`.
  4. Emit the devices and their preset rows exactly as the flat path does.
- [X] T011 [P] [US1] Add `FakeBrowserLibrary : IBrowserLibrary` to `Smoke/BrowserTestSupport.cs`: in-memory favourites (a `HashSet<string>`), no tags (empty lists, no-op tag methods).
- [X] T012 [US1] Update the existing browser search assertion in `Smoke/Program.cs` (around line 14510): with the category view on by default, the expected sequence for "Glacier" is `Synths, Nota Aurora, Pads, Glacier Pad`.
- [X] T013 [US1] Add category-view checks to the browser block of `Smoke/Program.cs`, using a fresh `BrowserViewModel` over `EmptyPluginCatalog`:
  - **(a)** On each tab, the `Group` row names equal `DeviceCategories.Order(type)`.
  - **(b)** Each built-in appears exactly once, under its `DeviceCategories.For` header.
  - **(c)** Within each category, devices are in `ShortName` order.
  - **(d)** Header `GroupCount` equals the rows beneath it.
  - **(e)** With a `FakeBrowserLibrary` where Nota Delay is a favourite, Delay is first under Reverb & delay (FR-004).
  - **(f)** Expanding a device in the category view inserts its preset rows directly beneath it, inside its category (FR-007).
  - **(g)** A search for "Compressor" shows Compressor under the Dynamics header; every header shown has at least one device row beneath it, and categories without matches are absent (FR-006).
- [X] T014 [US1] Build, run the smoke test, then capture the three tabs in all four themes into `/tmp/claude-0/shots/browser-after/` with `browser-tabs.sh`. Check:
  - the headers match the Built-in header style (FR-008, SC-005);
  - "EQ & filter" keeps its casing;
  - every device is visible under a header (SC-001).

  *Result*: the category checks live in `Smoke/BrowserCategoryTests.cs` (run in sequence from `Program.cs`, or alone with `--browser`) rather than inline, so they can be run in seconds. 18/18 pass. Studio Dark capture: headers in order with counts, devices alphabetical, "EQ & filter" keeps its casing. The four-theme run is in T025 and T034, once the caret is in.

**Checkpoint**: MVP. The browser is categorised by default; folding, the off switch and plug-in mapping follow.

---

## Phase 4: User Story 2 — Turn the category view off (Priority: P2)

**Goal**: "Group by category" in ⋮, persisted; off restores today's list exactly (contract C4, research R5).

**Independent test**: untick "Group by category" and the flat list returns row for row; it stays off after a restart.

- [X] T015 [US2] In `App/BrowserView.cs` `ShowOptionsMenu`, add a `CheckItem("Group by category", _vm?.CategoryView ?? true, v => …)` directly after "Show type tags" and before "Group by source". It sets `_vm.CategoryView`, saves `_settings.Current.BrowserCategoryView`, and calls `RefreshCounts()`.
- [X] T016 [US2] In `App/BrowserView.cs` `SetSettings` and `SetViewModel`, apply `settings.Current.BrowserCategoryView` to `vm.CategoryView`, alongside `GroupBySource` and `FavoritesFirst`.
- [X] T017 [US2] Add checks to `Smoke/Program.cs`:
  - **(a)** With `CategoryView = false`, the Instruments sequence is `BUILT-IN` followed by every built-in instrument in `Name` order, which is today's flat list (SC-003). Same for the Audio effects tab.
  - **(b)** Turning it back on restores the category headers.
  - **(c)** `JsonSerializer.Deserialize<Settings>("{}")` has `BrowserCategoryView == true` and an empty `BrowserFoldedCategories` (FR-003, existing users).
- [X] T018 [US2] Manual check under Xvfb (quickstart § 2 step 4): untick, relaunch with the same `HOME`, and it's still off. Compare the flat-list screenshot with the T002 baseline: it must be identical.

  *Result*: the checks are in `Smoke/BrowserCategoryTests.cs`; all pass. Under Xvfb, unticking wrote `"BrowserCategoryView": false`. After a relaunch the Instruments list matches the T002 baseline except a 40×36 patch on the rail button that had just been clicked (hover state); the list itself is pixel-identical.

---

## Phase 5: User Story 3 — Fold the categories you don't need (Priority: P2)

**Goal**: headers fold on click; folds persist per tab; a search ignores folds without changing them; counts are unaffected (contract C2, C3, C5; research R4, R6).

**Independent test**: fold all but one category on Audio effects; only that category's devices show; switch tabs and restart, and the folds are kept; a search reveals hits inside folded categories, and clearing the search restores the folds.

- [X] T019 [US3] In `VM`, add the fold state:
  - `HashSet<string> _folded` (`StringComparer.Ordinal`) with keys `"{TabKey}:{name}"`;
  - `SetFoldedCategories(IEnumerable<string>)`, which loads without raising the event;
  - `IReadOnlyCollection<string> FoldedCategories`;
  - `public event Action? CategoryFoldsChanged`;
  - `ToggleCategoryFold(BrowserItem header)`. It does nothing unless `header.IsFoldable`. Otherwise it parses the tab from `Path` (`cat:<tabKey>:<name>`), flips the key, `RebuildVisible(tab)`, and raises the event.
- [X] T020 [US3] In `VM` `RebuildVisible`'s category branch:
  - **No query**: a header in `_folded` gets `IsFolded = true` and its devices and presets are skipped; the header and its `GroupCount` stay.
  - **Query active**: headers get `IsFoldable = false` and `IsFolded = false`, and folds are ignored.
- [X] T021 [US3] In `VM`, record per-tab `(builtin, plugins)` counts of the filtered devices *before* folding, during `RebuildVisible` (both branches), and have `VisibleDeviceCounts(tab)` return them (R6). Check that `App/BrowserView.cs` `StatusFor` still reads correctly.
- [X] T022 [US3] In `App/BrowserView.cs` `GroupRow`, when `item.IsFoldable`:
  - **Caret**: put `RailIcon(item.IsFolded ? GlyphKind.CaretRight : GlyphKind.CaretDown, 12, "Brush.TextTertiary")` before the label in a new leading `Auto` column, with the label's left margin adjusted so the caret sits where a device row's disclosure sits.
  - **Click**: `Cursor = Hand`; `PointerPressed` calls `_vm?.ToggleCategoryFold(item)`, then `RefreshCounts()`, and sets `e.Handled = true`.
  - **Accessible name**: `AutomationProperties.SetName(grid, $"{name}, {count} devices, {(folded ? "folded" : "open")}")`.

  Non-foldable group rows (Built-in / Plug-ins, or any header during a search) render exactly as today. The new tooltip and automation strings must pass `RunLabels`.
- [X] T023 [US3] In `App/BrowserView.cs`:
  - `SetSettings` / `SetViewModel` call `vm.SetFoldedCategories(settings.Current.BrowserFoldedCategories)`;
  - subscribe once to `vm.CategoryFoldsChanged`, which writes `_settings.Current.BrowserFoldedCategories = vm.FoldedCategories.ToList()` and calls `_settings.Save()`.
- [X] T024 [US3] Add checks to `Smoke/Program.cs`:
  - **(a)** Folding Dynamics on Audio effects removes Compressor, Prism, Ceiling and Shutter. The Dynamics header stays with `IsFolded` and `GroupCount == 4`.
  - **(b)** `VisibleDeviceCounts(1)` is unchanged by the fold.
  - **(c)** A search for "Prism" shows Dynamics (`IsFoldable == false`) with Prism. Clearing the query brings back the folded Dynamics, and `FoldedCategories` is unchanged throughout (US3/AC2).
  - **(d)** `ToggleCategoryFold` during a search is a no-op.
  - **(e)** Folding on Audio effects doesn't affect Instruments' `Racks` (keys are per tab).
  - **(f)** `SetFoldedCategories(new[]{"fx:Dynamics","fx:Nonexistent"})` folds Dynamics and ignores the unknown key.
- [X] T025 [US3] Manual check under Xvfb (quickstart § 2 steps 2–3, 7):
  - fold, switch tabs, relaunch: the folds are kept;
  - a search shows hits in folded categories with no caret, and clearing it restores the folds;
  - the minimum browser width truncates captions with the caret and count visible.

  Screenshot folded and open headers in all four themes.

  *Result*: all fold checks pass (`--browser`). Under Xvfb:
  - Dynamics and Modulation fold with their counts kept, and the status line stays "26 built-in";
  - "comp" shows Dynamics open with no caret, and clearing it restores both folds;
  - a relaunch restores the folds from `settings.json`;
  - at the 160 px minimum width the caption, caret and count stay visible, with no overlap.

  Two fixes found here:
  - a header click no longer opens the empty preview player (it cleared `_listPointerDown`; the same flash existed on the old Built-in header);
  - the search-box count and the empty state include devices under folded headers, so a tab with every category folded doesn't say "No audio effects yet".

  Four-theme captures are in T034.

---

## Phase 6: User Story 4 — Plug-ins land in the same categories (Priority: P3)

**Goal**: plug-ins map from their reported category; unknown goes to Other, last; source order respected (contract C1, research R2, R3).

**Independent test**: with fake plug-ins reporting `Fx/Reverb` and `Effect`, the first appears under Reverb & delay and the second under Other, last.

- [X] T026 [US4] Implement `DeviceCategories.ForPlugin(bool instrument, string reported)` in `Application/Palette/DeviceCategories.cs` per research R2:
  - split on `/` and `|`, then trim and lower-case each token;
  - the first token found in the tab's table wins;
  - skipped tokens are listed in a comment;
  - empty or unrecognised maps to `Other`.

  Token tables:
  - **Instruments**: `synth` → Synths; `piano` → Keys & physical; `drum` → Drums; `sampler` → Samplers.
  - **Effects**:
    - `eq`, `filter` → EQ & filter;
    - `dynamics`, `mastering` → Dynamics;
    - `reverb`, `delay` → Reverb & delay;
    - `modulation` → Modulation;
    - `distortion` → Distortion;
    - `pitch shift` → Pitch & time;
    - `analyzer`, `tools`, `spatial`, `surround`, `restoration`, `up-downmix` → Utility.
- [X] T027 [US4] In `VM` `Rebuild()`, set plug-in rows' `DeviceCategory = DeviceCategories.ForPlugin(isInstrument, category)`, replacing the T007 placeholder.
- [X] T028 [P] [US4] Add `FakePluginCatalog(params string[] descriptions) : IPluginCatalog` to `Smoke/BrowserTestSupport.cs`. Descriptions use the `Name | Format | inst|fx | Vendor | Category` form that `Rebuild()` parses; `Id` is `"fake:" + index`.
- [X] T029 [US4] Add checks to `Smoke/Program.cs`:
  - **(a)** `ForPlugin` table cases:
    - `Fx/Reverb` → Reverb & delay;
    - `Fx/Dynamics/EQ` → Dynamics (first recognised);
    - `Fx/Pitch Shift` → Pitch & time;
    - `Instrument/Synth/Sampler` → Synths;
    - `Instrument/Drum` → Drums;
    - `Effect` → Other;
    - `""` → Other;
    - `Fx` → Other.
  - **(b)** A view model over a `FakePluginCatalog` with "Ambience | VST3 | fx | Smart | Fx/Reverb", "Mystery | AU | fx | Acme | Effect" and "Ana | VST3 | inst | Sonic | Instrument/Synth":
    - with Group by source on, Reverb & delay lists Chamber, Delay, Reverb, then Ambience;
    - with it off, it lists Ambience, Chamber, Delay, Reverb (interleaved by short name);
    - Other is the last header on Audio effects and holds Mystery;
    - Ana is under Synths after the built-ins;
    - the MIDI tab is unaffected.
  - **(c)** No separate `BUILT-IN` / `PLUG-INS` header appears in the category view (FR-005).

  *Result*: T026–T028 landed with Phase 2–3 (`ForPlugin` in T003, plug-in rows in T007, `FakePluginCatalog` with T011). T029's checks pass: the mapping table; Reverb & delay with Group by source on (Chamber, Delay, Reverb, Ambience) and off (Ambience, Chamber, Delay, Reverb); Other last; a synth plug-in after the built-in synths; no source headers; the MIDI tab untouched; counts 26 + 2.

---

## Phase 7: Polish & Cross-Cutting

- [X] T030 [P] `DESIGN.md`:
  - **Browser bullet** (layout section, around line 651): device tabs group under foldable category headers in the group-header style, with a 12 px disclosure caret and the count; Other is last; a search ignores folds.
  - **"Enforced by tests" list**: add `RunBrowser`.

  Regenerate `DESIGN.html` with `python3 scripts/design-html.py`.
- [X] T031 [P] `.claude/skills/nota-instrument/SKILL.md`: add "add the device to its category in `Application/Palette/DeviceCategories.cs` (the `RunBrowser` check fails otherwise)" to the registration checklist.
- [X] T032 [P] `CHANGELOG.md` `[Unreleased]` Added: the browser groups instruments, audio effects and MIDI effects into foldable categories by default (plug-ins included, by their reported category). "Group by category" in ⋮ turns it off.
- [X] T033 [P] Run the nota-features skill for `FEATURES.md`: the browser section (categories, folding, the view option). Note nota-docs (the sibling repo `nota-daw/nota-docs`, unavailable here) as a follow-up in the PR. nota-shortcuts is N/A (no shortcut change).
  *Result*:
  - DESIGN.md has a "Browser categories" bullet and a `RunBrowser` row. `DESIGN.html` is regenerated from tokens only, so it is unchanged.
  - FEATURES.md gains a "Device categories" bullet and "group by category" in View options. The compact-index bullet's stale "tertiary Nota prefix" claim now describes the short names shipped in 008.
  - The nota-docs follow-up goes in the PR.

- [X] T034 Run quickstart.md § 2 in full under Xvfb in all four themes. Make before/after contact sheets of the three device tabs (T002 vs final) for the PR.
- [X] T035 Nota closing checks:
  - the two-line SPDX header is on `Application/Palette/DeviceCategories.cs` and `Smoke/DesignTokenCheck.Browser.cs`;
  - no new dependency (`LICENSES/third-party.md` N/A);
  - no engine behaviour change (smoke engine coverage N/A).
  *Result*:
  - **T034**: the three device tabs, captured in all four themes plus Studio Light with folds, read correctly. Headers match the Built-in header style, and the caret sits in the device caret column. Contact sheets were sent in the session.
  - **T035**: SPDX headers are on the three new files. No dependency, native or project-format change.

- [X] T036 *(moved to T049, after the chips rework)* Final gate: a full `scripts/build-linux.sh`. Only the known "Consort stress" failure is allowed (it also fails on the base). Then run `/speckit-converge`.

---

## Dependencies & Execution Order

- **Setup (1)** → **Foundational (2)** blocks every story: categories, guard, settings and row fields.
- **US1 (3)** is the MVP and comes before US2, US3 and US4, which all build on the category branch in `RebuildVisible` (T010).
- **US2 (4)** and **US3 (5)** are independent of each other once US1 is done. Both touch `App/BrowserView.cs`, so run them one after the other (US2 first, it's smaller).
- **US4 (6)** depends only on US1 (T010) and T007. It can run alongside US2 and US3 except where `Smoke/Program.cs` is shared, which is sequential.
- **Polish (7)** comes last.

Within phases:
- T004 ∥ T006 (different files) after T003;
- T011 ∥ T009–T010;
- T028 ∥ T026–T027;
- T030 ∥ T031 ∥ T032 ∥ T033.

## Parallel Example: Phase 2

```text
T003 DeviceCategories.cs
  → T004 DesignTokenCheck.Browser.cs ∥ T006 Settings.cs
  → T005 wire RunBrowser → T007 BrowserItem fields + Rebuild → T008 build
```

## Implementation Strategy

1. **MVP**: Phases 1–3. Built-ins are categorised by default, guarded by `RunBrowser`. This is shippable alone.
2. Then US2 (off switch), US3 (folding) and US4 (plug-ins), one commit each.
3. Then docs, the screenshot run and converge.

## Notes

- **The flat path is frozen.** T010 must not change today's branch of `RebuildVisible`, and T017(a) proves it.
- **Device rows are shared objects** between the two views. Never copy a `BrowserItem` device to place it under a header, or drag, favourite and expand state would split.
- **Header names come from `DeviceCategories` only.** Never retype a category string in the view or tests: use `DeviceCategories.Order` and `For`.

---

## Phase 8: Rework — category chips instead of headers (research R9)

**Why**: after seeing the headers build (US1 and US3 above), the user chose Ableton-style
chips. Spec FR-002–FR-006, US1–US3, the plan summary, the data model and the contract are
revised. The category data, `RunBrowser`, the plug-in mapping and the test fakes stay.
The header branch, folding and their checks are removed.

**Independent test**: each device tab shows its chips. A chip lists exactly its category,
and with no chip the list is today's.

- [X] T037 In `VM`, remove the headers build:
  - `CategoryView`, the `RebuildCategoryView` branch and its call;
  - the fold state (`_folded`, `SetFoldedCategories`, `FoldedCategories`, `CategoryFoldsChanged`, `ToggleCategoryFold`, `FoldedDeviceCount`, `_foldedDevices`) and the folded counts in `VisibleDeviceCounts`, which goes back to as before;
  - `BrowserItem.IsFoldable` / `IsFolded`.

  Keep `DeviceCategory`, `TabKey` and `TypeOf`.
- [X] T038 In `VM`, add the category filter (data-model § Category filter):
  - `string[] _categoryFilter = { "", "", "" }`;
  - `CategoryFilter(int tab)`;
  - `SetCategoryFilter(int tab, string name)`, which rebuilds that tab;
  - `CategoriesOn(int tab)`: `Order(TypeOf(tab))`, plus `Other` when any tree node's shelf is `Other`;
  - in `RebuildVisible`'s device loop, `if (!PassesCategoryFilter(tab, node)) continue;` beside `PassesLibraryFilter`. The shelf is `DeviceCategory`, normalised to `Other` when the tab doesn't list it.
- [X] T039 [P] In `Application/Settings.cs`, replace `BrowserCategoryView` and `BrowserFoldedCategories` with `public bool BrowserCategoryChips { get; set; } = true;` (doc: "Show the category chips above the device lists (Synths, Dynamics…)").
- [X] T040 In `App/BrowserView.cs`, remove the headers build:
  - restore `GroupRow` (no caret, no click, the original margins and columns);
  - restore `VisibleCount` without folded devices;
  - remove the fold load/save wiring, `CategoryView` application, and the "Group by category" ⋮ item.
- [X] T041 In `App/BrowserView.cs`, add the chip row:
  - a `WrapPanel _categoryChips` in a host `Border` (same margins as `_chipsHost`), placed in `topStack` between `headerRow` and `_chipsHost`;
  - `RebuildCategoryChips()` makes one `MakeChip(name, null, active, …)` per `_vm.CategoriesOn(_active)`; a click toggles `_vm.SetCategoryFilter(_active, active ? "" : name)`, then rebuilds the chips and calls `RefreshCounts()`;
  - shown only on tabs 0–2 while `BrowserCategoryChips` is on; rebuilt on tab change and on `DevicesRebuilt`;
  - a tooltip "<n> devices" per chip if cheap, else none.

  The chip spacing matches the Files tab's `_sampleChips`.
- [X] T042 In `App/BrowserView.cs`, add `CheckItem("Category chips", …)` in ⋮ after "Show type tags". It saves `BrowserCategoryChips`; turning it off calls `SetCategoryFilter(t, "")` for tabs 0–2, then updates the row's visibility and `RefreshCounts()`. Apply the setting in `SetSettings` / `SetViewModel`. `EmptyMessage`'s `filtering` also counts a picked category.
- [X] T043 Rewrite `Smoke/BrowserCategoryTests.cs` for chips:
  - **(a)** No pick: each tab equals today's list (`BUILT-IN` plus built-ins by `Name`).
  - **(b)** `CategoriesOn` equals `Order` on each tab (no plug-ins means no Other).
  - **(c)** For every category, picking it lists exactly its built-ins (`DeviceCategories.Of(type)`), and the union over the chips covers every device once.
  - **(d)** Clearing the pick restores (a).
  - **(e)** Picks are per tab.
  - **(f)** Dynamics plus the search "Prism" gives only Prism; Dynamics plus Favorites with Compressor favourited gives only Compressor.
  - **(g)** Expanding a device while picked shows its presets beneath it.
  - **(h)** The `ForPlugin` table.
  - **(i)** With `FakePluginCatalog`: Reverb & delay gives `BUILT-IN`, Chamber, Delay, Reverb, `PLUG-INS`, Ambience; `CategoriesOn(1)` ends with Other, which lists Mystery; Synths includes Ana; the MIDI tab has no Other.
  - **(j)** `Deserialize<Settings>("{}").BrowserCategoryChips == true`.

  Restore the `Program.cs` Glacier assertion to `BUILT-IN, Nota Aurora, Pads, Glacier Pad`.
- [X] T044 Build. Run `--browser` and the design checks (`check.sh`): all pass, `RunLabels` included (the new chip and ⋮ strings).
- [X] T045 Under Xvfb, run quickstart § 2 steps 1–6:
  - pick and clear a chip;
  - switch chips;
  - a chip plus search and Favorites;
  - the pick is kept per tab;
  - ⋮ off clears and hides it and survives a relaunch;
  - the minimum width wraps.

  Compare the no-pick list with the T002 baseline (it must be pixel-identical below the chip row).
- [X] T046 Capture the three tabs in all four themes, with and without a chip picked, and make before/after contact sheets for the PR (quickstart § 2 step 7).
  *Result*:
  - **Checks**: `--browser` 29/29; design checks 152/152 (`RunLabels` included). `BrowserView.cs` was restored to its pre-headers state and the chip row added fresh.
  - **Under Xvfb**:
    - Audio effects shows 8 chips in three wrapped rows above Favorites;
    - Reverb & delay narrows to Chamber, Delay and Reverb;
    - Dynamics switches the list, and with "prism" it shows only Prism;
    - Instruments is unfiltered while Audio effects keeps Dynamics.
  - **⋮ off**: clears the pick and hides the row. After a relaunch, the list matches the T002 baseline pixel for pixel (AE 0).
  - **Minimum width**: one chip per line, no overlap.
  - **Captures**: four themes, unpicked and picked. Contact sheets were sent in the session.

- [X] T047 [P] Docs:
  - `DESIGN.md`: replace the "Browser categories" bullet with the chip row (its own wrapping row above Favorites/tags, `MakeChip`, one pick per tab, Other last when used);
  - `FEATURES.md`: replace the "Device categories" bullet and the View options wording ("category chips");
  - `CHANGELOG.md`: rewrite the Added entry for chips.
- [X] T048 [P] Re-check `.claude/skills/nota-instrument/SKILL.md`. The category step stays as written: it names `DeviceCategories.cs` and the browser check, not headers.
- [X] T049 Final gate: a full `scripts/build-linux.sh`. Only the known "Consort stress" failure is allowed. Then run `/speckit-converge`.

  *Result*: 3265 passed and 1 failed. The failure is "Consort stress" (2.23 ms against a 2.90 ms budget), which also fails on the base (T001). Results:
  - `-- design: browser --` all pass;
  - `-- browser: device categories --` 29/29;
  - the restored search assertion passes (`BUILT-IN / Nota Aurora / Pads / Glacier Pad`).


## Phase 9: Convergence

- [X] T050 Reword `specs/013-browser-categories/plan.md` § "Nota: Read Before Planning" (line 44): the chips reuse `MakeChip` (the filter-chip style), not `GroupLabel` and the disclosure caret, per plan: R9 chips decision (contradicts)
- [X] T051 Update the `DESIGN.md` Filter chips row (line 466): the Favorites / tags strip overflows into `+N`, and the device tabs' category row wraps so every category stays in sight, per FR-008 (partial)
