---

description: "Task list for the Studio visual style restyle"
---

# Tasks: Studio Visual Style

**Input**: Design documents from `/specs/010-studio-visual-style/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-contract.md, quickstart.md

**Tests**: No new test suite was requested. The existing gate (`tests/Nota.SmokeTest`,
including `DesignTokenCheck`) is updated wherever a rule changes, so it keeps passing
and keeps enforcing the new system. Visual checks follow quickstart.md.

**Organization**: Grouped by user story. All UI paths are under
`src/managed/Nota.App/` (abbreviated `App/`).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US5)

---

## Phase 1: Setup

**Purpose**: Working build and a "before" reference.

- [X] T001 Initialise submodules (`git submodule update --init --recursive --depth 1`) and run `scripts/build-linux.sh` on unmodified `main` to record the baseline smoke-test result in `/tmp` (not committed)
- [X] T002 Capture "before" screenshots of the main window, a device card, the mixer and a context menu under Xvfb per specs/010-studio-visual-style/quickstart.md §2 (kept outside the repo, for the PR description)

---

## Phase 2: Foundational (blocking)

**Purpose**: The token and geometry layer every story paints from. Both variants are
edited together because `DesignTokenCheck.Run` fails on a dark/light or XAML/C# mismatch.

- [X] T003 Re-value every `Brush.*` key in both `ThemeDictionaries` (`Dark`, `Light`) of `App/Theme/NotaTheme.axaml` to the values in data-model.md §Surfaces, §Lines and grid, §Text, §Accent, §Semantics, §Data/graph chromas, §Track palette (including `Brush.Track1..9`, `ReturnA/B`, `Master`), and retint Fluent `SystemAccentColor*` ramps to the orange accent; rewrite the palette comments from "Ember Graphite/Paper, brass" to the Studio roles
- [X] T004 Add the new keys to both dictionaries of `App/Theme/NotaTheme.axaml`: `Brush.Seam`, `Brush.Rule`, `Brush.TextFaint`, `Brush.Mute`, `Brush.Solo`, `Brush.VizInk`, `Brush.VizGrid`, `Brush.Scrim`, and the `BoxShadows` `Shadow.Raised`, `Shadow.Sunken`, `Shadow.Popover`, `Shadow.Modal` with the exact strings in data-model.md §Elevation
- [X] T005 Mirror T003/T004 into `App/Theme/NotaPalette.cs`: re-value every `T("#dark","#light")` slot (same pairs as the XAML), add slots `Seam`, `Rule`, `TextFaint`, `Mute`, `Solo`, `VizInk`, `VizGrid`, `Scrim` with `// Brush.X` comments, extend `KeyMap` (and separate `Brush.SurfaceSelected` from `AccentSubtle`), re-value `TrackSlots` / `ReturnSlots` per data-model.md §Track palette, and update `InkOverrides`/`DeriveLight` so derived light inks land on the light grey ground (lightness reflected around the new ground; re-pin the four overrides)
- [X] T006 Add `NotaPalette.InkOn(SolidColorBrush fill)` in `App/Theme/NotaPalette.cs`: a cached `Derived` brush returning `#17181A` or `#FFFFFF`, whichever has the higher WCAG contrast on the fill's current colour
- [X] T007 Remap radii in `App/Theme/NotaTheme.axaml` and `App/Theme/NotaGeometry.cs` to data-model.md §Geometry (`Bar 1, Clip 2, Badge 2, Control 3, Tile 3, Panel 3, Body 3, Pill 3`), add `Radius.Modal` 4 and `Radius.Docked` 0 (XAML key + `NotaRadius.Modal`/`Docked`), and replace the literal `CornerRadius="2"` at `App/MainWindow.axaml` (~:365, ~:383) with `{StaticResource Radius.Clip}`
- [X] T008 Update `tests/Nota.SmokeTest/DesignTokenCheck.cs` so the token layer passes: `Run` parity also covers `BoxShadows` keys in both dictionaries; `RunGeometry` reads the new radius values/keys; keep every existing ban
- [X] T009 Run `scripts/build-linux.sh`; the smoke test must pass with the new token layer before story work continues

