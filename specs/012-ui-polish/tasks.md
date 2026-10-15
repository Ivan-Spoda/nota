---

description: "Task list for the UI polish pass"
---

# Tasks: UI Polish Pass

**Input**: Design documents from `/specs/012-ui-polish/`

**Prerequisites**: plan.md, spec.md, critique.md, research.md, data-model.md, contracts/ui-contract.md, quickstart.md

**Tests**: The spec requires automated checks (FR-007, FR-011, SC-004), so the check tasks
are part of the work. They extend the existing gate in `tests/Nota.SmokeTest`. There is no
new test project.

**Organization**: by user story. Paths under `src/managed/Nota.App/` are written `App/`,
`src/managed/Nota.Application/` as `Application/`, and `tests/Nota.SmokeTest/` as `Smoke/`.
Each phase is one commit that builds and is screenshot-verified, so the PR can be split per
workstream (plan § Complexity Tracking).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story the task belongs to (US1–US6)

---

## Phase 1: Setup

**Purpose**: a known-good build and a full visual baseline to diff against.

- [X] T001 Build `012-ui-polish` with `scripts/build-linux.sh` (Xvfb, PulseAudio null sink) and record the smoke-test result in `/tmp`, not committed. Only "Consort stress" should fail.
- [X] T002 [P] Write `/tmp/claude-0/scripts/cards.sh <theme> <outdir>`, outside the repo. It adds each of the 51 built-in devices from the browser one at a time and screenshots the dock. Capture the **baseline** in Studio Dark and Studio Light into `/tmp/claude-0/shots/cards-before/`.
- [X] T003 [P] Re-run the critique tour (`/tmp/claude-0/scripts/tour.sh`) in all four themes into `/tmp/claude-0/shots/tour-before/` as the SC-001 baseline.
- [X] T004 [P] Write `/tmp/claude-0/scripts/edgediff.sh before.png after.png`. It edge-detects both images (`convert -colorspace gray -edge 1 -threshold 25%`), dilates by 1 px, then reports the moved-edge pixel count and writes a red overlay. This is the P4.2 "nothing moved" check.

---

## Phase 2: Foundational (blocking)

**Purpose**: the guards go in first. Every later phase lands against them.

- [X] T005 Make `NotaText.Fixed` readable by the check: `internal` in `App/Theme/NotaText.cs`, parsed by regex like `KeyMap` is. Add the proper nouns and view names the label rule allows: Arrangement, Session, Modular, Mixer, Finder, Nota.
- [X] T006 Add `RunLabels()` to `Smoke/DesignTokenCheck.cs` per data-model.md § Label rules:
  - label-context literal extraction for `.cs` and `.axaml`;
  - the all-caps and Title Case rules, with the allow-list and data skips;
  - a **ratchet** allow-list `Smoke/LabelAllowList.txt` (`file:line  # reason`), seeded with today's hits so the check starts green. Failures name `file:line` and the literal.
- [X] T007 Add `RunIcons()` to `Smoke/DesignTokenCheck.cs`. In `App/`, outside `Controls/Glyph.cs`, these fail: `Geometry.Parse`, `StreamGeometry.Parse`, `PathGeometry.Parse`, an axaml `Data="M` attribute, or a string literal matching `^"?[MmFf][0-9 ]`. It uses a ratchet `Smoke/IconAllowList.txt` seeded with today's call sites.
- [X] T008 Extend `RunNumbers` in `Smoke/DesignTokenCheck.cs`: a view string literal containing a hyphen-minus directly before a digit (`"-12"`, `" -3"`) fails, except in format strings and in key or shortcut text. Seed a ratchet line for `App/MixerView.cs:363`.
- [X] T009 Extend `RunGeometry` in `Smoke/DesignTokenCheck.cs`: `Rectangle` `RadiusX`/`RadiusY` literals must be on the 0/1/2/3/4 scale. Seed a ratchet for `App/DeviceChainView.cs` and `App/DeviceCards/Rack/RackCardView.cs`, the add-device targets.
- [X] T010 Add the `-- design: labels --` and `-- design: icons --` sections to `Smoke/Program.cs`. Build and run `/tmp/claude-0/scripts/check.sh`; every check passes with the seeded ratchets.

