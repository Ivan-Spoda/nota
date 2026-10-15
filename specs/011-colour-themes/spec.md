# Feature Specification: Selectable Colour Themes

**Feature Branch**: `011-colour-themes`

**Created**: 2026-10-13

**Status**: Draft

## Clarifications

### Session 2026-10-13

- Q: Should a theme hold both a dark and a light version (switch picks the side), or be one fixed look per entry? → A: One fixed look per entry (Studio Dark, Studio Light, Ember Graphite, Ember Paper…), each marked dark or light. "Follow system" needs its own pairing: a chosen theme for dark mode and one for light mode.
- Q: Should each theme set its own colours for tracks, or should track colours stay the same in every theme? → A: Each theme sets its own colour for each of the nine track colour slots (and the return and master colours); a track keeps its slot and shows the active theme's colour for it.
- Q: If an old Ember colour fails today's readability minimum, adjust it or keep it exact? → A: Keep Ember exact except values that fail; nudge each failing one just enough to pass, and list every change (old → new, why) in the design docs.

**Input**: User description: "Selectable colour themes: turn Nota's two fixed palette variants (the dark/light token pairs) into a set of named colour themes the user picks in Preferences → Appearance — e.g. Studio Dark, Studio Light, and a warm 'Ember' colour set — keeping the Studio layout, type and behaviour unchanged; designed so user-editable/importable themes (a theme editor) can follow later."

## Nota Context *(fill in — drives plan and tasks)*

**Feature area** *(pick the main one)*:

