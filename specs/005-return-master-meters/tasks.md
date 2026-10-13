---

description: "Tasks for Return & Master track levels"
---

# Tasks: Return & Master Track Levels

**Input**: Design documents from `/specs/005-return-master-meters/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/ui-footer-row.md](contracts/ui-footer-row.md), [quickstart.md](quickstart.md)

**Tests**: The spec does not ask for TDD and the engine is untouched (no smoke-test
extension). Verification is the headless Avalonia harness from
[quickstart.md](quickstart.md), run per story checkpoint and in the final phase.

**Organization**: Tasks are grouped by user story. Almost all code lives in
`src/managed/Nota.App/ArrangementView.cs`, so tasks inside it are sequential; the
`MixerView` and `MainWindow` changes can run in parallel with them.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US4)

---

## Phase 1: Setup

No setup: no new project, file, dependency or tool. Skipped.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The footer row's new skeleton and the master-volume plumbing every story
builds on.

- [X] T001 In `src/managed/Nota.App/ArrangementView.cs`, extract the track header's dB formatting (`ShowDb` in `BuildTrackCard`, and the duplicate in `UpdateMeters`) into one `private static string VolumeDbText(double v)` returning `"−∞"` when `v <= 0.0011`, else `AudioMath.LinToDb(v).ToString("0.0", NotaNum.Culture)`; use it in both places (note: `UpdateMeters` currently formats without `NotaNum.Culture` — the helper fixes that)
- [X] T002 In `src/managed/Nota.App/ArrangementView.cs`, add footer-only state next to `_meters`/`_volFaders`/`_volDb`: `_footerMeters : Dictionary<int, MeterBar>`, `_footerFaders : Dictionary<int, MiniFader>`, `_footerDb : Dictionary<int, TextBlock>`, and `_masterRowMeter : MeterBar?`, `_masterRowFader : MiniFader?`, `_masterRowDb : TextBlock?`; clear/null all of them at the top of `RebuildFooterHeaders()` (never in the track-header rebuild that clears `_meters`)
- [X] T003 In `src/managed/Nota.App/ArrangementView.cs`, add public callbacks `public Func<float>? GetMasterVolume { get; set; }` and `public Action<float>? SetMasterVolume { get; set; }` (same shape as `SessionView.Mixer.cs`), with a one-line comment that `TransportViewModel.MasterVolume` owns the master volume
- [X] T004 [P] In `src/managed/Nota.App/MainWindow.axaml.cs`, next to the other `Timeline.*` hooks (~line 189), wire `Timeline.GetMasterVolume = () => (float)vm.Transport.MasterVolume;` and `Timeline.SetMasterVolume = v => vm.Transport.MasterVolume = v;`
- [X] T005 In `src/managed/Nota.App/ArrangementView.cs`, restructure `BuildSlimRow(title, sub, colorIndex, trackId, bgKey, masterSpine)` per [contracts/ui-footer-row.md](contracts/ui-footer-row.md): keep the 3 px spine, card size `HeaderW × FooterRowH` (40), background, selection, right-click menu and `Select` on press; replace the single `Grid` row with a `DockPanel` whose right dock is a slot for the meter and whose content is a two-line `Grid` (row 0: name `*` with `TextTrimming = CharacterEllipsis` + dB `TextBlock` Mono 9, `Brush.TextTertiary`, Width 30, right-aligned; row 1: a slot for the fader), content padding 8,4. Return the parts the callers fill (e.g. an out record with the dock panel, the dB `TextBlock` and the fader slot). Remove the constant `masterSpine ? "0.0" : "−∞"`

**Checkpoint**: The footer renders as before but with the two-line layout and empty slots; app builds.

---

## Phase 3: User Story 1 — See the return and master levels while playing (Priority: P1) 🎯 MVP

**Goal**: Live meters on every Return row and on the Master row.

**Independent Test**: Play with a send raised to a return; the return and master row meters move and match the Mixer strips; a muted return stays empty.

- [X] T006 [US1] In `ArrangementView.RebuildFooterHeaders()` (`src/managed/Nota.App/ArrangementView.cs`), for each return create `new MeterBar { VerticalAlignment = VerticalAlignment.Stretch, Width = 8, Margin = new Thickness(0, 6, 3, 6) }` — exactly the track header meter — dock it right in the row and register it in `_footerMeters[r.Id]`; for the master row create the same and store it in `_masterRowMeter`
- [X] T007 [US1] In `ArrangementView.UpdateMeters()` (`src/managed/Nota.App/ArrangementView.cs`), push readings: for each `_footerMeters` entry `eng.TryGetTrackMeter(id, out var m) ? bar.Push(m) : bar.Reset()`; `_masterRowMeter?.Push(eng.MasterMeter())` (NOT `TryGetTrackMeter(MasterTrackId)` — that is the master effect-chain track whose meter is never written, research R2)
- [X] T008 [US1] Headless check per [quickstart.md](quickstart.md): footer meters exist for each return + master, `UpdateMeters` pushes non-zero engine readings into them during playback (or with synthetic readings where audio can't be pumped), muted return → empty

**Checkpoint**: MVP — meters live on Return and Master rows.

---

## Phase 4: User Story 2 — The dB number tells the truth (Priority: P1)

**Goal**: The number on Return/Master rows shows the row's real volume in dB and follows every change.

**Independent Test**: Default return reads `0.0`; changing return/master volume from the Mixer, MST or MCP updates the number; silence reads `−∞`.

- [X] T009 [US2] In `ArrangementView.RebuildFooterHeaders()`, set each return's dB text from `r.Volume` (`TrackVM.Volume`, already filled in `Refresh`) via `VolumeDbText`, and register it in `_footerDb[r.Id]`; set the master's from `GetMasterVolume?.Invoke() ?? 1f` and store it in `_masterRowDb`
- [X] T010 [US2] In `ArrangementView.UpdateMeters()`, inside the existing `for (i < eng.TrackCount)` loop, also refresh `_footerDb[ti.Id]` from `ti.Volume` (when changed by > 1e-3 from the last shown value); after the loop refresh `_masterRowDb` from `GetMasterVolume()` — this covers Mixer, MST, MCP, Remote, return automation and project load
- [X] T011 [US2] Headless check: default template returns read `0.0`; `engine.SetTrackVolume(returnId, 0.5f)` + `UpdateMeters()` → `-6.0`; `vm.Transport.MasterVolume = 0.0` → master `−∞`

**Checkpoint**: Return/Master numbers are truthful (the bug half is fixed).

---

## Phase 5: User Story 4 — Adjust return and master volume in place (Priority: P2)

**Goal**: Mini volume faders on Return/Master rows; master volume has one owner everywhere (FR-006, FR-007).

**Independent Test**: Drag a return fader → return volume + Mixer change; drag the master fader → MST follows; Mixer master change survives save/reload.

- [X] T012 [US4] In `ArrangementView.RebuildFooterHeaders()`, put a `MiniFader(volume) { VerticalAlignment = VerticalAlignment.Center, Default = 1.0 }` in each row's fader slot. Return: `ValueChanged += v => { _engine?.SetTrackVolume(id, (float)v); db.Text = VolumeDbText(v); }`, register in `_footerFaders[r.Id]`. Master: initial `GetMasterVolume?.Invoke() ?? 1f`, `ValueChanged += v => { SetMasterVolume?.Invoke((float)v); db.Text = VolumeDbText(v); }`, store in `_masterRowFader`. No `BeginAutomationWrite`/`EndAutomationWrite` (spec FR-006). `MiniFader` marks its press handled, so the card's select-on-press doesn't rebuild the row mid-drag — keep it that way
- [X] T013 [US4] In `ArrangementView.UpdateMeters()`, sync faders like the track headers do: `_footerFaders[ti.Id]` from `ti.Volume` when `!f.Dragging && |f.Value − ti.Volume| > 1e-3` (return automation, Mixer, MCP); `_masterRowFader` from `GetMasterVolume()` under the same rule; update the paired dB text with `VolumeDbText` (merge with T010 so each value is formatted once)
- [X] T014 [P] [US4] In `src/managed/Nota.App/MixerView.cs`, add `GetMasterVolume` / `SetMasterVolume` callbacks, remove the private `_masterVolume`; the master strip's `FaderRow(-1, …)` starts from `GetMasterVolume?.Invoke() ?? 1f`, `SetVolume` for `id < 0` calls `SetMasterVolume?.Invoke((float)v)` (no direct `_engine.SetMasterVolume`); in `UpdateMeters()` sync the master fader + dB from `GetMasterVolume()` when not dragging (keep a reference to the master fader/dB `TextBlock` from `FaderRow`)
- [X] T015 [P] [US4] In `src/managed/Nota.App/MainWindow.axaml.cs`, at `_mixer = new MixerView(vm.Engine) { … }` (~line 369) set `GetMasterVolume = () => (float)vm.Transport.MasterVolume, SetMasterVolume = v => vm.Transport.MasterVolume = v` (depends on T014)
- [X] T016 [US4] Headless check (synthetic pointer drags, memory recipe): return fader drag changes engine return volume and the dB text; master fader drag changes `vm.Transport.MasterVolume` and the MST read-out; double-click resets to `0.0`; Mixer master change → save → load → Master row, MST and Mixer show the saved value

**Checkpoint**: Faders work; master volume is one value across Mixer, MST and the Master row.

---

## Phase 6: User Story 3 — Return and Master rows look like track rows (Priority: P2)

**Goal**: Visual consistency with track headers in both themes.

**Independent Test**: Side-by-side screenshots of a return row and a track header in Ember Graphite and Ember Paper.

- [X] T017 [US3] Headless screenshots of the arrangement footer + one track header in Ember Graphite and Ember Paper (`NotaThemeService.Set(AppTheme.Light)` by reflection); check against `DESIGN.md` § meters and [contracts/ui-footer-row.md](contracts/ui-footer-row.md): row height 40, meter 28 px tall at the right, name trims before dB/meter shrink, selected row keeps a visible working meter; adjust paddings/margins in `BuildSlimRow` (`src/managed/Nota.App/ArrangementView.cs`) if anything clips
- [X] T018 [US3] Edge cases from spec: add a third return, delete the first, rename/recolour a return, select rows repeatedly — each remaining row's meter/fader addresses its own return; project with no returns shows only a metering Master row; no exceptions in the log

**Checkpoint**: All four stories done.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T019 Run the full [quickstart.md](quickstart.md) table once more on the final code
- [X] T020 Remove every temporary harness hook / `// TEMP-VERIFY` line from the repo (harness itself lives under `/tmp`)

