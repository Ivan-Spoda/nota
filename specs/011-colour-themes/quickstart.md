# Quickstart: verifying colour themes

## Prerequisites

As for any Nota build: [`BUILD.md`](../../BUILD.md). On Linux, the smoke test needs an
audio sink (PulseAudio null sink is fine). The screenshots need an X server (Xvfb).

## 1. Automated gate

```bash
scripts/build-linux.sh        # or scripts/build.sh on macOS
```

Expect in the smoke test output:
- `-- design: themes --`: for each of the four built-ins, `catalogue complete`,
  `values valid`, every T3 pairing ✓, and `studio: no Ember value` for the two Studio
  themes.
- `-- design: appearance --`: the migration/fallback table of
  [data-model.md](data-model.md) § Resolution, one ✓ per case (legacy Dark / Light /
  System; unknown id; wrong-kind per-mode id; empty file).
- The existing `-- design: studio --` checks unchanged.

Negative check: delete one key from a copy of `ember-paper.json` (in a scratch build).
The check fails and names `ember-paper` and that token.

## 2. Manual: pick a theme (US1)

1. Launch Nota and open a project with several tracks in different colours, a device
   card, and the mixer.
2. Preferences → Appearance → pick **Ember Graphite**. Every window recolours at once.
   Playback keeps going, and selection and scroll stay where they were.
3. Pick **Ember Paper**, **Studio Light**, then **Studio Dark**. Each applies fully.
   Open a context menu and a tooltip in each: they match.
4. Quit and relaunch: Nota opens in the last theme, with no flash of another.

## 3. Manual: follow system (US2)

1. Turn on **Follow system**. Set *In dark mode* = Studio Dark and
   *In light mode* = Ember Paper.
2. Flip the OS appearance both ways. Nota follows within 1 s.
3. Pick a list theme: follow system turns off. Turn it back on: your two choices are
   still set.

## 4. Screenshots (SC-003, SC-004)

Retake the 010 screen set (arrangement, mixer, session, device cards, browser, menus,
preferences, command palette, export dialog) in all four themes.
- **Studio Dark / Light**: pixel-compare against the 010 shots. They must be identical.
- **Ember**: overlay on the Studio shot of the same kind. Element edges must coincide;
  only colour differs.

## 5. Migration (US1/AC4)

Start once from a `settings.json` containing only `"Theme": 2` (System). Nota opens in
follow system with Studio Dark / Studio Light. After any change to Appearance, the file
holds the new fields and `Theme` still reflects the mode.
