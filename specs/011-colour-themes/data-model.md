# Data Model: Selectable Colour Themes

## Colour theme

| Field | Type | Rules |
|---|---|---|
| `id` | string | Unique. Lower-case kebab (`^[a-z0-9]+(-[a-z0-9]+)*$`). Stable: settings store it. `"system"` is reserved. |
| `name` | string | Display name, 1–40 chars, shown as written (not sentence-cased). |
| `kind` | `"dark"` \| `"light"` | Picks the native chrome / Fluent base variant and the follow-system list it appears in (FR-009). |
| `formatVersion` | int | `1`. A future reader rejects a higher major version it does not know. |
| `colors` | map token → colour | **Exactly** the token catalogue: no missing token, no unknown token. Colour = `#RRGGBB` or `#AARRGGBB`, upper- or lower-case hex. |

Built-ins (FR-013), in this list order:

| id | name | kind | Source |
|---|---|---|---|
| `studio-dark` | Studio Dark | dark | today's dark values, unchanged |
| `studio-light` | Studio Light | light | today's light values, unchanged |
| `ember-graphite` | Ember Graphite | dark | 0.52.1 dark values + R8 derivations, nudged per R7 |
| `ember-paper` | Ember Paper | light | 0.52.1 light values + R8 derivations, nudged per R7 |

Format: [contracts/theme-file.md](contracts/theme-file.md).

## Colour token (catalogue)

A name, fixed by the design language. Themes supply values only. Groups, in the order the
design doc lists them:

- **Surfaces**: `SurfaceAbyss`, `BgApp`, `Gutter`, `BgSunken`, `CanvasBg`, `SurfaceDeep`,
  `LaneB`, `Panel`, `SurfaceCard`, `SurfaceInset`, `SurfaceRaised`, `SurfaceHover`,
  `SurfaceActive`, `TrackOff`
- **Lines and grid**: `Seam`, `Rule`, `BorderDefault`, `BorderStrong`, `BorderBrass`,
  `GraphBorder`, `GridBeat`, `GridSubBeat`, `GridRow`, `GridBar`, `VizGrid`, `WellGrid`,
  `PadGrid`
- **Text**: `TextHeading`, `TextPrimary`, `TextStrong`, `TextSecondary`, `TextMuted`,
  `TextTertiary`, `TextFaint`, `TextDisabled`, `TextAxis`, `TextOnAccent`, `VizInk`
- **Accent**: `Accent`, `AccentHover`, `AccentBright`, `AccentDeep`, `AccentDim`,
  `AccentEdge`, `AccentMute`, `AccentPale`, `AccentSubtle`, `SystemAccent`,
  `SystemAccentLight1..3`, `SystemAccentDark1..3`
- **Semantics**: `Record`, `RecordInk`, `Danger`, `DangerHover`, `DangerBright`,
  `DangerDeep`, `DangerPale`, `Warning`, `Mute`, `Solo`, `Success`, `SuccessDim`,
  `MuteTint`
- **Data**: `Teal`, `TealBright`, `Rose`, `RoseBright`, `Steel`, `Sage`, `Threshold`,
  `Marker`, `MarkerHot`, `Frozen`, `SignalIn`, `SignalInFill`, `HandleSel`
- **Tinted chrome**: `SelHeaderBg`, `FrozenHeaderBg`, `Veil`, `TooltipBg`, `TooltipText`
- **Keys**: `KeyWhite`, `KeyBlack`, `MiniKeyWhite`, `MiniKeyBlack`
- **Shade targets**: `ShadeUp`, `ShadeDown`
- **Tracks**: `Track1`…`Track9`, `ReturnA`, `ReturnB`, `Master`
- **Elevation**: `ShadowBevel`, `ShadowPopover`, `ShadowModal`

The authoritative list is the set of `T(nameof(X))` declarations in
`Theme/NotaPalette.cs` (plus the named track/return slots), together with the elevation
and `SystemAccent*` tokens: 85 + 11 + 3 + 7 = **106 tokens** at the time of planning.
`DesignTokenCheck` derives the list from the code and compares every theme file against
it, so the list above can never drift from what the check enforces.

**Derived, not tokens**: washes (`Wash(slot, alpha)`, incl. `SurfaceHoverClear`,
`DangerSubtle`), `Derived(...)` brushes (track shades, clip bands), `InkOn(fill)`,
gradients, `Ink("#hex")` device hues (follow the theme's *kind*, R3).

## Appearance setting (persisted in `settings.json`)

| Field | Type | Default | Meaning |
|---|---|---|---|
| `ColourTheme` | string | `""` | A theme id, or `"system"` for follow system. `""` = not yet migrated (read the legacy field). |
| `SystemDarkTheme` | string | `"studio-dark"` | Theme used while the OS is in dark mode. Must be a dark-kind theme. |
| `SystemLightTheme` | string | `"studio-light"` | Theme used while the OS is in light mode. Must be a light-kind theme. |
| `Theme` *(legacy)* | `AppTheme` (Dark/Light/System, stored as 0/1/2) | `Dark` | Kept and **written in step** for older Nota versions: the active theme's kind, or System when following. Read only for migration. |

### Resolution (`Appearance.Resolve`, pure, in `Nota.Application`)

Input: the stored fields and the set of known theme ids with their kinds. Output: the
mode (single theme or follow), the single theme id, the dark-mode id and the light-mode id.
All ids returned are known and of the right kind.

1. **Migration**: if `ColourTheme == ""`, derive it from the legacy field:
   Dark → `studio-dark`; Light → `studio-light`; System → `system`.
2. **Single theme**: an unknown id → `studio-dark` (FR-007).
3. **Per-mode ids**: an unknown or wrong-kind `SystemDarkTheme` → `studio-dark`.
   An unknown or wrong-kind `SystemLightTheme` → `studio-light`.
4. Resolution never throws and never writes. The caller saves only when the user changes
   something.

### State transitions (Preferences)

```text
Single(t) --pick theme u--------------> Single(u)                  (per-mode ids kept)
Single(t) --turn on follow system-----> Follow(darkId, lightId)    (stored per-mode ids)
Follow    --pick dark-mode theme d-----> Follow(d, lightId)
Follow    --pick light-mode theme l----> Follow(darkId, l)
Follow    --pick a theme u from list---> Single(u)                  (per-mode ids kept, FR-005)
```

Every transition saves the settings, re-applies the theme live (FR-003) and sets the
legacy `Theme` field to match.

## Unchanged

- Project files and track colour indices (`base × 3 + shade`). A theme recolours the
  slot; the stored index is untouched.
- Geometry, type and spacing tokens (`Radius.*`, `Control.*`, `Space.*`, `NotaType`).
