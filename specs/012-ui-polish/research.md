# Research: UI Polish Pass

Code research against `012-ui-polish`, which carries 010 and 007. Paths are under
`src/managed/Nota.App/` unless they say otherwise. Each R maps to the critique findings
(F#) and the user stories in the spec.

## R1 — F1: a new track drawn over the returns (US1)

**Structure** (`ArrangementView.cs:257-427`):
- Normal tracks live in a vertical `ScrollViewer` (`_scroller`). Its content is an
  absolute `Canvas` of headers (`RebuildHeaders`, `:1652`; each card is 64 px, placed at
  `RowTop(i)`) plus the lanes.
- Returns and Master live in a separate **pinned footer** docked to the bottom
  (`_footer`, 40 px per row, `RebuildFooterHeaders`, `:2414`). The footer never scrolls.
- The space below the last track is the empty part of the scroll viewport, painted in the
  header-column colour (`leftBackdrop`, `:361`). There is no fixed spacer.

**On add**: `MainWindow.ApplyInsertResult` → `Timeline.Refresh()` rebuilds the headers,
the footer and the heights. Nothing scrolls the new track into view: `_scroller`'s offset
is never set in code.

**Most likely cause**: two gaps together.
- No seam separates the scroll area from the pinned footer.
- A new track is never revealed.

When the new 64 px card lands at the bottom of the viewport, it is drawn flush against the
first return row with no boundary, and reads as overlapping. A secondary risk:
`Refresh(rebuildHeaders:false)` during recording updates the lanes but not the headers.

**Decision**:
- Draw a 1 px `Seam` on top of the footer band.
- Scroll a newly added or selected track into view after `Refresh`.
- Give the empty area below the tracks the lane colour on the lane side.
**Reproduction** (Xvfb, adding a Drum Rack from the browser, screenshot at once and 5 s
later):
- **010 and 012**: the new track's 64 px header is clipped by the bottom of the scroll
  viewport, so only its name strip shows. It sits directly on the pinned Reverb row with
  no boundary. It persists after 5 s, so it is not a relayout race. Confirmed as
  described.
- **0.52.1**: inconclusive. The scripted browser click missed under the older floating
  layout, so no track was added. The 010 layout made the defect visible by removing the
  12 px gutter between the islands.

## R2 — F2: Session toolbar overflow; inspector covering headers (US1)

**Toolbar** (`SessionView.Toolbar.cs:42-144`):
- Two `Auto` groups in a `Grid "Auto,*,Auto"` inside a 44 px border.
- Left group: Session rec, Fixed off/1/2/4/8, Quant, Follow, Capture scene.
- Right group: I/O·Sends·Mixer, position readout, Back to arrangement, Stop all.
- Together they need about 1130 px. At a 1200 px window the session area is about 855 px
  wide, because the browser takes 344. There is no overflow handling, so the right group
  runs off the edge.

**Inspector**: a real 300 px grid column (`SessionView.cs:40,50,123`). The column headers
scroll horizontally with the cells and are clipped at the inspector's edge.

**Decision**:
1. The toolbar collapses by priority as the width shrinks (a `SizeChanged` breakpoint):
   - first, the right group's text labels become icons with tooltips;
   - then the position readout and Back to arrangement move into an overflow "⋯" menu.

   Every control stays reachable (FR-002).
2. The inspector becomes collapsible with a toolbar toggle that is remembered, and
   narrows to 260 px.
3. When the inspector is open and the content is wider than the space, the header row
   keeps a visible scroll cue, so nothing is silently covered.

## R3 — F3: Modular shows an empty track list (US1)

`ModularView.RefreshSidebar()` (`:243`) runs only from `Show(trackId)` or from a
`Refresh()` guarded by `_global || _trackId > 0`. `MainWindow.SetView(Modular)` calls
`Show` only when a track is selected, or when `_lastInstrumentTrackId > 0`, which is 0
after opening a project. So with no selection the sidebar is never built, and nothing can
be clicked.

**Decision**:
- `SetView(Modular)` always refreshes the sidebar and auto-selects the first non-return
  track when none is selected.
- Add an empty state for the sidebar ("No tracks yet") and for the canvas ("Pick a track
  to see its signal graph").

## R4 — F4: "Overlays" are OS windows (US2, spec correction)

- **Mixer**: a `NotaWindow` (`MixerWindow.cs:33`), fixed at 1040 × 460, shown non-modal.
  Strips are fixed 154 px boxes in a left-aligned `StackPanel` (`MixerView.cs:40,138`), so
  dead space follows them. It has no Escape handling.
- **Export**: a modal `NotaWindow` (`ExportWindow.cs:63`), 560 wide, closed by Export,
  Cancel or the OS. It has no Escape handling.
- **Command palette**: the only true in-window overlay (`PaletteLayer`, with
  `Shadow.Modal`, Escape, click-outside and focus restore).
- **On macOS and Windows**, `NotaWindow` draws its own 32 px title bar with the window
  name. On Linux it uses the native frame (`NotaWindow.cs:47-54`).

The critique's "no edge, no header, no shadow" came from Xvfb having **no window
manager**, so native frames and shadows were missing. That is an artefact of the test
harness, not what users see.

**Decision (spec amended, see Clarifications)**:
- Keep the mixer and export as windows. Making them in-window overlays would change
  behaviour: for example, you could no longer put the mixer on a second screen.
- Fix what users actually see:
  - **Mixer**: the window width fits its strips, with a minimum of 560 and a maximum of
    the screen width. There is no dead area inside it.
  - **Mixer and export**: Escape closes them, the same as the palette.
  - **Export**: on every platform, the window title reads "Export audio", in sentence
    case.
- No new shared overlay host is needed. The palette stays as it is.

## R5 — F5/F6: label case (US3)

**Sources of all caps**:
- `Nota.Application/Palette/PaletteSearch.cs`: `"SUGGESTED · " + …ToUpperInvariant()`
  (`:377`), `"RECENT"` (`:370`), and the group labels `"ACTIONS"`… (`:284`). These are
  rendered raw at `CommandPaletteView.cs:292`.
- Drum Rack meta (`RackCardView.Drum.cs:660`), `MixerView` `Mono("PAN")` (`:328`).
- The session inspector's `SectionHead`/`Field`/`Mono` literals (`SessionView.Inspector.cs`).
- Preferences sidebar groups and shortcut group titles (`PreferencesWindow.cs:63-75,
  1050-1172`).
- `KeyPicker` "MAJOR/MINOR".

**Sources of Title Case**:
- The native menu (`MainWindow.axaml:27-109`), which already mixes cases.
- Command titles (`Nota.Application/Palette/CommandCatalog.cs:22-90`), shown in the palette
  and menus.
- Browser, session, mixer and freeze labels: "Add to Favorites", "Stop All Clips",
  "Capture Scene", "Session Rec", "Live Freeze", "Monitor In", "All Ins", "Separate Stems…",
  "Convert Melody to New MIDI Track", and others.
- Window titles: "Export Audio", "Edit Tags", "Don't Save", "Connect Phone".

**Decision**:
- Fix at the source: correct the literals, and route the builders (`Header()`,
  `SectionHead`, `Mono`, `Field`) through `NotaText.Label`.
- Palette headers become "Suggested · Arrangement", "Recent", "Actions"…
  `tests/Nota.SmokeTest/PaletteTests.cs:195-196` and `:100` change with them.
- Out of scope, kept as written because they are data:
  - device and product names ("Nota Auto Filter");
  - preset names, which DESIGN.md says are title-cased places;
  - automation and parameter display names from the engine;
  - MIDI-learn binding names;
  - user data;
  - proper nouns ("Finder").
- **New check `RunLabels`** in `DesignTokenCheck`:
  - It scans string literals in label contexts: `Text=`, `Content=`, `Header=`, `Title=`,
    `ToolTip`, `MenuItem`/`NativeMenuItem` headers, command titles, and the first argument
    of the local label builders.
  - It fails on all caps (two or more letters, no lower case, not entirely `NotaText.Fixed`
    acronyms, and not reaching `NotaText.Label`).
  - It fails on Title Case (a capitalised word after the first one that isn't on the
    allow-list: view and panel names, acronyms, "Nota …" names, proper nouns).
  - It skips interpolated strings and the data files listed above.
  - It starts as a ratchet allow-list (`file:line`, each with a reason) that this feature
    shrinks to the data-only exemptions.
  - `RunStudio`'s upper-casing check is extended to `PaletteSearch.cs`.

## R6 — F7: device-card headers (US4)

**One builder already exists**: `DeviceChainView.Shell.cs:112` `BuildCardShell(ShellSpec,
body)`.
- Today's order: the name on the left; on the right, the badge (`Subtitle`), the preset
  picker, `HeaderExtra`, then bypass (power).
- The power key appears only when `Bypassable`, which means effects. Instruments are not
  bypassable.

**Differences come from the spec**:
- Synth, Keys, Mosaic and the MIDI bodies add a Learn button plus an S/L size toggle
  through `HeaderAccessory`.
- Drum Rack sets `Subtitle` "DRUMS · N PADS".

**Cards that bypass the shell**:
- Instrument Rack draws its own frame and `RackHeader`.
- `SimpleCard` is used by the generic instrument card and the Sends card.
- `RackShell` is dead code.

**Decision**:
- The header order becomes **name · preset stepper · badges (subtitle, accessories) ·
  power**, with fixed slots. The power slot is reserved for every card, empty when the
  device has no bypass. This keeps the right edge aligned down a chain.
- Instrument Rack, `SimpleCard` and the Sends card move onto `BuildCardShell` through
  `ShellSpec`. Delete `RackHeader` and the dead `RackShell`.
- Fix the shell's header comment ("26px chrome bar" → 22).
- Plugin cards already use the shell, so they follow automatically.

## R7 — Rule-divided sections (US4, FR-020)

There is no shared section builder. The copies:
- 13 private `SectionBox` copies (one per instrument card);
- `TabFrame` in 16 effect bodies, plus Pentad and Consort;
- `IslandBox` in the 6 MIDI bodies;
- `PluginCard.Section`, `RhythmInstrumentCard.Section`, Pendulum's `StripBox`, Drum Rack's
  `DrumBox`;
- about 50 inline framed `Border`s (`SurfaceCard` + `BorderDefault`, radius 3–6).

Graph windows draw their own frame in `NotaGraph.Window`, and wells (`BgSunken` +
`GraphBorder`) are a different pattern. Both stay.

**Decision**:
- Add `DeviceCardKit.Section(body, edges)`: no fill and no frame, just a 1 px `Rule` on
  the edges it shares with a neighbour (top or left), with the inner padding unchanged.
- Make the local helpers one-line delegates to it.
- Sweep the inline boxes by pattern: `SurfaceCard`/`Card2` background with `BorderDef`
  border.
- Positions and sizes must not change. Every card is screenshot before and after, and an
  edge overlay confirms nothing moved.
- Update DESIGN.md `:516-517` and `:167-168`.

## R8 — F8 + the view switcher: one icon set (US5)

**`Glyph`** (`Controls/Glyph.cs`): 44 kinds, monoline at `max(1.2, size × 1.5/16)`, round
caps.

**About 45 icons drawn outside it**:
- the browser rail and `GridIcon`;
- the Preferences pages and store OS marks;
- the palette kind icons (`Stretch.Uniform`, which the browser comment warns against);
- history, the preview player, the Rhythm folder;
- the transport toggles in `MainWindow.axaml`;
- `NotaIcons.ChainLink`.

Stroke weights run from 1.0 to 1.2 px effective, with several duplicates (star, plug,
downloads, options).

**Decision**:
- Extend `Glyph` into the one icon set:
  - Add the missing kinds: Keyboard, Sliders, Folder, Downloads, Star, Bookmark, Doc, Midi,
    Map, History, Sidebar, Options, Search, Gamepad, Phone, Plug, Appearance, Loop,
    Speaker, the speaker levels, Metronome, Follow, Snap, Automation, ChainLink, the
    palette kinds, and the OS marks (filled).
  - Add **Arrangement** (three horizontal lanes with clip blocks), **Session** (a 2×3 grid
    of clip cells) and **Modular** (three nodes joined by cables).
- Every kind is drawn from path data on a 16-unit grid at the shared stroke. A shape is
  either stroked or filled, never `Stretch.Uniform`.
- Replace every inline path with a `Glyph`.
- New check: in `Nota.App`, outside `Controls/Glyph.cs`, any of `Geometry.Parse`,
  `StreamGeometry.Parse`, `PathGeometry.Parse`, a `Data="M` attribute or an SVG path
  string constant (`"M\d`) fails.

**View switcher** (`MainWindow.axaml:154-162`): three `ToggleButton.seg` with text, about
230 px wide, no tooltips. The `view.arrangement/session/modular` commands have **no
keyboard shortcuts** (empty gestures in `CommandCatalog.cs:49-51`).

**Decision**:
- Three 28 × 24 icon segments, about 88 px in total, which saves about 140 px (SC-007).
- Tooltip = the view name. If a shortcut is ever assigned, the tooltip shows it.
- `AutomationProperties.Name` = the view name.
- The spec's "existing shortcut" wording becomes "menu command / palette command", since
  there is no shortcut today. No shortcut is added: that would be a behaviour change.

## R9 — F10–F12: browser prefix, add-device target, numbers (US6)

- **Browser prefix**: `BrowserViewModel.Prefix`/`ShortName` (`:70-72`) are rendered as
  `RowPrefix` (`BrowserView.cs:823`).
  - Drop the prefix element from rows.
  - Search already matches the full `Name`, so "Nota Aurora" and "Aurora" both match.
  - Make the type tag quieter by one ink step (`TextFaint`) while keeping it ≥ T3.3 in
    every theme.
- **Add device** (`DeviceChainView.cs:242`; rack `RackCardView.cs:1264`): 140 × 260 with
  a dashed `BorderStrong` outline at radius 7, which is off the radius scale.
  - It becomes a 28 px-wide slim target with a "+" glyph and a tooltip.
  - It is dashed (radius 3) only while a drag is over the chain.
  - The rack sibling gets the same treatment.
  - `RunGeometry` learns about `Rectangle.RadiusX`.
- **Numbers**:
  - The loop readout already uses an en dash (`TransportViewModel.cs:103`). F12's
    "hyphen" was the glyph at 11 px. Keep it.
  - Real defect: the mixer scale `{"+6","0","-12","-48"}` (`MixerView.cs:363`) uses
    hyphen-minus. Fix it with U+2212.
  - Ranges mix spaced and unspaced en dashes. Rule: closed (unspaced) between plain
    numbers ("1–127"), spaced when an endpoint contains a space or a unit with a space.
    Document the rule in DESIGN.md § Numbers.
  - `RunNumbers` gains a check for `"-\d` inside literals in views.

## R10 — Verification harness

- **Screenshots**: the same Xvfb harness, at 1200 × 800, in all four themes.
  - Before/after of every built-in card (51) for the sections and header changes, using
    an edge-overlay diff to prove nothing moved.
  - The critique tour re-run for SC-001.
- **Limits**: Xvfb has no window manager, so window chrome (mixer and export title bars)
  is verified on macOS by the user.