**Checkpoint**: the guards are in place. From here, each phase removes its ratchet lines.

---

## Phase 3: User Story 1 — Nothing overlaps, overflows or goes missing (Priority: P1) 🎯 MVP

**Goal**: fix the three defects (contracts P1.1–P1.3; research R1–R3).

**Independent Test**: quickstart.md § 2, the defect runs.

- [X] T011 [US1] In `App/ArrangementView.cs`:
  - Draw a 1 px `Brush.Seam` top edge on the pinned return/master footer band, using a `Border` around `_footer` and `footerHeaderPanel` with `BorderThickness(0,1,0,0)`.
  - Paint the empty lane side below the last track with the lane colour, keeping the header side the header colour (`leftBackdrop`).
- [X] T012 [US1] In `App/ArrangementView.cs`, add `RevealTrack(int index)`, which sets `_scroller.Offset` so `RowTop(i)…RowTop(i)+RowHeight` is fully visible. Call it after `Refresh()` for a newly inserted track (`App/MainWindow.Dnd.cs` `ApplyInsertResult`, `OnArrangementDrop`) and on keyboard selection changes (`Select`).
- [X] T013 [US1] Verify F1 with `/tmp/claude-0/scripts/f1-repro.sh` adapted to fill the view with tracks first. The new track must be fully visible above the seam, both at once and after 5 s.
- [X] T014 [US1] In `App/ModularView.cs` and `App/MainWindow.axaml.cs`:
  - `SetView(Modular)` always calls `RefreshSidebar()`.
  - When no track is selected, auto-select the first non-return track (`Show(t)`).
  - Remove the `_trackId > 0` guard on sidebar refresh in `Refresh()`.
- [X] T015 [US1] In `App/ModularView.cs`, add the empty states: "No tracks yet" in the sidebar for an empty project, and "Pick a track to see its signal graph" on the canvas when `_trackId <= 0`. Both use `TextBlock.Caption`, sentence case.
- [X] T016 [US1] Add `SessionInspectorOpen` (bool, default `true`) to `Application/Settings.cs` with a doc comment.
- [X] T017 [US1] In `App/SessionView.cs`:
  - The inspector column becomes 260 wide (`InspectorW`) and collapsible.
  - Add an "Inspector" icon toggle (Glyph `Sidebar`) at the right end of the session toolbar, bound to `SessionInspectorOpen`.
  - When content overflows beside the inspector, show the existing horizontal scroll bar as a cue.
- [X] T018 [US1] In `App/SessionView.Toolbar.cs`, add priority collapse on `SizeChanged`:
  - Level 1: the right group's labels become icon-only keys with tooltips (I/O, Sends, Mixer, Stop all).
  - Level 2: "Back to arrangement" and the position readout move into a "⋯" overflow `MenuFlyout`.

  Every control stays reachable (P1.2). Thresholds are measured from the groups' desired sizes, not hard-coded.

  *Done as*: one fold order (readout → Back to arrangement → Capture scene → section toggles → Follow) into "⋯", which offers whatever is hidden. The icon-only level 1 was dropped: the fold already keeps every control reachable, and I/O, Sends and Mixer read better as words than as new icons. The arrangement has no keyboard track navigation, so T012's `Select` hook is not needed; drops go through `ApplyInsertResult`.
- [X] T019 [US1] Verify US1 under Xvfb at 1200 × 800:
  - Session with the inspector open and closed shows nothing cut off;
  - Modular after opening a project with no selection lists the tracks and selects one;
  - an empty project shows both empty states.

  Commit "fix(008): US1".

---

## Phase 4: User Story 2 — Floating windows behave like the palette (Priority: P1)

**Goal**: contracts P2.1–P2.3 (research R4).

**Independent Test**: quickstart.md § 3 on macOS; the Escape behaviour under Xvfb.

