# Research: Studio Visual Style

Phase 0 decisions. Sources: the Studio direction in [spec.md](spec.md) (palette, type,
geometry, elevation, component treatments, accent rules) and a survey of
`src/managed/Nota.App` (file:line references below are from that survey, 2026-10-13).

## R1 — How the palette changes: remap slot values, keep slot names

- **Decision**: Keep every existing `Brush.*` key and `NotaPalette` slot name; re-point
  their dark and light values to the Studio role that matches their *use* (table in
  [data-model.md](data-model.md)). Add a handful of slots for roles Studio has and Ember
  lacked: `Seam`, `Rule`, `TextFaint`, `Mute`, `Solo`, `VizInk`, `VizGrid`, and the
  shadow resources `Shadow.Control`, `Shadow.Popover`, `Shadow.Modal`.
- **Rationale**: ~1,000 call sites reference these slots (Controls 385, DeviceCards 475,
  root views 171 counting `Brass` aliases). A value remap repaints all of them with no
  edit, keeps `DesignTokenCheck`'s parity and ban machinery working, and keeps the live
  appearance switch (slots re-tint in place).
- **Alternatives**: Renaming slots to role names (`bgWell`, `bgPanel`…) — touches
  every view for no visual gain and would break the Ink()/KeyMap resolution; rejected.
- **Consequence**: Some slot names now lie (`BorderBrass`, `AccentSubtle` "Brass Wash").
  Comments are updated; names stay. A rename pass can follow later if wanted.

## R2 — Sentence-case labels

- **Decision**: Add one helper, `NotaText.Label(string)`, that turns an all-caps label into
  sentence case ("PRE-DELAY" → "Pre-delay", "FREQ" → "Freq") and keeps a fixed acronym
  list upper-case (LFO, EQ, ADSR, MIDI, CV, FM, AM, HP, LP, BP, BPM, XY, VST3, AU, A/B,
  L/R, M/S, dB, Hz, kHz, ms…). Route every label path through it:
  - the four builders that uppercase today — `DeviceCardKit.KnobCell` (:66), `Switch`
    (:91, :102), `SliderRow` (:214), `Labeled` (:317) — replace `ToUpperInvariant()`;
  - the 39 local `Caps(...)` helpers and the two `FormattedText Caps(...)`
    (`ChamberViews.cs:33`, `PrismViews.cs:41`) — call the helper, drop
    `LetterSpacing 0.8` and Bold;
  - `NotaGraph` titles (:79, :121), the card `Subtitle` badge
    (`DeviceChainView.Shell.cs:132`), and the remaining `.ToUpper*()` call sites that
    produce labels (53 across 39 files; note names and key names excluded);
  - the theme's caps classes (`SectionLabel`, `CellLabel`, `Eyebrow`, `DeviceSection`,
    `RowLabel`, `KnobLabel`, `GroupLabel`) lose letter-spacing; `NotaType.*Tracking`
    become 0;
  - literal captions in XAML (transport `LOOP`, `BPM`, `TAP`, `SIG`, `KEY`, `GRID`, `MST`)
    are edited by hand.
- **Rationale**: ~1,600 all-caps literals exist; converting at the render choke points
  changes the look everywhere without touching every literal or the MCP-facing names.
- **Alternatives**: Rewriting all literals — huge diff, high risk of breaking MCP
  parameter names that share strings; rejected. CSS-like text-transform — Avalonia has
  none; rejected.
- **Guard**: `DesignTokenCheck` gains a rule: no `ToUpperInvariant()/ToUpper()` in the
  label builders and `Caps` helpers, and no positive `LetterSpacing` in the theme's text
  classes.

## R3 — Accent only for live or chosen state

- **Decision**:
  - `Knob`: resting arc → `VizInk` (neutral); arc and pointer turn `Accent` while
    dragged or under the pointer (add hover tracking; `_drag` exists at :81). A
    modulation `ArcColor` keeps its source chroma (that is data, not state). `IsModified`
    no longer colours label/value in brass; a modified value is `TextPrimary`, an
    unmodified one `TextSecondary`.
  - Same rule for `MiniFader`, `PanBar`, `SliderTrack`, `RangeSliderTrack`, `ValueBar`,
    `MixBar` (resting fill → `VizInk`/`TextSecondary`; accent while dragged/hovered).
  - `SwitchTrack` on and `Segments` selected stay accent — a toggle that is on and a
    chosen option are "chosen".
  - `NotaGraph`: `Chroma(0)`/`PrimaryPen` → `VizInk`; only the selected or dragged node is
    accent. Secondary chromas become Studio's teal / rose / blue.
  - Waveforms at rest (`WaveformView.WavePen`, `AccentMute` previews) → neutral ink.
  - Device views with their own resting accent fills (ChamberViews, BeatRepeatViews,
    ConsortViews, ModularView, PianoRollView…) are audited file by file; selection,
    playhead, loop, play state and "playing step" keep the accent.
- **Rationale**: Spec FR-004 and the clarified decision. The shared controls carry most
  of the resting accent, so fixing them fixes most cards.
