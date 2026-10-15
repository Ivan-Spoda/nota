# Feature Specification: Nota Remote in Studio, wearing the app's theme

**Feature Branch**: `feat/remote-studio`

**Created**: 2026-10-15

**Status**: Draft

**Input**: User description: "Nota Remote (the phone web app Nota serves on the local network, src/web/remote) matches the desktop app: it wears the app's current colour theme, live, and is restyled to the Studio design language. (1) Live theme colours: instead of the hub sending only a `dark` boolean and the phone choosing between two hard-coded Ember palettes, the hub sends the active theme's token values (from Theme/Themes/*.json) and the phone applies them as CSS variables, updating live when the user changes theme in Preferences → Appearance (including when Follow system flips). The phone's own "Follow phone" preference uses the user's dark-mode and light-mode theme picks. No colour values hard-coded in the phone beyond a first-paint fallback. (2) Studio restyle of every phone screen and sheet: sentence-case labels without letter-spacing, seams between panels, radii 2/3/4, neutral values at rest with the accent only for what is live or chosen, mute amber / solo blue / arm red with derived ink, Geist for words and Geist Mono for numbers. (3) The built bundle in Nota.Remote/wwwroot is rebuilt and svelte-check stays clean; the smoke test covers the theme payload."

## Nota Context *(fill in — drives plan and tasks)*

**Feature area** *(pick the main one)*:

