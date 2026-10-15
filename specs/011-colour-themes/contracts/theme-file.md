# Contract: Colour Theme File (format version 1)

The single shape every colour theme takes: the four built-ins now, user themes later
(FR-012, US5). Built-ins live in `src/managed/Nota.App/Theme/Themes/<id>.json` and ship
as embedded resources.

```json
{
  "formatVersion": 1,
  "id": "ember-graphite",
  "name": "Ember Graphite",
  "kind": "dark",
  "colors": {
    "BgApp": "#0B0A09",
    "Panel": "#141310",
    "Accent": "#D8A03D",
    "ShadowBevel": "#08FFFFFF",
    "...": "one entry per catalogue token"
  }
}
```

## Rules

| # | Rule | On violation |
|---|---|---|
| F1 | `formatVersion` is `1`. | Built-in: check fails. Loader: theme skipped. |
| F2 | `id` matches `^[a-z0-9]+(-[a-z0-9]+)*$`, is not `system`, and is unique across loaded themes. | same |
| F3 | `name` is 1–40 characters. | same |
| F4 | `kind` is `dark` or `light`. | same |
| F5 | `colors` has **every** catalogue token ([data-model.md](../data-model.md)) and **no other** key. | Check names the theme and each missing or unknown token. |
| F6 | Each value is `#RRGGBB` or `#AARRGGBB` (hex, either case). Tokens other than `ShadowBevel`, `ShadowPopover` and `ShadowModal` are opaque (alpha `FF` or omitted). | Check names theme + token + value. |
| F7 | The theme meets every readability pairing in [ui-contract.md](ui-contract.md) § T3. | Check names theme + pairing + ratio. |
| F8 | Studio themes only: no value equals an Ember-era palette value (the existing Studio rule, FR-016). | Check names the token. |

The loader never throws to the UI. A built-in that fails F1–F6 is a build-time error,
because the smoke test fails. At runtime the theme is left out of the list and the
fallback of FR-007 applies.

## Key order and comments

Keys are written in catalogue order (surfaces → lines → text → accent → semantics → data
→ tinted chrome → keys → shade targets → tracks → elevation), so diffs between themes
line up. JSON has no comments. The *role* of each token is documented once, in
`DESIGN.md` and in `NotaPalette.cs`, not repeated per file.
