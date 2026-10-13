# Feature Specification: Adaptive Snap Grid

**Feature Branch**: `003-adaptive-snap-grid`

**Created**: 2026-10-13

**Status**: Draft

**Input**: User description: "Adaptive snap grid for the arrangement view and the piano roll, with triplets. Problem: the snap step is fixed (default 1/4) and independent of zoom — zooming in keeps snapping to quarters while the drawn grid shows finer lines; zooming out leaves a sub-pixel snap step. Changing it means cycling a single button forward through 1/16→1/8→1/4→1/2→1 bar. Desired: (1) Adaptive snap by default — the snap step equals the visible grid step, chosen from zoom (finest step from a ladder 1/32…1/16…1/4…1 bar…4 bars keeping at least N px between lines), so what you see is what you snap to. (2) Adaptive density modes Narrow / Medium / Wide (different px thresholds). (3) Fixed mode still available: explicitly picking e.g. 1/8 keeps it regardless of zoom; the control shows the mode, e.g. "Auto · 1/8" vs "1/8". (4) Triplet grids (1/4T, 1/8T, 1/16T …) in both adaptive and fixed modes, toggleable. (5) Controls: clicking the snap control opens a menu with all values (Adaptive + density, fixed steps, triplets toggle, snap off) instead of cycling; Ableton-style shortcuts — narrow grid, widen grid, toggle triplets, toggle snap, toggle adaptive; Alt-drag still bypasses snapping. (6) Same behaviour in both arrangement view and piano roll, sharing one grid-step source (piano roll already has adaptive visual-only GridStepBeats). Drawn grid lines and snap step must stay consistent in both editors."

## Nota Context *(fill in — drives plan and tasks)*

**Feature area** *(pick the main one)*:

- [ ] Engine (C++, `src/native/nota.engine`) — real-time audio rules apply, see `ARCHITECTURE.md` AR table
- [x] Managed app (C#, `src/managed/*`) — domain, application, infrastructure, presentation
- [x] UI (Avalonia, `src/managed/Nota.Presentation`) — follow `DESIGN.md` (Ember)
- [ ] Plugin hosting (`pluginhost/`) — the only place JUCE is allowed
- [ ] Build / packaging / CI (`scripts/`, `.github/`) — see `BUILD.md`
- [ ] Docs only

The engine is not involved: snapping is an editing concern resolved in the app before
any clip or note edit reaches the engine.

**Nota docs to keep in mind** *(links relative to `specs/<slug>/`, where this file lands)*:
[`ARCHITECTURE.md`](../../ARCHITECTURE.md) (AR rules),
[`CONTRIBUTING.md`](../../CONTRIBUTING.md) (licensing, dependency policy, PR
expectations), [`BUILD.md`](../../BUILD.md), [`DESIGN.md`](../../DESIGN.md),
[`FEATURES.md`](../../FEATURES.md).

**Current state (for reference)**:

- Arrangement: a fixed snap step (default 1/4) cycled forward by one toolbar cell
  (1/16 → 1/8 → 1/4 → 1/2 → 1 bar), plus a separate Snap on/off latch. The drawn grid
  shows only bar and beat lines (beats from about 12 px per beat) and never matches a
  finer snap step. Alt while dragging already snaps freely, and nearby clip edges
  already attract the dragged clip (magnetic snap).
- Piano roll: the drawn grid already subdivides as you zoom, but snapping and Quantize
  use a fixed step picked in the clip properties GRID dropdown (1/1 … 1/64 and
  1/4T … 1/32T, default 1/16). What you see and what you snap to differ.

## Clarifications

### Session 2026-10-13

- Q: Where are grid settings stored — app-wide, per project, or both? → A: App-wide, one setting per editor; survives restarts, identical across projects, project files unchanged.
- Q: What step does piano-roll Quantize use when the grid is adaptive? → A: Its own Quantize value in the GRID panel (default 1/16, triplets available), independent of the grid mode and zoom.
- Q: Are the arrangement and piano-roll grid settings independent or shared? → A: Independent — each editor has its own mode, density, fixed step, triplets and snap on/off.
- Q: In Fixed mode, what is drawn when the chosen step's lines would merge at a far zoom? → A: Lines are thinned to the density threshold; snapping still uses the chosen step; the snap control marks that the step is finer than the visible grid.
- Q: How does the arrangement's two-line toolbar cell show Adaptive mode? → A: Step on top, "AUTO GRID" caption below ("GRID" when Fixed), instead of "Auto · <step>" on one line.
- Q: Does the audio clip editor get the grid, and with which setting? → A: Yes, as a fourth story. It shares the piano roll's setting: both are the clip editor in the detail panel, one chip and one setting.
- Q: What snaps in the audio clip editor? → A: The trim brackets, warp markers and envelope points.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Snap follows the zoom (Priority: P1)

A user arranging a song zooms in on a few bars to place a clip on an off-beat eighth. They
drag the clip and it lands on the eighth they can see, without touching any grid
control. They zoom out to the whole song, drag a section of clips, and it moves by whole
bars instead of jittering on tiny steps. The same applies when editing notes in the piano
roll.

**Why this priority**: This is the reported pain. Adaptive snapping removes the constant
manual switching on its own, without the other stories.

**Independent Test**: With default grid settings, zoom the arrangement in and out. At each zoom
level, drag a clip and check that it lands only on lines that are visible on screen.
Repeat with a note in the piano roll.

**Acceptance Scenarios**:

1. **Given** default grid settings (Adaptive · Medium) and the arrangement zoomed in
   so that eighth-note lines are drawn, **When** the user drags a clip, **Then** its
   start lands on the nearest drawn eighth-note line.
2. **Given** the arrangement zoomed out so that only bar lines are drawn, **When** the
   user drags a clip, **Then** its start lands on a bar line.
3. **Given** adaptive snap in the piano roll at a zoom that draws 1/32 lines, **When**
   the user draws or moves a note, **Then** the note's start and end land on drawn 1/32
   lines.
4. **Given** any zoom level in either editor, **When** the user looks at the snap
   control, **Then** it shows the step currently in effect (for example "Auto · 1/8"),
   and that step changes as the user zooms.
5. **Given** adaptive snap, **When** the user holds Alt while dragging, **Then** the
   item moves freely, as it does today.

---

### User Story 2 - Pick a grid quickly from a menu or the keyboard (Priority: P2)

A user wants to lock the grid to 1/16 while programming hi-hats, then return to
adaptive. They click the snap control, see every option at once and pick one. Later
they make the grid coarser or finer with the keyboard and switch snapping off and on
without reaching for the mouse.

**Why this priority**: It replaces forward-only cycling, the second half of the
complaint, and lets fixed and adaptive modes coexist.

**Independent Test**: Open the snap control's menu in each editor, pick each kind of
option, and check the result. Then repeat using only the shortcuts.

**Acceptance Scenarios**:

1. **Given** either editor, **When** the user clicks the snap control, **Then** a menu
   opens listing: Adaptive (Narrow, Medium, Wide), every fixed step, a Triplets toggle,
   and Snap off, with the current choice marked.
2. **Given** the user picks fixed 1/8, **When** they zoom in or out, **Then** the snap
   step stays 1/8 and the control reads "1/8" (no "Auto").
3. **Given** a fixed step, **When** the user presses the "narrower grid" shortcut,
   **Then** the step halves (1/8 → 1/16). The "wider grid" shortcut doubles it, and both
   stop at the ends of the range.
4. **Given** adaptive mode, **When** the user presses narrower or wider, **Then** the
   density moves one notch (Wide → Medium → Narrow, and back) and the control updates.
5. **Given** a fixed step, **When** the user presses the "toggle adaptive" shortcut,
   **Then** the editor returns to adaptive with the last used density. Pressing it again
   restores the last fixed step.
6. **Given** snap is on, **When** the user presses the "toggle snap" shortcut, **Then**
   snap turns off (items move freely and the control shows that it is off). Pressing it
   again turns snap back on with the previous mode.

---

### User Story 3 - Triplet grid (Priority: P3)

A user writing a shuffle part switches the grid to triplets. In the piano roll and in the
arrangement, lines and snapping follow triplet divisions, both in adaptive and in fixed
modes, until they switch triplets off.

**Why this priority**: Triplets are essential for some genres but are used less often
than straight grids. The piano roll already offers fixed triplet steps, so this extends
them to adaptive mode and to the arrangement.

**Independent Test**: Turn triplets on in each editor. Check that the drawn lines and the
snapped positions fall on triplet divisions at several zoom levels and with a fixed step.

**Acceptance Scenarios**:

1. **Given** triplets on and a fixed 1/8, **When** the user moves a note, **Then** it
   snaps to 1/8T (three per beat) and the control reads "1/8T".
2. **Given** triplets on and adaptive mode, **When** the user zooms, **Then** the step
   moves through triplet values (for example 1/4T → 1/8T → 1/16T), and lines and
   snapping stay in agreement.
3. **Given** triplets on, **When** the user presses the "toggle triplets" shortcut,
   **Then** the grid returns to the matching straight step (1/8T → 1/8).
4. **Given** the Quantize value set to 1/8T, **When** the user runs Quantize in the
   piano roll at any zoom and in any grid mode, **Then** notes move to 1/8T.

---

### User Story 4 - The same grid in the audio clip editor (Priority: P2)

A user warps a drum loop and trims it in the audio clip editor. As in the piano roll, they
zoom in to place a warp marker on a sixteenth, zoom out to trim to whole bars, and draw
clip-envelope points on the lines they see. The grid control sits in the editor's header,
where the old on/off Snap switch was.

**Why this priority**: The audio editor draws a grid that subdivides with zoom but snaps
only to whole beats — the original complaint, in a second editor. Without it the feature
is half done for audio users.

**Independent Test**: Open a warped audio clip, zoom in and out, and drag a warp marker, a
trim bracket and an envelope point. Each lands on a drawn line, and the header chip shows
the step.

**Acceptance Scenarios**:

1. **Given** a warped audio clip zoomed in until sixteenth lines are drawn, **When** the
   user drags a warp marker, **Then** it lands on the nearest drawn sixteenth line.
2. **Given** the same clip zoomed out to bar lines, **When** the user drags a trim bracket,
   **Then** the edge lands on a bar line.
3. **Given** Envelope mode, **When** the user adds or drags an envelope point, **Then** its
   time lands on a drawn line of the envelope's own timeline.
4. **Given** the piano roll was set to Fixed 1/8, **When** the user opens an audio clip,
   **Then** the audio editor's chip reads "1/8": the two clip editors share one setting.
5. **Given** any of these drags, **When** the user holds Alt, **Then** the item moves freely.

---

### Edge Cases

- **Extreme zoom-in**: adaptive never goes finer than 1/32 (1/32T with triplets), even if
  finer lines would fit. Lines remain drawn at that step and are simply farther apart.
- **Extreme zoom-out**: adaptive never goes coarser than 4 bars. If even 4-bar lines are
  closer than the density threshold, the step stays at 4 bars.
- **Bar-level triplets**: at bar-level steps (1 bar, 2 bars, 4 bars) adaptive snaps to
  bars even with triplets on. Triplets apply only to subdivisions of the bar.
- **Time signature**: "1 bar" means the project's current bar length (for example 3
  beats in 3/4). Bar-based steps follow the time signature; note-value steps (1/4, 1/8 …)
  are note lengths and do not change with it.