- [ ] Engine (C++, `src/native/nota.engine`) — real-time audio rules apply, see `ARCHITECTURE.md` AR table
- [ ] Managed app (C#, `src/managed/*`) — domain, application, infrastructure, presentation
- [x] UI (Avalonia, `src/managed/Nota.Presentation`) — follow `DESIGN.md` (Studio)
- [ ] Plugin hosting (`pluginhost/`) — the only place JUCE is allowed
- [ ] Build / packaging / CI (`scripts/`, `.github/`) — see `BUILD.md`
- [ ] Docs only

The UI here is Nota Remote's web page (`src/web/remote`), plus a small change to what
Nota Remote's hub (`src/managed/Nota.Remote`) tells the phone and what the app
(`src/managed/Nota.App`) hands the hub.

**Nota docs to keep in mind** *(links relative to `specs/<slug>/`, where this file lands)*:
[`ARCHITECTURE.md`](../../ARCHITECTURE.md) (AR rules),
[`CONTRIBUTING.md`](../../CONTRIBUTING.md) (licensing, dependency policy, PR
expectations), [`BUILD.md`](../../BUILD.md), [`DESIGN.md`](../../DESIGN.md),
[`FEATURES.md`](../../FEATURES.md).

**Depends on**: [`010-studio-visual-style`](../010-studio-visual-style/spec.md) and
[`011-colour-themes`](../011-colour-themes/spec.md) (the Studio design language and colour
themes as data). This feature branches from `feat/studio-ui` and lands after it.

**Current state (for reference)**:

- The desktop app wears one of four colour themes (Studio Dark — the default — Studio
  Light, Ember Graphite, Ember Paper) or follows the OS with a dark-mode and a light-mode
  pick. Every colour lives in the theme files; the switch is live.
- Nota Remote was never restyled. It still wears the old Ember look: warm browns and a
  brass accent, two palettes hard-coded in the page. Nota tells the phone only whether it
  is dark or light, so a phone next to a Studio-themed Nota looks like another app.
- The phone has its own theme preference in its ⋯ menu: **Follow Nota** (the default) or
  **Follow phone** (the phone's own dark/light mode). Today Follow phone picks one of the
  two Ember palettes.
- The page uses tracked uppercase labels ("PLAY ON TRACK", "MASTER", "ARM", "BAR 5"),
  rounded corners of 5–8 px, an accent for resting values (knob arcs, XY lines, the scale
  key source tag), a wash-tinted brass for toggles that are on, and grey/accent instead of
  the semantic mute and solo colours.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — The phone wears the theme Nota wears (Priority: P1)

A musician has Nota in Studio Dark and opens Nota Remote on their phone. The phone shows
the same greys, the same orange accent, the same track colours as the computer. They
switch Nota to Ember Paper in Preferences → Appearance; within a moment the phone turns
to Ember Paper too, without a reload and without dropping a held note.

**Why this priority**: it is the visible mismatch that prompted the feature, and it is
what makes the phone read as part of Nota.

**Independent Test**: with a phone (or a phone-width browser) connected and set to
Follow Nota, pick each built-in theme in Preferences → Appearance in turn and check the
phone's ground, panels, text and accent match the desktop's for that theme.

**Acceptance Scenarios**:

1. **Given** Nota in any built-in theme and the phone set to Follow Nota, **When** the
   phone connects, **Then** its first screen already wears that theme.
2. **Given** a connected phone, **When** the user picks another theme in Preferences →
   Appearance, **Then** the phone changes to it within one second, with no reload and the
   current screen, held notes and fader positions unaffected.
3. **Given** Nota set to Follow system, **When** the computer's OS switches between dark
   and light mode, **Then** the phone follows the theme Nota switches to.
4. **Given** the phone set to Follow Nota, **When** Nota's theme is light, **Then** the
   browser chrome tint (status bar, address bar where the browser shows one) matches the
   page's ground, not a dark colour.

---

### User Story 2 — Follow phone uses the user's own theme picks (Priority: P1)

A musician keeps Nota in Studio Dark but prefers their phone to follow its own dark/light
mode. In Preferences → Appearance they have picked Ember Graphite for dark mode and Studio
Light for light mode. With the phone on Follow phone, it wears Ember Graphite while the
phone is in dark mode and Studio Light when the phone turns light at sunrise.

**Why this priority**: the phone already offers Follow phone; without this it would fall
back to hard-coded colours that match no theme.

**Independent Test**: set distinct dark-mode and light-mode picks in Nota, set the phone
to Follow phone, switch the phone (or the browser's emulated colour scheme) between dark
and light, and check each time that the phone wears the matching pick.

**Acceptance Scenarios**:

1. **Given** Follow phone and a phone in dark mode, **When** connected, **Then** the phone
   wears the user's dark-mode pick, whatever Nota itself is wearing.
2. **Given** Follow phone, **When** the phone switches between dark and light mode,
   **Then** the page switches between the dark-mode and light-mode picks at once, without
   waiting for Nota.
3. **Given** Follow phone, **When** the user changes either pick in Preferences →
   Appearance, **Then** the phone updates live.
4. **Given** the user switches the phone's menu between Follow Nota and Follow phone,
   **Then** the page changes at once to the theme the new choice names.

---

### User Story 3 — Every phone screen reads as Studio (Priority: P2)

On every screen — Pads, Keys, XY, Mixer, Macros, Scenes, the big transport, the header,
the sheets (tracks, menu, key, devices), the pairing and connection covers, the count-in
and the toast — the phone follows the same rules the desktop app does: sentence-case labels
with no letter-spacing, flat panels meeting on 1 px seams, small square-ish corners, values
at rest drawn neutral with the accent only for what is playing, held, chosen or on, mute
amber, solo blue, arm and record red with ink derived from the fill, Geist for words and
Geist Mono for numbers.

**Why this priority**: the theme colours alone would still sit in Ember's shapes and
conventions (brass washes, tracked caps, accent knob arcs); the restyle completes the match.

**Independent Test**: screenshot every screen at phone width in Studio Dark and Studio
Light and review each against `DESIGN.md` § The rules.

**Acceptance Scenarios**:

1. **Given** any screen, **When** reviewed, **Then** no label is set in tracked capitals;
   capitals remain only for acronyms and units (BPM, MIDI, USB, dB), note names, Roman
   chord numerals, the axis letters X / Y and the M / S keys.
2. **Given** a toggle that is on (Play while playing, Loop, Click, Repeat, Full velocity,
   Expressive, Tilt, Follow), a chosen segment, the selected track row or a pad or key
   under the finger, **Then** it shows the accent, solid, with derived ink; **and** no
   element at rest (a knob arc, a fader, the XY crosshair, a resting pad, a tag) does.
3. **Given** the Mixer, **When** a track is muted, soloed or armed, **Then** the key turns
   solid amber, blue or red respectively with readable derived ink.
4. **Given** any surface, **Then** corners are 0, 2 or 3 px (4 is reserved for modals,
   which the phone has none of), panels and strips meet on seams, and no gradient, glow
   or inset dark shadow remains.

---

### User Story 4 — The theme travels with the build (Priority: P3)

A contributor changes a value in a theme file or adds a theme. The phone picks it up with
no edit to the phone app, and the build's smoke test checks that the phone is told the
theme's colours.

**Why this priority**: keeps the theme files the one place a colour lives, as on the
desktop.

**Independent Test**: the smoke test connects a phone over a real socket and checks the
theme message it receives.

**Acceptance Scenarios**:

1. **Given** the smoke test, **When** a phone connects, **Then** it receives a theme
   message naming the active theme, whether it is dark, and a value for each token the
   phone uses.
2. **Given** the host's theme changes, **When** the hub ticks, **Then** connected phones
   receive the new theme once, and an unchanged theme is not re-sent.

### Edge Cases

- **First paint before Nota answers** (the page opens, the socket is still connecting, the
  pairing cover, "Nota isn't responding"): the phone wears the last theme it received if
  it has one stored, otherwise a built-in Studio Dark fallback. No flash of Ember.
- **Follow phone before connection**: the phone uses the stored dark/light picks it last
  received; with none stored, the fallback.
- **An older Nota** (one that sends only `dark`): the phone wears the fallback, switching
  to the Studio Light fallback values when `dark` is false. *(Assumption: Nota serves its
  own page, so this only arises with a cached page; a reload heals it.)*
- **Track colours under Follow phone**: Nota sends each track's colour as it draws it, in
  Nota's active theme. When the phone wears a different theme, track colours stay Nota's.
  Track hues share one lightness across themes, so they stay readable on either ground.
- **A track-colour fill** (a pad the track plays, a playing or queued clip): the ink on it is
  derived from the fill, never fixed.
- **A theme file with a token the phone does not use, or a future token**: ignored by the
  phone. A token the phone uses but the message lacks keeps the fallback value.
- **The phone's own system colour scheme changes while backgrounded**: on return the page
  wears the right pick.

## Requirements *(mandatory)*

### Functional Requirements

**Theme on the phone**

- **FR-001**: Nota MUST tell each connected phone the colours of the theme it is wearing
  (its id, whether it is dark, and the values of the colour tokens the phone uses), when
  the phone connects and whenever the active theme changes — plus the two inks the desktop
  derives text on a solid fill from (dark and light), so the phone derives ink as Nota does.
- **FR-002**: Nota MUST also tell the phone the user's dark-mode and light-mode theme picks
  (with their colours), so Follow phone can wear them without asking Nota.
- **FR-003**: The phone MUST apply the theme by setting its colour variables from the
  message — no reload, no change to screen, held notes or controls.
- **FR-004**: Follow Nota MUST wear Nota's active theme; Follow phone MUST wear the dark-mode
  pick while the phone's colour scheme is dark and the light-mode pick while it is light,
  switching at once when either the phone's scheme or a pick changes.
- **FR-005**: The phone MUST NOT hold colour values of its own beyond a first-paint
  fallback (Studio Dark, with Studio Light for a light fallback). Every colour the page
  paints MUST come from a theme variable or be derived from one (a wash, a derived ink).
- **FR-006**: The phone MUST remember the last theme message it received and wear it on
  the next open until Nota answers.
- **FR-007**: The browser chrome tint MUST follow the worn theme's ground colour.
- **FR-008**: The theme message MUST be sent only when it changes (per phone), not on every
  tick.

**Studio on the phone**

- **FR-009**: Every label on the phone MUST be sentence case with no letter-spacing;
  capitals stay only on acronyms and units, product names ("Nota", "Nota Remote"), note
  names, Roman numerals, axis letters and the M / S key letters. The tracked-caps style
  is removed.
- **FR-010**: Corners MUST be 2 px (rows, inner keys, clips, badges, switch tracks), 3 px
  (buttons, fields, segmented groups, tiles — pads, scale cells, chords, macro cells —
  graph fields, popover sheets, the toast) or 4 px (modals only); docked bars, strips and
  full-screen covers 0. The piano keys' free ends keep the 2 px key corner.
- **FR-011**: Docked areas (header, banner, tab bar, rail, mixer strips, sheets) MUST meet on
  1 px seams; items inside a panel MUST be divided by 1 px rules. No gradients, glows or
  inset black shadows; a raised control carries the theme's top bevel.
- **FR-012**: The accent MUST appear only for what is live or chosen: playing, recording
  count-in, a toggle that is on, a chosen segment or bank, the selected row or screen, a
  pad / key / cell / chord under the finger, a value control while it is being dragged,
  the caret in a focused field. Resting values (macro knob arcs, faders, the pan
  indicator, the XY crosshair and point, the key-source tag, chord numerals) MUST be
  neutral.
- **FR-013**: A toggle that is on and a chosen segment MUST be a solid accent fill with
  derived ink (no wash-tinted brass).
- **FR-014**: Mute on MUST be solid mute amber, solo on solid solo blue, arm on and record
  on solid record red — each with derived ink; mute and solo keep their letters so they
  do not rely on colour alone.
- **FR-015**: Geist MUST set every word and Geist Mono every number a user reads (position,
  tempo, dB, pan, percentages, note numbers, latency, counts); names are never mono.
- **FR-016**: Numbers MUST be set as on the desktop: U+2212 minus, a thin space before a
  unit, fixed precision (dB one decimal, tempo two where fractional).
- **FR-017**: Opacity MUST NOT signal a state (disabled, muted, dimmed) except the existing
  time states: the link-lost dimming of the play surface and the blinks (a queued clip, the
  connecting / reconnecting dot, the code caret).
- **FR-018**: Layout, sizes of touch targets, gestures, screens and behaviour MUST stay as
  they are: this is a visual change.

**Build**

- **FR-019**: The built phone page in `Nota.Remote/wwwroot` MUST be rebuilt from the
  sources, and the type check of the phone sources MUST pass with no errors or warnings.
- **FR-020**: The smoke test MUST check the theme message a phone receives (contents, and
  that it is re-sent only when the theme changes).

### Key Entities

- **Theme message**: what Nota tells a phone about colour — the active theme, the dark-mode
  pick and the light-mode pick, each with an id, a dark/light kind and a map of token name
  → colour.
- **Phone theme preference**: Follow Nota or Follow phone (exists today; stored on the
  phone).
- **Stored theme**: the last theme message, kept on the phone for first paint.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For each of the four built-in themes, the phone's ground, panel, primary text
  and accent colours equal the desktop's values for that theme (exact match).
- **SC-002**: A theme change in Preferences reaches a connected phone in under one second,
  with no reload and no note cut.
- **SC-003**: Under Follow phone, switching the phone's colour scheme changes the page in
  under 100 ms, without waiting for Nota.
- **SC-004**: Zero colour literals in the phone's components; the only colour values in the
  phone sources are the first-paint fallback block.
- **SC-005**: Zero labels with letter-spacing or forced uppercase on any phone screen.
- **SC-006**: The full build and smoke test pass, including the new theme checks.
- **SC-007**: Screenshots of every phone screen in Studio Dark and Studio Light are reviewed
  against `DESIGN.md` § The rules with no open violations.

## Assumptions

- Phones are paired to and served by the same Nota, so the page and the hub are always the
  same version; protocol compatibility with older pages is best-effort (a cached page
  falls back gracefully and a reload fixes it).
- The phone uses a subset of the theme tokens; sending the full token set of three themes
  is acceptable on a local network (a few kilobytes, only on change).
- Track colours remain as Nota draws them in its active theme (see Edge Cases).
- The phone page keeps its own sizes (touch targets of 36–56 px) rather than the desktop's
  26 px shell controls; Studio's rules (colour, corners, seams, type, case) apply, its
  desktop control heights do not.
- The phone's type sizes stay phone-sized (11–16 px for words, larger for the big position
  and count-in); Studio's "nothing larger than 13" shell rule is a desktop rule.
- Nota Remote's icons stay as small inline drawings in the page; the desktop `Glyph` set is
  native code the page cannot use. They keep the shared stroke style (round caps and joins,
  ~1.5 px).

## Nota Compliance Checklist *(review before implementation; re-check in tasks.md)*

- [x] Every new source file will carry the AGPL-3.0-only SPDX header (two lines, see CONTRIBUTING.md)
- [x] No new dependency
- [x] Audio-thread impact considered: none — the hub builds messages on the UI tick; nothing touches the engine's audio thread
- [x] Managed/native crossings go through the pure C ABI in `nota_engine.h` only (AR-7); no JUCE outside `pluginhost/` — no crossing changes
- [x] Smoke-test impact assessed: the Remote session test gains theme-message checks
- [x] User-visible change → `CHANGELOG.md` `[Unreleased]` entry and FEATURES.md planned; the user manual's Remote page via nota-docs
- [x] UI change → follows `DESIGN.md` (Studio design language)
