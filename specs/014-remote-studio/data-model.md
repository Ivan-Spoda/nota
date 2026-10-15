# Data model: Nota Remote theme

## Host side (`Nota.Remote`, implemented by `Nota.App`)

### `RemoteTheme` (record)

| Field | Type | Rule |
|---|---|---|
| `Id` | string | A built-in theme id (`studio-dark` …) |
| `Name` | string | Display name ("Studio Dark") |
| `Dark` | bool | The theme's kind |
| `Colors` | `IReadOnlyDictionary<string, string>` | Every catalogue token → CSS hex: `#RRGGBB`, or `#RRGGBBAA` when translucent |

### `RemoteAppearance` (record)

| Field | Type | Rule |
|---|---|---|
| `Active` | `RemoteTheme` | The theme Nota is wearing now (`NotaPalette.Theme`) |
| `DarkPick` | `RemoteTheme` | `NotaThemeService.Choice.Dark` |
| `LightPick` | `RemoteTheme` | `NotaThemeService.Choice.Light` |
| `Ink` | `(string Dark, string Light)` | `NotaPalette.InkDark` / `InkLight` as CSS hex |

Identity: the app returns the **same instance** until the worn theme or the choice changes
(cache key `(NotaPalette.Theme, NotaThemeService.Choice)`); `RemoteTheme` instances are
cached per theme id. The hub relies on reference identity for change detection.

`IRemoteHost.DarkTheme` is replaced by `IRemoteHost.Appearance`. The `proj` message keeps
`dark` (now `Appearance.Active.Dark`) for a page cached from an older Nota.

### `RemoteClient.SentTheme`

The theme JSON string last sent to that phone (reference-compared). Null on a new
connection, so a phone always gets the theme first.

## Phone side (`src/web/remote/src/lib/theme.svelte.ts`)

### `ThemeMessage` (as received, see [contracts/theme-message.md](contracts/theme-message.md))

### Look state

| Field | Source | Notes |
|---|---|---|
| `msg` | last `theme` message, or the stored one | Persisted as `nota-remote:theme` |
| `phoneDark` | `matchMedia('(prefers-color-scheme: dark)')`, live | |
| `legacyDark` | `proj.dark` when no `theme` message was ever received | Older Nota only |
| `worn` | derived | `prefs.theme === 'phone'` → `msg.dark` / `msg.light` by `phoneDark`; else `msg.on`. No `msg` → the fallback (Studio Dark, or Studio Light when `legacyDark === false`) |
| `vars` | derived from `worn` | Every token as `--kebab-case`, plus the derived `--ink-on-*` |

Applying `vars`: set each on `document.documentElement.style`, set `data-theme` to
`dark` / `light` (drives `color-scheme`), set `meta[name=theme-color]` to `--bg-app`, and
store the resolved map as `nota-remote:vars` for the head script.

### Derived variables

| Variable | Rule |
|---|---|
| `--ink-on-accent` / `--ink-on-mute` / `--ink-on-solo` / `--ink-on-record` / `--ink-on-success` | `inkOn(fill)`: of `ink.dark` and `ink.light`, the one with the higher WCAG contrast against the fill (ties → dark), as `NotaPalette.ReadableInkOn` |
| Per-track ink | `inkOn(track.color)`, computed in the component for track-colour fills |