- **Zoom during a drag**: if the zoom changes in the middle of a drag (for example
  wheel-zoom while dragging), the new step applies from that moment on.
- **Magnetic snap**: nearby clip edges and markers keep attracting the dragged clip, as
  today, in both adaptive and fixed modes.
- **Fixed step too fine to draw**: at a far zoom, Fixed 1/32 draws only lines that are
  far enough apart (for example beats or bars), keeps snapping to 1/32, and the snap
  control marks the step as finer than the visible grid (FR-005, FR-005a).
- **Snap off**: drawn grid lines still follow the adaptive step, so the user keeps a
  visual reference. Only positioning becomes free.
- **Song sections**: section edges keep snapping to whole bars regardless of the grid
  setting, as today.
- **Keyboard focus**: grid shortcuts act on the editor that has focus (arrangement or
  piano roll). When neither has focus (for example after clicking a toolbar button or a
  device card), they act on the editor the user last worked in.
- **Text entry**: shortcuts do not fire while the user types in a text field.
- **Unwarped audio clips**: they have no beat grid, so the trim brackets keep moving by
  source frames and no beat lines are drawn. The envelope still uses the clip's played
  timeline, so in Envelope mode lines and snapping work as for a warped clip.
- **Reversed audio clips**: the grid mirrors with the waveform, as the beat lines do today.
  The envelope stays in playing order (left to right), and so does its grid.

## Requirements *(mandatory)*

### Functional Requirements

**Step resolution**

- **FR-001**: Each editor (arrangement, piano roll) MUST have a grid mode that is one of
  Adaptive (with density Narrow, Medium or Wide) or Fixed (with a chosen step), plus
  independent Triplets on/off and Snap on/off settings. The two editors' settings MUST be
  fully independent: changing any of them in one editor MUST NOT change the other.
