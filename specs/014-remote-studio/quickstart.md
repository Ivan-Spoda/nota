# Quickstart: verifying Nota Remote in Studio

## Prerequisites

As for any Nota build ([`BUILD.md`](../../BUILD.md)), plus Node for the phone page.

## 1. Automated gate

```bash
cd src/web/remote && npm ci && npm run check && npm run build && cd -
scripts/build.sh
```

Expect:
- `svelte-check found 0 errors and 0 warnings`.
- `vite build` writes `src/managed/Nota.Remote/wwwroot` (index.html, assets/, fonts/,
  icons/, sw.js, manifest, keepawake.mp4).
- In the smoke test's Remote section: the phone receives a `theme` message right after
  `welcome` naming the active theme and both picks, with every catalogue token per theme
  ([contracts/theme-message.md](contracts/theme-message.md) T1–T3); a host theme change
  sends it once more; an unchanged one does not.
- In the design section: the phone-source checks (no colour literal outside the generated
  fallback, no tracking, no forced caps, radii 0–4).

## 2. Manual: Follow Nota (US1)

1. Run Nota, turn Remote on, pair a phone (or a desktop browser at 390 × 844 — the
   in-app browser or Chrome device mode).
2. Preferences → Appearance → pick each of Studio Dark, Studio Light, Ember Graphite,
   Ember Paper. The phone follows within a second each time; a held pad keeps sounding.
3. Follow system in Nota, flip the OS appearance: the phone follows.

## 3. Manual: Follow phone (US2)

1. In Nota set In dark mode = Ember Graphite, In light mode = Studio Light.
2. On the phone: ⋯ → Theme → Follow phone. Flip the phone's (or the browser's emulated)
   colour scheme. The page switches at once to the matching pick.
3. Change a pick in Nota: the phone updates.

## 4. Manual: first paint

Reload the page with Nota closed: the "Nota isn't responding" cover appears in the last
theme worn, with no flash. Clear site data and reload: Studio Dark.

## 5. Screenshots without a running Nota

`npm run mock` (in `src/web/remote`) serves the built page with a stand-in Nota that speaks
the Remote protocol with a fixed demo project and the real theme files. Open
`http://127.0.0.1:7790/#p=1234` at phone width. `?theme=studio-light` (any built-in id)
picks the theme the stand-in wears, `?pick=ember-graphite,studio-light` its dark-mode and
light-mode picks, and `?legacy=1` makes it an older Nota that sends only `dark`.