- [ ] Engine (C++, `src/native/nota.engine`) — real-time audio rules apply, see `ARCHITECTURE.md` AR table
- [ ] Managed app (C#, `src/managed/*`) — domain, application, infrastructure, presentation
- [x] UI (Avalonia, `src/managed/Nota.Presentation`) — follow `DESIGN.md` (Studio)
- [ ] Plugin hosting (`pluginhost/`) — the only place JUCE is allowed
- [ ] Build / packaging / CI (`scripts/`, `.github/`) — see `BUILD.md`
- [ ] Docs only

**Nota docs to keep in mind** *(links relative to `specs/<slug>/`, where this file lands)*:
[`ARCHITECTURE.md`](../../ARCHITECTURE.md) (AR rules),
[`CONTRIBUTING.md`](../../CONTRIBUTING.md) (licensing, dependency policy, PR
expectations), [`BUILD.md`](../../BUILD.md), [`DESIGN.md`](../../DESIGN.md),
[`FEATURES.md`](../../FEATURES.md).

**Depends on**: [`010-studio-visual-style`](../010-studio-visual-style/spec.md) (the Studio
restyle, PR #1). This feature builds on Studio's token set and branches from it; it lands
after 010 does.

**Current state (for reference)**:

- Every colour the app paints is a named token (surfaces, lines, text, accent,
  semantics, graph chromas, the nine track hues, shadows). Each token has exactly **two
  values**: one for the dark appearance and one for the light.
- Preferences → Appearance offers **Dark / Light / System**. The choice applies live and
  is remembered between launches. System follows the operating system and flips with it.
- Those two value sets are the **Studio** colours. The earlier **Ember**
  colours (warm brown neutrals, brass accent; "Ember Graphite" dark, "Ember Paper"
  light) were replaced by 010 and survive only in project history.
- Layout, geometry, type, the "live or chosen" accent rule and the state treatments are
  part of Studio's structure, not its colours. This feature does not touch them.

**Shape of the feature (clarified)**: a **colour theme** is a single, fixed look. It has a
name, is marked **dark** or **light**, and gives every colour token one value. The
built-ins are Studio Dark, Studio Light, Ember Graphite and Ember Paper. The appearance
setting becomes: **one chosen theme**, or **follow system**. Follow system uses a
*theme for dark mode* and a *theme for light mode*, both chosen by the user. The
current Dark / Light / System control is replaced by this.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Pick a colour theme (Priority: P1)

A producer opens Preferences → Appearance, chooses **Dark**, and sees the dark colour
themes as cards, each a miniature Nota window in that theme's colours. They pick "Ember
Graphite". Every
open window recolours at once — arrangement, mixer, device cards, browser, menus,
dialogs — without a restart and without losing their place. They quit, relaunch, and
Nota opens in Ember Graphite.

**Why this priority**: This is the feature. Without it there are no themes, only the two
fixed variants.

**Independent Test**: With a project open, switch theme in Preferences and check that
every surface recolours live. Relaunch and confirm the choice is kept.

**Acceptance Scenarios**:

1. **Given** Nota on Studio Dark, **When** the user picks Ember Graphite in
   Preferences → Appearance, **Then** every open window repaints in Ember Graphite at
   once. Nothing else changes: playback, selection, scroll, open panels and window
   layout stay as they were.
2. **Given** the user chose Ember Paper and quit, **When** Nota launches again, **Then**
   it opens in Ember Paper, and no frame is first drawn in another theme.
3. **Given** any theme, **When** the user looks at the Preferences theme cards, **Then**
   each card shows its name and a miniature Nota window in that theme's colours (ground,
   panel, text, accent, seams and the first track colours), and sits under the mode of
   its kind. The active theme is marked as chosen.
4. **Given** a settings file from before this feature, **When** Nota launches, **Then**
   the old appearance maps across: Dark → Studio Dark, Light → Studio Light, System →
   follow system with Studio Dark for dark mode and Studio Light for light mode.

---

### User Story 2 - Follow the system with a theme for each mode (Priority: P1)

A producer works by day in a bright room and at night in a dark one, with their OS set
to switch automatically. In Preferences → Appearance they turn on "Follow system" and
pick Ember Paper for light mode and Studio Dark for dark mode. When the OS flips, Nota
flips with it to the matching theme.

**Why this priority**: Nota already offers "System" today. Moving to single-look themes
must not take it away.

**Independent Test**: Turn on follow system, choose a theme for each mode, flip the OS
appearance both ways, and check that Nota follows. Relaunch in each OS mode.

**Acceptance Scenarios**:

1. **Given** follow system with Ember Paper (light) and Studio Dark (dark), **When** the
   OS switches to dark, **Then** Nota shows Studio Dark; **when** it switches back,
   Ember Paper.
2. **Given** follow system is on, **When** the user picks a theme for dark mode,
   **Then** only themes marked dark are offered. The same goes for light mode.
3. **Given** follow system is on, **When** the user chooses **Dark** or **Light**,
   **Then** follow system turns off and that mode's theme applies. The two per-mode
   choices are kept for next time.
4. **Given** follow system and the OS in light mode at launch, **Then** Nota opens in the
   light-mode theme on the first frame.

---

### User Story 3 - Built-in themes that hold up everywhere (Priority: P1)

Whatever theme is chosen, Nota stays fully usable. Text is readable on every surface, live
state (playing, selected, armed, muted, soloed) is still obvious, meters still read
safe / warning / over, and track colours stay distinguishable from each other. No surface
falls back to another theme's colour or to a raw default.

**Why this priority**: A theme that breaks one view (unreadable menu text, an invisible
mute state, a stray Studio grey in an Ember window) is worse than no theme. This story is
the quality bar for every built-in theme.

**Independent Test**: For each built-in theme, walk the screenshot set used
for 010 (arrangement, mixer, session, device cards, browser, menus, preferences, command
palette, dialogs) and run the automated colour checks.

**Acceptance Scenarios**:

1. **Given** any built-in theme, **When** the automated check runs,
   **Then** every colour token has a value, and every text-on-surface pairing the design
   names meets its contrast minimum (≥ 4.5:1 for text, ≥ 3:1 for glyphs and state
   fills against their ground).
2. **Given** any built-in theme, **When** a track is muted, soloed or armed, **Then** the
   state is shown with that theme's mute / solo / arm colours, and the ink on them is
   readable.
3. **Given** any built-in theme, **When** the transport is stopped, nothing is selected and
   no toggle is on, **Then** no accent colour is on screen. The Studio "live or chosen"
   rule holds in every theme.
4. **Given** a project with tracks in all nine track colours, **When** the theme changes,
   **Then** each track keeps its colour *role*, with the hue that theme gives the role,
   and the project file is unchanged.

---

### User Story 4 - Ember comes back as a colour set (Priority: P2)

Someone who preferred the old warm look picks "Ember Graphite" (dark) or "Ember Paper"
(light) and gets the warm brown neutrals and the brass accent back. Studio's
structure stays: docked seam panels, sentence-case labels, neutral resting values,
orange only for live or chosen, which in Ember becomes brass only for live or chosen.

**Why this priority**: Ember is the second theme that proves the system works, and it
gives back the old palette without bringing back the old layout. It ranks below US1–US3
because those cover any theme.

**Independent Test**: Pick each Ember theme. Compare screenshots against Ember-era screenshots for
colour only (ground, panel, text, accent hues). Layout must match the Studio screenshots,
not the Ember-era ones.

**Acceptance Scenarios**:

1. **Given** Ember Graphite, **When** the main window shows, **Then** its ground, panel,
   raised and text tones and its accent are the Ember Graphite values of release 0.52.1.
2. **Given** Ember Paper, **Then** they are the Ember Paper values of 0.52.1.
   *Exception for 1 and 2*: a value nudged to meet the readability minimum (FR-013),
   which is listed in the design docs.
3. **Given** an Ember theme, **When** any view is compared with the Studio theme of the
   same kind (dark or light), **Then** sizes,
   radii, spacing, type and which elements carry the accent are identical. Only the
   colours differ.

---

### User Story 5 - Themes as data, ready for user themes later (Priority: P3)

A contributor adds a new built-in theme by writing one complete theme definition: a
name, its kind (dark or light), and a value for every colour token. Nothing else in the app changes.
If they leave out a token, the automated check names the missing token before the change
can ship. This is the groundwork a later theme editor or theme import will use. That
editor is **not** part of this feature.

**Why this priority**: The user asked for themes "designed so user-editable themes can
follow". This story keeps that path open at low cost now, without building the editor.

**Independent Test**: Add a throwaway theme definition in a test build. Confirm it shows
in Preferences and applies fully. Remove one token and confirm the check fails and names
it.

**Acceptance Scenarios**:

1. **Given** a new complete theme definition, **When** the app runs, **Then** the theme
   appears in the Preferences list and applies like the built-in ones, with no other code
   or view changes.
2. **Given** a theme definition that lacks a token or has an invalid colour, **When** the
   automated check runs, **Then** it fails and names the theme and the token.

---

### Edge Cases

- **Stored theme no longer exists** (renamed or removed in a later version, or a
  hand-edited settings file): Nota falls back to Studio Dark. For a per-mode choice in
  follow system, it falls back to Studio Dark or Studio Light for that mode. It does not
  crash or show an error dialog.
- **Per-mode choice of the wrong kind** (e.g. a hand-edited file puts a light theme as
  the dark-mode choice): treated as unknown, and the same fallback applies.
- **OS reports no appearance** (platforms or sessions without a dark/light signal):
  follow system uses the light-mode theme, as the platform default.
- **Theme changed while a popup, menu, tooltip or modal is open**: the open surface
  recolours too, or closes cleanly. No half-recoloured popup stays on screen.
- **Theme changed during playback or recording**: audio is not interrupted and no
  dropout is attributable to the switch (the switch is UI-only).
- **Colours derived from tokens** (washes, selection tints, shades of track colours, ink
  computed for contrast on a fill, one-off device hues mapped onto roles): these follow
  the active theme, not Studio's values.
- **Stored per-item colours** (e.g. a custom tag or device hue saved in a project): shown
  through the same theme mapping as today, and never rewritten in the file.
- **Secondary windows** (Preferences, export, plugin windows' Nota chrome, the
  connect-phone view, the welcome screen): all follow the theme, including windows opened
  after the switch.
- **Kind and native chrome**: the operating system's own window parts (title bar on
  some platforms, scroll bars, native dialogs) follow the active theme's kind. A dark
  theme gets dark native chrome.