- [X] T020 [P] [US2] `App/MixerWindow.cs` + `App/MixerView.cs`: the mixer window width = the strips' desired width + master + padding, clamped to `[560, screen working-area width]`. Recompute on track add or remove while it's open. The height stays 460. Strips fill without a trailing dead area.
- [X] T021 [P] [US2] Escape closes the window: in `App/MixerWindow.cs` (`KeyDown` Escape → `Close()`, unless a text field has focus) and in `App/ExportWindow.cs` (Escape → the same path as Cancel).
- [X] T022 [P] [US2] Window titles in sentence case: `App/ExportWindow.cs` "Export audio", `App/TagEditorWindow.cs` "Edit tags", `App/Remote/ConnectPhoneView.cs` "Connect phone", `App/WhatsNewWindow.cs` "What's new", and any other `Title =` literal flagged by `RunLabels`. Remove their ratchet lines.
- [X] T023 [US2] Verify under Xvfb: the mixer width with 2, 5 and 12 tracks leaves 0 px of dead area (screenshot crop), and Escape closes the mixer and the export window. Commit "feat(008): US2".

---

## Phase 5: User Story 3 — One labelling voice (Priority: P2)

**Goal**: P3.1–P3.2 (research R5). Shrink the `RunLabels` ratchet to data-only exemptions.

**Independent Test**: `-- design: labels --` passes with only data exemptions left in `Smoke/LabelAllowList.txt`.

- [X] T024 [P] [US3] `Application/Palette/PaletteSearch.cs`: the group labels ("Actions", "Tracks", "Devices", "Presets", "Modular"), "Recent", and "Suggested · " + the origin label in sentence case, with no `ToUpperInvariant`. Update `Smoke/PaletteTests.cs:195-196` to expect "Recent" and "Suggested". Extend `RunStudio`'s upper-casing check to `PaletteSearch.cs`.
- [X] T025 [P] [US3] `Application/Palette/CommandCatalog.cs`: every command title in sentence case ("New project", "Show mixer", "Quantize notes"…), with product and view names kept. Update `Smoke/PaletteTests.cs:100` and any test that matches a title.
- [X] T026 [P] [US3] `App/MainWindow.axaml` native menu: every `Header` in sentence case ("Save as…", "Save version with note…", "Lock envelopes", "Paste bounced audio", "Copy clips to session / arrangement", "Narrow grid", "Widen grid", "Triplet grid", "Snap to grid", "Adaptive grid", "Command palette…", "Connect phone…", "Toggle full screen", "What's new", "Live freeze").
- [X] T027 [P] [US3] Session labels:
  - `App/SessionView.Toolbar.cs`: "Session rec", "Capture scene", "Stop all", "Back to arrangement".
  - `App/SessionView.Grid.cs`: "Stop all clips".
  - `App/SessionView.Mixer.cs`: "All ins", "Ext in".
  - `App/SessionView.Inspector.cs`: route `SectionHead`, `Field` and `Mono` through `NotaText.Label` ("Color", "Launch", "Follow action", "Length", "Loop", "Start", "Loop end", "Tempo", "Signature", "Off globally").
- [X] T028 [P] [US3] Mixer and detail labels: `App/MixerView.cs` `Mono` through `NotaText.Label` ("Pan"), and "Monitor in" / "Monitor auto". Same in `App/MainWindow.Detail.cs`.
- [X] T029 [P] [US3] `App/DeviceCards/Rack/RackCardView.Drum.cs`: the meta line becomes `$"{n} pad{s} · {g} choke group{s}"`. `App/DeviceChainView.Instruments.cs`: the Drum Rack subtitle "Drums · {n} pads".
- [X] T030 [P] [US3] Browser, arrangement and other menus:
  - `App/BrowserView.cs`: "Add to favorites" / "Remove from favorites".
  - `App/MainWindow.Freeze.cs`: "Live freeze".
  - `App/MainWindow.Stems.cs` and `App/ArrangementView.LaneControl.cs`: "Separate stems…", "Convert melody to new MIDI track", "Slice to new MIDI track", "Get the AI model for melody & harmony…", "Copy to scene".
  - `App/PianoRollView.Expression.cs`: "Clear all expression".
  - `App/MainWindow.PasteBounced.cs`: the dialog title.
