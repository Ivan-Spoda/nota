# Research: Selectable Colour Themes

Decisions that the plan rests on, each with what was checked and what was rejected.
All findings are against branch `010-studio-visual-style` (Studio), which this feature
builds on, and against release 0.52.1 (`main`, Ember) for the Ember values.

## R1 — One source of colour: theme data, not XAML dictionaries

**Today**: every colour exists twice. One copy is a `T("#dark", "#light")` slot in
`Theme/NotaPalette.cs`, read by the custom-drawn views. The other is a
`SolidColorBrush x:Key="Brush.*"` in each of the two `ThemeDictionaries` of
`Theme/NotaTheme.axaml`, read by XAML and styles through `{DynamicResource}`.
`DesignTokenCheck` keeps the two copies in step.

**Decision**: the theme files (R2) become the only place colour values live.
- `NotaPalette` keeps its long-lived slot brushes, but a slot is now declared by **token
  name** only (`T(nameof(BgApp))`). `Apply(theme)` sets each slot's `Color` from the
  theme's values.
- At startup, `NotaThemeService` puts **the same brush objects** into
  `Application.Resources` under their `Brush.*` keys. The mapping comes from the
  existing `KeyMap`, extended to every key the XAML uses (R6).
- The colour entries are removed from `NotaTheme.axaml`'s `ThemeDictionaries`. Every
  `{DynamicResource Brush.*}` then resolves to a slot brush, and that brush re-tints in
  place on a theme switch. The XAML and custom-drawn layers can no longer disagree.

**Why**: two copies × four themes would be eight hand-synced lists. With one copy,
adding a theme is one file (FR-012, SC-006). Avalonia's `SolidColorBrush` is mutable
and invalidates what it paints when `Color` changes. `NotaThemeService.RepaintEverything`
already covers the custom-drawn views that read brushes inside `Render()`.

**Rejected**:
- *A custom `ThemeVariant` per theme* (`new ThemeVariant("EmberGraphite",
  ThemeVariant.Dark)`) with generated `ThemeDictionaries`. This works in Avalonia, but
  it leaves the XAML/C# duplication in place. It also makes `ActualThemeVariant` a
  custom value, which complicates reading the OS mode for follow system (R4).
- *Keeping XAML dictionaries and adding two more per theme*. This doubles the duplication.

## R2 — Theme file format: JSON, embedded

**Decision**: one JSON file per built-in theme in `src/managed/Nota.App/Theme/Themes/`
(`studio-dark.json`, `studio-light.json`, `ember-graphite.json`, `ember-paper.json`),
shipped as embedded resources and parsed with `System.Text.Json`, which is part of the
.NET base library (no new dependency). The shape is in
[contracts/theme-file.md](contracts/theme-file.md): `id`, `name`, `kind`, `formatVersion`,
`colors{token: "#RRGGBB" | "#AARRGGBB"}`.

**Why**: the smoke test reads theme files as plain text, as it already reads the XAML,
and checks them without loading the app. A later "import theme" feature can read the
same shape from a user folder unchanged (FR-012, US5). JSON is the format
`settings.json` already uses.

**Rejected**:
- *C# literals per theme*: a user theme can't take that shape.
- *XAML resource files*: they can't be validated without an Avalonia runtime, and the
  format isn't friendly to users.

## R3 — The token catalogue

**Decision**: the token set is the slot set `NotaPalette` declares. That is the 86
named slots, plus `Track1`…`Track9`, `ReturnA` and `ReturnB` (the anonymous array
slots, named now), plus the theme-only tokens below. A theme must give every token a
value; the check fails on a missing or extra token (FR-008, FR-015).

