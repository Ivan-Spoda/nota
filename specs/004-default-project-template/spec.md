# Feature Specification: Default Project Template

**Feature Branch**: `004-default-project-template`

**Created**: 2026-10-13

**Status**: Draft

**Input**: User description: "По умолчанию при создании нового проекта добавлять 1 инструментальную дорожку, 1 аудиодорожку и 2 return-дорожки: в первом return — reverb, во втором — delay."

## Nota Context *(fill in — drives plan and tasks)*

**Feature area** *(pick the main one)*:

- [x] Engine (C++, `src/native/nota.engine`) — real-time audio rules apply, see `ARCHITECTURE.md` AR table
- [x] Managed app (C#, `src/managed/*`) — domain, application, infrastructure, presentation
- [ ] UI (Avalonia, `src/managed/Nota.Presentation`) — follow `DESIGN.md` (Ember)
- [ ] Plugin hosting (`pluginhost/`) — the only place JUCE is allowed
- [ ] Build / packaging / CI (`scripts/`, `.github/`) — see `BUILD.md`
- [ ] Docs only

The new project is assembled from operations the app already uses when the user adds
tracks and devices by hand. The engine gains one small call (see plan): forgetting the
undo history, so the template becomes the undo baseline.

**Nota docs to keep in mind** *(links relative to `specs/<slug>/`, where this file lands)*:
[`ARCHITECTURE.md`](../../ARCHITECTURE.md) (AR rules),
[`CONTRIBUTING.md`](../../CONTRIBUTING.md) (licensing, dependency policy, PR
expectations), [`BUILD.md`](../../BUILD.md), [`DESIGN.md`](../../DESIGN.md),
[`FEATURES.md`](../../FEATURES.md).

**Current state (for reference)**:

- File → New (and "New project" on the welcome screen) resets the engine and transport
  and leaves an empty arrangement: no tracks, no returns.
- On app launch the workspace also starts as an empty, unmodified project, before or
  without the welcome screen.
- Adding an instrument track, an audio track or a return track by hand, and putting the
  built-in Reverb or Delay on a track, are all existing actions.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - New project is ready to play (Priority: P1)

A user picks File → New (or "New project" on the welcome screen). Instead of an empty
arrangement they get one instrument track, one audio track and two return tracks. They
can immediately play notes on the instrument track or drop a sample on the audio track
without creating anything first.

**Why this priority**: It is the core of the request — removing the same four setup
steps from every new project.

**Independent Test**: Create a new project; count the tracks and their kinds in the
arrangement and the mixer.

**Acceptance Scenarios**:

1. **Given** any open project, **When** the user creates a new project, **Then** the
   project contains exactly one instrument track, one audio track and two return tracks,
   in that order, and nothing else.
2. **Given** a new project, **When** the user arms the instrument track and plays the
   computer keyboard or a MIDI keyboard, **Then** they hear the instrument with no
   further setup.
3. **Given** a new project, **When** the user drops an audio file on the audio track,
   **Then** the clip lands on that track as on any audio track.

---

### User Story 2 - Reverb and delay sends are ready (Priority: P1)

The two return tracks come with effects: the first holds a reverb, the second a delay.
The user turns up a track's send to return A to add space, or to return B to add echoes,
without building the return chains by hand.

**Why this priority**: Part of the explicit request; returns without effects would not
deliver the value.

**Independent Test**: Create a new project, open each return's device chain; raise the
instrument track's send to each return while playing and listen.

**Acceptance Scenarios**:

1. **Given** a new project, **When** the user opens the first return's devices, **Then**
   it holds exactly one device, the built-in Reverb.
2. **Given** a new project, **When** the user opens the second return's devices,
   **Then** it holds exactly one device, the built-in Delay.
3. **Given** a new project, **When** the user plays the instrument track with every
   send at its default, **Then** the sound is dry — no reverb or delay is heard until a
   send is raised.
4. **Given** a new project, **When** the user raises the instrument track's send to the
   reverb return, **Then** only the reverberated signal is added on top of the dry
   track signal (the return effect is fully wet, the dry signal is not doubled).

---

### User Story 3 - The template is the starting point, not a change (Priority: P2)

The prepared tracks are the project's starting state. The project is not marked as
modified, and undo cannot take the template apart.

**Why this priority**: Without it, closing a just-created project would ask to save,
and the first Undo would delete a track the user never added.

**Independent Test**: Create a new project, try to close it, then press Undo.

**Acceptance Scenarios**:

1. **Given** a just-created new project, **When** the user quits or opens another
   project, **Then** no "save changes?" prompt appears.
2. **Given** a just-created new project, **When** the user presses Undo, **Then**
   nothing is undone (the history is empty).
3. **Given** a just-created new project, **When** the user deletes, renames or edits
   any of the template tracks, **Then** it behaves like any track the user added
   themselves, and the change is undoable.

---

### Edge Cases

- App launch: the empty startup workspace gets the same template, so launching with the
  welcome screen switched off also opens a ready project (see Assumptions).
- Opening an existing project or recovering a crash snapshot never adds template tracks;
  the project loads exactly as saved.
- Opening a project and then creating a new one replaces everything with the template —
  nothing from the previous project leaks in (tracks, sends, devices, MIDI mappings).
- A new project followed immediately by Save writes the template tracks into the project
  file like any other tracks; reopening shows the same four tracks and two devices.
- Creating a new project while playback runs stops/resets transport as today, then
  builds the template.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Creating a new project (File → New, the welcome screen's "New project",
  and the equivalent shortcut) MUST produce a project with exactly these tracks, top to
  bottom: one instrument track, one audio track; and exactly two return tracks.
- **FR-002**: The instrument track MUST carry the same default instrument a user gets
  when adding an instrument track by hand, so it is playable at once.
- **FR-003**: The first return track MUST contain the built-in Reverb as its only
  device; the second return track MUST contain the built-in Delay as its only device.
- **FR-004**: Both return effects MUST be set fully wet, so a send adds only the
  effected signal.
- **FR-005**: All sends from the instrument and audio tracks to both returns MUST start
  at their default (silent) level.
- **FR-006**: Track names, colours and other properties of the template tracks MUST
  follow the same defaults as tracks added by hand, except that the returns are named
  after their effect ("Reverb", "Delay").
- **FR-007**: A freshly created project MUST be unmodified (no save prompt) and have an
  empty undo history.
- **FR-008**: The empty workspace shown on app launch MUST start from the same template.
- **FR-009**: Opening an existing project, recovering a crash snapshot or switching to a
  saved version MUST NOT add any template tracks.
- **FR-010**: Template tracks MUST be ordinary tracks: they can be renamed, re-ordered,
  edited and deleted like any other, and are saved and reloaded like any other.

### Key Entities

- **Default project template**: the fixed starting content of a new project — an
  instrument track, an audio track, a "Reverb" return with the built-in Reverb and a
  "Delay" return with the built-in Delay.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In 100% of new projects the user can hear the instrument track within one
  action (arm/select and play), with zero tracks or devices to create first.
- **SC-002**: Adding reverb or delay to a track in a new project takes one action
  (raising a send) instead of the current four or more (add return, add device, set mix,
  raise send).
- **SC-003**: Creating a new project is perceived as instant — no noticeable delay
  compared with today's empty new project.
- **SC-004**: 0 "save changes?" prompts and 0 undo steps on an untouched new project.

## Assumptions

- The template is fixed and not configurable in this feature; a user-editable template
  ("save as default project") or a Preferences switch to get an empty project is out of
  scope and can follow later.
- "New project" also covers the empty project on app launch — otherwise users who skip
  the welcome screen would still start empty, which contradicts the intent.
- The return effects use their own default settings apart from the wet/dry mix; no
  special "send" presets are introduced.
- Return order is A = Reverb, B = Delay, matching the request ("в первом return —
  reverb, во втором — delay").
- The MCP tools and the remote control see the template tracks as ordinary tracks; no
  MCP change is needed beyond what follows from the new starting state.

## Nota Compliance Checklist *(review before implementation; re-check in tasks.md)*

- [ ] Every new source file will carry the AGPL-3.0-only SPDX header (two lines, see CONTRIBUTING.md)
- [x] No new dependency — or: license checked, AGPL-compatible, `LICENSES/third-party.md` row planned
- [x] Audio-thread impact considered: no alloc / lock / I/O on the audio thread (AR-6), command queue for data in (AR-4), immutable snapshots for structural edits (AR-5) — reuses existing track/device add operations only
- [x] Managed/native crossings go through the pure C ABI in `nota_engine.h` only (AR-7); no JUCE outside `pluginhost/`
- [ ] Smoke-test impact assessed: engine behaviour change → extend `tests/Nota.SmokeTest` (engine unchanged; assess whether the smoke test relies on an empty new project)
- [ ] User-visible change → `CHANGELOG.md` `[Unreleased]` entry planned (and FEATURES.md / user docs via the nota-features / nota-docs skills)
- [x] UI change → follows `DESIGN.md` (Ember design language) — no new UI