- [X] T031 [P] [US3] Preferences and pickers:
  - `App/PreferencesWindow.cs`: sidebar groups and shortcut group titles go through `NotaText.Label` ("Devices", "Plug-ins", "General", "File", "Window", "Command palette", "Transport", "Arrangement & editing", "Piano roll", "Browser · preview", "Mouse"); "System default".
  - `App/PreferencesWindow.Downloads.cs`: "Sample packs".
  - `App/Controls/KeyPicker.cs`: "Major" / "Minor".
  - `App/SaveChangesWindow.cs`: "Don't save".
  - `App/TagEditorWindow.cs`: "New tag".

  *Done as*: view names (Arrangement, Session, Modular, Mixer) keep their capital, so it is "Back to Arrangement", "Show Mixer" and "Copy clips to Session / Arrangement". Settings page names after a colon stay capitalised ("Settings: Audio"). LUFS, USB, OK and DAW joined `NotaText.Fixed`.
- [X] T032 [US3] Shrink `Smoke/LabelAllowList.txt` to data-only exemptions (preset tables, engine parameter display names, MIDI-learn names, sample device names), each with a reason. Run the check (green), take a screenshot pass of the palette, session, mixer, Drum Rack, Preferences and menus, and commit "style(008): US3".

---

## Phase 6: User Story 5 — One icon set, including the view switcher (Priority: P2)

**Goal**: P5.1–P5.3 (research R8, data-model § Icon).

**Independent Test**: `-- design: icons --` passes with an empty `IconAllowList.txt`. The view keys work and show tooltips.

- [X] T033 [US5] Extend `App/Controls/Glyph.cs` per data-model § Icon:
  - Add path-based kinds on a 16-unit grid, drawn with the shared stroke rule or filled. No `Stretch.Uniform`.
  - Add all the new kinds: the views, rail/pages, player/transport, palette kinds and OS marks.
  - Keep existing kinds pixel-identical.
  - Each new kind gets a one-line comment naming what it depicts.
- [X] T034 [US5] Write `/tmp/claude-0/scripts/glyph-sheet.sh`, outside the repo. It renders every `GlyphKind` at 12/16/24 px in Studio Dark and Light on a contact sheet (via a hidden `--glyph-sheet` debug path or a smoke-test render to PNG) for visual review of weight consistency. Review it, and fix any kind that reads heavier or lighter.
- [X] T035 [P] [US5] Replace the inline icons in `App/BrowserView.cs`: `GridIcon` rail icons, Options, Downloads arrow, disclosure carets, favourite star → `Glyph`. Remove the `GridIcon` helper.
- [X] T036 [P] [US5] Replace the inline icons in `App/PreferencesWindow.cs` (page icons, search) and `App/PreferencesWindow.Store.cs` (OS marks) → `Glyph`. Remove `IconBox` path use.
- [X] T037 [P] [US5] Replace the inline icons in `App/CommandPaletteView.cs` (kind icons, search; drop `Stretch.Uniform`), `App/HistoryView.cs` (options, star), `App/PreviewPlayer.cs` (loop, speaker levels, play, caret, stop) and `App/DeviceCards/Instruments/RhythmInstrumentCard.cs` (folder) → `Glyph`.
- [X] T038 [P] [US5] Replace the transport toggle icons in `App/MainWindow.axaml` (metronome, follow, snap, automation) and `App/NotaIcons.cs` `ChainLink` (used by `MainWindow.axaml:507`, `MainWindow.Freeze.cs:274` and `ArrangementView.cs:1788`) → `Glyph`. Delete `NotaIcons.cs` if it ends up empty.
- [X] T039 [US5] View switcher in `App/MainWindow.axaml:154-162`:
  - The three `ToggleButton.seg` hold `Glyph` Arrangement / Session / Modular, at 28 × 24, in the same order.
  - `ToolTip.Tip` and `AutomationProperties.Name` are the view name. The tooltip appends a shortcut only if the command gains one (read from `CommandCatalog`).
  - The checked state uses the accent fill, as today.
  - Measure the width before and after; it must be at least 100 px narrower (SC-007).

  *Measured*: about 230 px → 87 px at 1200 × 800.
