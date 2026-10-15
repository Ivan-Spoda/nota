# Quickstart: validating device categories

**Feature**: [spec.md](spec.md) · **Contract**: [contracts/ui-contract.md](contracts/ui-contract.md)

## Prerequisites

- A Linux build environment as for 012: .NET 10 SDK, CMake toolchain, and Xvfb plus a
  PulseAudio null sink for screenshots.
- `scripts/build-linux.sh` (or `scripts/build.sh` on macOS) builds native and managed
  code and runs the smoke test.

## 1. Automated checks

```bash
scripts/build-linux.sh                                                  # full gate
dotnet tests/Nota.SmokeTest/bin/Release/net10.0/Nota.SmokeTest.dll --browser   # categories alone
```

Expect these to pass:

- **`-- design: browser --`**:
  - every built-in is categorised;
  - no stale names;
  - 18 / 26 / 6;
  - sentence case.
- **`-- browser: device categories --`**:
  - with no chip picked, each tab is today's list exactly;
  - each category's chip lists exactly its devices, and every device is in one;
  - clearing a chip restores the full list;
  - picks are per tab;
  - the chip stacks with search and with the Favorites chip;
  - with fake plug-ins, Reverb & delay lists the built-ins and then the VST3 reverb under
    Plug-ins;
  - an AU `Effect` makes an Other chip appear and lists under it;
  - the MIDI tab has no Other chip;
  - an existing `settings.json` has the chips on.

## 2. Manual / screenshot checks (Xvfb, all four themes)

1. Fresh `HOME`, Audio effects tab: the 8 chips appear above Favorites, none picked, and
   the full list shows.
2. Pick Reverb & delay: only Chamber, Delay and Reverb show, and the chip is engaged.
   Click it again to get the full list back. Pick Dynamics, then Modulation: the list
   switches.
3. With Dynamics picked, type "Prism": only Prism. Clear the search, favourite Compressor
   and turn on Favorites: only Compressor.
4. Switch to Instruments (unfiltered) and back: Dynamics is still picked.
5. ⋮ → untick "Category chips": the row hides and the full list returns. Relaunch: still
   off. Tick it again.
6. Narrow the browser to its minimum: the chips wrap onto more lines and nothing overlaps.
7. Capture the three tabs in all four themes, with and without a chip picked.

## 3. Plug-ins (macOS, optional)

Scan plug-ins (Settings → Plug-ins). A VST3 reverb appears under Reverb & delay. An
AU-only effect sits under the Other chip (a known limitation, research R2).
