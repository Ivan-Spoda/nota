# Feature Specification: Device categories in the browser

**Feature Branch**: `013-browser-categories`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User description: "Better categorise the built-in devices in the browser. Today each browser tab (Instruments, Audio effects, MIDI effects) shows the built-in devices as one long flat list. Give every device a category (for example synths, drums, samplers, keys for instruments; filters, dynamics, reverb/delay, modulation, distortion, EQ, utility for effects) and let the list be grouped/filtered by category, with that category view enabled by default."

## Nota Context *(fill in — drives plan and tasks)*

**Feature area** *(pick the main one)*:

- [ ] Engine (C++, `src/native/nota.engine`) — real-time audio rules apply, see `ARCHITECTURE.md` AR table
- [x] Managed app (C#, `src/managed/*`) — domain, application, infrastructure, presentation
- [x] UI (Avalonia, `src/managed/Nota.Presentation`) — follow `DESIGN.md` (Studio)
- [ ] Plugin hosting (`pluginhost/`) — the only place JUCE is allowed
- [ ] Build / packaging / CI (`scripts/`, `.github/`) — see `BUILD.md`
- [ ] Docs only

**Nota docs to keep in mind** *(links relative to `specs/<slug>/`, where this file lands)*:
[`ARCHITECTURE.md`](../../ARCHITECTURE.md) (AR rules),
[`CONTRIBUTING.md`](../../CONTRIBUTING.md) (licensing, dependency policy, PR
expectations), [`BUILD.md`](../../BUILD.md), [`DESIGN.md`](../../DESIGN.md),
[`FEATURES.md`](../../FEATURES.md).

**Builds on**: `012-ui-polish` (browser rows without the "Nota" prefix, quieter type tags,
the shared icon set). Stacked on that branch.

### Today

- The Instruments tab lists 18 built-in instruments, the Audio effects tab 26 built-in
  effects and the MIDI effects tab 6 MIDI effects. Each tab is one alphabetical list under a
  single "Built-in" header, followed by a "Plug-ins" header for installed plug-ins.
- Each row already carries a short type tag on the right ("wavetable synth", "drum
  machine", "envelope / LFO"…), but the tag is not something the list can be grouped or
  narrowed by.
- The view options (⋮) offer "Group by source" (the Built-in / Plug-ins headers),
  "Favorites first" and "Show type tags". The filter chips under the search field are
  Favorites plus the user's own tags.
- Installed plug-ins report their own category (for example a VST3 "Fx|Reverb" or an Audio
  Unit type); the browser keeps it but does not use it for grouping.

## Clarifications

### Session 2026-10-14

- Q: How should the categories appear? → A: ~~As section headers in the list, each
  foldable. No category chips.~~ **Revised after seeing the headers built:** as category
  chips, like Ableton — a row of chips above the list; picking one narrows the list to that
  category. No category headers and no folding.
- Q: How do the chips behave? → A: One category at a time per tab: a click picks it, a
  second click clears it. Each tab keeps its own pick while switching tabs; a restart clears
  it. Chips combine with search, Favorites and tags; with no chip picked the list is today's
  list. The chip row is on by default and "Category chips" in ⋮ hides it.
- Q: Should plug-ins be categorised too? → A: Yes — plug-ins join the same categories from
  the category they report; anything unrecognised goes under "Other".
- Q: How should the 18 instruments be grouped? → A: Synths (Synth, Volt, Bass, Monolith,
  Pentad, Consort, Operator, Aurora, Flux, Pendulum) · Keys & physical (Keys, Physical) ·
  Drums (Rhythm, Drum Rack) · Samplers (Sampler, Mosaic, Grain) · Racks (Instrument Rack).
- Q: How should the 26 built-in audio effects be grouped? → A: EQ & filter (EQ-3, EQ-8,
  Dynamic EQ-8, Auto Filter) · Dynamics (Compressor, Prism, Ceiling, Shutter) · Reverb &
  delay (Reverb, Chamber, Delay) · Modulation (Chorus, Flanger, Phaser, Orbit) · Distortion
  (Valve, Vintage, Forge, Crush) · Pitch & time (Auto Shift, Beat Repeat, Strata) · Utility
  (Utility, Level, Lens) · Racks (Audio Effect Rack).
- Q: Should the 6 MIDI effects be categorised? → A: Yes — Generate (Arp, Chord, Random) ·
  Shape (Scale, Length, Velocity).

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Narrow the list to one kind of device (Priority: P1)

A producer opens the Audio effects tab looking for "something for space". Above the list
is a row of category chips — EQ & filter, Dynamics, Reverb & delay, Modulation, Distortion,
Pitch & time, Utility, Racks. They click **Reverb & delay** and the list shows just Chamber,
Delay and Reverb. A second click on the chip brings the whole list back. The Instruments
tab (Synths, Keys & physical, Drums, Samplers, Racks) and the MIDI effects tab (Generate,
Shape) work the same way.

**Why this priority**: it is the whole point of the request. The list grows with every
release, and a category is how musicians think about devices ("I need a compressor", not
"I need something starting with C"). It is how Ableton's browser works, which many users
know.

**Independent Test**: on a fresh profile, each device tab shows its category chips in a
fixed order; picking a chip lists exactly that category's devices; clearing it restores the
full list.

**Acceptance Scenarios**:

1. **Given** a fresh profile, **When** the Audio effects tab is opened, **Then** the 8
   category chips show above the list, none picked, and the list is today's full list.
2. **Given** the Audio effects tab, **When** the user picks Reverb & delay, **Then** the
   list shows exactly Chamber, Delay and Reverb, and the chip shows as engaged.
3. **Given** a chip is picked, **When** the user clicks it again or picks another one,
   **Then** the list returns to all devices or switches to the other category.
4. **Given** a chip is picked, **When** the user double-clicks or drags a device, expands
   it to see its presets, favourites it or tags it, **Then** everything works exactly as in
   the full list.

---

### User Story 2 — Hide the chips (Priority: P2)

A long-time user who knows every device by name doesn't want the extra row. They switch
"Category chips" off in the browser's view options; the row disappears, the list is the
full list, and the choice is remembered the next time Nota starts.

**Why this priority**: the chips are the new default, but nobody should lose the compact
browser they already know.

**Independent Test**: switch the chips off, restart Nota, and the row is still hidden and
the list unfiltered; switch them on again and the chips return.

**Acceptance Scenarios**:

1. **Given** a chip is picked, **When** the user turns "Category chips" off, **Then** the
   row hides and the list shows every device again.
2. **Given** the user turned the chips off, **When** Nota restarts, **Then** they are still
   off.

---

### User Story 3 — Combine a category with search and favourites (Priority: P2)

A user picks Dynamics, then types "multi" in the search, or turns on the Favorites chip:
the list shows only devices that are in Dynamics *and* match the search / are favourites.

**Why this priority**: narrowing works best when the filters stack; a chip that reset the
search or the Favorites chip would feel broken.

**Independent Test**: pick Dynamics, type "Prism": only Prism shows; clear the search:
Dynamics' four devices; turn on Favorites with Compressor favourited: only Compressor.

**Acceptance Scenarios**:

1. **Given** Dynamics is picked, **When** the user searches, **Then** only matches inside
   Dynamics show, and the search-box count counts them.
2. **Given** Dynamics is picked, **When** the Favorites chip (or a tag chip) is on, **Then**
   only Dynamics devices that are favourites (or carry the tag) show.
3. **Given** a chip is picked on Audio effects, **When** the user switches to Instruments
   and back, **Then** Instruments is unfiltered and Audio effects still has its chip picked.

---

### User Story 4 — Plug-ins in the same categories (Priority: P3)

A user with installed plug-ins picks Reverb & delay and sees a third-party reverb alongside
Nota's own reverbs.

**Why this priority**: valuable for users with many plug-ins, but the built-in categories
deliver the core of the request on their own.

**Independent Test**: with a plug-in that reports a reverb category installed, picking
Reverb & delay lists it; a plug-in reporting nothing recognisable is listed under an
"Other" chip.

**Acceptance Scenarios**:

1. **Given** an installed plug-in that reports a category Nota recognises, **When** that
   category's chip is picked, **Then** the plug-in is listed (under the Plug-ins header when
   "Group by source" is on).
2. **Given** a plug-in whose category Nota does not recognise, **When** the tab is shown,
   **Then** an "Other" chip appears last and lists it; the plug-in is never hidden from the
   full list.

### Edge Cases

- **A chip with a search that matches nothing in it**: the list is empty and shows the
  existing "No matches" state.
- **"Other" with nothing in it** (no unrecognised plug-ins, or the MIDI tab): no "Other"
  chip.
- **Favorites first / Group by source**: unchanged — they order the narrowed list exactly
  as they order the full one.
- **A device that fits two categories** (Auto Filter: filter or modulation? Strata: pitch
  or texture?): it is in exactly one, its primary job. No duplicates.
- **Rack devices** (Instrument Rack, Drum Rack, Audio Effect Rack) have a clear place.
- **A new built-in device added in a later release without a category**: it is still
  listed in the full list and under "Other", and the design check fails until it gets one.
- **A plug-in reporting several categories** (for example VST3 "Fx|Delay|Reverb"): it is
  in one category, the first Nota recognises.
- **Narrow browser**: the chip row wraps onto a second line rather than hiding chips;
  nothing overlaps.
- **Turning the chips off while one is picked**: the pick clears with the row, so a hidden
  filter never narrows the list.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every built-in instrument, audio effect and MIDI effect MUST belong to
  exactly one category. The categories and their order MUST be fixed per tab (listed in
  Assumptions), named in sentence case.
- **FR-002**: The device tabs MUST show a row of category chips above the list, in the
  tab's category order, wrapping when the browser is narrow. Picking a chip MUST narrow the
  list to that category; a second click MUST clear it; picking another MUST switch. One
  chip is picked at most per tab. There are no category headers and no folding.
- **FR-003**: The chip row MUST be shown by default, for a new profile and for existing
  users, and MUST be switchable off in the browser's view options ("Category chips"); the
  choice MUST persist across restarts. Turning it off clears any picked chip.
- **FR-004**: The picked chip is remembered per tab while Nota runs (switching tabs keeps
  it) and is cleared by a restart.
- **FR-005**: Installed plug-ins MUST join the same categories, mapped from the category
  they report; a plug-in whose category is missing or not recognised MUST be listed under an
  "Other" chip (last, shown only when it has devices), and always in the full list.
- **FR-006**: The chip MUST combine with search, the Favorites chip and tag chips (a device
  shows only if it passes all of them). With no chip picked the list MUST be exactly
  today's list. "Group by source" and "Favorites first" order the narrowed list as they
  order the full one.
- **FR-007**: Opening, dragging, previewing, expanding presets, favouriting and tagging a
  device MUST work identically with a chip picked.
- **FR-008**: Category chips MUST follow `DESIGN.md`: the existing filter-chip style
  (20 tall, pill, engaged in the accent wash), sentence case, no new colours or icon
  styles.
- **FR-009**: The design checks MUST fail if a built-in device has no category, so a device
  added later cannot silently land under "Other".
- **FR-010**: No device, preset, shortcut or saved project changes. Tags and favourites
  are unaffected. The Files, Presets, Projects, Map and History tabs are unchanged.

### Key Entities

- **Device category**: a named group on one tab ("Reverb & delay"), with a fixed position
  in that tab's order. A built-in device has exactly one. Plug-ins map to one from the
  category they report, or to "Other".
- **Category chips setting**: show the chip row, on/off, per user, persisted, default on.
- **Picked category**: per tab, at most one, held while Nota runs.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100 % of built-in devices (18 instruments, 26 audio effects, 6 MIDI effects)
  belong to a category, each to exactly one, and each chip lists exactly its devices.
- **SC-002**: Getting to a given kind of device (for example "a delay") takes one click on
  a chip, after which the list fits on one screen in a 1200 × 800 window.
- **SC-003**: With no chip picked, or with the chips off, the list is exactly today's list
  (same order, same headers); the off setting survives a restart in 3 of 3 tries.
- **SC-004**: Every existing browser action (open, drag, preview, expand presets,
  favourite, tag, search) works with a chip picked, with no regressions in the smoke tests.
- **SC-005**: The chips read correctly in all four built-in themes and pass the design
  checks.

## Assumptions

- **Categories** (confirmed in clarify; names sentence case):
  - *Instruments* (confirmed): Synths (Synth, Volt, Bass, Monolith, Pentad, Consort,
    Operator, Aurora, Flux, Pendulum), Keys & physical (Keys, Physical), Drums (Rhythm, Drum
    Rack), Samplers (Sampler, Mosaic, Grain), Racks (Instrument Rack) — in that order.
  - *Audio effects* (confirmed): EQ & filter (EQ-3, EQ-8, Dynamic EQ-8, Auto Filter),
    Dynamics (Compressor, Prism, Ceiling, Shutter), Reverb & delay (Reverb, Chamber, Delay),
    Modulation (Chorus, Flanger, Phaser, Orbit), Distortion (Valve, Vintage, Forge, Crush),
    Pitch & time (Auto Shift, Beat Repeat, Strata), Utility (Utility, Level, Lens), Racks
    (Audio Effect Rack) — in that order.
  - *MIDI effects* (confirmed): Generate (Arp, Chord, Random), Shape (Scale, Length,
    Velocity) — in that order.
- The existing type tag on each row stays; the category is a separate, coarser grouping.
- "Other" exists only for plug-ins (or a future uncategorised device); its chip is last
  and only shown when it has devices.
- Plug-in categories are mapped from the VST3 sub-categories and Audio Unit types the
  plug-in catalogue already stores; the exact mapping table is a planning detail.
- The chip-row setting is a per-user preference stored with the other browser view options;
  the picked chip is not saved. No project-format change.
- Picking one chip at a time (not several) keeps the list's meaning obvious; picking
  several can follow if users ask for it.
- Users of other languages see the categories in English, like the rest of the app today.

## Nota Compliance Checklist *(review before implementation; re-check in tasks.md)*

- [ ] Every new source file will carry the AGPL-3.0-only SPDX header (two lines, see CONTRIBUTING.md)
- [x] No new dependency
- [x] Audio-thread impact considered: none (browser UI and catalogue data only)
- [x] Managed/native crossings go through the pure C ABI in `nota_engine.h` only (AR-7); no JUCE outside `pluginhost/` — no new crossing
- [ ] Smoke-test impact assessed: design check for uncategorised built-ins (FR-009); browser grouping covered by the existing browser tests
- [ ] User-visible change → `CHANGELOG.md` `[Unreleased]` entry planned, FEATURES.md (browser section)
- [ ] UI change → follows `DESIGN.md` (Studio design language)
