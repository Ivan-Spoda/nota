# UI Contract: Colour Themes

What a user sees and what the checks enforce. The Studio contract of
[010](../../010-studio-visual-style/contracts/ui-contract.md) (C1–C6) stays in force for
every theme; the only change is that "token values" now means the active theme's.

## T1 — Preferences → Appearance

- T1.1 **Colour theme** leads with a segmented control: **Dark · Light · Follow system**.
  Below it sit two rows of cards, **Dark** then **Light** themes, in the built-in order
  of [data-model.md](../data-model.md). Both rows always show, so the page never changes
  height with the mode. Each card is:
  - a 184 × 112 preview: a miniature Nota window (title bar, transport with the view
    keys, browser names, four arrangement lanes with clips in the first track colours and
    the playhead, the device panel) drawn in **that theme's own colours**, whichever
    theme is live;
  - in a 2 px frame, radius 3: seam at rest, rule on hover, the accent when chosen —
    state changes the colour only;
  - the theme name under it.
- T1.2 With **Dark** or **Light**, one card is chosen and the row in use has the brighter
  label. With **Follow system**, the labels read **In dark mode** and **In light mode** and
  one card is chosen in each row. Labels change text and ink only. The segment keys keep
  their weight when chosen (the accent fill marks the choice), so the switch never
  changes width.
- T1.3 **Dark** or **Light** wears that mode's remembered pick, so switching modes and
  back returns to the same theme. Picking a card wears it and remembers it for its mode,
  so a card in the other row switches mode; while following, it sets only that mode's
  theme (`Appearance.WithMode` / `WithPick`).
- T1.4 Turning follow system on applies the theme for the current OS mode (FR-005).
  The cards are a `ListBox`, so keyboard focus and selection work as in any list.
- T1.5 All changes apply on click (no Apply button), are saved at once, and are live
  in every open window within 0.5 s (SC-001).
- T1.6 Labels are sentence case per Studio. Theme names are shown as written.

## T2 — Switching

- T2.1 A switch re-tints every colour the app paints, including:
  - XAML-styled controls;
  - custom-drawn views (arrangement, mixer, piano roll, device cards, graphs, meters,
    waveforms);
  - washes, derived track shades and contrast-picked ink;
  - shadows;
  - Fluent's accent ramp;
  - popups, menus, tooltips and windows that are open or opened later.
- T2.2 Nothing but colour changes: no layout pass changes a size or position, and
  selection, scroll, playback, open panels and focus are kept.
- T2.3 Native chrome follows the theme's kind.
- T2.4 Audio is unaffected. Switching happens on the UI thread only, and nothing
  crosses to the engine.

## T3 — Readability pairings (every built-in theme; check: `DesignTokenCheck.RunThemes`)

Calibrated on Studio (research R7). Ratio = WCAG 2.x contrast ratio. ΔE = CIE76.

| # | Pairing | Minimum |
|---|---|---|
| T3.1 | `TextPrimary`, `TextSecondary`, `TextMuted` on `BgApp`, `Panel`, `SurfaceRaised`, `SurfaceHover`, `BgSunken` | 4.5 |
| T3.2 | `TextTertiary` on the same five | 3.0 |
| T3.3 | `TextFaint`, `TextDisabled`, `TextAxis` on the same five | 2.5 |
| T3.4 | `AccentBright` on `BgApp`, `Panel`, `BgSunken` | 2.3 |
| T3.5 | Derived ink (`ReadableInkOn`) on `Accent`, `Mute`, `Solo`, `Record` | 4.5 |
| T3.6 | Derived ink on `Track1`…`Track9`, `ReturnA`, `ReturnB` | 3.0 |
| T3.7 | `Track1`…`Track9` pairwise | ΔE ≥ 12 |
| T3.8 | `Accent`, `Mute`, `Solo`, `Record` pairwise | ΔE ≥ 15 |

Track hues are not compared with state colours: Studio's solo blue is the Keys track
blue by design (R7).

## T4 — Ember

- T4.1 Every Ember token equals its 0.52.1 value, or its R8 derivation for a
  Studio-only token, **except** values nudged to meet T3. Each nudge keeps the hue and
  changes lightness only, by the smallest step that passes.
- T4.2 `DESIGN.md` lists every nudge: token, theme, 0.52.1 value → shipped value, and
  the T3 pairing it failed (FR-017).
