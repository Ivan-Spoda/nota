# Feature Specification: Studio Visual Style

**Feature Branch**: `010-studio-visual-style`

**Created**: 2026-10-13

**Status**: Draft

## Clarifications

### Session 2026-10-13

- Q: Should the floating rounded islands give way to docked panels on seams? → A: Yes — docked flat panels meeting on 1 px seams; region order and contents unchanged.
- Q: Should device cards grow to a 360 px height to fit the general type scale, or keep Nota's frame? → A: Keep the 700 × 260 frame (and documented width exceptions); reskin inside it.
- Q: Should resting values (knob arcs, fader fills, curves) go neutral, with orange only for live or chosen state? → A: Yes — the "live or chosen" rule applies throughout.

**Input**: User description: "Please restyle the Nota UI. I don't like the current Nota UI; give it a calmer, more focused studio look that puts the work first, while keeping all of Nota's features. We just want the UI styling to change."

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

**Design direction**: this feature replaces Nota's current design language ("Ember")
with a new one, **Studio**, as the visual standard the app is held to. Its aim is that
the chrome recedes and the work — track colours, clips, waveforms, meters — reads first.

**Current state (for reference)**:

- Nota's look is **Ember**: warm brownish neutrals (hue ~40°) in nine surface steps, one
  brass/gold accent, two variants (Ember Graphite dark, Ember Paper light), switchable
  live in Preferences → Appearance (Graphite / Paper / System).
- The shell is a set of **floating islands** — transport, browser, canvas and detail
  panel — with rounded 6 px corners, floating on a darker gutter with 12 px gaps.
- Labels lean on **uppercase tracked caps** (section labels, knob labels, eyebrows,
  badges). Brass marks "active, selected, changed" and also colours resting values
  (knob arcs, audio-path fills).
- Track colours are a nine-role palette at one lightness; mute/solo/arm use brass and red.
- A design-token test in the smoke test enforces the Ember rules (token parity between the
  two variants, bans on drop shadows, literal colours, font names, etc.).

**Studio, in short** (the direction this feature sets, and why):

- **Content carries the colour; the chrome recedes.** A **neutral, cool-to-neutral grey
  studio** stepped in a few exact tones (well, ground, panel, raised, raised-hover),
  separated by a **dark 1 px seam** between panels and a lighter 1 px **rule** inside a
  panel, so track colours, clips, waveforms and meters read first.
- **One accent, one meaning.** A single warm orange, used only for what is *live or
  chosen* (play, selection, playhead, loop, toggles that are on, a control while it is
  being dragged or focused). Resting values are neutral ink, so orange always means
  something.
- **Fixed state colours**: **red** for record/arm, **amber** for active mute, **blue** for
  active solo, **green** for safe meter range and positive status.
- **Docked, flat panels** butted against seams instead of floating islands — more room
  for the work and a clearer structure. Depth is tone steps plus seams, with a faint top
  bevel on raised controls. Real shadows only on things that float (menus, popovers,
  modal windows).
- **Small, nearly square corners** (2 / 3 / 4 px), dense rows (22–32 px), small crisp
  type: Geist for words, Geist Mono with tabular numerals for every number.
- **Sentence-case labels, no shouting** — no uppercase tracked labels; section labels are
  quiet muted text. Calmer, easier to read, one labelling voice.
- Selection marked by an **accent edge** (a 2 px left tick on rows, a 2 px underline on
  tabs); menu hover is a full accent fill with dark ink.
- **Studio Dark** is the default (a DAW is used in dark rooms for long sessions); **Studio
  Light** is the same system in light greys.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The app wears the Studio look (Priority: P1)

A producer opens Nota and sees the Studio look: neutral grey panels docked edge to edge on
dark seams, a single orange accent on what is playing or selected, crisp small Geist type,
nearly square corners. The arrangement, browser, transport, detail panel, mixer and
dialogs all share it. Nothing they used to do has moved or disappeared.

**Why this priority**: It is the request itself. Without the shell, panels and shared
controls reskinned, nothing else matters.

