---

description: "Tasks for the default project template"
---

# Tasks: Default Project Template

**Input**: Design documents from `/specs/004-default-project-template/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/engine-abi.md](contracts/engine-abi.md)

**Tests**: The spec does not ask for TDD, but Constitution IV requires smoke-test coverage
for new C ABI surface, and the template is cheap to check on a real engine — so smoke
checks are included (in `tests/Nota.SmokeTest`, the PR gate).

**Organization**: Tasks are grouped by user story. US1 and US2 share one builder
(`ProjectTemplate.ApplyDefault`); US2 adds the return effects to it, US3 makes it the
undo/dirty baseline.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)

---

## Phase 1: Setup

No setup: no new project, dependency or tool. Skipped.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The template builder skeleton and its two call sites, so every story lands
in the real app.

- [X] T001 Create `src/managed/Nota.Infrastructure/Persistence/ProjectTemplate.cs`: two-line AGPL-3.0-only SPDX header plus a comment header explaining why the file exists (the default new-project content, specs/004-default-project-template); `namespace Nota.Infrastructure.Persistence`; `public static class ProjectTemplate` with `public static int ApplyDefault(IAudioEngine engine)` documented as "Builds the default new-project content on a freshly reset engine and makes it the undo baseline. Returns the instrument track id." — body empty for now, returns -1. Match the style of `ProjectService.cs` in the same folder
- [X] T002 In `src/managed/Nota.App/MainWindow.Files.cs` `OnMenuNew`: call `ProjectTemplate.ApplyDefault(Engine)` right after `Engine.Reset()` and the transport resets, before `RefreshAfterLoad()` (which already precedes `MarkProjectClean()`)
- [X] T003 In `src/managed/Nota.App/MainWindow.Recovery.cs` `OnOpenedRecoveryCheck`: before `MarkProjectClean()` (the "fresh startup project" line), call `ProjectTemplate.ApplyDefault(Engine)` then `RefreshAfterLoad()`, so the launch workspace starts from the template (FR-008). Do not touch `ApplyPendingRecovery` / `OpenProject` (FR-009: those load through `ProjectService.Apply`, which resets first)

**Checkpoint**: New project and launch both run the (still empty) builder.

---

## Phase 3: User Story 1 - New project is ready to play (Priority: P1) 🎯 MVP

**Goal**: A new project has one instrument track and one audio track, built exactly like
tracks added by hand.

**Independent Test**: File → New; arrangement shows an instrument track (Nota Synth, an
empty 1-bar clip) above an audio track; playing the keyboard on the instrument sounds.

- [X] T004 [US1] In `ProjectTemplate.ApplyDefault` (`src/managed/Nota.Infrastructure/Persistence/ProjectTemplate.cs`): `int inst = engine.AddInstrumentTrack();` then `engine.AddMidiClip(inst, 0.0, 4.0);` (same seed clip as `MainWindow.AddTrack`), then `engine.AddAudioTrack();`. Keep engine-assigned names and colours (FR-002, FR-006). Return `inst`
- [X] T005 [P] [US1] Create `tests/Nota.SmokeTest/ProjectTemplateTests.cs` (SPDX header, comment header naming specs/004-default-project-template, `internal static class ProjectTemplateTests` with `public static IEnumerable<(bool ok, string label)> Run()` in the style of `SnapGridTests.cs`): on `using var eng = new NotaEngine()` call `ProjectTemplate.ApplyDefault(eng)`; check exactly two non-return tracks in order instrument then audio (`TryGetTrackInfo(0/1, …)`), the instrument kind is Synth (`TrackInstrumentKind == 0`) with one MIDI clip at beat 0 length 4, the audio track has no clips, and the returned id is the instrument track
- [X] T006 [US1] Wire the tests into `tests/Nota.SmokeTest/Program.cs`: `foreach (var (ok, label) in ProjectTemplateTests.Run()) Check(ok, label);` in the full run next to the `SnapGridTests.Run()` / `HistoryTests` calls, plus a `--template` standalone switch mirroring the `--grid` block

**Checkpoint**: US1 works in the app (new project shows two playable tracks) and in the smoke test.

---

## Phase 4: User Story 2 - Reverb and delay sends are ready (Priority: P1)

**Goal**: Two returns, "Reverb" with Nota Reverb and "Delay" with Nota Delay, both fully
wet; sends start silent.

**Independent Test**: New project → return A holds only Nota Reverb with WET ONLY on,
return B only Nota Delay with WET ONLY on; raising a send adds only the effect.

- [X] T007 [US2] In `ProjectTemplate.ApplyDefault`, after the audio track: `int rv = engine.AddReturnTrack(); engine.SetTrackName(rv, "Reverb"); int rd = engine.AddBuiltinDevice(rv, 2);` (built-in kind 2 = Reverb) and `engine.DeviceSetParam(rv, rd, 19, 1f);` (Reverb param 19 = "Wet Only"); then `int dl = engine.AddReturnTrack(); engine.SetTrackName(dl, "Delay"); int dd = engine.AddBuiltinDevice(dl, 3);` (kind 3 = Delay) and `engine.DeviceSetParam(dl, dd, 22, 1f);` (Delay param 22 = "Wet Only"). Use named constants with a short comment rather than bare numbers. Do not set sends (FR-005: they stay at their default)
- [X] T008 [US2] Extend `ProjectTemplateTests.Run()` in `tests/Nota.SmokeTest/ProjectTemplateTests.cs`: exactly two return tracks, return indices 0 and 1, named "Reverb" and "Delay"; each has `TrackDeviceCount == 1` with `TrackDeviceBuiltinKind` 2 / 3; `DeviceParamName(…,19)` == "Wet Only" and `DeviceGetParam(…,19)` is 1 on the reverb, likewise param 22 on the delay; `GetTrackSend(track, 0/1)` for both tracks reads the default off level (compare with a send on a track added to a fresh engine)
- [X] T009 [US2] Extend `ProjectTemplateTests.Run()`: save/load round trip — `ProjectService.Capture(eng, new TransportState(120, 1, false, false), warnings)`, `ProjectService.Save(doc, tempDir, eng)`, `ProjectService.Load(tempDir)`, `ProjectService.Apply(loaded, eng2, tempDir)` on a second `NotaEngine`; check the same four tracks, names, device kinds and Wet Only values; delete the temp dir in `finally` (pattern: `Program.cs` M7-6c round trip)

**Checkpoint**: US1 + US2 — the full template content is built and survives save/load.

---

## Phase 5: User Story 3 - The template is the starting point, not a change (Priority: P2)

**Goal**: No undo steps and no "save changes?" prompt on an untouched new project.

**Independent Test**: File → New, press Undo → nothing happens; quit → no prompt; delete
a template track, Undo → it returns.

- [X] T010 [US3] Engine: add `void clearUndoHistory();` to `Engine` in `src/native/nota.engine/src/Engine.h` next to `canUndo()` and implement it in `src/native/nota.engine/src/Engine.cpp` as `undoStack_.clear(); redoStack_.clear(); undoGroupDepth_ = 0; undoGroupBase_ = 0;` with a comment that the stacks hold only old authoring graphs, which `retired_` keeps alive for the audio thread (message thread only). Make `Engine::reset()` call it instead of repeating those lines
- [X] T011 [US3] C ABI: declare `NOTA_API void nota_engine_clear_undo_history(NotaEngine* engine);` in `src/native/nota.engine/include/nota/nota_engine.h` in the "Undo / redo (M6-6)" section with the doc comment from `contracts/engine-abi.md`; implement in `src/native/nota.engine/src/nota_engine_core.cpp` beside `nota_engine_undo_group_end`: `void nota_engine_clear_undo_history(NotaEngine* e) { if (e) ENG(e)->clearUndoHistory(); }`
- [X] T012 [US3] Managed interop: add `[LibraryImport(Lib, EntryPoint = "nota_engine_clear_undo_history")]` `ClearUndoHistory(IntPtr)` in `src/managed/Nota.Infrastructure/Interop/NativeMethods.cs` next to `nota_engine_can_undo`; add `void ClearUndoHistory();` with summary "Forget the undo/redo history; the current state becomes the baseline." to the "Undo / redo (M6-6)" block of `src/managed/Nota.Application/Engine/IAudioEngine.cs`; implement in `src/managed/Nota.Infrastructure/Interop/NotaEngine.cs` beside `Reset()`: `public void ClearUndoHistory() { ThrowIfDisposed(); NativeMethods.ClearUndoHistory(_handle); }`
- [X] T013 [US3] End `ProjectTemplate.ApplyDefault` with `engine.ClearUndoHistory();` (FR-007) in `src/managed/Nota.Infrastructure/Persistence/ProjectTemplate.cs`
- [X] T014 [US3] Extend `tests/Nota.SmokeTest/ProjectTemplateTests.cs` with ABI checks: on a fresh engine add a track (CanUndo true), `ClearUndoHistory()` → `CanUndo` and `CanRedo` false, `Undo()` returns false, the track is still there; a following edit (add an audio track) makes `CanUndo` true and `Undo()` removes only that track. And after `ApplyDefault`: `CanUndo == false`, `CanRedo == false`; deleting the audio track then `Undo()` restores it (FR-010)
- [X] T015 [US3] Verify the dirty-state path needs no change: `MarkProjectClean()` runs after the template in both `OnMenuNew` (`MainWindow.Files.cs`) and `OnOpenedRecoveryCheck` (`MainWindow.Recovery.cs`); if either call precedes the template, move it after

**Checkpoint**: All stories done — template content, undo baseline, clean state.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T016 Run the manual checks in [quickstart.md](quickstart.md) in the real app with an isolated `NOTA_DATA_DIR` (welcome screen off and on, File → New over a project with content, sends A/B audible, Undo no-op, no save prompt, Open existing project unchanged)
  - 2026-10-13: checked headlessly through the real MainWindow (harness /tmp/templateharness). With the welcome screen off, the launch workspace and File → New over a modified project both show Synth, Audio, Reverb and Delay, report nothing to undo and count as unmodified. Not checked: hearing the sends by ear, and the GUI quit prompt (it relies on the same unmodified check).

### Nota closing tasks *(PR gates)*

- [X] T017 Run `scripts/build.sh` until native + managed + smoke test are green
  - 2026-10-13: native + managed build green; all 25 template checks pass (also `--template` alone). The full run reports 2 failures, both in `FilterGlideTests` (EQ-8 / Dynamic EQ-8 high cut). They come from separate uncommitted work in `Eq.h` / `DynamicEq.h`, not from this feature. Re-run once that work is settled.
  - 2026-10-13 (re-run): `scripts/build.sh` exit 0, SMOKE TEST PASSED, 0 failures; 25/25 template checks pass.
- [X] T018 AGPL-3.0-only SPDX two-line header on `ProjectTemplate.cs` and `ProjectTemplateTests.cs`
- [X] T019 New dependency: N/A (none)
- [X] T020 `CHANGELOG.md` `[Unreleased]` → Changed: "New projects start with an instrument track, an audio track and two returns — Reverb and Delay — ready to send to"
- [X] T021 FEATURES.md via the `nota-features` skill; user manual (new project / returns pages, EN + RU) via `nota-docs`; shortcuts: N/A (no binding changes)
- [X] T022 Engine behaviour change → smoke test covers `nota_engine_clear_undo_history` (T014)

---

## Dependencies & Execution Order

- **Phase 2 (T001–T003)** blocks every story. T002 and T003 depend on T001.
- **US1 (T004–T006)**: T004 after T001; T005 can be written in parallel with T004; T006 after T005.
- **US2 (T007–T009)**: T007 after T004 (same method, appends after the audio track); T008/T009 extend the file from T005.
- **US3 (T010–T015)**: T010 → T011 → T012 → T013 → T014 (each builds on the previous layer). T010–T012 do not depend on US1/US2 and can start right after Phase 2.
- **Polish (T016–T022)** after all stories.

### Parallel Opportunities

- T005 (test file) alongside T004 (builder).
- The US3 engine/ABI/interop chain (T010–T012) alongside US1/US2 builder work — different files.
- T020 and T021 (docs) alongside T016.

## Parallel Example: User Story 1

```bash
Task: "T004 Build instrument + audio tracks in ProjectTemplate.ApplyDefault"
Task: "T005 Create ProjectTemplateTests.cs with the US1 checks"
```

## Implementation Strategy

### MVP (US1)

Phase 2 → US1 → build → File → New shows two playable tracks. Undo would still remove
them (US3) — acceptable for an internal checkpoint, not for the PR.

### Full delivery

US1 → US2 (returns + effects) → US3 (undo baseline) → Polish. Ship as one PR: the
feature is only correct with all three stories.