**Checkpoint**: The app already repaints in Studio greys/orange in both appearances via the slots; structure and rules still Ember.

---

## Phase 3: User Story 1 — The app wears the Studio look (P1) 🎯 MVP

**Goal**: Shell, shared control styles, menus/popovers/dialogs and accent rules follow Studio.

**Independent test**: quickstart.md §2 — main window, transport, browser, detail panel, menus, tooltips, preferences, export dialog, command palette match contracts C1–C3 in dark.

### Shell (docked seams)

- [X] T010 [US1] In `App/MainWindow.axaml`, make the shell docked: `DockPanel`/`BodyGrid` background `Brush.Seam`; Browser margin `0,0,1,0`, Timeline margin `0`, DetailPanel margin `0,1,0,0` (seam shows through, 0 at window edges); DetailPanel `CornerRadius` → `Radius.Docked`; BrowserSplitter and DetailSplitter become 6 px transparent strips with −3 margins centred on the seam (same min sizes and drag behaviour)
- [X] T011 [US1] Restyle the title bar in `App/MainWindow.axaml` (`TitleBar`) to 32 px on `Brush.BgApp` with a 1 px `Brush.Seam` bottom border; keep the macOS traffic-light inset and the 180 px Windows caption reserve (check `App/MainWindow.axaml.cs` / `NotaWindow.cs` for hard-coded 36)
- [X] T012 [US1] Transport row: in `App/Theme/NotaTheme.axaml` set `Control.TransportBar` 44, `Control.Console` 30, `Control.Transport` 26, `Control.Play` 36 (mirror in `App/Theme/NotaGeometry.cs` `NotaSize`); `Border.transport` → `Brush.Panel`, no radius (`Radius.Docked`), 1 px `Brush.Seam` bottom only; `Border.console` → `Brush.BgSunken` + `Shadow.Sunken` (rule edge), `Radius.Control`; remove the `Margin="12,12,12,0"` on `TransportBar` in `App/MainWindow.axaml`
- [X] T013 [US1] Re-measure the transport folding thresholds in `App/MainWindow.TransportFit.cs` for the new 26/36 key sizes and 12 px body text so nothing folds earlier than before at the same window width
- [X] T014 [P] [US1] Docked panel chrome in code: `App/BrowserView.cs` (~:434-446), `App/ArrangementView.cs` (~:417-425), `App/ModularView.cs` (~:182) and the `NotaSpace.Gutter` margins at `App/MainWindow.axaml.cs` (~:381) → radius `NotaRadius.Docked`, no outer border (seams come from the grid), background per role (browser `Panel`, canvas `BgApp`)
- [X] T015 [US1] Set `NotaSpace.Gutter`/`Space.Gutter` to 1 in `App/Theme/NotaGeometry.cs` and `App/Theme/NotaTheme.axaml`, and audit remaining users of `NotaSpace.Gutter` (`grep -rn "NotaSpace.Gutter\|Space.Gutter" App/`) so none relies on a 12 px gap for layout
- [X] T016 [US1] Panel toolbars: detail-panel header (`App/MainWindow.axaml` DetailPanel header, `App/MainWindow.Detail.cs`) and browser header (`App/BrowserView.cs`) render as 32 px `Brush.Panel` strips with a 1 px `Brush.Seam` bottom; selected mode segment uses the accent underline/fill per contract C3

### Control styles (NotaTheme.axaml)