## Requirements *(mandatory)*

### Functional Requirements

**Choosing a theme**

- **FR-001**: Preferences → Appearance MUST offer a **colour theme** choice, mode first:
  **Dark**, **Light** or **Follow system**, over the dark and the light themes by name.
  Both kinds always show, so changing mode never moves the page; the mode decides which
  theme is chosen (one per kind while following the system).
- **FR-002**: Each theme MUST be shown with a preview of that theme's colours (at least
  ground, panel, primary text and accent), drawn as a miniature Nota window. The active
  theme MUST be visibly marked as chosen, following Studio's selected-option treatment.
- **FR-003**: Choosing a theme MUST recolour every open window and every colour drawn by
  the app (controls, custom-drawn views, graphs, meters, waveforms, track colours,
  shadows) right away, without a restart, and without changing any other state.
- **FR-004**: With **follow system** on, the user MUST be able to choose one theme for
  the OS's dark mode and one for its light mode. Each choice offers only themes of the
  matching kind. Nota MUST switch between them live when the OS appearance changes.
- **FR-005**: Choosing a single theme MUST turn follow system off. The two per-mode
  choices MUST be kept, so turning follow system on again restores them.
- **FR-006**: The appearance setting (single theme, or follow system with its dark-mode
  and light-mode choices) MUST be remembered across launches. Nota MUST start in the
  right theme without first showing another.