**Independent Test**: Open a representative project in the default dark appearance and
compare each main surface (transport, browser, arrangement, session, modular, detail
panel, mixer, clip editor, preferences, export dialog, command palette) against the
Studio rules above: palette, seams, corners, type, accent usage and button states read as
one system.

**Acceptance Scenarios**:

1. **Given** the default dark appearance, **When** the main window opens, **Then** every
   surface uses Studio's neutral grey ladder and none of Ember's warm brown tints or brass
   remain anywhere.
2. **Given** the main window, **When** the user looks at the panel layout, **Then** the
   transport, browser, canvas and detail panel are docked flat panels that meet on 1 px
   dark seams, with no rounded floating islands and no visible gutter between them.
3. **Given** the transport is stopped, nothing is selected and no toggle is on, **When**
   the user scans the screen, **Then** no element is orange.
4. **Given** the user presses play, selects a clip, enables loop or metronome, **When**
   they look at the screen, **Then** exactly those live or chosen things turn orange (the
   play key, the selected clip's band, the loop range, the toggle) and nothing else does.
5. **Given** any button, toggle, field, tab, list row or menu, **When** the user hovers,
   presses, focuses, selects or disables it, **Then** it shows the Studio treatment for
   that state (hover lifts one tone step, press sinks to the well, toggled-on is a solid
   accent fill with dark ink, selected rows/tabs get an accent edge, focus is a 1 px
   accent edge, disabled uses the faint text tier).
6. **Given** a menu, popover or modal dialog, **When** it opens, **Then** it floats with a
   seam ring and a soft drop shadow (modals over a dimmed scrim); docked surfaces never
   carry a blurred shadow.

---

### User Story 2 - Every feature still works exactly as before (Priority: P1)

The same producer goes through their usual workflow — browse and drag a device, record a
take, edit notes, automate a parameter, mix, export — and every control, shortcut,
gesture, menu item and window is still there and behaves the same.

**Why this priority**: The user explicitly asked to keep all of Nota's features. A
restyle that loses or breaks one is a regression, not a feature.

**Independent Test**: Run the smoke test and walk through the FEATURES.md capabilities
in the restyled app; every capability is reachable from the same place and behaves as
before.

**Acceptance Scenarios**:

1. **Given** the restyled app, **When** the user uses any capability listed in
   FEATURES.md, **Then** it is reachable from the same panel, menu, shortcut or gesture as
   before and produces the same result.
2. **Given** an existing project saved before the restyle, **When** it is opened, **Then**
   it loads unchanged (tracks, clips, devices, automation, track colour choices) and saving
   it does not alter its musical content.
3. **Given** any device card, **When** the user drags a knob, double-clicks to reset,
   right-clicks for MIDI Learn / modulation, or switches presets, **Then** the interaction
   is the same as before; only its appearance differs.
4. **Given** the arrangement or piano roll, **When** the user clicks, drags or zooms,
   **Then** hit targets land on the same positions as before the restyle.

---

### User Story 3 - Light appearance is Studio Light (Priority: P2)

A producer who works in daylight switches Preferences → Appearance to the light option
(or follows the OS). The app becomes the same system in Studio Light's greys, live, with
no restart.

**Why this priority**: Nota already offers a light variant and a System option; dropping
them would remove a feature, and leaving Ember Paper would make the light mode look like
the old app.

**Independent Test**: Toggle Appearance between dark, light and System while a project
is open and compare against the Studio Light values.

**Acceptance Scenarios**:

1. **Given** the app in dark, **When** the user picks light, **Then** every surface
   switches immediately to the light grey ladder, with the accent darkened enough to hold
   contrast on pale grey, and no surface keeps a dark or warm-paper colour.
2. **Given** System is selected, **When** the OS switches appearance, **Then** Nota
   follows, as before.
3. **Given** either appearance, **When** text or a glyph sits on a solid accent or
   coloured fill, **Then** it remains legible (contrast at least 4.5:1 for text).

---

### User Story 4 - Device cards and custom views speak the same language (Priority: P2)

Device cards (instruments, audio and MIDI effects, hosted plug-ins) and the
custom-drawn views (arrangement, piano roll, waveform/audio clip editor, meters, graphs,
mixer, modular, session grid) adopt the Studio treatment so the whole app reads as one
system rather than a Studio shell around Ember devices.

**Why this priority**: Devices and canvases are most of the screen while working; a
half-converted app would look inconsistent. It comes after the shell because the shell
sets the tokens these views draw from.

**Independent Test**: Open one instrument, one audio effect, one MIDI effect and one
hosted plug-in card, plus the arrangement, piano roll, audio editor, mixer and modular
views, and check each against the Studio rules below.

**Acceptance Scenarios**:

1. **Given** a device card, **When** it is shown, **Then** it has a panel-grey body with
   3 px corners and a seam ring, a raised header with a faint top bevel, the accent power
   key, name and preset stepper; parameter labels are sentence case (no tracked caps) and
   every value is set in the mono face with tabular numerals.
2. **Given** a knob, fader or slider at rest, **When** the user looks at it, **Then** its
   value arc or fill is neutral ink; **When** the user drags or focuses it, **Then** it
   turns orange, and back to neutral on release.
3. **Given** a track header in the arrangement or a mixer strip, **When** mute, solo or
   arm is engaged, **Then** it fills amber, blue or red respectively, with legible ink.
4. **Given** a clip in the arrangement, **When** it is drawn, **Then** it is a flat
   rectangle in its track colour with a darker title band, and selecting it swaps the band
   to the accent.
5. **Given** a device graph (EQ curve, filter response, envelope, waveform), **When** it is
   drawn, **Then** it sits in a recessed well, its curves are neutral ink at rest, and only
   the selected or dragged node is orange.

---

### User Story 5 - Design documentation and guard tests describe the new look (Priority: P3)

A contributor (human or agent) who builds new UI next week reads the design guideline,
the design reference page and the design skill, and they describe the Studio
system the app now uses; the design-token checks in the smoke test enforce it.

**Why this priority**: Without this, the next change follows the old Ember rules and the
app drifts back. It does not change what users see today, so it comes last.

**Independent Test**: Read DESIGN.md, DESIGN.html and the nota-design skill; every value
and rule they state matches the app, and the smoke test passes with the new rules and
would fail on a reintroduced Ember colour or rule violation.

**Acceptance Scenarios**:

1. **Given** the updated design guideline, **When** a contributor looks up a colour,
   radius, size or type style, **Then** it matches what the app renders.
2. **Given** the design-token checks, **When** someone adds a blurred shadow to a docked
   surface, a hard-coded colour, or an uppercase tracked label, **Then** the smoke test
   fails, as it does today for the Ember bans.

---

### Edge Cases

- **Stored track colours**: projects store a track colour index; after the restyle each
  index maps to a Studio clip colour, so old projects open with a consistent
  (possibly different) hue and no error.
- **Hosted plug-in editors** (VST3 / AU windows) draw their own UI and are out of reach;
  only Nota's frame around them is restyled.
- **User-chosen colours stored as hex** (e.g. tag swatches, custom clip colours): they are
  data and render as stored; ink on them is derived for legibility.
- **Narrow windows / small screens**: the docked layout must not clip controls that were
  reachable before at the same window size.
- **Session view play state**: Nota turns Play green while Session clips run; under the
  Studio rules this stays a distinct live state and remains distinguishable from the
  arrangement play state.
- **Overload and recording**: clip indicators and recording remain red and unmistakable;
  orange is never used for a warning that used to be red.
- **Text overflow**: switching uppercase labels to sentence case, or any type size change,
  must not truncate a label that fitted before.
- **Platform title bar**: macOS traffic lights and Windows caption buttons keep their
  reserved space in the restyled header.

## Requirements *(mandatory)*

### Functional Requirements

**Look — palette and surfaces**

- **FR-001**: The app MUST replace the Ember palette with Studio's: a neutral grey ladder
  (well, ground, panel, raised, raised-hover), a dark seam colour, a lighter rule colour,
  three text tiers plus a faint tier for disabled, one orange accent with its line,
  pressed and on-accent variants, red (record/error), amber (mute/warning), blue (solo),
  green (safe range / positive status), and Studio's muted clip colours.
- **FR-002**: The app MUST offer the same appearance choices as today (dark, light,
  System) and switch between them live; dark MUST use the Studio Dark values and light MUST
  use the Studio Light values.
- **FR-003**: No Ember-specific colour (warm brown neutrals, brass, bronze, Ember track
  palette) MUST remain visible anywhere in Nota's own UI in either appearance.
- **FR-004**: The orange accent MUST appear only on live or chosen state: transport
  playing, selection, playhead, loop range, toggles that are on, the selected tab/row
  edge, menu-item hover, a device's power key, and a value control while it is focused or
  dragged. Resting values (knob arcs, fader fills, meters, device curves) MUST be neutral.
- **FR-005**: Mute, solo and arm/record MUST use amber, blue and red respectively when
  engaged, wherever they appear (arrangement, mixer, session).
- **FR-006**: Text and glyphs on any solid coloured fill MUST derive their colour for
  contrast (at least 4.5:1) rather than use a fixed colour.

**Look — shell and structure**

- **FR-007**: The main window's regions (header, transport, browser, canvas, detail
  panel, status line) MUST be docked flat panels that meet on 1 px seams, replacing the
  floating rounded islands and gutter; their order and the features they contain stay as
  they are today.