### Nota closing tasks *(PR gates)*

- [X] T021 Run `scripts/build.sh` until native + managed + smoke test are green
- [X] T022 AGPL-3.0-only SPDX header on every new source file — N/A (no new source files)
- [X] T023 New dependency — N/A
- [X] T024 `CHANGELOG.md` `[Unreleased]`: Added — level meter and volume fader on Return and Master rows in the arrangement; Fixed — Return rows always showed −∞ and Master 0.0; Fixed — master volume set in the Mixer started at 0 dB, was not saved and did not show in the transport
- [X] T025 FEATURES.md via the `nota-features` skill; user docs (arrangement page, Return/Master rows; EN + RU) via `nota-docs`; shortcuts — N/A (no new bindings)
- [X] T026 Engine behaviour change → smoke test — N/A (engine untouched)

---

## Dependencies & Execution Order

- **Foundational (T001–T005)** blocks all stories. T004 is [P] (different file).
- **US1 (T006–T008)** needs T002, T005.
- **US2 (T009–T011)** needs T001, T002, T003, T005; independent of US1.
- **US4 (T012–T016)** needs T003–T005 and US2's read-out (T009/T010 — the fader writes the same dB text). T014 → T015.
- **US3 (T017–T018)** needs US1 + US2 + US4 rendered (it checks the finished row).
- **Polish (T019–T026)** last.

Story order: US1 → US2 → US4 → US3 (US3 is a check of the finished row, so it moves after US4).

## Parallel Opportunities

- T004 (`MainWindow.axaml.cs`) alongside T001–T003/T005 (`ArrangementView.cs`).
- T014 (`MixerView.cs`) alongside T012–T013 (`ArrangementView.cs`); T015 follows T014.
- Everything else touches `ArrangementView.cs` → sequential.

## Parallel Example: User Story 4

```text
Task: "T012–T013 footer faders in src/managed/Nota.App/ArrangementView.cs"
Task: "T014 master volume callbacks in src/managed/Nota.App/MixerView.cs"
```

## Implementation Strategy

1. Foundational → US1 (meters) → validate: **MVP**, the visible gap is closed.
2. US2 → the wrong numbers are gone (the bug half of the request).
3. US4 → faders + the Mixer master-volume fix.
4. US3 → visual pass in both themes, edge cases.
5. Polish + closing gates.

## Notes

- Out of scope (plan § Out of Scope): pan on these rows, automation write, live automated master volume, MIDI-learn master-volume bypass.
- Don't commit — the user reviews and commits.