Theme-only tokens (needed by XAML; not palette slots today):
- **Shadow colours**: `ShadowBevel` (the raised control's top-edge highlight),
  `ShadowPopover` and `ShadowModal` (the blurred drop colour). The shadow *geometry*
  (offsets, blur, inset, the seam ring that uses `Seam`, the sunken ring that uses
  `Rule`) is Studio structure and is built in code (FR-010).
- **Fluent accent ramp**: `SystemAccent`, `SystemAccentLight1..3`,
  `SystemAccentDark1..3`. These are stored explicitly so Studio stays bit-exact. Ember's
  ramp is computed once with the rule Studio's ramp follows (light n = mix toward white
  0.12·n; dark n = accent × (1 − 0.10·n)) and stored.

Two XAML keys are washes of other tokens, not tokens: `SurfaceHoverClear` is `SurfaceHover`
at alpha 0, and `DangerSubtle` is `Danger` at alpha 0x33. Both become
`NotaPalette.Wash(...)` and follow the theme on their own.

**One-off device hues** (`NotaPalette.Ink("#hex")`) are not tokens. They are identity
hues of single visualisers. They follow the active theme's **kind**: a dark theme uses
the hex as given, and a light theme uses the derived or pinned light value as today.
`LegacyRole` hexes still resolve to role slots, so they follow the theme (FR-011).

## R4 — Appearance: single theme or follow system

**Decision**: there are two modes, set by `RequestedThemeVariant` on the application.
- **Single theme**: `RequestedThemeVariant` is set to the theme's kind (`Dark` /
  `Light`), so Fluent's own internals and the native window chrome match the kind
  (FR-009).
- **Follow system**: `RequestedThemeVariant = Default`. `ActualThemeVariantChanged`
  (already hooked in `NotaThemeService`) applies the dark-mode or light-mode theme.
  If the OS gives no signal, Avalonia reports Light, which matches the spec's
  fallback (spec, Edge Cases).

`App.OnFrameworkInitializationCompleted` already applies the theme before the first
window is created. The new call stays in the same place (FR-006, SC-007).

## R5 — Settings and migration

**Decision**: `Settings` gains three fields:
- `ColourTheme`: a theme id, or `"system"`. An empty string means "not migrated yet".
- `SystemDarkTheme` and `SystemLightTheme`: theme ids.