- **FR-008**: Each docked panel that has a toolbar or header row MUST present it as a
  Studio panel toolbar (panel grey, separated from the content by a seam).
- **FR-009**: Elevation MUST follow Studio: flat by default, raised controls with a faint
  top bevel and a seam ring, blurred shadows only on floating layers (menus, popovers,
  dropdowns, modal windows, detached windows), modals over a dimmed scrim.
- **FR-010**: Corner radii MUST follow Studio's scale: 2 px (inner keys, rows, menu
  items, clips), 3 px (controls, groups, cards, readouts, popovers), 4 px (modal
  windows). Segmented groups round only their outer corners.

**Look — type, icons and labels**

- **FR-011**: Typography MUST follow Studio's hierarchy (title, body, label, readout,
  position readout, device label/value/readout) using the Geist and Geist Mono faces Nota
  already bundles; every number a user reads to make a decision MUST be set in the mono
  face with tabular numerals.
- **FR-012**: Labels MUST be sentence case at normal letter spacing; uppercase tracked
  labels, eyebrows and caps badges MUST be removed from Nota's own UI.
- **FR-013**: Icons MUST follow Studio's monoline treatment (16 px grid, 1.5 px stroke,
  round caps; 14 px inside pills and rows; filled play and record glyphs).

**Look — controls and views**

