# Feature Specification: Return & Master Track Levels

**Feature Branch**: `005-return-master-meters`

**Created**: 2026-10-13

**Status**: Draft

## Clarifications

### Session 2026-10-13

- Q: Should Return/Master rows get the volume fader (and pan) of regular track headers? → A: Volume fader yes, pan no (option B).

**Input**: User description: "Уровни громкости на дорожках Return и Master: сейчас Return всегда показывает минус бесконечность, а Master — 0. Нужно, чтобы они показывали реальный уровень, и чтобы на них был такой же meter, как на обычных дорожках."

## Nota Context *(fill in — drives plan and tasks)*

**Feature area** *(pick the main one)*:

- [ ] Engine (C++, `src/native/nota.engine`) — real-time audio rules apply, see `ARCHITECTURE.md` AR table
- [ ] Managed app (C#, `src/managed/*`) — domain, application, infrastructure, presentation
- [x] UI (Avalonia, `src/managed/Nota.Presentation`) — follow `DESIGN.md` (Ember)
- [ ] Plugin hosting (`pluginhost/`) — the only place JUCE is allowed
- [ ] Build / packaging / CI (`scripts/`, `.github/`) — see `BUILD.md`
- [ ] Docs only

**Nota docs to keep in mind** *(links relative to `specs/<slug>/`, where this file lands)*:
[`ARCHITECTURE.md`](../../ARCHITECTURE.md) (AR rules),
[`CONTRIBUTING.md`](../../CONTRIBUTING.md) (licensing, dependency policy, PR
expectations), [`BUILD.md`](../../BUILD.md), [`DESIGN.md`](../../DESIGN.md),
[`FEATURES.md`](../../FEATURES.md).

**Current state (for reference)**:

- In the arrangement, regular track headers carry a live stereo level meter (vertical
  bar at the right edge) and a volume fader with a dB read-out; group headers carry a
  thin horizontal meter.
- Return and Master rows are slim rows pinned under the tracks. They show only a name
  and a fixed text: "−∞" on every return, "0.0" on the master. The text never changes —
  it reflects neither the signal nor the volume. There is no meter.
- The engine already measures return and master levels: the Mixer strips and the
  transport's master meter show them live. So the gap is in the arrangement rows only.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See the return and master levels while playing (Priority: P1)

A user raises a send to the Reverb return and presses play. Next to the return's name
in the arrangement they see a meter move with the reverb signal, the same way the
source track's meter moves. The Master row shows the level of the whole mix.

**Why this priority**: It is the core of the request — today these rows give no
feedback, so the user cannot tell whether a return receives signal or the mix clips
without opening the Mixer.

**Independent Test**: Play a project with a send raised to a return; watch the return
and master rows in the arrangement and compare with the same strips in the Mixer.

**Acceptance Scenarios**:

1. **Given** a playing track with a send to return A, **When** the user looks at
   return A's row, **Then** its meter shows the return's signal, moving in real time.
2. **Given** a return with no signal (sends at zero, or the return muted), **When**
   playback runs, **Then** its meter stays empty.
3. **Given** playback of any non-silent material, **When** the user looks at the
   Master row, **Then** its meter shows the mix level; when playback stops and tails
   ring out, the meter falls back to empty.
4. **Given** the same moment of playback, **When** the user compares a return's or
   the master's meter in the arrangement with the Mixer, **Then** both show the same
   level.
5. **Given** the master is driven above 0 dBFS, **When** it peaks, **Then** the Master
   meter shows the clip indication the regular track meters show.

---

### User Story 2 - The dB number tells the truth (Priority: P1)

The number printed on the Return and Master rows is no longer a fixed placeholder: it
shows the real value, the same quantity that regular track headers show next to their
fader (the track volume in dB).

**Why this priority**: A number that always reads "−∞" on a return that is clearly
audible is misleading; it is the bug half of the request.

**Independent Test**: Change a return's and the master's volume in the Mixer; read
the number on their arrangement rows.

**Acceptance Scenarios**:

1. **Given** a return at its default volume, **When** the user looks at its row,
   **Then** the number reads "0.0", not "−∞".
2. **Given** a return's or the master's volume changes (Mixer, MST module, return
   automation, MCP, project load), **When** the change lands, **Then** the row's
   number updates to the new value in dB.
3. **Given** a volume at silence, **When** the row is shown, **Then** it reads "−∞",
   as regular tracks do.

---

### User Story 3 - Return and Master rows look like track rows (Priority: P2)

The meter on the Return and Master rows looks and behaves like the one on regular
tracks: same colours, scale, peak hold and clip indication, so the arrangement reads
as one consistent column.

**Why this priority**: Explicit in the request ("такой же meter"), but the value is
already delivered by Story 1; this story is about visual consistency.

**Independent Test**: Compare a return row and a track row side by side, in both
Ember Graphite and Ember Paper themes.

**Acceptance Scenarios**:

1. **Given** the arrangement in either theme, **When** the user looks at a return or
   master row, **Then** its meter uses the same visual style as the track header meter
   and fits the row without changing the row's height or clipping the name.
2. **Given** a selected return or master row, **When** it is highlighted, **Then** the
   meter stays visible and keeps working.

---

### User Story 4 - Adjust return and master volume in place (Priority: P2)

A user hears the reverb is too loud and pulls down the Reverb return's fader right on
its arrangement row, without opening the Mixer. Same for the master.

**Why this priority**: Chosen in clarification (option B); a convenience on top of
Stories 1–2.

**Independent Test**: Drag the fader on a return row and on the master row; check the
sound, the dB read-out, the Mixer and Undo.

**Acceptance Scenarios**:

1. **Given** a return row, **When** the user drags its fader, **Then** the return's
   volume changes audibly, the row's dB read-out follows, and the Mixer strip shows the
   same value.
2. **Given** the master row, **When** the user drags its fader, **Then** the master
   volume changes and the transport's MST gain shows the same value.
3. **Given** any of these faders, **When** the user double-clicks it, **Then** it
   resets to 0 dB.
4. **Given** volume automation on a return, **When** playback runs, **Then** the
   return's fader and read-out follow the automated value.
5. **Given** the master volume changed in the Mixer, **When** the user saves and
   reopens the project, **Then** the Master row, the MST module and the Mixer all show
   the saved value.

---

### Edge Cases

- Adding, deleting, renaming or re-colouring a return rebuilds the rows; the meter of
  each remaining return keeps tracking the right return (no meter shows a neighbour's
  level).
- A project with no returns: only the Master row exists and it still meters.
- Transport stopped: return effect tails still ring out and the return and master
  meters show them until they decay.
- A soloed track: returns are exempt from solo, so their meters still move.
- Very narrow header column: name, number and meter stay legible; the name is cut
  before the meter disappears.
- The Mixer and the transport master meter are unaffected.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every Return row in the arrangement MUST show a live level meter of that
  return's output (post its effects and volume), updated at the same rate as regular
  track meters.
- **FR-002**: The Master row in the arrangement MUST show a live level meter of the
  master output, matching the master meter elsewhere in the app.
- **FR-003**: The Return and Master meters MUST look and behave like the regular track
  header meter (stereo, same scale, same peak and clip indication, same colours in both
  themes).
- **FR-004**: The number on Return and Master rows MUST show the row's current volume in
  dB, formatted exactly like the regular track header read-out ("−∞" only at silence),
  and update whenever the volume changes from any source.
- **FR-005**: Return and Master rows MUST keep their current height, selection,
  context menu, drop target and automation behaviour.
- **FR-006**: Return and Master rows MUST carry a mini volume fader like the one on
  regular track headers (same look, drag, double-click reset to 0 dB). Moving it
  changes that row's volume and updates the dB read-out; the same value shows in the
  Mixer and, for the master, in the transport's MST module. A return fader follows that
  return's volume automation during playback; the Master fader shows the master's set
  volume, as the MST module does. Like every volume fader in Nota, a move is not an
  undo step; and like the MST fader, these faders do not record automation (return
  automation has no visible lane in the arrangement). Pan is NOT added to these rows.
- **FR-007**: The master volume MUST be one value everywhere: a change in the Mixer,
  the MST module or the Master row shows in the other two and is what the project
  saves.
- **FR-008**: The meters and read-outs MUST stay correct after returns are added,
  removed, reordered or renamed, after undo/redo of structural edits and after loading a project.

### Key Entities

- **Return row / Master row**: the slim rows pinned under the tracks in the
  arrangement; each now carries a name, a volume fader with a dB read-out and a live
  meter.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: During playback with an active send, a return row's meter visibly moves
  in 100% of cases; with no signal it stays empty.
- **SC-002**: At any moment, the level shown on a return or master row differs from the
  same strip in the Mixer by no more than one meter step.
- **SC-003**: The dB number on return and master rows matches their actual volume in
  100% of checks after changes from the Mixer, MST module, return automation and project load.
- **SC-004**: Adding the meters causes no noticeable change in UI smoothness with 8
  returns during playback.
- **SC-005**: Changing a return's or the master's volume takes one action on its
  arrangement row, with no need to open the Mixer.

## Assumptions

- The engine already provides live levels and volume control for returns and master; this feature is a UI
  change only, with no engine or C ABI change.
- The dB number shows volume (as on regular track headers), not the momentary signal
  level — the meter shows the signal. This keeps the two kinds of rows consistent.
- Master volume here is the same master volume as in the transport's MST module and the
  Mixer — one value, shown in several places.
- The rows stay 40 px tall; the meter and fader are laid out to fit (exact layout
  decided in the plan, per `DESIGN.md`).
- Pan control on Return/Master rows is out of scope (clarified: option B).
- Showing the live automated master volume on the Master fader is out of scope: the
  MST module does not show it either, and it would need a new engine read-out.
- Recording return/master volume automation from these faders is out of scope; return
  automation lanes in the arrangement footer would be a separate feature.

## Nota Compliance Checklist *(review before implementation; re-check in tasks.md)*

- [ ] Every new source file will carry the AGPL-3.0-only SPDX header (two lines, see CONTRIBUTING.md)
- [x] No new dependency — or: license checked, AGPL-compatible, `LICENSES/third-party.md` row planned
- [x] Audio-thread impact considered: no alloc / lock / I/O on the audio thread (AR-6), command queue for data in (AR-4), immutable snapshots for structural edits (AR-5) — reads existing meters from the UI thread only
- [x] Managed/native crossings go through the pure C ABI in `nota_engine.h` only (AR-7); no JUCE outside `pluginhost/`
- [x] Smoke-test impact assessed: engine behaviour change → extend `tests/Nota.SmokeTest` — no engine change
- [ ] User-visible change → `CHANGELOG.md` `[Unreleased]` entry planned (and FEATURES.md / user docs via the nota-features / nota-docs skills)
- [ ] UI change → follows `DESIGN.md` (Ember design language)
