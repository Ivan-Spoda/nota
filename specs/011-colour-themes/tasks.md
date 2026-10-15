---

description: "Task list for selectable colour themes"
---

# Tasks: Selectable Colour Themes

**Input**: Design documents from `/specs/011-colour-themes/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/theme-file.md, contracts/ui-contract.md, quickstart.md

**Tests**: The spec requires automated checks (FR-015, FR-016, US5/AC2), so the smoke-test
tasks are part of the work, not optional. They extend the existing gate
(`tests/Nota.SmokeTest`); no new test project.

**Organization**: grouped by user story. Paths under `src/managed/Nota.App/` are written
`App/`, `src/managed/Nota.Application/` as `Application/`, and `tests/Nota.SmokeTest/` as
`Smoke/`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story the task belongs to (US1–US5)

---

## Phase 1: Setup

**Purpose**: A working build and a Studio reference to diff against.

- [X] T001 Build `011-colour-themes` with `scripts/build-linux.sh` (PulseAudio null sink, Xvfb) and record the smoke-test result (same two environmental failures as 010 expected: "Consort stress", palette keystroke timing) in `/tmp` (not committed)
- [X] T002 Capture the Studio reference screenshot set (arrangement, mixer, session, a device card, browser, a context menu, Preferences → Appearance, command palette, export dialog) in Dark and Light under Xvfb, kept outside the repo, for the SC-003 pixel diff

---

## Phase 2: Foundational (blocking)

**Purpose**: Colour gets one home (research R1–R3, R6). After this phase Studio must
render pixel-identical to T002, and nothing user-visible changes yet.

- [X] T003 Write a scratch generator (outside the repo, `/tmp/claude-0/scripts/themes-gen.py`) that emits `App/Theme/Themes/studio-dark.json` and `studio-light.json` from the current `T("#dark","#light")` pairs in `App/Theme/NotaPalette.cs` (85 named slots plus `Track1..9`, `ReturnA`, `ReturnB`), the `Shadow.*` colours and the `SystemAccentColor*` ramp in `App/Theme/NotaTheme.axaml`, as tokens `ShadowBevel`, `ShadowPopover`, `ShadowModal`, `SystemAccent`, `SystemAccentLight1..3` and `SystemAccentDark1..3`. Shape per contracts/theme-file.md: `formatVersion` 1, `id`, `name`, `kind`, `colors` in catalogue order; 106 tokens
- [X] T004 Generate and commit `App/Theme/Themes/studio-dark.json` and `App/Theme/Themes/studio-light.json` with T003, and add `<EmbeddedResource Include="Theme/Themes/*.json" />` to `App/Nota.App.csproj`
- [X] T005 Create `App/Theme/NotaColourTheme.cs` (SPDX header). It holds:
  - `sealed record NotaColourTheme(string Id, string Name, NotaThemeKind Kind, IReadOnlyDictionary<string, Color> Colors)`;
  - `enum NotaThemeKind { Dark, Light }`;
  - `NotaThemes.BuiltIn`, the ordered list loaded from the embedded JSON via `System.Text.Json`;
  - validation of F1–F6. A failing file is left out and logged, never thrown to the UI;
  - `NotaThemes.Find(id)` and `NotaThemes.OfKind(kind)`.
- [X] T006 Rework `App/Theme/NotaPalette.cs` to declare every slot by token name:
  - `T(nameof(BgApp))`, plus `"Track1".."Track9"`, `"ReturnA"`, `"ReturnB"`; keep every field name, its brush identity and the role comments.
  - Replace `Apply(NotaThemeVariant)` with `Apply(NotaColourTheme)`. It sets each slot from `theme.Colors[name]`, then re-runs washes, gradients, track/return arrays and derivations as today.
  - Replace `NotaThemeVariant` with `NotaThemeKind` throughout, including `Ink()`: dark kind uses the hex, light kind uses the derived or pinned value.
  - Add `ColorIn(SolidColorBrush slot, NotaColourTheme theme)`.
  - Start in `studio-dark`.
- [X] T007 Extend `KeyMap` in `App/Theme/NotaPalette.cs` to every `Brush.*` key the XAML uses (research R6):
  - add the text inks, `Track1..9`, `ReturnA/B`, `Master` and the aliases `ChromeBg`, `Hairline` and `SurfaceSelected`;
  - add `SurfaceHoverClear = Wash(SurfaceHover, 0x00)` and `DangerSubtle = Wash(Danger, 0x33)`;
  - expose `NotaPalette.AllKeys` for publishing.
- [X] T008 In `App/Theme/NotaThemeService.cs`, add `Publish()`, called once at startup: put each `KeyMap` brush into `Application.Current.Resources` under its `Brush.*` key. Add `ApplyStructural(theme)`, which on every switch sets:
  - `Shadow.Raised` (inset bevel `ShadowBevel`);
  - `Shadow.Sunken` (inset ring `Rule`);
  - `Shadow.Popover` and `Shadow.Modal` (ring `Seam` + drop `ShadowPopover`/`ShadowModal`, the geometry of data-model 010 § Elevation);
  - the seven `SystemAccentColor*` `Color` resources.
- [X] T009 Remove every `SolidColorBrush`, `Color` and `BoxShadows` entry from the `Dark` and `Light` `ThemeDictionaries` in `App/Theme/NotaTheme.axaml`. Drop the empty dictionaries. Rewrite the header comment: colour lives in `Theme/Themes/*.json`, slot brushes are published by `NotaThemeService`, and views keep using `{DynamicResource Brush.*}`. Then grep the `.axaml` files for any `DynamicResource` key that no longer resolves (`Color` vs brush) and fix it.
- [X] T010 Wire startup in `App/App.axaml.cs`: `NotaThemeService.Publish()`, then apply `studio-dark` or `studio-light` from the legacy `Settings.Theme` (temporary until US1/US2), before the first window as today.
- [X] T011 Update `Smoke/DesignTokenCheck.cs` `Run`. Replace the "XAML key exists in both dictionaries / slot equals XAML pair" checks with two checks: every `Brush.*` key referenced in `App/**/*.axaml|*.cs` is a `KeyMap` key, and every `T(nameof(X))` slot name is a catalogue token. Keep `RunStudio`, pointed at the Studio theme files.
- [X] T012 One-time parity proof, scratch and not a permanent check: have the T003 generator diff `studio-*.json` against the 010 pairs read with `git show 010-studio-visual-style:…`. Expect 0 differences in 106 tokens × 2. Record the result in the PR description.
- [X] T013 Build and run the smoke test (`/tmp/claude-0/scripts/check.sh`). Retake the T002 set and pixel-diff it against T002 with ImageMagick `compare -metric AE`. Expect 0 differing pixels in all shots (SC-003). Fix any drift before going on.

**Checkpoint**: one source of colour; Studio unchanged; checks green.

---

## Phase 3: User Story 3 — Built-in themes that hold up everywhere (Priority: P1)

**Goal**: the theme checks exist before more themes do, so every later theme lands
against the bar (FR-014, FR-015, contracts/ui-contract.md § T3).

**Independent Test**: `-- design: themes --` passes for both Studio themes. A scratch
copy with a token removed or a pairing broken fails and names the theme and token.

- [X] T014 [US3] Add `RunThemes()` to `Smoke/DesignTokenCheck.cs`. For each `App/Theme/Themes/*.json` it checks:
  - F1–F6 of contracts/theme-file.md, with the catalogue derived from `NotaPalette.cs` plus the elevation and `SystemAccent*` tokens;
  - the T3.1–T3.8 pairings: WCAG contrast; ΔE76 in Lab; derived ink = `ReadableInkOn`'s rule, dark `#17181A` or white;
  - F8 (no Ember-era value) for `studio-*` only, reusing `RunStudio`'s Ember list.

  Failures name theme, token or pairing, and the measured value.
- [X] T015 [US3] Add the `-- design: themes --` section to `Smoke/Program.cs`, calling `RunThemes()`
- [X] T016 [US3] Prove the negative path: a scratch run with a token deleted from a temporary copy of `studio-light.json`, and one with `TextMuted` set to the ground colour. Each must fail and name the theme and token. Revert, and record the output in the PR description, not the repo.
- [X] T017 [US3] Confirm both Studio themes pass T3 unchanged (research R7 floors). If one doesn't, the floor table is wrong, not Studio: correct contracts/ui-contract.md § T3 and research.md R7 with the measured value and a note.

**Checkpoint**: quality bar enforced for any theme.

---

## Phase 4: User Story 1 — Pick a colour theme (Priority: P1) 🎯 MVP

**Goal**: Preferences lists themes. Picking one recolours everything live and survives a
restart. Old settings migrate.

**Independent Test**: quickstart.md §2 and §5. Requires a second theme to pick: until
US4 lands, use Studio Dark ↔ Studio Light, which already shows the full path.

- [X] T018 [P] [US1] Add `ColourTheme` (string, default `""`), `SystemDarkTheme` (default `"studio-dark"`) and `SystemLightTheme` (default `"studio-light"`) to `Application/Settings.cs` with doc comments. Keep `Theme` (`AppTheme`) and document it as legacy: written in step, read only for migration.
- [X] T019 [P] [US1] Create `Application/Appearance.cs` (SPDX header), a pure resolver per data-model.md § Resolution:
  - `ThemeIds` constants (`StudioDark`, `StudioLight`, `EmberGraphite`, `EmberPaper`, `System = "system"`);
  - `record AppearanceChoice(bool FollowSystem, string Single, string Dark, string Light)`;
  - `Appearance.Resolve(Settings, IReadOnlyDictionary<string, bool /*isDark*/> known)`, which migrates when `ColourTheme == ""` and falls back for unknown or wrong-kind ids;
  - `Appearance.Store(Settings, AppearanceChoice, bool activeIsDark)`, which also writes the legacy `Theme` in step.
- [X] T020 [US1] Add a `-- design: appearance --` section to `Smoke/Program.cs` covering every row of data-model.md § Resolution:
  - legacy Dark, Light and System;
  - an empty file;
  - an unknown single id;
  - an unknown dark-mode id and a wrong-kind light-mode id;
  - a round trip through `Store`, checking that the legacy `Theme` is written.
- [X] T021 [US1] Rework `App/Theme/NotaThemeService.cs`:
  - `Set(AppearanceChoice)` resolves the active theme, sets `RequestedThemeVariant` from its kind, calls `NotaPalette.Apply(theme)` and `ApplyStructural(theme)`, then repaints;
  - `Apply(themeId)` does the single-theme path;
  - keep `RepaintEverything`, and extend the walk to open popup roots if T025 shows a stale popup.
- [X] T022 [US1] Startup in `App/App.axaml.cs`: resolve with `Appearance.Resolve(settings, NotaThemes.BuiltIn → kinds)` and call `NotaThemeService.Set(...)` before the first window. This replaces T010's temporary call.
- [X] T023 [US1] Replace `ThemePicker()` and `ThemeSwatch` in `App/PreferencesWindow.cs` with a **Colour theme** list per contracts/ui-contract.md T1.1, T1.2, T1.5 and T1.6:
  - one 26 px row per `NotaThemes.BuiltIn` entry: a preview chip of four 10×10 squares (`BgApp`, `Panel`, `TextPrimary`, `Accent` read via `ColorIn(slot, theme)`), the name, and the kind in `TextMuted`;
  - Studio's selected-row treatment for the active row;
  - on click, `Appearance.Store`, save, then `NotaThemeService.Set`.
- [X] T024 [US1] Update the Appearance section's label and help text in `App/PreferencesWindow.cs` to sentence case, per Studio: "Colour theme", plus a one-line hint.
- [X] T025 [US1] Verify US1 per quickstart.md §2 and §5 under Xvfb:
  - switch Studio Dark ↔ Studio Light with a context menu and a tooltip open, and during playback;
  - relaunch, and confirm no first frame in the wrong theme by screenshotting at the earliest window map;
  - migrate from `{"Theme": 1}` and from `{"Theme": 2}`.

**Checkpoint**: MVP: themes selectable and persisted (with the two Studio themes).

---

## Phase 5: User Story 2 — Follow the system with a theme for each mode (Priority: P1)

**Goal**: follow system with a chosen theme per OS mode (FR-004, FR-005).

**Independent Test**: quickstart.md §3.

- [X] T026 [US2] In `App/Theme/NotaThemeService.cs`, follow mode sets `RequestedThemeVariant = Default`. On `ActualThemeVariantChanged` it applies `choice.Dark` or `choice.Light`. When there is no OS signal, Avalonia reports Light and the light-mode theme applies.
- [X] T027 [US2] Add a **Follow system** row under the list in `App/PreferencesWindow.cs` (contracts/ui-contract.md T1.3–T1.4):
  - same selected treatment as the list rows; no list row is selected while it is on;
  - while on, it shows **In dark mode** and **In light mode** drop-downs listing `NotaThemes.OfKind(Dark/Light)` with the preview chip;
  - picking a list row turns follow off and keeps both per-mode ids.
- [X] T028 [US2] Verify US2 per quickstart.md §3: *(Verified on macOS by the user, 2026-10-13: Follow system switches immediately when the OS appearance flips.)*
  - flip the variant by forcing `ActualThemeVariant` via the platform setting available under Xvfb, or a debug hook if none, and record which was used;
  - relaunch in follow mode;
  - the per-mode ids survive switching to a single theme and back.

**Checkpoint**: Dark / Light / System parity with today, plus per-mode choice.

---

## Phase 6: User Story 4 — Ember comes back as a colour set (Priority: P2)

**Goal**: Ember Graphite and Ember Paper, faithful to 0.52.1 except for documented
readability nudges (FR-013, contracts/ui-contract.md T4).

**Independent Test**: both pass `-- design: themes --`. Screenshots overlay the Studio
shots of the same kind with identical geometry (SC-004).

- [X] T029 [US4] Extend the scratch generator (T003) to emit `App/Theme/Themes/ember-graphite.json` and `ember-paper.json`:
  - every token present in 0.52.1 is taken by slot name from `git show f37f51e:src/managed/Nota.App/Theme/NotaPalette.cs` (and `NotaTheme.axaml` for `SystemAccent*` and the `Shadow.Raised` bevel);
  - Studio-only tokens are derived per research.md R8;
  - the `SystemAccent*` ramp is computed by the R3 rule.
- [X] T030 [US4] Run `RunThemes()` on the generated Ember files and list every T3 failure. Then nudge each failing token per research R8 (same hue, HSL lightness in 1 % steps, smallest step that passes) in the generator, not by hand. Iterate until both pass, then commit the two JSON files.
- [X] T031 [US4] Have the generator also print the nudge table: theme, token, 0.52.1 value → shipped value, failing pairing and ratio before → after. Keep it for T035.
- [X] T032 [US4] Screenshot the T002 set in Ember Graphite and Ember Paper. Overlay each on the Studio shot of the same kind, comparing edges: `compare` on an edge-detected pair, or a visual check of a 50 % blend. Confirm no element moved or resized (SC-004). Check that mute, solo and arm, meters and track colours read correctly.

**Checkpoint**: four built-in themes, all passing.

---

## Phase 7: User Story 5 — Themes as data (Priority: P3)

**Goal**: adding a theme needs only its file and the built-in list (FR-012, SC-006).

**Independent Test**: quickstart.md §1 negative check, plus a throwaway fifth theme.

- [X] T033 [US5] Make the built-in list data-driven: `NotaThemes.BuiltIn` orders by a `BuiltInOrder` array of ids in `App/Theme/NotaColourTheme.cs`, the only line a new built-in adds besides its file. Confirm no view or control code names a theme id except `Appearance` defaults and that array, using a grep in `RunThemes`.
- [X] T034 [US5] Throwaway check, not committed: add a copy of `studio-dark.json` as `test-theme.json` with a new id and name, and confirm it appears in Preferences and applies. Remove one token and confirm the smoke test fails and names it. Revert, and record in the PR description.

---

## Phase 8: Polish & Cross-Cutting

- [X] T035 [P] DESIGN.md: add a § Colour themes section covering what a theme is, the token catalogue by group with roles, the four built-ins, follow system, how to add a theme (file + `BuiltInOrder`), the T3 readability table, and the R7 Studio Light finding. Include the Ember nudge table from T031. Update the token tables to say values live in `Theme/Themes/*.json`.
- [X] T036 [P] `scripts/design-html.py`: read the palette from `src/managed/Nota.App/Theme/Themes/*.json` instead of `NotaTheme.axaml`, and render one palette section per built-in theme. Regenerate `DESIGN.html`.
- [X] T037 [P] `.claude/skills/nota-design/SKILL.md`: update where colour lives (theme files, not XAML), how to add a token (`NotaPalette` slot + every theme file + `KeyMap` if XAML uses it), and how to add a theme.
- [X] T038 [P] `CHANGELOG.md` `[Unreleased]` → Added: colour themes (Studio Dark/Light, Ember Graphite/Paper) and follow system with a theme per mode. Changed: the Appearance setting replaces Dark / Light / System, and existing settings carry over.
- [X] T039 [P] Run the nota-features skill to update `FEATURES.md` (Preferences → Appearance: colour themes, follow system)
- [ ] T040 nota-docs: the docs repo (`../nota-docs`) isn't available in this environment, the same as 010 T069. Note the affected page (Preferences → Appearance) in the PR description for a follow-up.
- [X] T041 Final gate: full `scripts/build-linux.sh`. Only the two known environmental failures are allowed, and both must be shown to fail identically on the base branch. Retake the SC-003 Studio diff (0 px). Then run `/speckit-converge`.

---

## Dependencies & Execution Order

- **Setup (1)** → **Foundational (2)** blocks everything.
- **US3 (3)** comes next, so every later theme lands against the checks.
- **US1 (4)** depends on Foundational and US3. It is the MVP.
- **US2 (5)** depends on US1 (service + Preferences).
- **US4 (6)** depends on US3 (checks) and Foundational (format). It can run in parallel with US1 and US2, since it touches only the generator and the JSON files.
- **US5 (7)** depends on US1 (list in Preferences).
- **Polish (8)** after US4. T035 needs T031's table.

Within phases:
- T018 ∥ T019 (different files).
- T035–T039 are mutually parallel.
- Everything touching `NotaPalette.cs`, `NotaThemeService.cs` or `PreferencesWindow.cs` is sequential.

## Parallel Example: after Phase 3

```text
Stream A (US1 → US2): T018 ∥ T019 → T020 → T021 → T022 → T023 → T024 → T025 → T026 → T027 → T028
Stream B (US4):        T029 → T030 → T031 → T032
```

## Implementation Strategy

1. **MVP**: Phases 1–4 give themes in Preferences with live switching, persistence and migration, using the two Studio themes. Studio is pixel-identical.
2. **Parity**: Phase 5 restores System, now with per-mode choice.
3. **The point of it**: Phase 6 brings Ember back.
4. **Groundwork and docs**: Phases 7–8, then `/speckit-converge` before marking the PR ready.

## Notes

- The theme JSON files are generated once by a scratch script outside the repo, as in 006. After that the files are the source of truth. The Ember derivations (R8) and every nudge (DESIGN.md table) are documented, so any value can be traced without the script.
- Keep each commit buildable. Commit per phase checkpoint.

---

## Phase 9: Convergence

- [X] T042 Add a `RunThemes` guard that the Preferences theme chip draws each theme's own colours (`NotaPalette.ColorIn(slot, _theme)` in `App/PreferencesWindow.cs` `ThemeChip`), never the live slot colour, per FR-015 (partial)
- [ ] T043 With follow system on, open a context menu and a tooltip, then flip the OS appearance. Confirm the open popups recolour with the windows, on a desktop with an appearance signal (the macOS build), together with T028, per spec Edge Cases / contracts T2.1 (partial)

## Phase 10: Appearance page, mode first (revision)

The first picker showed themes as list rows with four 10 px squares; it read as a font
menu and hid Follow system's pairing behind drop-downs. Revised after review: mode first,
then cards.

- [X] T044 `Nota.Application/Appearance.cs`: `AppearanceMode`, `ModeOf`, `WithMode`, `WithPick` — the page's rules, pure, with `RunAppearance` checks (remembered per-mode pick, follow-system picks set one mode)
- [X] T045 `App/PreferencesWindow.cs`: replace the list and drop-downs with the Dark · Light · Follow system segmented control and a `ListBox.themes` row of cards per kind; `ThemePreview` draws a miniature window with `NotaPalette.ColorIn(slot, _theme)`; the label row is top-aligned (`TopRow`)
- [X] T046 `Theme/NotaTheme.axaml`: `ListBox.themes` (no list chrome) and `Border.ThemeFrame` (2 px, seam / rule / accent by state); `RunThemes` guard now reads `ThemePreview`
- [X] T047 Contract T1.1–T1.4, FR-001/FR-002, US1/US2 scenarios, CHANGELOG, FEATURES and DESIGN updated; screenshots in all four themes and in follow system
- [X] T048 No layout shift: both rows always show (labels change text and ink, the mode only decides which cards are chosen), and shell `ToggleButton.seg` keeps its weight when checked so keys never change width (DESIGN.md rule 11); measured identical in every mode