- **FR-014**: Buttons, icon buttons, toggles, segmented groups, pills/chips, fields,
  search, tabs, menus, list/browser rows, dropdowns, scrollbars, tooltips and dialogs MUST
  follow Studio's component treatments and state rules (rest, hover, pressed, toggled-on,
  selected, focus, disabled) as listed in User Story 1, scenario 5.
- **FR-015**: Device cards MUST adopt Studio's card treatment (panel body, seam ring,
  raised bevelled header with power key, name, preset stepper and actions; recessed
  display; titled sections divided by rules; label above operator above value) while
  keeping their current frame sizes (700 × 260 and the documented width exceptions) and
  control layout. Type inside cards rises toward Studio's general scale only where the frame allows
  it without truncation or reflow.
- **FR-016**: Knobs MUST adopt Studio's dial look (raised face with seam, well-coloured
  track, neutral value arc, primary-ink pointer, accent arc only while focused or
  dragged, bipolar arcs growing from twelve o'clock).
- **FR-017**: Custom-drawn views — arrangement (clips with darker title band, accent band
  when selected), piano roll (velocity shades of the clip colour, accent frame on
  selection), audio clip editor, automation lanes, meters, graphs, mixer strips (track
  colour top bar), session grid, modular view — MUST adopt Studio's treatment and draw all
  colours from the active appearance.

