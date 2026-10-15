# Quickstart: validating the Studio visual style

## Prerequisites

- Linux host with the packages listed at the top of `scripts/build-linux.sh`, .NET SDK 10,
  and the JUCE / flac submodules (`git submodule update --init --recursive --depth 1`).
- For screenshots: `xvfb` and ImageMagick (`import`) or `xwd`.

## 1. Build and smoke test (the gate)

```bash
scripts/build-linux.sh
```

Expected: native + managed build, smoke test green, including `DesignTokenCheck`
(token parity, radius scale, shadow rule, no caps/tracking in labels, layout sizes from
[data-model.md](data-model.md)).

## 2. Visual pass, dark and light

```bash
Xvfb :99 -screen 0 1600x1000x24 &
DISPLAY=:99 dotnet src/managed/Nota.App/bin/Release/net10.0/Nota.App.dll &
DISPLAY=:99 import -window root shots/after-dark-main.png
```

Switch Preferences → Appearance to Light (and back to System) and repeat. Capture:
main window (arrangement), session, modular, detail panel with one instrument, one audio
effect, one MIDI effect, mixer, clip editor (MIDI + audio), preferences, export dialog,
command palette, an open context menu, a tooltip.

Check each capture against [contracts/ui-contract.md](contracts/ui-contract.md):

| Scenario | Expected |
|---|---|
| Stopped, nothing selected | zero orange pixels (C1.2) |
| Press play, select a clip, enable loop | only those turn orange (C1.3) |
| Hover / drag a knob | arc turns orange, back to neutral on release (C1.4) |
| Mute / solo / arm a track | amber / blue / red fills, legible ink (C1.5) |
| Main window | docked panels on 1 px seams, no gutter, no rounded islands (C2) |
| Any device card | sentence-case labels, no truncation, mono values (C4) |
| Open a context menu | raised, 3 px, soft shadow; hover = orange fill (C3.2) |
| Toggle appearance | every surface switches within 1 s, no leftovers (C6.4) |

## 3. Behaviour regression pass

Open an existing project from before the change: it loads unchanged; save, reopen,
diff the project file — only non-visual fields may differ (none expected). Walk the
FEATURES.md sections: each feature reachable from the same place.

## 4. Docs

`python3 scripts/design-html.py` regenerates DESIGN.html with no error; DESIGN.md token
tables match `NotaTheme.axaml`.
