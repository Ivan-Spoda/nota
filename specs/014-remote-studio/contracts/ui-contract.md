# Contract: Studio on the phone

What each phone surface becomes. Variables are the theme tokens in kebab case
([theme-message.md](theme-message.md)); `ink-on-*` are derived
([data-model.md](../data-model.md)).

## Shared

| Thing | At rest | Live / chosen |
|---|---|---|
| Ground | `bg-app` | — |
| Docked bar (header, banner, tab bar, rail) | `bg-app` / `panel`, a 1 px `seam` edge to its neighbour, radius 0 | — |
| Button `.btn` | `surface-raised`, 1 px `seam` border, bevel `inset 0 1px 0 shadow-bevel`, radius 3, `text-strong` 12/500 | on: solid `accent`, `ink-on-accent`, no bevel · pressed: `bg-sunken`, no bevel |
| Segmented `.seg` | keys `surface-raised` butted on 1 px `seam`, group radius 3, outer corners only | chosen: solid `accent`, `ink-on-accent`, weight unchanged |
| Field / readout | `bg-sunken`, 1 px `rule` edge, radius 3, mono value | focus: edge `accent` |
| Switch | 28 × 16, radius 2, squared knob 12; off `bg-sunken` track + `text-tertiary` knob | on: `accent` track + `panel` knob |
| Section label `.label` | Geist 600 · 11, `text-muted`, sentence case, no tracking | — |
| Row | 52 tall, `rule` divider, radius 0 inside its sheet | selected: `surface-hover` + 2 px `accent` tick |
| Badge (You, a name, "Anna has it") | `bg-sunken`, radius 2, `text-secondary` 10/600 | — |
| Popover sheet, toast | `surface-raised`/`panel`, radius 3, `0 0 0 1px seam, 0 10px 28px shadow-popover` | — |
| Scrim | `shadow-modal` | — |
| Numbers | Geist Mono, tabular; U+2212 minus; thin space before units | — |
| Icons | inline SVG, stroke 1.5, round caps and joins, `currentColor` | — |

## Screens

| Surface | Change |
|---|---|
| Header | Track button raised with seam; colour bar 3 px radius 1; position readout a well with rule edge; Stop / Play / Record keys raised; Play on = solid accent; Record at rest = raised key with a `record` disc, on or counting in = solid `record` with a `record-ink` disc; link dot `success` / `warning` / `danger` |
| Banner | `panel` strip, `seam` below; MIDI Learn: an `accent` dot, title `text-primary`, sub `text-secondary`; reconnecting: `text-muted` dot |
| Screen tabs (portrait) | Ghost keys on the bar; selected = `text-primary` + 2 px `accent` line on the edge facing the screen |
| Rail (landscape, tablet) | Ghost rows; selected = `surface-hover` + 2 px `accent` tick |
| Pads | Tiles radius 3, raised with seam + bevel; under the finger = solid `accent`, `ink-on-accent`; played by the track = solid track colour, derived ink; note numbers mono `text-tertiary` |
| Keys | White keys `key-white`, black keys `key-black` with a `seam` edge, free-end corners 2; held = `accent` with `ink-on-accent`; lit band = track colour; scale cells and chords tiles radius 3; tonic cells `surface-hover`; key-source tag and chord numerals neutral (`text-muted`) |
| XY | A graph field: `bg-sunken`, 1 px `rule` frame, radius 3, `viz-grid` lines; crosshair `rule` at rest; point a `viz-ink` ring at rest — `accent` while dragged or under tilt; no fill under the point; axis labels mono `text-faint` in the corners; "Tilt is moving the point" an `accent` chip with `ink-on-accent` |
| Mixer | Strips `panel`, meeting on 1 px `seam`; group strips `bg-sunken`; fader slot a well; cap raised with seam + bevel, its line `text-primary` → `accent` while dragged; pan dot `viz-ink` → `accent` while dragged; dB mono, `accent-bright` while dragged; meters `success` → `warning` (−6 dB) → `danger` (0 dB); M on = solid `mute` + `ink-on-mute`, S on = solid `solo` + `ink-on-solo`, Arm on = solid `record` + `ink-on-record`; "Master" a label |
| Macros | Cells radius 3 on `panel`; knob: groove `bg-sunken` r21 stroke 5, face `surface-raised` r14 with a `seam` edge, value arc `viz-ink` → `accent` while dragged, pointer `text-primary`; name sentence case as the parameter is named (no forced caps); automated dot `text-muted` |
| Scenes | Clip slots radius 2; filled = raised; playing / queued = solid track colour with derived ink (queued blinks); recording = solid `record` + `ink-on-record`; progress line in the clip's ink; empty = `bg-sunken`; status words sentence case |
| Big transport | Keys radius 3, raised; Play on, Loop on, Click on = solid `accent`; Record on = solid `record`; position mono `text-primary` (`record` while recording); sections: current = 2 px `accent` tick, looping = solid `accent`; "Bar 5" / "Looping" sentence case |
| Covers (down, pair, left) | Full-screen `bg-app`, radius 0; steps list `panel` rows with `rule` dividers, radius 3; step numbers mono `text-muted`; code cells wells with `rule` edge, current = `accent` edge, caret `accent`; "Connect again" an ordinary raised key, 600 |
| Count-in | `bg-app`; the number mono `accent-bright`; caption sentence case in `record` |
| Empty (audio track) | Centred; title `text-primary` 13/600, line `text-tertiary`, an ordinary key |