- **FR-007**: Settings from before this feature MUST map across: Dark → Studio Dark,
  Light → Studio Light, System → follow system with Studio Dark and Studio Light. If no
  setting is stored, Nota MUST use Studio Dark. An unknown or wrong-kind stored theme MUST
  fall back to the Studio theme of the needed kind.

**What a theme is**

- **FR-008**: A colour theme MUST consist of a unique identifier, a display name, a kind
  (**dark** or **light**), and a value for **every** colour token. The colour tokens are
  the full set the design language defines, including the nine track colour slots and
  the return and master colours, semantic colours
  (mute, solo, arm/record, warning, success) and shadow colours.
- **FR-009**: A theme's kind MUST decide which family the operating system's native
  chrome uses (dark or light) and which list it appears in for follow system.
- **FR-010**: A theme MUST NOT change anything except colour: geometry, spacing, radii,
  type, icons, layout, which elements carry the accent, and all behaviour stay those of
  the Studio design language.
- **FR-011**: Colours that the app derives from tokens (tints, shades, contrast-picked
  ink, mappings of stored one-off hues onto roles) MUST be derived from the **active**
  theme's values.
- **FR-012**: Built-in themes MUST be defined in one place per theme, as data in the same
  complete form a future user-supplied theme would take. Adding a built-in theme MUST
  require no change outside its own definition and the list of built-ins.

**Built-in themes**

- **FR-013**: Nota MUST ship these four built-in themes:
  - **Studio Dark** and **Studio Light**: today's dark and light colours, unchanged.
    Studio Dark is the default.
  - **Ember Graphite** (dark) and **Ember Paper** (light): the pre-Studio Ember colours,
    mapped onto today's token set. For tokens that Studio added and Ember never had
    (seam, rule, faint text, mute, solo, viz ink, viz grid, the inset surface), the
    values are derived in Ember's palette by the same roles Studio uses. Any Ember value
    that fails FR-014 is moved the smallest step that passes (same hue, lightness only
    where possible). All other Ember values stay exactly as released.
- **FR-014**: Every built-in theme MUST meet the contrast minimums the design language
  sets (≥ 4.5:1 for text on its surface, ≥ 3:1 for glyphs and state fills). Each MUST
  also keep the nine track colours and the mute / solo / arm colours distinguishable
  from each other and from the accent.

**Guarding quality**

- **FR-015**: The automated design check MUST verify, for every built-in theme, that
  every token has a valid value, that the contrast minimums of FR-014
  hold, and that a theme's preview colours come from that theme. On failure it MUST name
  the theme and the token.
- **FR-016**: The existing Studio checks (no Ember value in Studio's colours, neutral
  resting values, and the others) MUST keep applying to both Studio themes. Checks about
  structure (radii, type, shadows only on overlays, no tracked caps) MUST apply
  regardless of theme. The Ember themes' colour values are exempt from the "no Ember value" rule
  and from nothing else.

