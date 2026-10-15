# Research: Device categories in the browser

**Feature**: [spec.md](spec.md) · **Plan**: [plan.md](plan.md) · **Date**: 2026-10-14

All the code facts below come from the current tree (branch `013-browser-categories`, on top
of 012). There are no NEEDS CLARIFICATION items left: the five spec decisions settled
the questions about how the feature behaves, and the rest is design.

## What exists today

- `Nota.Presentation/ViewModels/BrowserViewModel.cs`:
  - holds one tree per device tab (`_instrTree`, `_fxTree`, `_midiTree`);
  - flattens each tree into a visible collection (`Instruments`, `Effects`,
    `MidiEffects`) in `RebuildVisible(tab)`, which:
    - applies the search query, the favourite/tag chip, "Favorites first" (a stable
      `OrderByDescending`) and "Group by source" (`OrderBy(IsPlugin)`);
    - emits `BrowserItemKind.Group` rows captioned `BUILT-IN` / `PLUG-INS` with a
      `GroupCount`.
- Built-ins come from `Nota.Application/Palette/BuiltinDeviceCatalog.cs`:
  - the tree is sorted by `Name`;
  - plug-ins are appended in catalogue order, unsorted;
  - each plug-in already carries its reported category in `BrowserItem.Category`. It comes
    from `pluginhost` as JUCE's `PluginDescription::category`, with `|` replaced by `/`:
    VST3 gives `Fx/Reverb`, AU gives JUCE's type name (for example `Effect`, `Synth`).
- `Nota.App/BrowserView.cs`:
  - `GroupRow` draws a header: a `GroupLabel` caption through `NotaText.Label`, a hairline
    and a mono count, 22 px high (`GroupRowH`);
  - group rows can't be selected (`SelectionChanged` clears them);
  - `ShowOptionsMenu` holds the ⋮ check items, "Group by source" among them, and persists
    them through `ISettingsService`;
  - `StatusFor` counts devices from `VisibleDeviceCounts`, which counts collection rows.
- `Nota.Application/Settings.cs` has `BrowserGroupBySource`, `BrowserFavoritesFirst` and
  `BrowserShowTypeTags` (all default true). It already persists lists (`RecentProjects`,
  `PaletteRecent`).
- The smoke test (`tests/Nota.SmokeTest/Program.cs`, around line 14490) builds a
  `BrowserViewModel` over `EmptyPluginCatalog` and asserts the row sequence
  `BUILT-IN, Nota Aurora, Pads, Glacier Pad` for a search.

## R1: Where categories are defined

**Decision**: a new pure class, `Nota.Application/Palette/DeviceCategories.cs`, sitting
next to `BuiltinDeviceCatalog`. It holds:

- for each tab (Instrument / AudioEffect / MidiEffect), the ordered category list, each
  category with its built-in device names, written exactly as the spec's Assumptions list
  them;
- `Other`, a constant that always sorts last;
- `For(BuiltinDevice)`, which returns the device's category, or `Other` if none;
- `ForPlugin(bool instrument, string reported)`, the plug-in mapping (R2);
- `Order(type)`, which returns the category names in tab order.

**Rationale**:

- One block that reads like the spec makes membership easy to review and to change.
- It keeps the catalogue's semantic descriptors (which the palette reads) separate from the
  browser's shelving.
- Pure code can be tested from the smoke test without a UI.

**Alternatives rejected**:

- A `Category` field on `BuiltinDevice`: this touches 50 constructor lines and scatters one
  category's members across the file.
- Deriving the category from `SemanticDescriptor.Role` (`synth.subtractive`…): roles are
  finer-grained and don't match the confirmed groups (Prism's role is dynamics, but the
  palette may change roles for search reasons).

## R2: Mapping plug-in categories

**Decision**: split the reported string on `/` and `|`, then trim and lower-case each
token. The first token in the tab's table wins, and an unrecognised or empty string maps to
`Other`. Tables:

| Tab | Reported token(s) | Category |
|---|---|---|
| Instruments | `synth` | Synths |
| | `piano` | Keys & physical |
| | `drum` | Drums |
| | `sampler` | Samplers |
| Audio effects | `eq`, `filter` | EQ & filter |
| | `dynamics`, `mastering` | Dynamics |
| | `reverb`, `delay` | Reverb & delay |
| | `modulation` | Modulation |
| | `distortion` | Distortion |
| | `pitch shift` | Pitch & time |
| | `analyzer`, `tools`, `spatial`, `surround`, `restoration`, `up-downmix` | Utility |