- **FR-002**: The straight step ladder MUST be 1/32, 1/16, 1/8, 1/4, 1/2, 1 bar, 2 bars,
  4 bars. With triplets on, the sub-bar steps become 1/32T, 1/16T, 1/8T, 1/4T, 1/2T
  (each 2/3 of its straight value), and bar steps stay unchanged.
- **FR-003**: In Adaptive mode, the step in effect MUST be the finest ladder step whose
  on-screen spacing is at least the density threshold, clamped to the ladder ends. Wide
  MUST produce coarser steps than Medium, and Medium coarser than Narrow, at the same
  zoom. With the default density (Medium) and the default zoom, the arrangement MUST
  resolve to 1/4, matching today's default behaviour.
- **FR-004**: In Fixed mode, the step in effect MUST be the chosen step (made triplet when
  triplets are on), independent of zoom.
- **FR-005**: The step in effect MUST be the single source for snapping and for the
  finest drawn grid lines in that editor. In Adaptive mode, every drawn subdivision
  line MUST be a snap position and every snap position MUST have a drawn line, at any
  zoom. In Fixed mode, lines MUST be drawn at the fixed step whenever its spacing meets
  the current density threshold; when it does not, lines MUST be drawn at the finest
  ladder step that meets the threshold (a coarser multiple of the fixed step), while
  snapping still uses the fixed step.
- **FR-005a**: While a Fixed step is finer than the drawn lines, the snap control MUST
  show that (for example a dimmed or marked label and a tooltip such as "1/32 — finer
  than the visible grid"). The mark MUST disappear once the zoom makes the step visible.
- **FR-006**: The drawn grid MUST still distinguish bar, beat and sub-beat lines visually,
  as it does today, and MUST draw triplet lines when triplets are on.

**What snaps**

- **FR-007**: In the arrangement, every operation that snaps today (moving, resizing and
  creating clips, time and loop selection, marker and automation placement, paste
  position) MUST use the step in effect.
- **FR-008**: In the piano roll, drawing, moving and resizing notes and paste position
  MUST use the step in effect.
- **FR-008a**: Piano-roll Quantize MUST use its own Quantize value, chosen in the GRID
  panel next to the Quantize button from the FR-002 straight and triplet steps (default
  1/16). It MUST NOT depend on the grid mode, the zoom or the Snap on/off state, and it
  persists app-wide like the grid settings (FR-017).
- **FR-009**: Holding Alt during a drag MUST keep bypassing snapping in both editors, and
  magnetic snapping to nearby clip edges MUST keep working in the arrangement.
- **FR-010**: With Snap off, positions MUST be free in both editors, and drawn lines MUST
  continue to follow the step in effect.

**Controls**

- **FR-011**: Each editor MUST have a snap control showing the current state: "Auto · <step>"
  in Adaptive mode (the step updates live with zoom), "<step>" in Fixed mode, a triplet
  suffix "T" when applicable, and a clear off state when Snap is off. The arrangement's
  toolbar cell MAY split this over its two lines to stay narrow: the step on top and the
  caption "AUTO GRID" (Adaptive) or "GRID" (Fixed) below.
- **FR-012**: Clicking the snap control MUST open a menu with: Adaptive Narrow / Medium /
  Wide, each fixed step from FR-002, a Triplets toggle, and Snap on/off, with the current
  choice marked. Choosing an item MUST apply it immediately. This menu MUST replace the
  forward-only cycling in the arrangement and, in the piano roll, the GRID dropdown's role
  as the snap step (that dropdown becomes the Quantize value, FR-008a).
- **FR-013**: The app MUST provide these shortcuts, acting on the focused editor:
  - **⌘1** narrower grid: in Fixed mode, halve the step (one notch down the ladder);
    in Adaptive mode, move density one notch finer (Wide → Medium → Narrow).
  - **⌘2** wider grid: the reverse of ⌘1.
  - **⌘3** toggle triplets.
  - **⌘4** toggle snap on/off.
  - **⌘5** toggle between Adaptive and Fixed, restoring the last-used density or step.
  (⌘ is Ctrl on Windows and Linux.)
- **FR-014**: Every grid change made by a shortcut MUST give visible feedback (the snap
  control updates, and the status line names the new state, as other toggles do today).
- **FR-015**: The new shortcuts MUST appear in Preferences → Shortcuts and in the app menu
  where editing commands are listed.

**Audio clip editor**

- **FR-018**: The audio clip editor MUST use the piano roll's grid setting (one setting
  for both clip editors) and show it with the same chip and menu in its header. The chip
  MUST replace the old Snap switch.
