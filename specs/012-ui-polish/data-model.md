# Data Model: UI Polish Pass

No project or preset format changes. The "data" here is the shape of a few shared UI
contracts.

## Icon (the shared set, `Controls/Glyph.cs`)

| Field | Rule |
|---|---|
| Kind | A `GlyphKind` enum value. The only way to draw an icon (`RunIcons`). |
| Grid | 16 units. Drawn from path data scaled from 16 to the requested size, never `Stretch.Uniform`. |
| Style | **Stroked**: round caps and joins, stroke = `max(1.2, size × 1.5/16)`. **Filled**: transport shapes, OS marks, star, disclosure carets. |
| Size | Any. The natural size is 16 unless the kind declares otherwise (transport keeps 11/10). |

**New kinds** (R8):
- Views: `Arrangement` (three horizontal lanes with clip blocks), `Session` (a 2×3 clip
  grid), `Modular` (three nodes joined by cables).
- Rail and pages: `Keyboard`, `Sliders`, `Folder`, `Downloads`, `Star`, `Bookmark`, `Doc`,
  `Midi`, `Map`, `History`, `Sidebar`, `Options`, `Search`, `Gamepad`, `Phone`, `Plug`,
  `Appearance`, `Library`.
- Player and transport: `Loop`, `Speaker`, `SpeakerLow`, `SpeakerHigh`, `SpeakerMute`,
  `Metronome`, `Follow`, `Snap`, `Automation`, `ChainLink`.
- Palette kinds: `Action`, `Track`, `Device`, `Preset`, `Patch`.
- OS marks (filled): `Apple`, `Windows`, `Linux`.

Duplicates collapse into one kind: Star, Plug, Downloads, Options.

## Device-card header (`BuildCardShell`)

The slots run left to right, with fixed positions. The header is 22 px tall.

| Slot | Content | Empty when |
|---|---|---|
| Name | The device name. The Compact variant drops the "Nota " prefix, as today. | never |
| Preset | `DeviceCardKit.PresetPicker` (stepper and menu) | the device has no presets |
| Badges | Subtitle badge (sentence case), voice count, `HeaderAccessory` / `HeaderExtra` (Learn, S/L size) | none |
| Power | Bypass `SwitchTrack` | not bypassable (instruments). The slot is still reserved, so right edges align down a chain. |

## Device-card section (`DeviceCardKit.Section`)

| Field | Rule |
|---|---|
| Fill | None. It sits on the card surface. |
| Edges | A 1 px `Rule` only on edges shared with a neighbour (`Top`, `Left`), chosen by the caller. |
| Padding | Unchanged from the box it replaces, so content does not move. |
| Not covered | Graph windows (`NotaGraph.Window`) and wells (`BgSunken` + `GraphBorder`) keep their own frame. |

## Label rules (`RunLabels`)

- **Label context**:
  - C# assignments to `Text`, `Content`, `Header`, `Title` and `ToolTip.SetTip`;
  - axaml attributes of the same names;
  - `MenuItem` / `NativeMenuItem` headers;
  - command titles in `CommandCatalog`;
  - the first argument of local label builders (`Mono`, `SectionHead`, `Field`, `Caps`,
    `DrumCap`, `Header`).
- **Fails on**:
  - **All caps**: two or more letters, no lower case, not entirely `NotaText.Fixed`
    tokens, and not passed through `NotaText.Label`.
  - **Title Case**: a capitalised word after the first one that isn't on the allow-list.
- **Allow-list**:
  - acronyms and units from `NotaText.Fixed`;
  - view and panel names (Arrangement, Session, Modular, Mixer, Finder);
  - "Nota …" product and device names;
  - proper nouns.
- **Skipped as data**:
  - interpolated strings;
  - preset tables;
  - engine parameter display names (automation, MIDI-learn);
  - scale and warp tables;
  - sample device names.
- **Ratchet**: a `file:line` allow-list with a reason for each entry. After this feature it
  holds only data exemptions.

## Settings

| Field | Type | Default | Meaning |
|---|---|---|---|
| `SessionInspectorOpen` | bool | `true` | Whether the Session view's inspector column is shown (R2). |

## Number setting (DESIGN.md § Numbers addition)

- **Minus**: U+2212 everywhere a number is shown, including scale labels (`−12`).
- **Ranges**: an en dash U+2013. Closed between plain numbers (`1–127`, `1.1–5.1`); spaced
  when either endpoint contains a space (`20 Hz – 20 kHz`).