**Behaviour preserved**

- **FR-018**: Every user-visible capability in FEATURES.md MUST remain available from the
  same place, with the same shortcuts, gestures, menus and results.
- **FR-019**: Hit-testing geometry that interactions depend on — arrangement row heights,
  header column width, ruler, piano-roll key/row sizes, device card frame sizes, control
  hit areas — MUST NOT change as a side effect of the restyle unless a requirement above
  calls for it, and no control may become unreachable.
- **FR-020**: Project files, preset files and saved preferences MUST stay compatible in
  both directions; the restyle MUST NOT change any stored data format. A stored track
  colour index MUST map to a valid colour in the new palette.
- **FR-021**: The restyle MUST NOT change audio behaviour, engine code or the C ABI.

**Documentation and guards**

- **FR-022**: DESIGN.md, DESIGN.html and the nota-design skill MUST be rewritten to
  describe the new Studio design language (its rules, token values for both
  appearances, type, geometry, components) in place of Ember, and MUST agree with the app.
- **FR-023**: The design-token checks in the smoke test MUST be updated to enforce the
  new rules (token parity between appearances, Studio radius scale, shadows only on
  floating layers, no hard-coded colours or font names in views, no uppercase tracked
  labels), and MUST pass.
- **FR-024**: The change MUST get a CHANGELOG `[Unreleased]` entry, and the user manual
  screenshots/pages and FEATURES.md MUST be updated where they describe or show the old
  look.

### Key Entities

- **Design token**: a named colour, radius, size, spacing or type style, with a value per
  appearance (dark / light). The single source the whole UI draws from.
- **Appearance**: dark, light or System; selects which token values are live.
- **Track colour**: a stored per-track index into the clip colour palette; its rendition
  changes, its stored value does not.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Checked against the Studio rules in DESIGN.md, a reviewer finds every main
  surface listed in User Story 1 reads as one visual system (11 of 11 surfaces).
- **SC-002**: 100% of FEATURES.md capabilities remain reachable and working after the
  restyle; the smoke test passes.
- **SC-003**: With the transport stopped and nothing selected or toggled on, zero
  elements on screen use the accent colour.
- **SC-004**: Zero Ember colour values remain in Nota's own UI in either appearance.
- **SC-005**: All text on solid fills meets a 4.5:1 contrast ratio in both appearances;
  all other body text meets 4.5:1 against its background.
- **SC-006**: Switching appearance takes effect in under 1 second with no restart and no
  surface left in the previous appearance.
- **SC-007**: Every project in the repository's test projects opens and round-trips
  without changes to its stored data.
- **SC-008**: Every value DESIGN.md states matches what the app renders (spot-check of
  colours, radii and type styles finds no mismatch).

## Assumptions

- "UI styling" means the Studio design system (palette, type, geometry, elevation,
  component treatments, accent rules) as set out above and written into DESIGN.md,
  applied to Nota's existing screens. No new features come with it — in particular,
  user-editable colour tokens and a theme editor are out of scope.
- Nota keeps its own information architecture: the order of regions, which controls each
  panel holds, menus, windows, views (arrangement, session, modular) and the device
  catalogue. Studio changes the *treatment* of each surface, not its layout: layout
  changes that would move or resize Nota's features (an in-window menubar row, a
  three-column transport grid, 196 px track headers, 360 px device cards) are not part of
  this feature.
- Nota's nine track-colour roles stay nine (stored indices keep their meaning); each is
  retinted to Studio's muted mid-saturation clip-colour style: five base clip colours
  where a role matches, and new hues at the same saturation and lightness for the rest.
- Geist and Geist Mono are already bundled in Nota, so no new font dependency is needed.
- The "Nota" brand name, logo and app icon stay; only Nota's chrome changes, to Studio's
  unbranded look.
- The Nota design language gets a new name in the docs (it is no longer "Ember"); the
  name itself is an editorial choice made during planning.
- The restyle adds no third-party code, fonts or assets, so no new licence row is
  needed.