- [X] T017 [US1] Buttons in `App/Theme/NotaTheme.axaml`: default `Button` → `SurfaceRaised`, `TextPrimary`, `BoxShadow {DynamicResource Shadow.Raised}`, border 0, hover `SurfaceHover`, pressed `BgSunken`, disabled `TextFaint` (no opacity); `.primary` → weight 600 (Studio: primary action is weight, not colour — the accent stays reserved for live or chosen state) except where it is a toggled-on state; engaged/checked states (`ToggleButton:checked`, `.engaged`) → solid `Brush.Accent` + `Brush.TextOnAccent`; `.ghost`, `.cell`, `.chip` (radius `Radius.Pill`=3), `.tp-icon`, `.tp-play` (36 wide, accent fill only while playing; Session green kept as `SuccessDim`→`Success`)
- [X] T018 [US1] Segments in `App/Theme/NotaTheme.axaml`: `Border.segmented` → raised group with `Shadow.Raised`, `Radius.Control`, inner keys square with 1 px `Brush.Seam` separators; `ToggleButton.seg:checked` → solid accent + `TextOnAccent`
- [X] T019 [US1] Fields in `App/Theme/NotaTheme.axaml`: `TextBox.field`, `TextBox.search`, `ComboBox` → `BgSunken` (search: `BgApp`), `Shadow.Sunken` rule edge, `Radius.Control`, 26 tall; focus → 1 px accent inset edge (`inset 0 0 0 1` accent BoxShadow or BorderBrush `Brush.Accent`)
- [X] T020 [US1] Tabs/list rows in `App/Theme/NotaTheme.axaml`: `TabItem` (or the app's tab segments) 32 tall, 12 px 500 `TextSecondary`, selected `TextPrimary` + 2 px accent underline; `ListBoxItem` / tree rows hover `SurfaceHover`, selected `SurfaceSelected` + 2 px accent left tick
- [X] T021 [US1] Menus in `App/Theme/NotaTheme.axaml`: `MenuFlyoutPresenter`, `ContextMenu`, `FlyoutPresenter` (new default style) and the `ComboBox` popup border → `SurfaceRaised`, `Radius.Control`, padding 4, `BoxShadow {DynamicResource Shadow.Popover}` with a transparent presenter margin (≥12, bottom ≥28) so the blur is not clipped; `MenuItem` 24 tall, `Radius.Clip`, hover/selected → solid accent + `TextOnAccent`; gesture text `TextMuted` (remove `Opacity 0.45`); `Separator` → `Brush.Rule`
- [X] T022 [US1] Add a `ToolTip` style in `App/Theme/NotaTheme.axaml`: `SurfaceRaised`, `TextPrimary` 11 px, `Radius.Control`, padding 6,3, `Shadow.Popover`; re-value `TooltipBg`/`TooltipText` slots (custom-drawn lane tooltips) to raised/primary in `App/Theme/NotaPalette.cs`
- [X] T023 [US1] Scrollbars in `App/Theme/NotaTheme.axaml` and `App/Controls/ThinScrollBar*.cs`: transparent track, thumb `mix(muted 34%)` → use `NotaPalette.Wash(TextMuted, 0x57)`, hover `Wash(TextMuted, 0x85)`
- [X] T024 [US1] Text classes in `App/Theme/NotaTheme.axaml` and `App/Theme/NotaFonts.cs` (`NotaType`): apply the type scale in research.md R8 (Title 600·13, default 500·12, Caption 400·11 secondary, SectionLabel/GroupLabel 600·11 muted, Readout Mono 400·13, Value Mono 400·11, Eyebrow Mono 400·10 muted); set every `*Tracking` constant and `LetterSpacing` setter to 0; enable `ss01` on the UI font if Avalonia 12 exposes `FontFeatures`
- [X] T025 [US1] Sentence-case labels, shell side: add `NotaText.Label(string)` in `App/Theme/NotaText.cs` (new file, SPDX header) per research.md R2 with the acronym list; replace all-caps captions in `App/MainWindow.axaml` (LOOP, BPM, TAP, SIG, KEY, GRID, MST and any other caps literals) and the `.ToUpper*()` label sites in shell views (`App/DeviceWindow.cs:76`, `App/SessionView.Inspector.cs:226,:412`, `App/SessionView.Mixer.cs:62`, `App/MidiToolsView.cs:308,346,366`, `App/SessionView.Toolbar.cs:146`, `App/DeviceChainView.Shell.cs:109,132`)
- [X] T026 [US1] Dialogs: `App/NotaWindow.cs` body `Brush.Panel`, header 32 `SurfaceRaised` + `Shadow.Raised` bevel and seam bottom; restyle per-window bodies in `App/ConfirmWindow.cs`, `App/ExportWindow.cs`, `App/PreferencesWindow.cs` (section titles → Title 600·13, groups → SectionLabel), `App/SplashWindow.cs` (`Radius.Modal`)
- [X] T027 [US1] Command palette `App/CommandPaletteView.cs`: card `SurfaceRaised`, `Radius.Modal`, `Shadow.Modal`, full-window `Brush.Scrim` behind; rows 24, selected → accent fill + `TextOnAccent`
- [X] T028 [US1] Glyph stroke in `App/Controls/Glyph.cs`: stroke 1.5 px at 16 px (scale with size, min 1.2), round caps and joins; play/record stay filled
- [X] T029 [US1] Status line in `App/MainWindow.axaml`: on `Brush.BgApp` with a top `Brush.Seam`, `Caption` text 16 px in
- [X] T030 [US1] Build, then verify US1 against contract C1–C3 under Xvfb (quickstart.md §2, dark); fix findings

**Checkpoint**: The shell and all stock-styled UI read as Studio in dark.

---

## Phase 4: User Story 2 — Every feature still works (P1)

**Goal**: Prove nothing moved, broke or vanished.

**Independent test**: smoke test green; FEATURES.md walk-through; old project round-trip.

- [ ] T031 [US2] Run `scripts/build-linux.sh`; all smoke-test checks pass (including the updated `DesignTokenCheck`)
- [X] T032 [US2] Confirm hit-test geometry is untouched: `git diff main -- App/ArrangementView*.cs App/PianoRollView*.cs App/ClipEditorKit.cs App/DeviceCardKit.cs` shows no change to row heights, header column 228, overview/sections/ruler heights, piano keys/rows, `CardH`/`HeaderH`/`ParamCell`/knob sizes (contract C6.2); revert any accidental change
- [X] T033 [US2] Open a project saved before the change (smoke-test fixture or a newly saved one from `main`), save it with the restyled build, and diff the JSON: no difference in content or track colour indices (contract C6.3)
- [X] T034 [US2] Walk the FEATURES.md sections in the running app (Xvfb) and confirm each listed view/panel/menu/window opens from the same place; record anything not reachable and fix it

---

## Phase 5: User Story 3 — Light appearance (P2)

**Goal**: Light = the Studio Light palette, live switch intact.

**Independent test**: Preferences → Appearance Light / System; capture the same surfaces as US1.

- [X] T035 [US3] Capture all quickstart.md §2 surfaces in Light; fix any surface that keeps a dark or warm colour (e.g. a view that snapshots `.Color` at construction instead of reading the slot — see `NotaPalette` "nothing may snapshot" rule)
- [X] T036 [US3] Check derived light inks (`NotaPalette.Ink`) used by visualisers and tag swatches against the light ground; pin any that read poorly in `InkOverrides` in `App/Theme/NotaPalette.cs`
- [X] T037 [US3] Verify contrast in both appearances for text on solid fills (accent, mute, solo, arm, track colours) ≥ 4.5:1 via `NotaPalette.InkOn`, and body text ≥ 4.5:1 on its ground (SC-005); verify the switch completes with no leftovers (SC-006)

---

## Phase 6: User Story 4 — Device cards and custom views (P2)

**Goal**: Devices and canvases speak Studio.

**Independent test**: one instrument, one audio effect, one MIDI effect, one plug-in card, arrangement, piano roll, audio editor, mixer, session, modular against contract C4–C5.

### Shared device kit and value controls

- [X] T038 [US4] `App/DeviceCardKit.cs`: replace `ToUpperInvariant()` in `KnobCell` (:66), `Switch` (:91, :102), `SliderRow` (:214), `Labeled` (:317) with `NotaText.Label`; `BypassTag` "BYPASSED" → "Bypassed"; modified label/value no longer brass — modified value `TextPrimary`, unmodified `TextSecondary`; `Segments` selected fill stays accent with `TextOnAccent`; device header 22 on `SurfaceRaised` with `Shadow.Raised` bevel, card body `Panel`, `Radius.Body` (3) + seam ring; sections `SurfaceCard` (= panel) framed by `Brush.Rule`, titled with the `DeviceSection` class
- [X] T039 [US4] Device header power key and badge in `App/DeviceChainView.Shell.cs`: bypass switch on = accent ("power key"), processing-type `Subtitle` badge rendered via `NotaText.Label` in `TextMuted` mono 10 without tracking
- [X] T040 [P] [US4] `App/Controls/Knob.cs`: track pointer hover (PointerEntered/Exited) alongside `_drag`; resting arc and pointer → `NotaPalette.VizInk` / `TextStrong`; while hovered or dragged → `Accent` / `AccentBright`; `ArcColor` (modulation chroma) unchanged; cap `SurfaceRaised` with `Seam` stroke; groove `BgSunken`
- [X] T041 [P] [US4] Same rest-neutral / live-accent rule in `App/Controls/MiniFader.cs`, `App/Controls/PanBar.cs`, `App/Controls/SliderTrack.cs`, `App/Controls/RangeSliderTrack.cs`, `App/Controls/ValueBar.cs` (also replace render radius 4 with `NotaRadius.ControlValue`), `App/Controls/MixBar.cs`; `App/Controls/SwitchTrack.cs` on stays accent, track/knob radius 2 (square-ish: Studio has no pill switches) keeping `W = 18, H = 10, KnobD = 7, Inset = 1.5`
- [X] T042 [P] [US4] `App/Controls/NotaGraph.cs`: `Chroma(0)` / `PrimaryPen` → `VizInk`; chroma order viz-ink · teal · rose · blue (`Steel`); grid `VizGrid`; frame `Rule`, `Radius.Control`; titles (:79, :121) via `NotaText.Label` instead of upper-casing; selected/dragged node accent, others `TextSecondary`
- [X] T043 [P] [US4] Waveforms at rest → neutral: `App/Controls/WaveformView.cs` (`WavePen` → `VizInk`), `App/PreviewPlayer.cs` (~:1084-1086 `AccentMute`), `App/DeviceCards/**/MosaicViews.cs` (~:261); played/selected portion may stay accent

### Device bodies (labels and resting accent)

- [X] T044 [US4] Replace the 39 local `Caps(...)` helpers and the two `FormattedText Caps(...)` (`ChamberViews.cs:33`, `PrismViews.cs:41`) under `App/DeviceCards/` and `App/` with calls through `NotaText.Label` and the `DeviceSection`/`RowLabel` styles (no Bold, LetterSpacing 0); remove hand-set positive `LetterSpacing` on label text in views (112 occurrences / 65 files, `grep -rn "LetterSpacing" App/ --include=*.cs`)
- [X] T045 [US4] Audit resting accent in device visualisers file by file under `App/DeviceCards/` and `App/Controls/` (start with ChamberViews, BeatRepeatViews, ConsortViews, then the rest by `grep -rln "NotaPalette.Accent\|Brass\b" App/DeviceCards App/Controls`): resting curves/fills/bars → `VizInk` or `TextSecondary`; keep accent only for selection, playhead, active step, dragged node, toggles on (contract C1.3–C1.4)
- [X] T046 [US4] Screenshot every built-in device card (dark) and fix any truncated label caused by the new label size/case (contract C4.3)

### Custom-drawn views

- [X] T047 [US4] Arrangement `App/ArrangementView.cs`: `ChipToggle` (:2379) mute on → `Mute` fill + `InkOn(Mute)`, solo on → `Solo` fill + `InkOn(Solo)`, rest → `SurfaceRaised`/`TextSecondary` with seam edge; `ArmToggle` (:2359) armed → `Record` fill + `RecordInk`; track names 600 when selected; selected header `SelHeaderBg` + 2 px accent left tick
- [X] T048 [US4] Arrangement clips (`App/ArrangementView*.cs` clip painting): flat `Radius.Clip` rect in track colour, title band = track colour mixed 30 % toward black (via `NotaPalette.Derived`, cached per index), band → accent when selected; clip title text `InkOn(band)` 9.5 px 600
- [X] T049 [P] [US4] Mixer `App/MixerView.cs`: `Toggle` (:417) mute/solo → `Mute`/`Solo` fills with `InkOn`; `ArmToggle` (:401) → `Record`; strips `Panel` with seam separators, 3 px top bar in track colour (master `Master` grey), selected strip `SurfaceHover`
- [X] T050 [P] [US4] Session `App/SessionView.Mixer.cs` `MixToggle` (:182) → same mute/solo/arm scheme; session grid clip slots follow the clip treatment of T048
- [X] T051 [P] [US4] Piano roll / clip editor `App/PianoRollView*.cs`, `App/ClipEditorKit.cs`, `App/ClipEditorView.cs`: notes in velocity shades of the clip colour with a dark 1 px edge, selection = 2 px accent frame; keys `KeyWhite`/`KeyBlack` re-valued (T005); loop strip accent; tools-panel Apply = the one accent action only while a preview is pending
- [X] T052 [P] [US4] Audio editor and automation: `App/AudioClipEditorView.cs`, `App/ArrangementView.Automation.cs`, `App/ArrangementView.LaneControl.cs`: curve and points `VizInk`, selected point accent; automation clip body 16 % owner colour over well, title band 30 % toward black
- [X] T053 [P] [US4] Modular `App/ModularView*.cs`: canvas `BgApp`, node cards like device cards (T038), cable colours unchanged except any resting accent → `VizInk`
- [X] T054 [US4] Build and verify US4 against contract C4–C5 under Xvfb in dark and light

---

## Phase 7: User Story 5 — Docs and guards (P3)

**Goal**: The written design language and the smoke-test guards describe Studio.

**Independent test**: DESIGN.md/DESIGN.html/skill match the app; a reintroduced blurred shadow on a docked surface or a caps-tracked label fails the smoke test.

- [X] T055 [US5] `tests/Nota.SmokeTest/DesignTokenCheck.cs`: (a) blurred `BoxShadows` allowed only in `Shadow.Popover`/`Shadow.Modal` definitions and their users in `NotaTheme.axaml`, still banned in `.cs`; (b) radius values only 0/1/2/3/4; (c) no `ToUpper(Invariant)?()` in `DeviceCardKit.cs` label builders, `NotaGraph.cs`, and `Caps(` helpers, and no positive `LetterSpacing` in theme text classes; (d) `RunLayout`: TransportBar 44, console 30, header 22 kept, cards 700 kept; (e) update any Ember-specific assertion text
- [X] T056 [P] [US5] Rewrite `DESIGN.md` as the Studio design language: source-of-truth table, rules (from research.md R12), token tables for dark/light from data-model.md, geometry, type (R8), controls and states (contract C3), device cards, shell (docked), tests, accepted exceptions (700 × 260 cards; device type at 7.5–10.5 px; 9 track roles; transport 44), known drift
- [X] T057 [P] [US5] Update `scripts/design-html.py` key lists, group names and titles (:31-36, :59, :238-269) for the new tokens and the Studio name; run `python3 scripts/design-html.py` to regenerate `DESIGN.html`
- [X] T058 [P] [US5] Rewrite `.claude/skills/nota-design/SKILL.md` for the Studio language (non-negotiables updated: accent live-or-chosen, sentence case, docked seams, floating-only shadows, 2/3/4 radii, `InkOn`, `NotaText.Label`)
- [X] T059 [P] [US5] Update references to Ember/brass in `FEATURES.md` (~:53, :347, :810, :923-925, :975), `CLAUDE.md`/`CONTRIBUTING.md`/`.specify/memory/constitution.md` §VI and `.specify/templates/overrides/*.md` where they name "Ember" (wording only; constitution version bump PATCH)
- [X] T060 [US5] Add a `CHANGELOG.md` `[Unreleased]` → Changed entry describing the new look (Studio neutral greys, orange accent for live/selected state, docked panels, sentence-case labels; no feature changes)

---

## Phase 8: Polish & cross-cutting

- [X] T061 Remove dead Ember-only code left behind (unused slots/aliases with zero references, e.g. `AccentMute` if unused) only where `DesignTokenCheck` parity allows; keep names that are still referenced
- [ ] T062 Final `scripts/build-linux.sh` run; smoke test green
- [X] T063 Final before/after screenshots (dark + light) for the PR description; note `../nota-docs` screenshots as a follow-up (repository not available here)

---

## Dependencies & Execution Order

- **Setup (T001–T002)** → **Foundational (T003–T009)** blocks everything.
- **US1 (T010–T030)** after Foundational. T024–T025 (type + `NotaText.Label`) before US4's label work (T038, T042, T044).
- **US2 (T031–T034)** after US1 and again after US4 (it is the regression gate; T031 is re-run in T062).
- **US3 (T035–T037)** after US1 (light values come with Foundational; this story verifies and pins).
- **US4 (T038–T054)** after T024–T025 and T005–T006 (`InkOn`, new slots).
- **US5 (T055–T060)** after US1/US4 settle the rules and values.
- **Polish (T061–T063)** last.

### Parallel opportunities

- T014 runs alongside T010–T013 (different files).
- T040, T041, T042, T043 in parallel (separate control files).
- T049, T050, T051, T052, T053 in parallel (separate views).
- T056, T057, T058, T059 in parallel (separate docs).

### Parallel example: User Story 4

```text
T040 Knob.cs   |  T041 faders/sliders/switch  |  T042 NotaGraph.cs  |  T043 waveforms
then T044 Caps helpers → T045 visualiser audit → T046 truncation pass
T049 Mixer | T050 Session | T051 Piano roll | T052 Audio/automation | T053 Modular
```

## Implementation Strategy

- **MVP** = Foundational + US1: the token remap plus shell and control styles already
  make Nota read as Studio on every main surface.
- Then US4 (devices and canvases — the bulk of the screen while working), US3 (light
  pass), US2 as the regression gate after each, and US5 so the rules stick.
- Commits are layered to match: tokens → shell/styles → controls/devices → views → docs.

## Phase 9: Convergence

- [ ] T064 Restyle `src/managed/Nota.App/Remote/ConnectPhoneView.cs` (and `Remote/QrView.cs` comments): sentence-case labels via `NotaText.Label`, remove positive `LetterSpacing` and bold caps ("USB", section labels), resting ring/chip colours from `AccentDim`/`BorderBrass` to neutral ink, accent only while connecting per FR-012, FR-004 (partial)
- [ ] T065 Make `src/managed/Nota.App/Controls/Inactive.cs` dim `NotaPalette.VizInk` (and the neutral value fills that replaced brass) so a bypassed card's resting values take the disabled look per FR-015 / US4 bypassed card (partial)
- [ ] T066 Restyle device section frames in `src/managed/Nota.App/DeviceCards/**` (shared section builders) so sections sit on the panel tone divided by 1px rules instead of seam-framed boxes per FR-015 (partial)
- [ ] T067 Resolve floating-layer elevation for menus/popovers: either render popups in-window (Avalonia overlay popups) so `Shadow.Popover` can blur, or record the departure in spec FR-009 with the user's agreement per FR-009 (partial)
- [ ] T068 Replace the literal radius 4 in `src/managed/Nota.App/Controls/OperatorAlgoViz.cs:108` with `NotaRadius.ControlValue` per FR-010 (partial)
- [ ] T069 Re-shoot the user-manual screenshots in `../nota-docs` (EN + RU) for the Studio look via the nota-docs skill per FR-024 (partial)