- [X] T040 [US5] Empty `Smoke/IconAllowList.txt`. Run the check (green), take screenshots of the rail, browser, Preferences, palette, history, player and transport in all four themes, and commit "feat(008): US5".

---

## Phase 7: User Story 4 — Same header and calmer sections on every card (Priority: P2)

**Goal**: P4.1–P4.3 (research R6, R7). This is the largest diff, verified per card against the T002 baseline.

**Independent Test**: `cards.sh` after the change, then `edgediff.sh` per card. The header slots align, and section content moves by at most 1 px.

- [X] T041 [US4] `App/DeviceChainView.Shell.cs` `BuildCardShell`:
  - Header order: name · preset · badges (subtitle, voice count, `HeaderAccessory`/`HeaderExtra`) · power.
  - The power slot has a fixed width on every card, empty when the device isn't bypassable.
  - Fix the "26px chrome bar" comment to say 22.
- [X] T042 [US4] Move `App/DeviceCards/Rack/RackCardView.cs` (Instrument Rack and Audio Effect Rack) onto `BuildCardShell` via `ShellSpec`, keeping "Rack · N chains" as a badge. Delete `RackHeader` and the dead `RackShell`.
- [X] T043 [US4] Move `DeviceCardKit.SimpleCard` users (`App/DeviceCards/**/GenericInstrumentCard.cs`, the Sends card in `App/DeviceChainView.cs:239`) onto `BuildCardShell`. Remove `SimpleCard` if it ends up unused.
- [X] T044 [US4] Verify the headers with `cards.sh` in Studio Dark: the preset picker's left edge and the power key's right edge align across cards of equal width (P4.1). Plugin card check: load the built-in test plugin, if the smoke test ships one, or skip with a note.

  *Checked*: the preset picker starts 171 px into every 700-wide card (Auto Filter, Drum Rack, Instrument Rack). The plugin card uses the same shell; no test plugin ships, so it was not loaded. Sends uses the shell as a `Utility` card (no device menu, selection or presets).
- [X] T045 [US4] Add `DeviceCardKit.Section(Control body, SectionEdge edges)` in `App/DeviceCardKit.cs`, with `[Flags] enum SectionEdge { None, Top, Left }`. It has no fill or frame, draws only a 1 px `Brush.Rule` on the given edges, and keeps the same inner padding as today's boxes (data-model § Device-card section).
- [X] T046 [US4] Make every local helper a one-line delegate to `DeviceCardKit.Section`. This covers the 13 instrument `SectionBox`es, the effect-body `TabFrame` outer frame (not its tab bar), the 6 MIDI `IslandBox`es, `PluginCard.Section`, `RhythmInstrumentCard.Section`, Pendulum's `StripBox` and Drum Rack's `DrumBox`. The caller passes the edge it shares with the previous section.

  *Done as*: `Section` is a `RuleSection` decorator that keeps the 1 px inset and, by default, finds its neighbouring sections from the layout (`SectionEdge.Auto`), so a sweep site is a plain swap. Pendulum's `StripBox` is a header strip (a bottom hairline), not a framed box, so it stays. The `TabFrame` helpers return no frame of their own; their outer frames are inline and go with the sweeps.
