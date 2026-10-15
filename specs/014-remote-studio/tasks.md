---

description: "Tasks: Nota Remote in Studio, wearing the app's theme"
---

# Tasks: Nota Remote in Studio, wearing the app's theme

**Input**: Design documents from `specs/014-remote-studio/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: requested by the spec (FR-020, US4): the smoke test checks the theme message and
guards the phone sources.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US4)

## Phase 1: Setup

- [X] T001 Install the phone page's dev dependencies and record the baseline: `npm ci && npm run check` in `src/web/remote` (expect 0 errors)
- [X] T002 [P] Write the stand-in Nota for screenshots: `src/web/remote/mock/nota-mock.mjs` (SPDX header; no dependencies — static server for `src/managed/Nota.Remote/wwwroot` plus a hand-rolled WebSocket; pairs any code, sends `welcome`, `theme` built from `src/managed/Nota.App/Theme/Themes/*.json` per contracts/theme-message.md, `proj` with a demo project of drum / instrument / audio / group / return tracks, sections and a key, `tp`, `mix`, `xy`, `mac`, `ses`; the page URL's `?theme=<id>` picks the theme the stand-in wears and `?pick=<dark>,<light>` the picks; `?legacy=1` sends only `proj.dark`); add `"mock": "node mock/nota-mock.mjs"` to `src/web/remote/package.json`
- [X] T003 Take "before" screenshots of every screen at 390 × 844 in the old Ember dark and light looks using the current bundle and the mock, into the scratchpad (PR material)

## Phase 2: Foundational (blocking)

- [X] T004 Add `RemoteTheme(string Id, string Name, bool Dark, IReadOnlyDictionary<string,string> Colors)` and `RemoteAppearance(RemoteTheme Active, RemoteTheme DarkPick, RemoteTheme LightPick, string InkDark, string InkLight)` to `src/managed/Nota.Remote/RemoteContracts.cs`; replace `bool DarkTheme` with `RemoteAppearance Appearance` on `IRemoteHost` (doc: "the same instance until the worn theme or the choice changes")
- [X] T005 [P] Name the two inks in `src/managed/Nota.App/Theme/NotaPalette.cs`: `InkDark` (#17181A) and `InkLight` (#FFFFFF) used by `ReadableInkOn`
- [X] T006 [P] Write `src/web/remote/src/lib/palette.ts` (pure, no DOM): `cssName(token)` (`BgApp` → `--bg-app`, `SystemAccentLight1` → `--system-accent-light1`, `Track1` → `--track1`), `inkOn(fill, [dark, light])` (WCAG contrast, ties → dark, as `NotaPalette.ReadableInkOn`), `cssVars(colors, ink)` returning every token plus `--ink-on-accent`, `--ink-on-mute`, `--ink-on-solo`, `--ink-on-record`, `--ink-on-success`
- [X] T007 Write `src/web/remote/theme-plugin.ts`: a Vite plugin resolving `virtual:nota-theme.css` to `:root{…}` from `studio-dark.json` and `[data-theme='light']{…}` from `studio-light.json` via `cssVars`, the ink pair being Studio Dark's `TextOnAccent` and `ShadeUp`; `addWatchFile` both; register it in `src/web/remote/vite.config.ts`

## Phase 3: User Story 1 — the phone wears Nota's theme (P1) 🎯 MVP

**Goal**: Follow Nota shows the desktop's theme and follows changes live.

**Independent test**: with the mock (`?theme=` each id) or a real Nota, the phone's
`--bg-app`, `--panel`, `--text-primary`, `--accent` equal the theme file's values.

- [X] T008 [US1] Implement `IRemoteHost.Appearance` in `src/managed/Nota.App/MainWindow.Remote.cs`: a per-id cache of `RemoteTheme` built from `NotaThemes.BuiltIn` (CSS hex `#RRGGBB`, `#RRGGBBAA` when alpha < 255), and a `RemoteAppearance` cached on `(NotaPalette.Theme, NotaThemeService.Choice)`; Active = `NotaPalette.Theme ?? NotaThemes.Default`, picks from `Choice.Dark` / `Choice.Light`
- [X] T009 [US1] Hub: in `src/managed/Nota.Remote/RemoteHub.State.cs` add `ThemeJson(host)` (serialise per contracts/theme-message.md — `t`, `on`, `dark`, `light`, `ink`, `th` with distinct ids; rebuild only when the `Appearance` reference changed); in `Push` send it before `proj` when `!ReferenceEquals(json, c.SentTheme)`; `proj.dark` from `Appearance.Active.Dark`; add `internal string? SentTheme;` to `src/managed/Nota.Remote/RemoteClient.cs`
- [X] T010 [US1] Smoke test: in `tests/Nota.SmokeTest/RemoteTests.cs` give `FakeHost` an `Appearance` (two prebuilt appearances, switchable) and check: a `theme` message arrives with `on`, both picks, `ink`, `th` holding exactly the distinct ids, each with the same full token set and `BgApp`; `proj.dark` equals `th[on].dark`; a switch sends one new `theme`; further ticks send none
- [X] T011 [US1] Phone theme module `src/web/remote/src/lib/theme.svelte.ts`: `Look` with `msg` (stored `nota-remote:theme`), `phoneDark` (live `matchMedia` listener), `legacyDark`; `worn(pref)`; `apply()` sets every var on `documentElement.style`, `data-theme`, `meta[name=theme-color]` = `--bg-app`, stores `nota-remote:vars`; `inkOn(hex)` for track fills
- [X] T012 [US1] Wire it: `src/web/remote/src/lib/app.svelte.ts` handles `t: 'theme'` (store + apply), feeds `proj.dark` to `legacyDark`, drops `get dark`; `src/web/remote/src/lib/types.ts` adds `ThemeMessage`; `src/web/remote/src/App.svelte` replaces the dark/light effect with an effect that applies the worn theme; `src/web/remote/src/main.ts` imports `virtual:nota-theme.css` before `app.css`
- [X] T013 [US1] First paint: a head script in `src/web/remote/index.html` that applies `nota-remote:vars` (and `data-theme`, theme-color) inside try/catch before the body renders; drop the hard-coded `theme-color` value and `data-theme="dark"` default stays

## Phase 4: User Story 2 — Follow phone uses the picks (P1)

**Goal**: Follow phone wears the dark-mode or light-mode pick by the phone's scheme, instantly.

**Independent test**: mock with `?pick=ember-graphite,studio-light`, phone menu → Follow
phone, toggle the emulated colour scheme: `--bg-app` switches between the two picks.

- [X] T014 [US2] In `src/web/remote/src/lib/theme.svelte.ts` make `worn` follow `prefs.theme === 'phone'` → `msg.dark` / `msg.light` by `phoneDark`, re-applying on a scheme change, a preference change and a new message (an `$effect.root` in the module, reading `app.prefs.theme`)
- [X] T015 [US2] In `src/web/remote/src/components/Sheets.svelte` keep the Theme segment (Follow Nota / Follow phone) and add the worn theme's name under it as a muted line ("Studio Light · from Nota's light-mode pick")

## Phase 5: User Story 3 — every screen reads as Studio (P2)

**Goal**: the ui-contract applied to every surface.

**Independent test**: screenshots of every screen in Studio Dark and Studio Light reviewed
against DESIGN.md § The rules.

- [X] T016 [US3] Rewrite `src/web/remote/src/app.css`: remove the Ember palette blocks and `.caps`; base type (Geist, 13 px, `--text-primary` on `--bg-app`), `.mono`, `.label` (600 · 11, `--text-muted`), `.btn`, `.seg`, `.switch` (28 × 16 squared), `.learnable` per contracts/ui-contract.md § Shared
- [X] T017 [P] [US3] Restyle `src/web/remote/src/App.svelte` (rail, tabs: selected = primary ink + 2 px accent line; seams)
- [X] T018 [P] [US3] Restyle `src/web/remote/src/components/Header.svelte` (track button, transport keys incl. record disc at rest, position well, link dot; thin space in "12 ms")
- [X] T019 [P] [US3] Restyle `src/web/remote/src/components/Banner.svelte`
- [X] T020 [P] [US3] Restyle `src/web/remote/src/components/Sheets.svelte` (popover, rows with accent tick, badges, field, switches, sentence-case heads, Disconnect an ordinary row)
- [X] T021 [P] [US3] Restyle `src/web/remote/src/components/Overlays.svelte` (covers, steps list, code cells, Connect again, count-in, toast)
- [X] T022 [P] [US3] Restyle `src/web/remote/src/screens/Pads.svelte` (tiles radius 3; held accent; lit = track colour + derived ink via `look.inkOn`; "Full velocity")
- [X] T023 [P] [US3] Restyle `src/web/remote/src/screens/Keys.svelte` (key-white / key-black, held accent, lit band; tiles; key source "Phone / Project / Default" neutral; numerals neutral)
- [X] T024 [P] [US3] Restyle `src/web/remote/src/screens/XY.svelte` (graph field, neutral crosshair and point at rest, accent while dragged or tilting, no fill; axis labels; tilt chip; percent with thin space)
- [X] T025 [P] [US3] Restyle `src/web/remote/src/screens/Mixer.svelte` (strips on seams, cap and pan neutral → accent while dragged, meter zones, M / S / Arm semantic fills with derived ink, "Arm", "Master" label)
- [X] T026 [P] [US3] Restyle `src/web/remote/src/screens/Macros.svelte` (desktop knob geometry and colours, names as written, automated dot muted, percent with thin space)
- [X] T027 [P] [US3] Restyle `src/web/remote/src/screens/Scenes.svelte` (clips radius 2, track fills with derived ink, recording solid record, progress in ink, sentence-case states)
- [X] T028 [P] [US3] Restyle `src/web/remote/src/screens/BigTransport.svelte` (keys, on states, record, sections tick / looping, "Bar 5" / "Looping", tap tempo label)
- [X] T029 [P] [US3] Restyle `src/web/remote/src/screens/Empty.svelte`

## Phase 6: User Story 4 — the theme travels with the build (P3)

- [X] T030 [US4] Phone-source guard `tests/Nota.SmokeTest/DesignTokenCheck.Remote.cs` (`RunRemote`): in `src/web/remote/src` (`.svelte`, `.ts`, `.css`, via `Files()` / `Rel()`) no `#hex` colour, `rgb(` / `rgba(` / `hsl(`; no `letter-spacing` other than 0; no `text-transform: uppercase`; no `.toUpperCase()`; `border-radius` only 0 / 1 / 2 / 3 / 4 px or 50 %; and `theme-plugin.ts` reads the theme files rather than holding values. Register the section in `tests/Nota.SmokeTest/Program.cs`
- [X] T031 [US4] Rebuild the bundle (`npm run build` in `src/web/remote` → `src/managed/Nota.Remote/wwwroot`), `npm run check` clean

## Phase 7: Polish

- [X] T032 Run `scripts/build.sh` (smoke test included); rerun timing-budget checks on an idle machine before calling one real
- [X] T033 "After" screenshots of every screen at 390 × 844 in Studio Dark and Studio Light (plus one each in Ember Graphite / Ember Paper and a Follow-phone flip) with the mock; review against DESIGN.md § The rules and fix
- [X] T034 [P] `CHANGELOG.md` [Unreleased] entry; `FEATURES.md` via nota-features; `DESIGN.md` § Shell note on Nota Remote; user manual's Remote page via nota-docs if the sibling repo is present (it is not checked out here — follow-up)
- [X] T035 Commit, push `feat/remote-studio`, open a draft PR on chrislaupama/nota into `feat/studio-ui` with before/after screenshots

## Dependencies

- T004 → T008, T009, T010. T006 → T007, T011. T011 → T012–T015, T022 (inkOn).
- US1 (T008–T013) before US2 (T014–T015). US3 tasks T017–T029 depend on T016 and are
  parallel with each other. T030 after US3. T031 after all phone edits. T032–T035 last.

## Parallel examples

- T005 ∥ T006 ∥ T002 (different files).
- T017 … T029 (one file each) once T016 lands.

## Implementation strategy

MVP = Phases 1–3 (the phone wears Nota's theme live). Then Follow phone, then the restyle,
then the guard, then the gate and the PR.

## Nota compliance (re-check)

- [X] SPDX header on `palette.ts`, `theme.svelte.ts`, `theme-plugin.ts`, `nota-mock.mjs`, `DesignTokenCheck.Remote.cs`
- [X] No new dependency
- [X] No audio-thread code touched
- [X] Smoke test extended (T010, T030)
- [X] CHANGELOG / FEATURES / DESIGN updated (T034)

## Phase 8: Convergence

- [X] T036 Set the number in a clip's length ("4 bars", "6 beats") in Geist Mono in `src/web/remote/src/screens/Scenes.svelte` per FR-015 (partial)
- [X] T037 Drop the `nota-remote-mock` entry added to the tracked `.claude/launch.json` (a local dev convenience outside the plan; `npm run mock` is the documented way) per plan: structure (unrequested)
