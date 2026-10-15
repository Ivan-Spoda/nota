# Research: Nota Remote in Studio

## R1. What the hub sends: the full token set, three themes, on change

- **Decision**: a dedicated `theme` message carries the active theme, the dark-mode pick and
  the light-mode pick (deduplicated by id), each with **every** catalogue token, plus the
  derived-ink pair. Sent to a phone when it connects and when the host's appearance changes.
- **Rationale**: the phone can use any token without a C# change, so the theme files stay
  the one place a colour lives and adding a CSS use is a phone-only edit. Size: ~110
  tokens × ~24 bytes ≈ 2.6 KB per theme, ≤ 3 themes ≈ 8 KB, once per change on a LAN.
  Sending the picks lets Follow phone switch instantly (SC-003) without a round trip.
- **Alternatives**: (a) a phone-side subset list mirrored in C# — two lists to keep in step;
  (b) put the colours in the `proj` message — it is rebuilt and compared every few ticks,
  so 8 KB would be re-serialised constantly; (c) only the active theme — Follow phone
  would need a request/response.

## R2. Change detection without per-tick cost

- **Decision**: `IRemoteHost.Appearance` returns a cached `RemoteAppearance` object that the
  app replaces only when the worn theme or the appearance choice changes. The hub keeps
  the JSON for the last object it saw and compares by reference, per hub and per phone.
- **Rationale**: the hub ticks at 30 Hz; a reference check is free, and serialising happens
  once per change. `AppearanceChoice` is a record (value equality) and the worn theme is
  `NotaPalette.Theme` (one instance per theme), so the app's cache key is exact.
- **Alternatives**: an event from `NotaThemeService` — would need the hub to subscribe
  across the app/remote boundary; string comparison of 8 KB per phone per tick — wasteful.

## R3. Colours in CSS form

- **Decision**: the app formats each colour as CSS hex — `#RRGGBB`, or `#RRGGBBAA` for the
  translucent shadow tokens (Avalonia's `#AARRGGBB` reordered). The phone sets each token
  as a custom property named after it in kebab case: `BgApp` → `--bg-app`,
  `SystemAccentLight1` → `--system-accent-light1`.
- **Rationale**: one mechanical rule, no mapping table; CSS reads the names as the desktop
  code does (`--surface-raised` ↔ `SurfaceRaised`).

## R4. Derived ink

- **Decision**: the phone computes ink on a solid fill exactly as `NotaPalette.ReadableInkOn`
  does — the WCAG contrast of a dark and a light ink against the fill, the better one wins.
  Nota sends the ink pair in the theme message (`ink: [dark, light]`, from
  `NotaPalette.InkDark` / `InkLight`, two named constants factored out of
  `ReadableInkOn`). The phone exposes `--ink-on-accent`, `--ink-on-mute`, `--ink-on-solo`,
  `--ink-on-record`, `--ink-on-success` and computes per-track ink for track-colour fills.
- **Rationale**: identical result to the desktop; no ink literal in the phone.
- **First paint**: before any message, the pair is Studio Dark's `TextOnAccent` and
  `ShadeUp`, which are exactly the desktop's two inks; the fallback generator reads them
  from `studio-dark.json`.

## R5. First paint without a flash and without literals

- **Decision**: three layers.
  1. **Fallback CSS generated at build time** from `studio-dark.json` (`:root`) and
     `studio-light.json` (`[data-theme='light']`, for a Nota that sends only `dark`) by a
     small Vite plugin, imported as `virtual:nota-theme.css`. The phone sources hold no
     colour value; the bundle does.
  2. **The last worn theme**, stored on the phone (`localStorage`), applied by a short
     inline script in `index.html`'s head before first paint.
  3. **Live**: the `theme` message, applied as it arrives.
- **Rationale**: a returning phone paints in its last theme on the first frame; a new one
  in Studio Dark, the desktop's default; a value change in a theme file reaches the
  fallback on the next `npm run build`.
- **Alternatives**: hand-copied fallback values — the drift that left the phone in Ember;
  a `prefers-color-scheme` fallback — Nota's default is Studio Dark regardless of OS.

## R6. Follow phone

- **Decision**: a `matchMedia('(prefers-color-scheme: dark)')` listener holds the phone's
  scheme as reactive state; the worn theme is `prefs.theme === 'phone' ? (dark ? picks.dark
  : picks.light) : on`. Changing the scheme, the preference or receiving a message
  re-applies at once.
- **Rationale**: today's `app.dark` reads `matchMedia` once per evaluation and does not
  react to a scheme change; the listener fixes that (SC-003).

## R7. Browser chrome

- **Decision**: `meta[name=theme-color]` takes the worn theme's `BgApp`; `color-scheme`
  follows the theme's kind; the manifest keeps its static colours (only used on install).

## R8. Restyle scope and the phone's sizes

- **Decision**: Studio's colour, case, corner, seam, elevation and type-family rules apply
  to every phone surface; its desktop control heights (26 px) and the 13 px shell cap do
  not — touch targets stay 36–56 px and phone type 10–16 px (bigger for position and
  count-in). Icons stay inline SVG with the shared stroke (round caps and joins, 1.5 px).
- **Rationale**: FR-018 (behaviour and layout unchanged) and the spec's assumptions.

## R9. Meters

- **Decision**: meter fills keep Studio's zones — `Success` up to −6 dB, `Warning` to 0 dB,
  `Danger` above — as hard colour stops on a full-height bar, clipped to the level. The
  hub maps −60…+6 dBFS onto 0…1, so −6 dB sits at 54/66 and 0 dB at 60/66.
- **Rationale**: matches `MeterBar`; hard stops are zones, not a decorative gradient.

## R10. Guarding the rules

- **Decision**: extend the smoke test with a phone-source check (in `DesignTokenCheck`):
  no hex / `rgb(` / `rgba(` colour in `src/web/remote/src` outside the theme module's
  fallback import, no `letter-spacing` other than 0, no `text-transform: uppercase`, no
  `.toUpperCase()` on a label, and `border-radius` values only 0 / 1 / 2 / 3 / 4 px or
  50 % (round dots). Plus the hub's theme message (RemoteTests).
- **Rationale**: the same ratchet idea the desktop uses; it keeps the phone from drifting
  back.