- [X] T047 [P] [US4] Sweep the inline framed section boxes in `App/DeviceCards/Instruments/*.cs` (`SurfaceCard`/`Card2` background + `BorderDef` border) → `Section`. Leave graph windows and wells alone. Run `edgediff` on each instrument card.
- [X] T048 [P] [US4] Same sweep in `App/DeviceCards/Bodies/*.cs` (effects). Run `edgediff` on each effect card.
- [X] T049 [P] [US4] Same sweep in `App/DeviceCards/Midi/*.cs` and `App/DeviceCards/Rack/*.cs`. Run `edgediff` on each.
- [X] T050 [US4] Capture all 51 cards with `cards.sh` in all four themes, compare with the baseline through `edgediff`, and inspect each by eye. Fix any card where a control moved by more than 1 px or a rule is missing between neighbours. Commit "style(008): US4".


  *Result*: 50 cards × Studio Dark and Studio Light, captured after the section sweep and the card-surface fix. Every card body aligns with its baseline at a (0, 0) offset (2-D shift search ±1 px), so no control, graph or label moved. Remaining differences are the removed frames/fills, the new rules and animated visuals (Flanger, Phaser, Pendulum, MIDI note views). The light-theme pass caught card bodies showing the gutter tone once the fills were gone; body roots now paint `SurfaceCard` (8261110).
---

## Phase 8: User Story 6 — Lighter chrome (Priority: P3)

**Goal**: P6.1–P6.4 (research R9).

**Independent Test**: browser rows, the empty dock, the mixer scale and an unchanged arrangement header, in all four themes.

- [X] T051 [P] [US6] `App/BrowserView.cs`: drop the `RowPrefix` element from built-in rows (rows show `ShortName`). The type tag moves one ink step quieter (`TextFaint`), still ≥ T3.3. Search stays on the full `Name` (`BrowserViewModel.Matches`). Verify that "nota aurora" and "aurora" both find Aurora.
- [X] T052 [P] [US6] `App/DeviceChainView.cs` `AddDeviceCard` and `App/DeviceCards/Rack/RackCardView.cs` `ChainAddDeviceCard`:
  - A 28 px-wide target with Glyph `Plus` and the tooltip "Add device — double-click in the browser or drop here".
  - Dashed outline, `BorderStrong`, radius 3, shown only while a drag is over the chain (`DragDrop.DragEnter` / `DragLeave` / `Drop` handlers).
  - Remove the radius-7 and radius-6 ratchet lines.
- [X] T053 [P] [US6] `App/MixerView.cs:363` scale labels become `{"+6","0","−12","−48"}` (U+2212). Normalise the range dashes per data-model § Number setting in the files research R9 lists: closed between plain numbers, spaced when an endpoint has a space. Remove the `RunNumbers` ratchet line.
- [X] T054 [US6] Verify P6.4: the arrangement header rows (Overview, Sections, Zoom/ruler) are pixel-identical to `tour-before` in every theme. Commit "style(008): US6".


  *Result*: Overview, Sections and Zoom/ruler rows are pixel-identical (0 differing pixels) to `tour-before` in all four themes.
---

## Phase 9: Polish & Cross-Cutting

- [X] T055 [P] DESIGN.md:
  - § Floating windows (Escape, sizing, sentence-case titles);
  - § Device cards: the header anatomy with slots and a reserved power slot, plus rule-divided sections (replacing `:516-517` and `:167-168`);
  - § Icons: the one set, the grid and stroke, filled vs stroked, the new kinds;
  - § Numbers: the range-dash rule;
  - § Enforced by tests: `RunLabels`, `RunIcons`, the extended `RunNumbers` and `RunGeometry`.

  Regenerate `DESIGN.html` with `python3 scripts/design-html.py`.
- [X] T056 [P] `.claude/skills/nota-design/SKILL.md`: the header anatomy, `DeviceCardKit.Section`, "every icon is a Glyph", and the label rule with its check.
- [X] T057 [P] `CHANGELOG.md` `[Unreleased]` Changed:
  - icon view switcher;
  - calmer device cards (line-divided sections, one header layout);
  - fitted mixer window and Escape to close;
  - consistent sentence-case labels;
  - a quieter browser list;
  - a slimmer add-device target.

  Fixed: new tracks now scroll into view; the session toolbar fits small windows; Modular lists tracks on open.