- **Alternatives**: Re-pointing `NotaPalette.Accent` itself to a neutral — would also
  kill legitimate selection/playhead uses; rejected.

## R4 — Mute, solo, arm

- **Decision**: New slots `Mute` (= Studio warning amber) and `Solo` (= Studio blue);
  arm/record keeps `Record` (= Studio error red). These are fixed state colours, so an
  engaged mute, solo or arm reads the same everywhere. Engaged = solid fill with derived ink;
  rest = raised key with control shadow. Applies to arrangement `ChipToggle`
  (`ArrangementView.cs:2379`) and `ArmToggle` (:2359), mixer `Toggle`/`ArmToggle`
  (`MixerView.cs:417`, :401), session `MixToggle` (`SessionView.Mixer.cs:182`). The muted
  band `MuteTint` becomes an amber-tinted ground.
- **Derived ink**: add `NotaPalette.InkOn(SolidColorBrush fill)` — a `Derived` brush
  choosing `#17181a` or `#ffffff` by contrast. Used for text
  on accent, mute, solo, arm and track-colour fills.

## R5 — Shell: docked panels on seams

- **Decision**:
  - `DockPanel`/`BodyGrid` background `Gutter` → `Seam`; island margins 12 → 1 px only
    where two panels meet (the seam shows through), 0 at window edges.
  - Island radius (`Radius.Panel` on browser, arrangement, modular, detail panel,
    transport) → new `Radius.Docked` = 0. `BorderInnerClip` keeps working (radius 0).
  - Splitters: the 12 px transparent grab strips with −12 margins become 6 px strips with
    −3 margins centred on the seam — same drag behaviour, same minimum sizes.
  - Title bar 36 → 32 on `BgApp` (ground) with a seam below; macOS traffic-light inset and
    Windows' 180 px caption reserve unchanged.
  - Transport: the 60 px island becomes a flat 44 px row on `Panel` with a seam below; the
    console becomes a 30 px recessed group (`well` + field edge), transport keys 26 tall,
    Play 36 wide — Studio's transport metrics. Contents and order unchanged;
    `MainWindow.TransportFit.cs` folding thresholds are re-measured.
  - Detail panel header stays 32 (already Studio's panel-toolbar height) on `Panel` with a
    seam below. Status line stays 22 on ground.
  - `SessionView` already has no island chrome; it only takes the tokens.
- **Rationale**: Clarified decision 1. Panels butted edge to edge on 1 px seams leave
  more room for the work than floating islands and make the structure clearer; every
  panel opens with a 32 px panel toolbar on a seam, and the transport is one row plus a
  seam (40 px in the general metric). 44 rather than 40 keeps Nota's 42 px console
  contents (position + loop) legible without reflow.
- **Alternatives**: Keep 60 px transport (looks like Ember's island); an in-window 30 px
  menubar row (Nota uses a native menu on macOS — not a styling change); rejected.
- **Guard updates** (`DesignTokenCheck.RunLayout`): TransportBar 44, console 30,
  transport keys 26, Play 36; header 32; docked radius 0.

## R6 — Elevation: bevel, seam ring, floating shadows

- **Decision**: Per-variant `BoxShadows` resources in `NotaTheme.axaml`:
  - `Shadow.Raised` → `inset 0 1 0 0 #12FFFFFF, 0 0 0 1 <seam>` (light: `#B3FFFFFF` bevel);
  - `Shadow.Sunken` → `inset 0 0 0 1 <rule>` (field edge);
  - `Shadow.Popover` → `0 0 0 1 <seam>, 0 10 28 0 #6B000000` (light `#381E2024`);
  - `Shadow.Modal` → `0 0 0 1 <seam>, 0 24 64 0 #80000000`.
  Menus, context menus, flyouts, ComboBox drop-downs and tooltips get the raised fill and a
  seam ring but **no blur**: implementation showed Avalonia popups are separate windows sized
  to their content, so a blur is clipped (and can render as dark corners on Linux). The
  blurred shadows serve in-window overlays (command palette). The command
  palette overlay gets `Shadow.Modal` (no scrim: it keeps CP-17, never dimming the window). OS dialog windows (`NotaWindow`) get no
  app shadow (the OS draws one); their header becomes a raised bevelled strip.
- **Guard**: `new BoxShadows` stays banned in `.cs`; a new XAML check allows blurred
  shadows only in `Shadow.Popover` / `Shadow.Modal` and their users (menus, flyouts,
  tooltips, palette). `BoxShadows` keys join the Dark/Light parity check.
- **Avalonia note**: a popup's BoxShadow renders inside the popup's own window, so the
  presenter needs ~12 px transparent margin (bottom ~28) or the shadow is clipped.

## R7 — Radius scale 2 / 3 / 4

- **Decision**: Remap `Radius.*` values (and `NotaRadius` mirrors): `Bar` 1 (plot bars,
  unchanged), `Clip` 2, `Badge` 2, `Control` 3, `Tile` 3, `Panel` 3, `Body` 3, `Pill` 3
  (chips and switches become nearly square). Add `Radius.Modal` 4 (splash,
  command palette) and `Radius.Docked` 0. Fix the two literal `CornerRadius="2"`
  (`MainWindow.axaml:365`, :383) to keys and render-call radii such as `ValueBar.cs:105`.
- **Rationale**: FR-010. Nearly square corners suit flat panels that meet on seams:
  seams, not rounding, separate surfaces. The scale is small / medium / large = 2 / 3 / 4
  px, with pills at 3 px.

## R8 — Type scale

- **Decision** (Geist / Geist Mono already bundled):

  | Class | Ember | Studio |
  |---|---|---|
  | `Title` | 600 · 26 | 600 · 13 |
  | `Heading` | 600 · 13 | 600 · 13 |
  | `Name` | 500 · 12 | 500 · 12 |
  | default (buttons, tabs) | 500 · 11 | 500 · 12 |
  | `Caption` | 400 · 11, Ink 4 | 400 · 11, secondary |
  | `SectionLabel` / `GroupLabel` | 700 · 10 caps .14em | 600 · 11, muted, sentence case |
  | `Readout` | Mono 500 · 13 | Mono 400 · 13 (position 14, .02em) |
  | `Value` | Mono 400 · 10 | Mono 400 · 11 |
  | `Eyebrow` (badges) | Mono 500 · 9 caps .18em | Mono 400 · 10, muted, as written |
  | `DeviceName` | 600 · 12 | 600 · 12 |
  | `DeviceSection` | 700 · 9 caps | 600 · 10.5, muted |
  | `RowLabel` | 700 · 8 caps | 500 · 9 |
  | `KnobLabel` | 700 · 7 caps | 500 · 8 |
  | `KnobValue` | Mono 400 · 7 | Mono 400 · 8 |
  | `Axis` | Mono 400 · 7 | Mono 400 · 7.5 |

  Device text stays smaller than the shell's type because the 700 × 260 frame is kept
  (clarification 2); sentence case is narrower than tracked caps, so 8 px sentence case
  fits where 7 px caps did. `ss01` is enabled on the UI font via `FontFeatures` where
  Avalonia 12 supports it on `TextBlock`; otherwise skipped (cosmetic).
- **Verification**: truncation check by screenshot of every device card (quickstart).

## R9 — Icons

- **Decision**: `Glyph` stroke = 1.5 px at 16 px (scaled, min 1.2), round caps/joins; play
  and record stay filled. No icon set swap — Nota's glyphs are already monoline
  geometry.

## R10 — Track colours

- **Decision**: Keep nine roles and the base × 3 + shade index scheme
  (`ArrangementView.cs:2683`, `PaletteShades = 3`). Retint the nine bases in Studio's
  muted mid-saturation style: five base clip colours where the hue family matches, and
  new hues at matching chroma for the rest (values in data-model.md). Return buses take
  a neutral slate; Master becomes a muted grey. Stored indices are not
  migrated (same as today).
- **Rationale**: FR-020; spec assumption on nine roles.

## R11 — Stock control retint and unstyled popups

- **Decision**: Fluent `SystemAccentColor*` ramps → orange. Add Nota styles for
  `ToolTip` (raised, 11 px primary, 3 px, popover shadow) and plain `FlyoutPresenter`
  (raised, popover shadow) — today they fall back to Fluent defaults (84 files use
  tooltips). Menu item hover → full accent fill with derived ink, 24 px items, 2 px
  radius; gesture text uses `TextMuted` instead of `Opacity 0.45` (opacity is not a
  state).

## R12 — Rules carried over vs. dropped

Kept from Ember (they fit Studio): one accent; no gradients/glow/textures; state
changes colour not size; real-time not animated; numbers set (thin space, U+2212, fixed
precision); Geist for words, Geist Mono for numbers; no literals in views; nothing may
snapshot a colour; opacity is not a state.

Changed: "no drop shadows" → "blurred shadows only on floating layers"; "caps labels" →
"sentence case, no tracking"; "brass marks the audio path at rest" → "accent only live
or chosen"; "role chromas brass · teal · rose · steel" → "viz ink · teal · rose · blue";
islands → docked seams; radius scale.

## R13 — Documentation

- **Decision**: Rewrite DESIGN.md as the **Studio** design language, keep
  its structure (source-of-truth table, rules, token tables, type, controls, shell,
  tests, accepted exceptions, drift). Update `scripts/design-html.py` key lists and names (it
  hard-codes Ember groups at :31-36, :59, :238-269) and regenerate DESIGN.html. Rewrite
  `.claude/skills/nota-design/SKILL.md`. FEATURES.md lines that name Ember/brass
  (:53, :347, :810, :923-925, :975). CHANGELOG `[Unreleased]` → Changed.
- **Not reachable**: `../nota-docs` (user manual + screenshots) does not exist in this
  environment; recorded as a follow-up in the PR.

## R14 — Verification approach

- **Decision**: `scripts/build-linux.sh` (native + managed + smoke test incl.
  `DesignTokenCheck`) must pass. Visual check: run the app under Xvfb, capture each main
  surface in dark and light, compare against the Studio rules (checklist in
  [contracts/ui-contract.md](contracts/ui-contract.md)). Baseline screenshots are taken
  before the change for the before/after in the PR.