The legacy `Theme` (`AppTheme`) field stays and is **written back in step** (Dark /
Light by the active theme's kind, System when following), so an older Nota opening the
same `settings.json` still gets a sensible variant.

All reading goes through one pure function in `Nota.Application`, `Appearance.Resolve(...)`.
It handles migration, unknown ids and wrong-kind ids (FR-007, spec Edge Cases). The smoke
test can call it without a UI.

The settings file stores enums as numbers today (no `JsonStringEnumConverter`). Ids are
plain strings, so no converter change is needed.

## R6 — Every XAML key reaches a slot

The 70 `Brush.*` keys in each XAML dictionary map onto slots as follows:
- 46 match a slot's `// Brush.X` comment.
- The rest are aliases or names that are not commented:
  - aliases: `ChromeBg` → `BgSunken`, `Hairline` → `GraphBorder`, `SurfaceSelected` → `SelHeaderBg`, `Chroma*` → `Teal`/`Rose`/`Steel`…;
  - the text inks;
  - `Track1..9`, `ReturnA/B`, `Master`;
  - the two washes from R3.

`KeyMap` is extended to the full list. The check asserts that every
`Brush.*` key any view or style references is in `KeyMap` (this replaces today's
"key exists in both dictionaries" check).

## R7 — Readability bar: calibrated on Studio

The spec requires "the contrast minimums the design language sets" (FR-014).
`DESIGN.md` names no numeric pairings today, so this feature defines them, in
[contracts/ui-contract.md](contracts/ui-contract.md) § T3. WCAG 2.x is the target. Where
Studio itself sits below WCAG, the floor is Studio's own value, because Studio must not
change (SC-003).

Studio, measured (WCAG contrast ratio, worst pairing per class):

| Pairing | Studio Dark | Studio Light | Floor set |
|---|---|---|---|
| Body text (primary, secondary, muted) on ground, panel, raised, hover, well | 4.68 | 4.76 | **4.5** (WCAG AA) |
| Tertiary text on the same five | 3.73 | 3.40 | **3.0** |
| Faint / disabled / axis text on the same five | 3.03 | 2.58 | **2.5** (Studio floor; WCAG exempts disabled and incidental text) |
| Accent line (`AccentBright`) on ground, panel, well | 7.85 | **2.39** | **2.3** (Studio floor) |
| Derived ink on accent, mute, solo, record fills | ≥ 5.92 | ≥ 4.58 | **4.5** |
| Derived ink on track fills | ≥ 5.98 | ≥ 4.23 | **3.0** (glyph) |
| Track hues pairwise, ΔE76 | 17.7 | 15.7 | **12** |
| Accent, mute, solo, record pairwise, ΔE76 | 18.1 | 43.2 | **15** |

**Finding to raise (not fixed here)**: in Studio Light, the accent line on the ground
(2.39) and the faint text (2.58) sit below WCAG's 3:1 for graphics and incidental text.
This feature must leave Studio untouched (SC-003), so it records these values as the
floors rather than fixing them. Raising them is a separate, small Studio change.

**Interpretation of FR-014 "distinguishable"**: the nine track colours must differ from
each other, and accent, mute, solo and record must differ from each other. In Studio,
*solo = the Keys track blue* by design (one blue serves both), so the check does not
compare track hues against state colours.

## R8 — Ember values and the nudges they will need

**Source**: `main` at `f37f51e` ("Release 0.52.1"), `Theme/NotaPalette.cs` and
`Theme/NotaTheme.axaml`. Ember declares 77 named slots, and every one has a Studio slot
of the same name and role, because 010 re-valued slots by role and kept their names. So
Ember maps by **slot name**.

**Studio-only slots**, derived in Ember's palette by the role Studio gives them (FR-013):

| Token | Ember value from |
|---|---|
| `Seam` | Ember `Gutter` |
| `Rule` | Ember `BorderStrong` |
| `TextFaint` | Ember `TextDisabled` |
| `Mute` | Ember `Warning` (Ember's amber) |
| `Solo` | Ember `Steel` (Ember's blue) |
| `VizInk` | mix(Ember `TextPrimary`, Ember `BgSunken`, 0.92), Studio's formula |
| `VizGrid` | Ember `WellGrid` |
| `Master` | Ember `TextMuted` (Studio: the master strip is muted grey) |
| `ShadowBevel` | Ember's `Shadow.Raised` colour (`#08FFFFFF` / `#66FFFFFF`) |
| `ShadowPopover`, `ShadowModal` | Studio's drop alpha over Ember's darkest tone |
| `SystemAccent*` | R3 ramp rule applied to Ember `Accent` |

**Measured against R7 before any nudge** (the list that ships is produced by the check
during implementation and goes into DESIGN.md per FR-017):
- **Ember Graphite**:
  - muted text on hover is 4.45;
  - tertiary text on hover is 2.94;
  - disabled text is 2.01–2.51 on several surfaces;
  - ink on the record fill is 4.31.
- **Ember Paper**:
  - muted text is 3.54–4.09 on most surfaces;
  - tertiary text is 2.57–2.96;
  - disabled text is 2.00–2.67;
  - ink on the accent fill is 4.38;
  - Perc and Return track hues are ΔE 7.5 apart.

**Nudge rule (clarified)**: keep the hue and move lightness only, the smallest step that
passes, in steps of 1 % L in HSL. Each nudge is recorded as old → new plus the
failing pairing.

## R9 — Design docs page

`scripts/design-html.py` reads its palette from `NotaTheme.axaml`'s dictionaries. Once
the colours leave the XAML (R1), it reads the theme JSON files instead and renders one
palette section per built-in theme. It keeps the existing rule that the page can only
show values the app actually uses.

## R10 — Out of scope (confirmed by the spec)

These are left out:
- the theme editor;
- importing and exporting themes;
- per-project themes;
- the phone remote's web page;
- any change to Studio's values, including the R7 finding.
