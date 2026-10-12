# Tasks: Hide the window title bar in fullscreen

**Input**: Design documents from `/specs/002-fullscreen-hide-titlebar/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Not requested — no dedicated test tasks. The existing smoke test covers the
new gesture automatically (PaletteTests asserts every catalog gesture is listed in
Settings → Shortcuts); visual verification follows quickstart.md.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g. US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Managed app (C#)**: `src/managed/Nota.App/`, `src/managed/Nota.Application/`
- **Tests**: `tests/Nota.SmokeTest/` — the end-to-end gate every PR must pass
- Docs: `CHANGELOG.md`, `FEATURES.md`, `../nota-docs/` (sibling repo)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

No setup tasks — the change folds into the existing solution; no new project, file, or
configuration is needed (plan.md § Project Structure).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story

No foundational tasks — the three user stories touch disjoint code paths (MainWindow
state subscription / command pipeline / NotaWindow base) and are independently
implementable, as the spec requires.

**Checkpoint**: user story implementation can begin immediately.

---

## Phase 3: User Story 1 - Title bar vanishes in fullscreen and returns on exit (Priority: P1) 🎯 MVP

**Goal**: Main window's custom title bar band + hairline separator hide when the OS
fullscreen state is entered, content fills to the top edge; both restore on exit.

**Independent Test**: On macOS, click the green traffic light → bar + separator gone,
content reaches the top edge; leave fullscreen → bar back, draggable, double-click still
maximizes (quickstart.md § macOS steps 2–5).

### Implementation for User Story 1

- [x] T001 [US1] Subscribe to window-state changes in `src/managed/Nota.App/MainWindow.axaml.cs` (ctor, next to the existing Linux/extend-chrome block at ~lines 93–102): on every `WindowState` change set `TitleBar.IsVisible = !OperatingSystem.IsLinux() && WindowState != WindowState.FullScreen`. Derived rule only — never track transitions (spec FR-006 rapid-toggle convergence). Linux keeps today's always-hidden behaviour; maximized/normal keep the bar (spec FR-004, decision Q1).
- [x] T002 [US1] Verify US1 per quickstart.md § macOS (steps 2–5): green-button enter/leave, 10× rapid toggles, traffic lights reveal on hover at the top edge, drag + double-click-maximize unchanged after exit. Fix any regression found (same file).

**Checkpoint**: User Story 1 fully functional and independently testable (MVP delivered).

---

## Phase 4: User Story 2 - The behaviour works on every supported OS (Priority: P2)

**Goal**: A `view.fullscreen` command ("Toggle Full Screen", F11) in the View menu gives
Windows (no OS gesture) an in-app fullscreen entry; menu/keyboard/palette/Shortcuts all
share the one catalog definition.

**Independent Test**: On Windows, F11 and View → Toggle Full Screen enter/leave
fullscreen; the window returns to its previous state (Maximized if maximized — the app
default); the View menu shows the F11 gesture (quickstart.md § Windows).

### Implementation for User Story 2

- [x] T003 [US2] Add the catalog row in `src/managed/Nota.Application/Palette/CommandCatalog.cs` (View section, after `view.midiLearn` / before the palette entry): `C("view.fullscreen", "Toggle Full Screen", "View", "F11", "maximize immersive hide chrome")` — exact id/title/gesture per contracts/ui-contract.md.
- [x] T004 [P] [US2] Add the menu item in `src/managed/Nota.App/MainWindow.axaml` View menu, alongside the other View toggles (after "Toggle sections", keeping the separator structure tidy): `<NativeMenuItem Header="Toggle Full Screen" app:Commands.Id="view.fullscreen"/>`.
- [x] T005 [P] [US2] Bind the handler in `src/managed/Nota.App/MainWindow.Commands.cs` (View block): `B("view.fullscreen", ToggleFullscreen);` plus a private `ToggleFullscreen()` — enter: remember current `WindowState` (field, cleared on exit); toggle `WindowState.FullScreen` ⇄ remembered state (Maximized/Normal; never a surprising un-maximize). macOS-native entry (green button) restores natively and never reads the field (research.md D2).
- [x] T006 [P] [US2] Allow bare function keys as menu gestures: add `CanMenuGesture(string glyphs)` (true for chords OR a bare `F1`–`F12` token — function keys can't swallow typing, the reason `IsChord` is chord-only) in `src/managed/Nota.App/Commands.cs`, and use it in place of `Commands.IsChord(cmd.Gesture)` at `src/managed/Nota.App/MainWindow.MenuKeys.cs:42`. Existing bare "0"/"Space" gestures must stay unwired as menu equivalents.
- [x] T007 [P] [US2] Add the Shortcuts pane row in `src/managed/Nota.App/PreferencesWindow.cs` `ShortcutGroups` (~line 989; TRANSPORT group or a WINDOW row near FILE — match surrounding style): `("F11", "Enter / leave full screen — the title bar hides in full screen")`. Without it `tests/Nota.SmokeTest/PaletteTests.cs` fails with "shortcuts missing from Settings → Shortcuts: F11".

**Checkpoint**: Stories 1 AND 2 both work independently (US2's command toggles the state US1 reacts to, but each is testable alone).

---

## Phase 5: User Story 3 - Secondary windows behave consistently (Priority: P3)

**Goal**: `NotaWindow` dialog family (About, What's New, save-changes, …) hides its
shared chrome bar in fullscreen like the main window.

**Independent Test**: Fullscreen About (macOS green button) → its bar + hairline hide;
exit → restore (quickstart.md § macOS step 6).

### Implementation for User Story 3

- [x] T008 [US3] In `src/managed/Nota.App/NotaWindow.cs`, inside the non-Linux custom-chrome branch of the ctor (where `_chromeBar` is built, ~lines 59–97): subscribe to `WindowState` and set `_chromeBar.IsVisible = WindowState != WindowState.FullScreen`. Do NOT touch `DeviceWindow` — its own top band is a functional toolbar (presets/bypass) and stays (spec FR-008, research.md D3).

**Checkpoint**: All user stories independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T009 Run `scripts/build.sh` (or `scripts/build-linux.sh` / `scripts/build-win.ps1`) until native + managed + smoke test are green (includes the F11/Shortcuts consistency assertion from T007).
- [ ] T010 (needs a real desktop) Run the quickstart.md manual matrix on the platforms available (macOS primary; Windows for F11/menu; Linux no-regression).
- [x] T011 [P] Documentation sync: `CHANGELOG.md` `[Unreleased]` entry (user-visible change); FEATURES.md via the `nota-features` skill; nota-docs pages (EN + RU) via the `nota-docs` skill; regenerate the shortcuts docs page via the `nota-shortcuts` skill (new F11 binding).

### Nota closing tasks *(always include — these are the PR gates)*

- [x] T012 `scripts/build.sh` green — covered by T009; re-run after any later fix.
- [x] T013 AGPL-3.0-only SPDX two-line header on every new source file — N/A expected (edits only in existing files); verify none was created.
- [x] T014 New dependency, if any: license checked + row added to `LICENSES/third-party.md` — N/A (none).
- [x] T015 Engine behaviour change → extend `tests/Nota.SmokeTest` to cover it — N/A (UI-only, FR-009; the palette gesture assertion already extends coverage to the new command).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: none exist — skipped.
- **Foundational (Phase 2)**: none exist — skipped.
- **User Stories (Phases 3–5)**: independent of each other; sequential in priority order (US1 → US2 → US3) is natural for one implementer.
- **Polish (Phase 6)**: after all desired stories complete.

### User Story Dependencies

- **US1 (P1)**: no dependencies — start here (MVP).
- **US2 (P2)**: no hard dependency on US1 (the command works standalone; the bar only hides once US1 lands). Within US2: T003 first, then T004–T007 in parallel (different files).
- **US3 (P3)**: no dependencies on US1/US2.

### Within Each User Story

- Catalog/definition before wiring (T003 → T004–T007).
- Derived-state logic before manual verification (T001 → T002).

### Parallel Opportunities

- T004, T005, T006, T007 all touch different files after T003 lands.
- US1 (MainWindow) and US2 (catalog/menu/commands) could proceed in parallel by two implementers.

---

## Parallel Example: User Story 2

```bash
# After T003 (catalog row) lands, launch together:
Task: "Add the menu item in src/managed/Nota.App/MainWindow.axaml"          # T004
Task: "Bind handler + state memory in MainWindow.Commands.cs"               # T005
Task: "CanMenuGesture in Commands.cs + MenuKeys wiring"                     # T006
Task: "Shortcuts pane row in PreferencesWindow.cs"                          # T007
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 3 (US1): T001 → T002 — the core request, demoable on macOS.
2. Then Phase 4 (US2) for the "any OS" promise, Phase 5 (US3) for consistency.
3. Phase 6 gates the PR.

### Incremental Delivery

Each story is a shippable increment: US1 alone already delivers the user-visible ask on
macOS/Linux; US2 makes it true on Windows; US3 polishes the window family.

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- The user makes the commits (project convention) — leave changes staged in the working tree for review
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence

---

## Phase 7: Convergence

- [x] T016 Dispatch F11 on Windows/Linux: bare function keys never reach TryMenuShortcut — the call at src/managed/Nota.App/MainWindow.Input.cs:305 is gated on a modifier (`mod &&`). Add a bare F1–F12 branch that also runs TryMenuShortcut (today F11 is the only such menu gesture, so the Session grid's F2 handling is unaffected), then verify headlessly by raising a KeyEventArgs for F11 on MainWindow and asserting WindowState toggles per FR-007 (partial)