Tokens that are deliberately skipped:

- markers: `fx`, `instrument`, `mono`, `stereo`;
- `generator`, `external`, `network`;
- AU type names such as `effect` and `music effect`.

A plug-in whose tokens are all skipped lands in `Other`.

**Rationale**:

- VST3's sub-category vocabulary (`Steinberg::Vst::PlugType`) is fixed and small.
- Taking the first recognised token handles "Fx|Delay|Reverb" (spec edge case:
  listed once, under the first recognised category).
- Case-insensitive tokens also catch CLAP-style feature words (`reverb`, `delay`) if a
  host format reports them.

**Known limitation, recorded and not solved**:

- AU plug-ins report only their component type (`Effect`), so an AU-only effect lands in
  `Other`. AU instruments reporting `Synth` still map to Synths.
- Most AU plug-ins also ship as VST3, and that copy maps correctly.
- Guessing a category from the plug-in's name is out of scope (FR-005 says "mapped from
  the category they report").

## R3: Ordering inside the category view *(superseded by R9)*

**Decision**: the category view is a new branch in `RebuildVisible`. Today's flat path stays
as it is, so SC-003 ("restores today's flat list exactly") holds by construction. In the
category branch the visible devices are ordered by these keys, in priority order:

1. category position (`Other` last);
2. when "Group by source" is on: built-in before plug-in;
3. when "Favorites first" is on: favourite before the rest;
4. `ShortName` (case-insensitive, invariant). Built-ins sort without their "Nota "
   prefix, so plug-ins interleave correctly when "Group by source" is off.

Each category with at least one visible device gets a header carrying that count. With
"Group by source" on there are no separate Built-in / Plug-ins headers (FR-005).

**Rationale**:

- It keeps the meaning favourites have today: they float to the top of their source run,
  and the source run sits inside the category.
- A plain multi-key `OrderBy`/`ThenBy` over at most a few hundred rows is cheap.

**Alternative rejected**: favourites first across sources within a category. That breaks
the FR-005 rule that built-ins come before plug-ins.

## R4: Category headers and folding *(superseded by R9)*

**Decision**:

- **Header row**: a category header is a `BrowserItemKind.Group` row with `Path =
  "cat:<tab>:<name>"` and a new `IsFoldable = true`. `GroupRow` adds a caret in front of
  the caption: `RailIcon(CaretRight|CaretDown, 12, "Brush.TextTertiary")`, the same glyph
  and size device rows use for disclosure. Otherwise the row is unchanged: `GroupLabel`
  caption, hairline, mono count, 22 px. It gets a hand cursor, and a `PointerPressed` on
  the row toggles the fold.
- **Fold state**: the view model holds `HashSet<string>` keys `instr:<name>`,
  `fx:<name>`, `midi:<name>`. `ToggleCategoryFold(item)` flips a key, rebuilds that tab and
  raises `CategoryFoldsChanged`. `BrowserView` persists on that event (same pattern as the
  ⋮ toggles).
- **While searching**: a folded category's devices are hidden, and its header still shows
  the count. While a search query is active, folds are ignored: every matching category
  shows open, the caret is hidden, and clicks on the header do nothing. Clearing the query
  brings back the saved folds untouched (US3/AC2).
- **Accessibility**: `AutomationProperties.Name` is "<category>, <n> devices,
  folded|open".

**Rationale**:

- This reuses the existing header style (FR-008).
- Ignoring folds during a search keeps the spec's "search shows matches inside folded
  categories without changing the saved fold state" simple to reason about and to test.

**Alternatives rejected**:

- An expander control: a new control style, and DESIGN.md asks for reuse.
- Foldable `Folder` items: device rows would gain a depth and indent, which changes every
  row's look.

## R5: Settings and defaults *(revised by R9)*

**Decision**: two properties are added to `Settings`:

- `BrowserCategoryView` (bool, default `true`);
- `BrowserFoldedCategories` (`List<string>`, default empty).

How they behave:

- Missing keys in an existing `settings.json` take the initializer value, so existing users
  get the category view on (FR-003).
- The ⋮ menu gains "Group by category" as the first of the grouping toggles, above "Group
  by source". The name parallels the existing toggle, and its sentence case passes
  `RunLabels`.
- `BrowserViewModel.CategoryView` defaults to `true` as well, so a view model built without
  settings (the smoke test) matches a fresh profile.

## R6: Status-line counts *(superseded by R9)*