- **FR-019**: For warped clips, the trim brackets and warp markers MUST snap to the step in
  effect, resolved from the editor's zoom on the warp-beat axis. The drawn beat lines MUST
  come from the same positions. Warp markers MUST follow snap on/off and Alt (today they
  always snap to whole beats).
- **FR-020**: In Envelope mode, envelope points MUST snap in time to the step in effect on
  the envelope's own timeline (the clip's played beats), and the drawn lines MUST follow
  that timeline. The value axis is not snapped.
- **FR-021**: The grid shortcuts MUST act on the audio clip editor when it is the clip
  editor used last, as they do for the piano roll. The status line names it "Clip editor
  grid".

**Defaults and persistence**

- **FR-016**: On first launch (no saved preference) both editors MUST start in
  Adaptive · Medium, triplets off, snap on.
- **FR-017**: Each editor's grid settings MUST persist across app restarts as an app-wide
  preference, identical in every project. Grid settings MUST NOT be written to project
  files, and opening any project MUST NOT change them.

### Key Entities

- **Grid setting** (one for the arrangement, one shared by the two clip editors): mode (Adaptive or Fixed), density (Narrow, Medium,
  Wide; remembered while in Fixed), fixed step (remembered while in Adaptive), triplets
  on/off, snap on/off.
- **Quantize value** (piano roll only): a straight or triplet step used by Quantize,
  independent of the grid setting.
- **Step in effect**: derived from the grid setting and the editor's current zoom. It is
  shown on the control, drawn as the finest line, and used for snapping.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In Adaptive mode, at every zoom level of both editors, 100% of snapped
  positions coincide with a drawn grid line, and every drawn subdivision line is
  reachable by snapping. In Fixed mode the same holds whenever the snap control does not
  carry the "finer than the visible grid" mark.
- **SC-002**: Placing a clip on an eighth-note off-beat, then moving a group of clips by
  whole bars, takes zero grid-control interactions in the default mode (zoom only).
- **SC-003**: Any grid value is reachable in at most 2 clicks (open menu, choose) or one
  shortcut press per ladder notch, compared with up to 4 clicks with today's forward
  cycling.
- **SC-004**: Snapping feels the same as today: no visible delay between the pointer
  moving and the dragged item jumping to the next grid position.
- **SC-005**: Existing projects open and edit exactly as before. No stored clip or note
  position changes because of this feature.

## Assumptions

- Density thresholds: Narrow about 6 px, Medium about 12 px, Wide about 24 px between
  finest lines. Final values are tuned during implementation so that FR-003's default
  (1/4 at the default arrangement zoom) holds.
- The piano roll's adaptive density is chosen so its default view resolves to about 1/16,
  close to its current default.
- 1/64 and 1/32T exist today only in the piano roll's fixed list. 1/64 is dropped and the
  ladder stops at 1/32 in both editors. 1/32T stays as the finest triplet step.
- The arrangement and the clip editors share only the rule that computes the step in
  effect, not the setting itself (FR-001). The piano roll and the audio clip editor share
  the setting too (FR-018).
- ⌘1–⌘5 are currently unused in Nota. If the plan finds a conflict, the binding moves and
  the spec is updated.
- The MCP and Remote surfaces are out of scope. They do not expose the snap grid today.
- Groove/swing quantize is out of scope.

## Nota Compliance Checklist *(review before implementation; re-check in tasks.md)*

- [ ] Every new source file will carry the AGPL-3.0-only SPDX header (two lines, see CONTRIBUTING.md)
- [x] No new dependency — or: license checked, AGPL-compatible, `LICENSES/third-party.md` row planned
- [x] Audio-thread impact considered: no alloc / lock / I/O on the audio thread (AR-6), command queue for data in (AR-4), immutable snapshots for structural edits (AR-5) — UI-only, no engine change
- [x] Managed/native crossings go through the pure C ABI in `nota_engine.h` only (AR-7); no JUCE outside `pluginhost/` — no new crossings
- [x] Smoke-test impact assessed: engine behaviour change → extend `tests/Nota.SmokeTest` — no engine change; step resolution covered by managed unit tests
- [ ] User-visible change → `CHANGELOG.md` `[Unreleased]` entry planned (and FEATURES.md / user docs via the nota-features / nota-docs skills when working with an agent that has them); new shortcuts via nota-shortcuts
- [ ] UI change → follows `DESIGN.md` (Ember design language)
