# UI Contract: Studio Visual Style

The visual and behavioural contract every Nota surface must meet after this feature.
Each line is checkable by eye on a screenshot or by `DesignTokenCheck`. Token values:
[data-model.md](../data-model.md).

## C1 — Colour and accent

- C1.1 Every surface paints from tokens; no Ember value (warm brown neutral, brass,
  bronze, Ember track hue) appears in either appearance. *(Check: token values; grep for
  Ember hexes in `Theme/`.)*
- C1.2 With transport stopped, nothing selected, no toggle on and the pointer off any
  control: **zero** accent pixels on screen.
- C1.3 Accent appears on: play key while playing; playhead; loop range; selected clip
  band / note frame / row tick / tab underline; toggles that are on (metronome, follow,
  snap, automation, loop); selected segment; switch on; menu-item hover; device power key
  on; a value control while dragged or hovered; the selected graph node.
- C1.4 Resting knob arcs, fader/slider fills, value bars, graph curves and waveforms are
  neutral (`VizInk` / secondary ink). A modulation arc keeps its source chroma.
- C1.5 Mute on = amber fill, solo on = blue fill, arm/record on = red fill — arrangement,
  mixer and session alike — each with derived ink of ≥ 4.5:1 (text) / ≥ 3:1 (glyph).
- C1.6 Text on any solid fill (accent, mute, solo, arm, track colour) uses derived ink.

## C2 — Shell

- C2.1 Title bar 32 on ground, seam below; traffic-light inset (macOS) and 180 px caption
  reserve (Windows) unchanged.
- C2.2 Transport is a flat 44 px row on panel, full width, seam below; console is a 30 px
  recessed group; transport keys 26, Play 36 wide; order of items unchanged.
- C2.3 Browser, canvas (arrangement / session / modular) and detail panel are docked:
  radius 0, no gutter, separated by 1 px seams; splitters still resize with the same
  limits.
- C2.4 Every panel's header/toolbar is a 32 px panel-grey strip on a seam.
- C2.5 Status line on ground, 22 px, unchanged content.

## C3 — Controls and states

| State | Treatment |
|---|---|
| Rest (raised) | raised fill, primary text, `Shadow.Raised` (bevel + seam ring) |
| Hover | raised-hover fill (120 ms), nothing moves |
| Pressed | well fill |
| Toggled on / selected option | solid accent fill, derived ink |
| Selected row | raised-hover + 2 px accent left tick |
| Selected tab | primary text + 2 px accent underline |
| Focus (fields, readouts) | 1 px accent inset edge |
| Disabled | faint text, no opacity change |

- C3.1 Fields/readouts: well fill, 1 px rule inset edge, 3 px radius, 26 tall.
- C3.2 Menus / context menus / flyouts / drop-downs / tooltips: raised, 3 px radius, seam
  ring; menu items 24 tall, hover = full accent fill + derived ink, 2 px radius. No blur:
  Avalonia draws each popup in a window of its own, sized to its content, which would clip
  it (research R6).
- C3.3 Modal overlays (command palette): `Shadow.Modal`, 4 px radius. No scrim — the palette
  keeps its existing behaviour of not dimming the window (CP-17), per FR-018.
  OS dialog windows: panel body, raised bevelled header.
- C3.4 Radii only 0 (docked), 1 (plot bars), 2, 3, 4 (modals).

## C4 — Type and labels

- C4.1 No uppercase tracked labels anywhere in Nota's own UI; labels sentence case,
  letter-spacing 0; acronyms keep their case (LFO, EQ, MIDI, dB, Hz…).
- C4.2 Every number a user reads is Geist Mono; names are never mono.
- C4.3 No label truncates that fitted before (screenshot every device card).

## C5 — Device cards and views

- C5.1 Card: 700 × 260 (exceptions unchanged), panel body, 3 px, seam ring; header 22 on
  raised with bevel: power key, name, preset stepper, badge in muted text.
- C5.2 Sections on the panel tone, titled in muted 600 text, divided by rules.
- C5.3 Graph windows: well, 1 px rule frame, 3 px, viz-grid, viz-ink curve.
- C5.4 Arrangement clips: flat 2 px rect in track colour, darker title band
  (mix toward black .30), accent band when selected. Mixer strips: 3 px top bar in track
  colour (muted grey for master).

## C6 — Preserved behaviour (must not change)

- C6.1 All FEATURES.md capabilities, shortcuts, gestures, menus reachable as before.
- C6.2 Arrangement row heights, header column 228, overview 34 / sections 22 / ruler 24,
  piano-roll keys 48 / rows 12, card frames, knob sizes, `ParamCell` 53, list rows 26.
- C6.3 Project/preset/preferences formats unchanged; stored track colour indices valid.
- C6.4 Appearance switch (dark / light / System) is live.
