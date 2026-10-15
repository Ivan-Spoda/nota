# UI Contract: UI Polish Pass

Each line can be checked by eye on a screenshot or by an automated check. The Studio
contract (010 C1–C6) and the theme contract (011 T1–T4) still hold.

## P1 — Layout defects

- **P1.1** Adding a track scrolls it fully into view. The scroll area and the pinned
  return/master band meet on a 1 px `Seam`. Below the last track, the lane side is lane
  colour and the header side is header colour.
- **P1.2** At a 1200 × 800 window in Session view, every toolbar control is either visible
  or listed in the toolbar's "⋯" overflow menu. The inspector can be toggled (the setting
  is remembered), is 260 px wide, and covers no column header. When the content is wider
  than the space, a scroll cue shows.
- **P1.3** Opening Modular with tracks and no selection lists the tracks and selects the
  first non-return track. An empty project shows "No tracks yet" in the sidebar. With no
  graph to show, the canvas shows "Pick a track to see its signal graph".

## P2 — Floating windows

- **P2.1** The mixer window's width equals the width of its strips and master plus
  padding, clamped between 560 px and the screen width. There is 0 px of dead space to the
  right of the master.
- **P2.2** Escape closes the mixer, the export window (as Cancel) and the command palette.
- **P2.3** Window titles are in sentence case: "Mixer", "Export audio", "Edit tags",
  "Connect phone"…

## P3 — Labels

- **P3.1** No all-caps or Title Case label in Nota's own UI, except data and proper names
  (data-model § Label rules). For example, the palette headers read "Suggested ·
  Arrangement", "Recent" and "Actions".
- **P3.2** `RunLabels` fails, naming `file:line`, when such a label is reintroduced.

## P4 — Device cards

- **P4.1** Header order on every built-in and plugin card: name · preset · badges ·
  power. The power slot is reserved on every card. On all cards, the preset picker's left
  edge sits at the same x within 1 px for the same card width, and the power key's right
  edge sits at the card edge minus the inset.
- **P4.2** Sections inside built-in cards have no fill and no frame. Neighbouring sections
  are divided by a 1 px `Rule`. In an edge-overlay diff, the position of every control,
  graph and label is unchanged within 1 px. Graph windows and wells keep their frame.
- **P4.3** Instrument Rack, the generic instrument card and the Sends card use the same
  header as every other card.

## P5 — Icons and the view switcher

- **P5.1** Every icon is a `Glyph`. `RunIcons` finds 0 path geometry outside
  `Controls/Glyph.cs`.
- **P5.2** At 16 px, stroked icons draw 1.5 px lines, and below that, `max(1.2, …)`. Icons
  in the rail, browser, Preferences, palette, history and player match in weight.
- **P5.3** The view switcher is three 28 × 24 icon segments in the same place and order
  (Arrangement, Session, Modular). The current view uses the accent fill. The tooltip and
  the accessible name are the view name. It is at least 100 px narrower than before.

## P6 — Chrome

- **P6.1** Built-in browser rows show "Aurora", "Bass"… with no "Nota" prefix. The type tag
  is one ink step quieter than today and still meets T3.3. Searching "nota aurora" and
  "aurora" both find Aurora.
- **P6.2** The add-device target is 28 px wide with a "+" glyph and a tooltip. It is dashed
  (radius 3) only while a drag is over the chain. The same applies to the rack chain
  target.
- **P6.3** The mixer scale reads "+6, 0, −12, −48" with U+2212. Ranges follow the dash rule.
  `RunNumbers` fails on a hyphen-minus before a digit in a view literal.
- **P6.4** The arrangement header rows (Overview, Sections, Zoom/ruler) are unchanged.
