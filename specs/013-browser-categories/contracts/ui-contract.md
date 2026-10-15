# UI contract: Device categories in the browser

**Feature**: [../spec.md](../spec.md) · **Data model**: [../data-model.md](../data-model.md)

This is what a user (and the smoke test) can observe on the Instruments, Audio effects and
MIDI effects tabs. The Files, Presets, Projects, Map and History tabs are out of scope
(FR-010).

## C1: Layout

```text
[ Search ……………………… 26 ] [⋮]
(EQ & filter) (Dynamics) (Reverb & delay) (Modulation)      ← category chips, wrapping
(Distortion) (Pitch & time) (Utility) (Racks) (Other?)
(Favorites) (tag) (tag) … +N                                ← existing row, unchanged
Built-in ───────────────────────── 26
  Auto Filter                         envelope / LFO
  …
```

- **Chip order**: the tab's category order. "Other" comes last and only appears when a
  device on the tab maps there.
- **Look**: chips reuse `MakeChip`, so they are 20 tall pills, sentence case, with the
  engaged state in the accent wash (FR-008).
- **Narrow browser**: the row wraps; no chip goes behind an overflow menu.
- **Where the row shows**: only on the three device tabs, and only while "Category chips"
  is on.

## C2: Picking

- **Picking a chip** narrows the list to that category's devices (and their presets as
  usual). Clicking the picked chip clears it; clicking another switches to it.
- **Per tab**: each tab holds its own pick through tab switches. A restart clears all picks.
- **Device rows** are the same objects as in the full list: double-click, drag, preview,
  preset expansion, favourite and tag behave identically (FR-007).

## C3: Combining (FR-006)

- **Filters stack**: a device shows only if it passes the chip, the search, and the
  Favorites or tag chip.
- **Ordering**: "Group by source" (Built-in / Plug-ins headers) and "Favorites first" order
  the narrowed list as they do the full list.
- **Counts**: the search-box count and the status line count the narrowed list.
- **Empty result**: "No matches".

## C4: View options (⋮)

The order is:

1. Show type tags
2. **Category chips** (new; checked by default)
3. Group by source
4. Favorites first

- **Turning it off** hides the row and clears every tab's pick, so the full list returns,
  exactly as before 013 (SC-003).
- **Persistence**: the toggle is saved as `BrowserCategoryChips`.

## C5: Design check (FR-009)

`DesignTokenCheck.RunBrowser` fails when:

- a built-in device has no category;
- a category lists a name that isn't in the catalogue;
- a category has no built-ins;
- the per-tab totals stop being 18 / 26 / 6.