**Docs**

- **FR-017**: The design documentation MUST describe colour themes: what a theme
  contains, the built-in themes with their token values, follow system, and how to add a
  theme. It MUST list every Ember value that was adjusted from 0.52.1, with the old value,
  the new value and the contrast pairing it failed. The
  changelog and feature list MUST record the new Preferences option.

### Key Entities

- **Colour theme**: an identifier (stable, used in settings), a display name, a kind
  (dark or light), and one value per colour token. The built-in themes are Studio Dark,
  Studio Light, Ember Graphite and Ember Paper. Later, user themes take the same
  shape.
- **Colour token**: a named colour role in the design language (e.g. ground, panel, seam,
  primary text, accent, mute, track hue 3, popover shadow colour). The set of tokens is
  fixed by the design language; themes only supply values.
- **Appearance setting**: either *one chosen theme*, or *follow system* with a dark-mode
  theme and a light-mode theme. All three theme identifiers are stored, so switching
  modes loses no choice. It replaces the old Dark / Light / System value and is
  migrated from it (FR-007).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can change the colour theme from Preferences in 2 interactions
  (open Appearance, pick a theme), and every open window shows the new colours within
  0.5 s of the click.
- **SC-002**: For 100% of built-in themes (at least 4), the
  automated check passes with no missing token, invalid value or contrast failure.
- **SC-003**: In a side-by-side screenshot review of the 010 screen set, Studio Dark and
  Studio Light screenshots after this feature are indistinguishable from those before it.
- **SC-004**: In the same review under the Ember themes, zero elements differ from their Studio
  counterpart in size, position, radius or type. Only colour differs.
- **SC-005**: Switching theme during playback causes no audible dropout across 20
  switches.
- **SC-006**: Adding a further built-in theme in a test build touches only that theme's
  definition and the built-in list. No view or control code changes.
- **SC-007**: After relaunch, the stored theme is in effect on the first frame in 100%
  of launches, including follow system in either OS mode.
- **SC-008**: With follow system on, Nota matches an OS appearance change within 1 s,
  in both directions, in 100% of 10 flips.

## Assumptions

- **Theme = one fixed look (clarified).** Each theme is dark or light. Follow system pairs
  a dark-mode theme with a light-mode one. So a user can mix families, e.g. Ember Paper
  by day and Studio Dark by night.
- **User-editable and importable themes are out of scope.** That includes a theme editor,
  import/export of theme files, and per-project themes. FR-012 and US5 keep the data
  shape ready for them.
- **Four built-in themes** (Studio Dark/Light, Ember Graphite/Paper) are enough to ship. More, e.g. a high-contrast
  theme, can be added later per FR-012 without a new spec.
- **Track colours are themed (clarified).** Projects store track colours as palette roles, not raw
  colours, so a theme can give each role its own hue without touching project files.
- **Ember values** come from the last pre-Studio release (0.52.1) palette. Studio-only
  tokens get Ember-palette values by Studio's derivation rules (e.g. seam = Ember's
  gutter tone, rule = Ember's strong border).
- **Settings are per machine**, like the existing appearance setting. They are not stored
  in projects.
- **The phone remote's web page** keeps its own look. Only the in-app connect-phone view
  is themed.
- **Depends on 010** being merged first, or this branch carrying it.

## Nota Compliance Checklist *(review before implementation; re-check in tasks.md)*

- [x] Every new source file will carry the AGPL-3.0-only SPDX header (two lines, see CONTRIBUTING.md)
- [x] No new dependency
- [x] Audio-thread impact considered: UI-only change; no engine or audio-thread involvement
- [x] Managed/native crossings unchanged; no JUCE outside `pluginhost/`
- [x] Smoke-test impact assessed: the design-token check is extended per FR-015/FR-016; no engine behaviour change
- [x] User-visible change → `CHANGELOG.md` `[Unreleased]` entry and FEATURES.md planned (FR-017)
- [x] UI change → follows `DESIGN.md` (Studio design language); themes vary colour only (FR-010)
