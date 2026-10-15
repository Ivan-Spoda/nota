# Contract: the `theme` message (Nota → phone)

Sent over the Remote WebSocket, as one text frame of JSON.

```json
{
  "t": "theme",
  "on": "studio-dark",
  "dark": "ember-graphite",
  "light": "studio-light",
  "ink": ["#17181A", "#FFFFFF"],
  "th": {
    "studio-dark":    { "name": "Studio Dark",    "dark": true,  "c": { "BgApp": "#1E1F22", "ShadowBevel": "#FFFFFF12", "…": "…" } },
    "ember-graphite": { "name": "Ember Graphite", "dark": true,  "c": { "…": "…" } },
    "studio-light":   { "name": "Studio Light",   "dark": false, "c": { "…": "…" } }
  }
}
```

| Field | Meaning |
|---|---|
| `on` | The theme Nota is wearing now (Follow Nota wears it) |
| `dark` / `light` | The user's dark-mode and light-mode picks (Follow phone wears them) |
| `ink` | The dark and light inks text on a solid fill is chosen from |
| `th` | Each theme named by `on` / `dark` / `light`, once: name, kind, every token |
| `c` | Token name (as in `Theme/Themes/*.json`) → CSS hex; translucent tokens as `#RRGGBBAA` |

## Rules

- **T1** Sent to a phone right after `welcome`, before `proj`, and again whenever any field
  changes. Never re-sent unchanged.
- **T2** `th` holds exactly the distinct ids among `on`, `dark`, `light`.
- **T3** Every theme in `th` carries the full token catalogue.
- **T4** The phone ignores tokens it does not use and keeps its current value for a token a
  theme lacks.
- **T5** `proj.dark` stays, equal to `th[on].dark`, for pages from an older Nota.
