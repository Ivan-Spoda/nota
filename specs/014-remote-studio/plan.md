# Implementation Plan: Nota Remote in Studio, wearing the app's theme

**Branch**: `feat/remote-studio` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/014-remote-studio/spec.md`

## Summary

Nota tells each phone the colours of the theme it wears, and the user's dark-mode and
light-mode picks, in a new `theme` message built from the theme files; the phone sets them
as CSS custom properties named after the tokens and re-applies on every change, on a
Follow-phone scheme flip, and from storage on first paint. A build-time fallback generated
from `studio-dark.json` / `studio-light.json` means the phone sources hold no colour. Every
phone surface is restyled to Studio per [contracts/ui-contract.md](contracts/ui-contract.md).
The smoke test checks the message and guards the phone sources.

## Nota: Read Before Planning

- **UI work** → [`DESIGN.md`](../../DESIGN.md) (Studio design language) and the `nota-design` skill.
- **Anything** → [`CONTRIBUTING.md`](../../CONTRIBUTING.md): licensing, dependency policy, PR expectations.

## Technical Context

**Language/Version**: C# / .NET 10 (hub, app host), TypeScript 6 + Svelte 5 (phone page)

**Primary Dependencies**: none new. Vite 8, Svelte 5, svelte-check (existing dev
dependencies of `src/web/remote`).

**Storage**: the phone's `localStorage` (`nota-remote:theme`, `nota-remote:vars`).

**Audio-Thread Impact**: None. The hub builds messages on the UI tick
(`RemoteHub.Tick`, UI thread); the app reads theme objects on the UI thread. Nothing
reaches the engine.

**Testing**: `tests/Nota.SmokeTest` — `RemoteTests.Session` (real WebSocket against
`RemoteServer` with a fake host) and a new `DesignTokenCheck.Remote` source check;
`svelte-check`; screenshots at phone width.

**Target Platform**: Nota on macOS / Windows / Linux; the page on iOS Safari 16+ and
Chrome 100+ (the Vite build target).

**Project Type**: desktop app + embedded web page.

**Performance Goals**: theme change on the phone < 1 s after the desktop switches
(SC-002); Follow-phone flip < 100 ms (SC-003); zero per-tick cost when nothing changed.

**Constraints**: no colour literal in the phone sources; touch sizes and behaviour
unchanged; the page bundle stays small (the theme module adds well under 2 KB).

**Scale/Scope**: ~1,700 lines of Svelte across 13 components and screens, ~60 lines of
C# in the hub and app, one smoke-test section.

## Constitution Check

| Principle | Status |
|---|---|
| I. Real-time audio safety | ✅ No audio-thread code touched |
| II. Boundary discipline | ✅ No C ABI change; no JUCE |
| III. Licensing hygiene | ✅ New files carry the SPDX header; no new dependency |
| IV. Verification gate | ✅ Smoke test extended (theme message + phone-source guard); `scripts/build.sh` before the PR |
| V. One logical change | ✅ One feature: the phone matches the app (theme + restyle), stacked on `feat/studio-ui` |
| VI. Docs in sync | ✅ CHANGELOG [Unreleased], FEATURES.md, DESIGN.md (Remote note), user manual via nota-docs |
| VII. Spec first | ✅ This spec, plan, tasks |
| VIII. Rules not weakened | ✅ |

Post-design re-check: unchanged — the design adds one host property, one message, one
phone module, one Vite plugin and one guard.

## Design

### Host → hub (`Nota.Remote/RemoteContracts.cs`, `Nota.App/MainWindow.Remote.cs`)

- New records `RemoteTheme` and `RemoteAppearance` ([data-model.md](data-model.md)).
- `IRemoteHost.DarkTheme` → `IRemoteHost.Appearance`.
- App: build a `RemoteTheme` per `NotaColourTheme` once (dictionary by id, CSS hex), and a
  `RemoteAppearance` cached on `(NotaPalette.Theme, NotaThemeService.Choice)`.
- `NotaPalette.InkDark` / `InkLight`: the two inks `ReadableInkOn` already uses, named.

### Hub (`Nota.Remote/RemoteHub.State.cs`, `RemoteClient.cs`)

- `ThemeJson(host)`: serialise the appearance when its reference changed; keep the
  string. `Push` sends it first, before `proj`, when it is not the string the phone last
  got (`RemoteClient.SentTheme`, reference comparison).
- `proj.dark` = `Appearance.Active.Dark` (kept for older pages).

### Phone (`src/web/remote`)

- `src/lib/palette.ts` (pure, also imported by the Vite config): `cssName(token)`,
  `inkOn(fill, ink)`, `cssVars(colors, ink)` → `{ '--bg-app': '#…', …, '--ink-on-accent': … }`.
- `theme-plugin.ts` (Vite plugin, beside `vite.config.ts`): resolves
  `virtual:nota-theme.css` to `:root{…}` from `studio-dark.json` and
  `[data-theme='light']{…}` from `studio-light.json`; watches both files.
- `src/lib/theme.svelte.ts`: `Look` — the stored message, the phone scheme listener,
  `worn(prefs)`, `apply()`, `inkOn(hex)` for track fills. `app.svelte.ts` handles
  `t: 'theme'` and drops `get dark`.
- `index.html`: a head script applying `nota-remote:vars` before first paint.
- `app.css`: the Ember palette blocks go; shared styles (`.btn`, `.seg`, `.switch`, `.label`,
  `.mono`) rewritten per the UI contract; `.caps` removed.
- Every component and screen restyled per [contracts/ui-contract.md](contracts/ui-contract.md).
- `mock/nota-mock.mjs` (`npm run mock`): a dependency-free stand-in Nota (static server +
  hand-rolled WebSocket) that serves `wwwroot` with a demo project and real theme
  messages, for screenshots and design review without the desktop app.

### Tests

- `RemoteTests`: `FakeHost.Appearance` with two switchable appearances; checks T1–T3 and
  "not re-sent unchanged".
- `DesignTokenCheck.Remote`: the phone-source guard (research R10).

## Project Structure

### Documentation (this feature)

```text
specs/014-remote-studio/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── theme-message.md
│   └── ui-contract.md
├── checklists/requirements.md
└── tasks.md
```

### Source Code

```text
src/managed/
├── Nota.Remote/
│   ├── RemoteContracts.cs     # RemoteTheme, RemoteAppearance, IRemoteHost.Appearance
│   ├── RemoteHub.State.cs     # theme message, proj.dark from Appearance
│   └── RemoteClient.cs        # SentTheme
│   └── wwwroot/               # rebuilt bundle
└── Nota.App/
    ├── MainWindow.Remote.cs   # IRemoteHost.Appearance
    └── Theme/NotaPalette.cs   # InkDark / InkLight
src/web/remote/
├── index.html                 # first-paint head script
├── package.json               # "mock" script
├── theme-plugin.ts            # virtual:nota-theme.css from the theme files
├── vite.config.ts
├── mock/nota-mock.mjs
└── src/
    ├── app.css
    ├── main.ts
    ├── App.svelte
    ├── lib/{palette.ts, theme.svelte.ts, app.svelte.ts, types.ts}
    ├── components/{Header, Banner, Sheets, Overlays}.svelte
    └── screens/{Pads, Keys, XY, Mixer, Macros, Scenes, BigTransport, Empty}.svelte
tests/Nota.SmokeTest/
├── RemoteTests.cs
├── DesignTokenCheck.Remote.cs
└── Program.cs                 # registers the new section
```

**Structure Decision**: existing projects only; one new phone module pair, one Vite
plugin file, one dev mock, one smoke-test partial.

## Complexity Tracking

None.
