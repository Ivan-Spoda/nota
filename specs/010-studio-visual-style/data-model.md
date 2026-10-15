# Data Model: Studio Visual Style

No persisted data changes. The "model" of this feature is the design-token set: every
`Brush.*` key in `Theme/NotaTheme.axaml` and its mirror slot in `Theme/NotaPalette.cs`,
each with a dark and a light value, plus geometry and type tokens.

## Entities

### Design token
- **Key**: `Brush.<Name>` (XAML) ↔ `NotaPalette.<Name>` (C#) — unchanged names (R1).
- **Values**: `dark`, `light` (hex). Validation (existing `DesignTokenCheck.Run`): every
  key exists in both dictionaries; every `T("#d","#l"); // Brush.Key` slot equals its
  XAML pair; every `KeyMap` key exists.
- **New in this feature**: `Seam`, `Rule`, `TextFaint`, `Mute`, `Solo`, `VizInk`,
  `VizGrid`; shadow resources `Shadow.Popover`, `Shadow.Modal` (and `Shadow.Raised` /
  `Shadow.Sunken` re-valued); geometry `Radius.Docked`, `Radius.Modal`.

### Appearance
- `Dark` | `Light` | `System` (unchanged `AppTheme` / `NotaThemeVariant`). Switching
  re-tints slots in place (`NotaPalette.Apply`); no rebuild.

### Track colour
- Stored per track as an integer index = `base × 3 + shade` (9 bases × 3 shades = 27,
  returns from 27). **Unchanged.** Only the nine base hues and the two return hues are
  re-valued; shade derivation (`ShadeUp` 0.24 / `ShadeDown` 0.30) unchanged.

## Token map — Ember → Studio role

Studio values: the Studio Dark and Studio Light palettes, plus derived tokens mixed from
their base roles. "mix(a, b, t)" = t of a over b.

### Surfaces

| Slot | Studio role | Dark | Light |
|---|---|---|---|
| `BgApp` | ground (bg-base) | `#1E1F22` | `#C9CBCF` |
| `Gutter` | seam (now only visible as 1 px seams) | `#141517` | `#9DA0A6` |
| `SurfaceAbyss` | well (console recess) | `#17181A` | `#D6D8DB` |
| `BgSunken` / `ChromeBg` | well | `#17181A` | `#D6D8DB` |
| `SurfaceActive` | well (pressed sinks) | `#17181A` | `#D6D8DB` |
| `TrackOff` | well (off switch track) | `#17181A` | `#D6D8DB` |
| `CanvasBg` | ground | `#1E1F22` | `#C9CBCF` |
| `SurfaceDeep` | between well and ground | `#1B1C1F` | `#CFD1D4` |
| `LaneB` | alternate lane on ground | `#222326` | `#C4C6CA` |
| `Panel` | panel (bg-panel) | `#2B2D31` | `#E4E5E7` |
| `SurfaceCard` | panel (device sections divided by rules, not tone) | `#2B2D31` | `#E4E5E7` |
| `SurfaceInset` | between ground and panel | `#232427` | `#DCDEE0` |
| `SurfaceRaised` | raised (bg-raised) | `#34363B` | `#EEEFF1` |
| `SurfaceHover` | raised-hover (bg-raised-2) | `#393C41` | `#F7F8F9` |
| `SurfaceHoverClear` | raised-hover at alpha 0 | `#00393C41` | `#00F7F8F9` |
| `SurfaceSelected`, `SelHeaderBg` | selected row = raised-hover (+ accent tick) | `#393C41` | `#F7F8F9` |
| `AccentSubtle` | accent-soft = mix(accent, panel, .16) | `#4D3F2E` | `#E6D6C4` |

### Lines and grid

| Slot | Studio role | Dark | Light |
|---|---|---|---|
| **`Seam`** *(new)* | border-subtle — between panels, around controls | `#141517` | `#9DA0A6` |
| **`Rule`** *(new)* | border-strong — divides within a panel, field edge | `#45484E` | `#B3B6BB` |
| `BorderDefault` | seam | `#141517` | `#9DA0A6` |
| `BorderStrong` | rule | `#45484E` | `#B3B6BB` |
| `Hairline` (`GraphBorder`) | rule (viz-border) | `#45484E` | `#B3B6BB` |
| `BorderBrass` | accent-line = mix(accent, panel, .70) | `#BF7D22` | `#ECA54E` |
| `GridBeat` | viz-grid on ground | `#2A2C30` | `#BDC0C4` |
| `GridSubBeat` | finer grid | `#242528` | `#C3C5C9` |
| `GridRow` | piano-roll row divider | `#1B1C1F` | `#C5C7CB` |
| `GridBar` | bar line | `#3A3C41` | `#ADB0B5` |
| **`VizGrid`** *(new)*, `WellGrid` | viz-grid in a well = mix(muted, well, .16) | `#2E2F32` | `#C0C3C6` |
| `PadGrid` | XY-pad grid | `#2A2C30` | `#C8CACD` |

### Text

| Slot | Studio role | Dark | Light |
|---|---|---|---|
| `TextHeading` | primary, a step brighter | `#F4F5F6` | `#0E0F11` |
| `TextPrimary` | text-primary | `#E8E9EB` | `#16171A` |
| `TextStrong` | between primary and secondary | `#D4D6DA` | `#282A2E` |
| `TextSecondary` | text-secondary | `#C0C4C9` | `#3A3D42` |
| `TextMuted` | text-muted | `#A4A9B0` | `#4F535A` |
| `TextTertiary` | mix(muted, panel, .85) | `#92969D` | `#65696F` |
| **`TextFaint`** *(new)*, `TextDisabled`, `TextAxis` | text-faint = mix(muted, panel, .72) | `#82868C` | `#797C81` |
| `TextOnAccent` | on-accent (derived: dark ink wins on both accents) | `#17181A` | `#17181A` |
| **`VizInk`** *(new)* | viz-ink = mix(primary, well, .92) | `#D7D8DA` | `#252629` |

### Accent

| Slot | Studio role | Dark | Light |
|---|---|---|---|
| `Accent` | accent | `#FF9F1C` | `#EF8A0C` |
| `AccentBright`, `AccentHover` | accent-glow (line/ink variant) | `#FFB547` | `#C46A00` |
| `AccentDeep` | pressed | `#C77612` | `#D47800` |
| `AccentDim` | accent-dim | `#C77612` | `#F7C27A` |
| `AccentEdge` | accent-line | `#BF7D22` | `#ECA54E` |
| `AccentMute` | (no resting use left; kept for parity) | `#7A5F3D` | `#C9A77A` |
| `AccentPale` | hot edge highlight | `#FFD08A` | `#8A4A00` |
| Fluent `SystemAccentColor*` | accent ramp | `#FF9F1C` ±steps | `#EF8A0C` ±steps |

### Semantics

| Slot | Studio role | Dark | Light |
|---|---|---|---|
| `Record`, `Danger` | error (record, arm, clip) | `#F06A6E` | `#B3262C` |
| `RecordInk` | glyph on a record fill | `#FFFFFF` | `#FFFFFF` |
| `DangerHover` | | `#F48386` | `#C2343A` |
| `DangerBright` | overload readout text | `#F58F92` | `#9C1F24` |
| `DangerDeep` | over-threshold fill | `#C4474B` | `#8E1C21` |
| `DangerPale` | live-take waveform | `#F7A5A7` | `#7F161B` |
| `DangerSubtle` | | `#33F06A6E` | `#33B3262C` |
| **`Mute`** *(new)*, `Warning` | warning amber (mute on, meter −6…0) | `#F2B53C` | `#7A4F00` |
| **`Solo`** *(new)* | track-blue (solo on) | `#6A9FD8` | `#3F78B8` |
| `Success` | green (safe meter range) | `#6CC673` | `#22692B` |
| `SuccessDim` | green-dim | `#3F8A45` | `#79B880` |
| `MuteTint` | muted band = mix(mute, ground, .12) | `#3B3426` | `#BDB49F` |

### Data / graph chromas

| Slot | Role | Dark | Light |
|---|---|---|---|
| graph primary (`NotaGraph.Chroma(0)`) | viz-ink | `VizInk` | `VizInk` |
| `Teal` / `TealBright` | 2nd source | `#5FB3A8` / `#86C9C0` | `#3A8A7F` / `#2E7067` |
| `Rose` / `RoseBright` | 3rd source | `#D77A8F` / `#E49EAE` | `#B4566B` / `#93404F` |
| `Steel` | 4th source | `#6A9FD8` | `#3F78B8` |
| `Threshold` | | `#E0A84A` | `#8A5F14` |
| others (`Sage`, `Marker`, `MarkerHot`, `Frozen`, `SignalIn*`, `HandleSel`, `FrozenHeaderBg`, `Veil`, `TooltipBg/Text`, piano keys, `ShadeUp/Down`) | re-valued onto the grey ladder; hues kept where they carry meaning (frozen ice, markers) | see `NotaPalette.cs` | |

### Track palette (9 roles) and buses

| Role | Index base | Dark | Light | Source |
|---|---|---|---|---|
| Drums | 0 | `#7FB87A` | `#4A7F46` | new, Studio chroma |
| Perc | 1 | `#5FB3A8` | `#3A8A7F` | Studio teal |
| Bass | 2 | `#5AA6C4` | `#357C99` | new |
| Keys | 3 | `#6A9FD8` | `#3F78B8` | Studio blue |
| Texture | 4 | `#E0A84A` | `#B8862F` | Studio amber |
| FX | 5 | `#9D90C9` | `#7363A8` | Studio violet |
| Brass | 6 | `#D77A8F` | `#B4566B` | Studio rose |
| Vox | 7 | `#B3B45E` | `#7E7F2F` | new |
| Return | 8 | `#8E9DB0` | `#5D6C80` | new, slate |
| `ReturnA` / `ReturnB` | — | `#8E9DB0` / `#7A889A` | `#5D6C80` / `#4E5B6D` | slate pair |
| `Master` | — | `#A4A9B0` | `#4F535A` | master strip is muted grey |

### Elevation (per variant `BoxShadows`)

| Key | Dark | Light |
|---|---|---|
| `Shadow.Raised` | `inset 0 1 0 0 #12FFFFFF` (bevel; the seam ring is the control's 1px seam border) | `inset 0 1 0 0 #B3FFFFFF` |
| `Shadow.Sunken` | `inset 0 0 0 1 #45484E` | `inset 0 0 0 1 #B3B6BB` |
| `Shadow.Popover` | `0 0 0 1 #141517, 0 10 28 0 #6B000000` | `0 0 0 1 #9DA0A6, 0 10 28 0 #381E2024` |
| `Shadow.Modal` | `0 0 0 1 #141517, 0 24 64 0 #80000000` | `0 0 0 1 #9DA0A6, 0 24 64 0 #401E2024` |

### Geometry

| Key | Ember | Studio |
|---|---|---|
| `Radius.Bar` | 1 | 1 (plot bars) |
| `Radius.Clip` | 2 | 2 |
| `Radius.Badge` | 3 | 2 |
| `Radius.Control` | 4 | 3 |
| `Radius.Tile` | 5 | 3 |
| `Radius.Panel` | 6 | 3 |
| `Radius.Body` | 8 | 3 |
| `Radius.Pill` | 999 | 3 |
| **`Radius.Modal`** *(new)* | — | 4 |
| **`Radius.Docked`** *(new)* | — | 0 |
| `Control.TransportBar` | 60 | 44 |
| `Control.Console` | 42 | 30 |
| `Control.Transport` | 28 | 26 |
| `Control.Play` | 40 | 36 |
| `Space.Gutter` | 12 | 1 (the seam) |
| Title bar | 36 | 32 |
| Card 700 × 260, header 22, knob sizes, `ParamCell` 53, arrangement/piano-roll geometry, list rows 26 | — | **unchanged** (FR-019) |

Type tokens: see research.md R8.