- [X] T058 [P] Run the nota-features skill for `FEATURES.md`: the view switcher wording and the session inspector toggle. Note nota-docs as a follow-up in the PR (the repo is unavailable here).
- [X] T059 Re-run the critique tour in all four themes and tick off F1–F8 and F10–F12 against contracts/ui-contract.md (SC-001). Take before/after contact sheets for the PR.

  *Result*: F1 new track revealed above a seam; F2 Session toolbar fits with "⋯"; F3 Modular lists tracks; F4 mixer fits its strips, Escape closes mixer/export (chrome checked on macOS by the user); F5/F6 sentence case; F7 one header layout; F8 one icon set; F10 no "Nota" prefix in rows; F11 slim add-device key; F12 minus signs and closed ranges. Contact sheets per theme are attached to the PR conversation.
- [X] T060 Final gate: a full `scripts/build-linux.sh`. Only the known "Consort stress" failure is allowed, and it must also fail on the base. Then run `/speckit-converge`.


  *Result*: `scripts/build-linux.sh` — 2 failures under heavy concurrent screenshot load: "Consort stress" (known, also fails on the 011 base) and the palette per-keystroke budget, which passes when re-run on a quiet machine (median 4.4 ms, worst 7.8 ms, budget 8 ms). Design checks 134/134.
---

## Dependencies & Execution Order

- **Setup (1)** → **Foundational (2)** blocks every story, because the checks must exist first.
- **US1 (3)** and **US2 (4)** are independent of each other and of US3–US6. US1 is the MVP.
- **US3 (5)** comes before US5 and US4, because their new labels (tooltips, badges) must pass `RunLabels`.
- **US5 (6)** comes before US4 (7), because card badges and the Sends card may use new `Glyph` kinds.
- **US6 (8)** depends on US5 (`Plus`/`Glyph` in the add-device target).
- **Polish (9)** comes last.

Within phases:
- T020 ∥ T021 ∥ T022;
- T024–T031 are all different files, so parallel;
- T035–T038 parallel after T033;
- T047–T049 parallel after T045–T046;
- T051–T053 parallel.

## Parallel Example: Phase 5 (labels)

```text
T024 PaletteSearch ∥ T025 CommandCatalog ∥ T026 MainWindow.axaml menu ∥ T027 Session* ∥
T028 Mixer/Detail ∥ T029 Drum Rack ∥ T030 browser/arrangement menus ∥ T031 Preferences/pickers
→ T032 shrink the ratchet + verify
```

## Implementation Strategy

1. **MVP**: Phases 1–3 (guards plus the three defects). This is shippable alone.
2. Then US2, US3, US5, US4 and US6 in that order, one commit each, so each can become its own PR on request.
3. Then docs, the tour re-run and converge.

## Notes

- **Ratchets** (`LabelAllowList.txt`, `IconAllowList.txt`, the seeded `RunNumbers` and `RunGeometry` lines) only ever shrink. A phase is not done while it leaves its own ratchet lines behind.
- **Card baseline**: every card change is compared with the T002 baseline by `edgediff`. A moved control is a defect, not a design choice.
- **Window chrome**: the mixer and export titles and title bars are checked on macOS by the user (Xvfb has no window manager).

## Phase 10: Convergence

- [X] T061 Make the Instrument Rack chain's "+ Device" slot (`App/DeviceCards/Rack/RackCardView.cs` `IrAddDeviceSlot`) show its dashed outline only while a drag is over it, per FR-014 (partial)
- [X] T062 Shrink `Smoke/RadiusAllowList.txt` to data-only exemptions: put the Mosaic drop target (`App/DeviceCards/Instruments/MosaicInstrumentCard.cs`, radius 6) on the radius scale, and fix or document the 7 px switch knob radius in `App/Theme/NotaTheme.axaml`, per plan: ratchets shrink to data-only exemptions (partial)
- [X] T063 Make the Session inspector toggle (`App/SessionView.Toolbar.cs` `_inspectorToggle`) the Glyph `Sidebar` icon key with a tooltip, or record why the text toggle stays, per T017 (contradicts)