**Decision**: `VisibleDeviceCounts` counts the devices that passed search and filter
*before* folding, so folding a category doesn't change "26 audio effects · 26 built-in".
The view model records these counts during `RebuildVisible`.

**Rationale**: folding is a view choice, not a filter. The status line answers "what
matches", as today.

## R7: Guarding built-in membership (FR-009)

**Decision**: a new `DesignTokenCheck.RunBrowser()` section
(`tests/Nota.SmokeTest/DesignTokenCheck.Browser.cs`), wired into `Program.cs` next to the
other design sections. It asserts:

- every `BuiltinDeviceCatalog.All` entry is in exactly one category of its own tab (no
  `Other`);
- every name in `DeviceCategories` exists in the catalogue with the matching type (a stale
  entry fails, ratchet style);
- no category is empty of built-ins;
- the category names are sentence case per `NotaText.Label`, which returns them
  unchanged;
- the counts are 18 / 26 / 6 (SC-001).

Behavioural checks go in `Program.cs` next to the existing browser block:

- category order per tab;
- folding, and search inside a folded category;
- favourites within a category;
- the "Group by source" ordering with a fake plug-in catalogue;
- `Other`;
- the flat view equals today's sequence.

`BrowserTestSupport.cs` gains a `FakePluginCatalog` (descriptions in the
`Name | Format | inst|fx | Vendor | Category` form).

## R8: Documentation touch-points

- `CHANGELOG.md` `[Unreleased]`: one entry.
- `FEATURES.md` via `nota-features`: the browser section.
- The user manual's browser page via `nota-docs`. That manual lives in the sibling repo
  `nota-daw/nota-docs`, which isn't available in this session, so it is noted as a
  follow-up in the PR (as in 012).
- `DESIGN.md`, the browser bullet in the layout section (around line 651): category headers are foldable group headers with a caret, and
  "Other" comes last.
- The `.claude/skills/nota-instrument/SKILL.md` registration checklist: "add the device to
  its category in `DeviceCategories`" (the check fails otherwise).
- No shortcut changes, so `nota-shortcuts` is not needed.

## R9: Category chips instead of headers (revision)

The headers version (R3–R6) was built and shown. The user then chose Ableton-style chips
instead: a chip narrows the list to one category, and there are no headers or folding.

**Decision**:

- **Filter, not a layout**: the category is one more filter in `RebuildVisible`'s existing
  device loop, next to `PassesLibraryFilter`. The view model holds `_categoryFilter[3]`
  (one category name per device tab, `""` = none) and exposes:
  - `CategoryFilter(tab)` and `SetCategoryFilter(tab, name)`;
  - `CategoriesOn(tab)`: the tab's fixed order, plus `Other` when any device on the tab
    maps there.

  `BrowserItem.DeviceCategory` (R1/R2) stays. A device's shelf is its `DeviceCategory`,
  normalised to `Other` when its tab doesn't list that name. With no chip picked, the
  loop is exactly today's, so SC-003 holds by construction. "Group by source" and
  "Favorites first" order the narrowed list as before, so no ordering code is needed.
- **Chips get their own row**: today's Favorites/tags row is a single-line `ChipStrip`
  whose extra chips go into a "+N" menu, which would hide most of the 8 effect categories
  at the default width. The category chips therefore get their own `WrapPanel` row (like
  the Files tab's sample chips), between the search row and the Favorites/tags row. They
  are shown on the three device tabs while "Category chips" is on, and use the same
  `MakeChip` (20 tall pill, accent wash when engaged).
- **Pick behaviour**: one chip at a time per tab. A click picks it, a click on the picked
  chip clears it, and a click on another switches. The pick survives tab switches but not
  a restart, so the list never opens mysteriously narrowed.
- **Settings**:
  - `BrowserCategoryView` and `BrowserFoldedCategories` (both unreleased) are replaced by
    `BrowserCategoryChips` (bool, default `true`);
  - ⋮ gains "Category chips" where "Group by category" sat;
  - turning it off clears all three tabs' picks.
- **Empty state**: a picked chip counts as filtering, so an empty result says "No
  matches", not "No audio effects yet".
- **Removed with the headers**: the category branch, folds, folded counts, the header
  caret and click, and their tests.

**Alternatives rejected**:

- Category chips in the existing single-line strip: most would sit behind "+N".
- Several chips at once (OR): fine later; one-at-a-time matches the Favorites/tag chips
  and keeps the narrowed list's meaning obvious.
- Remembering the pick across restarts: a hidden narrowing on launch reads as missing
  devices.
