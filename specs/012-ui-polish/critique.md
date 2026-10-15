# Screenshot critique: Studio Dark, 2026-10-14

The input to this spec. These are 11 screens of the `011-colour-themes` build under Xvfb at
1200 × 800:
- arrangement with the device dock;
- session;
- modular;
- the mixer overlay;
- the browser context menu and preview;
- the command palette;
- the export dialog;
- the Auto Filter card and the Drum Rack card.

The screenshots are kept outside the repo. To reproduce them, see quickstart.md.

## Overall

The base is solid. The grey ladder, the seams and "orange only when live" read well, and
the device cards (Auto Filter especially) are the strongest part of the app. What remains
is leftovers and inconsistency rather than wrong direction:
- labels still in all caps or title case;
- overlays that don't read as overlays;
- toolbars that crowd or overflow;
- device-card headers that differ from card to card.

## Findings

| # | Area | Finding | Severity |
|---|---|---|---|
| F1 | Arrangement | After adding a Drum Rack track, its header was drawn in the gap above the returns, overlapping "Reverb". This is unconfirmed: it may be a relayout not yet finished when the shot was taken, or it may predate the restyle. | Critical, to confirm |
| F2 | Session | The toolbar overflows: the position and tempo readouts are cut off at the right. The inspector covers the column header of the last visible track ("Delay"). | Moderate |
| F3 | Modular | The "Tracks" list is empty while the project has four tracks and a master. Either a bug or a missing empty state. | Moderate |
| F4 | Mixer, export | They open over the arrangement with no edge or shadow and read as part of the page. The mixer stops short on the right and leaves dead space. Neither has a header naming it. | Moderate |
| F5 | Labels | All caps left over: the palette section header ("SUGGESTED · ARRANGEMENT"), the Drum Rack meta ("0 PADS · 0 CHOKE GROUPS", "CHOKE") and the mixer's "PAN". | Moderate |
| F6 | Labels | Title case left over: "Add to Favorites", "Stop All Clips", "Capture Scene", "Session Rec" and "Live Freeze". | Minor |
| F7 | Device cards | The header differs per card: Synth has S/L badges, Auto Filter a power key on the right, Drum Rack neither. The preset stepper sits in a different place on each. | Moderate |
| F8 | Icons | `Glyph` shapes sit next to about 60 inline icon paths with their own stroke weights (left rail, browser, Preferences, command palette, history, preview player, Rhythm card). | Minor |
| F9 | Arrangement | Three header rows (Overview, Sections, Zoom) come before the first track, and Zoom sits in the track-name column. | Minor, deferred (keep as is, per clarify) |
| F10 | Browser | A muted "Nota" prefix starts every built-in row, so the same word repeats down the whole list. | Minor |
| F11 | Device dock | The "Add device" drop zone fills most of an empty dock, and it is the only dashed element in the app. | Minor |
| F12 | Numbers | The loop range is shown as "1.1 - 5.1" with a hyphen. The mixer scale may use hyphens where it needs minus signs. | Minor |

## What works

- Device cards: Auto Filter's graph, tabs and neutral resting values.
- The command palette: clear structure, keyboard hints and a strong focus ring. It is the model for other overlays.
- Settings → Appearance and the browser preview player are clean and calm.
