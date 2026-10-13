# Implementation Plan: Return & Master Track Levels

**Branch**: `005-return-master-meters` | **Date**: 2026-10-13 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/005-return-master-meters/spec.md`

## Summary

The arrangement's pinned Return and Master rows get what regular track headers have
for level: a live vertical stereo `MeterBar`, a `MiniFader` for volume and a real dB
read-out (today a constant "−∞" / "0.0"). Returns meter and set volume through the
existing per-track engine calls; the master meters through `MasterMeter()` and sets
volume through `TransportViewModel.MasterVolume`, the single owner of the master
volume. `MixerView`, which today keeps a private master volume that is never saved,
is switched to the same owner. UI-only; no engine or C ABI change. See
[research.md](research.md).

## Nota: Read Before Planning

- **UI work** → [`DESIGN.md`](../../DESIGN.md) § meters (11/5 stereo, Signal → Caution
  → Alert, peak hold until clicked, no ballistics — all inherited from `MeterBar`) and
  the `nota-design` skill.
- **Anything** → [`CONTRIBUTING.md`](../../CONTRIBUTING.md).

## Technical Context

**Language/Version**: C# / .NET 10 (managed app only)

**Primary Dependencies**: Avalonia (existing); no new dependency

**Storage**: N/A — return volume and master volume are already persisted

**Audio-Thread Impact**: None. UI-thread reads of existing atomic meters
(`nota_engine_track_meter`, `nota_engine_master_meter`) and existing volume setters.

**Testing**: `scripts/build.sh` (smoke test unchanged); headless Avalonia harness for
layout, fader and sync checks ([quickstart.md](quickstart.md))

**Target Platform**: macOS / Windows / Linux desktop app

**Project Type**: desktop-app

**Performance Goals**: meters update in the existing ~30 Hz UI tick; ≤ 1 extra
P/Invoke per return per tick (8 returns → 8 calls, negligible)

**Constraints**: footer row height stays 40 px; header width 228 px

**Scale/Scope**: 2 views + MainWindow wiring; ~150 lines

## Constitution Check

| Principle | Status |
|---|---|
| I. Real-time audio safety | ✅ No engine change; reads existing meters from the UI thread |
| II. Boundary discipline | ✅ Uses existing C ABI calls only |
| III. Licensing hygiene | ✅ No new source files; no new dependency |
| IV. Verification gate | ✅ `scripts/build.sh` must pass; engine untouched → no smoke-test extension |
| V. One logical change | ✅ Arrangement footer levels + the master-volume single-owner fix it depends on (FR-007) |
| VI. Docs in sync | ⏳ CHANGELOG `[Unreleased]` entries (feat + fix), `nota-features`, `nota-docs` (arrangement page: return/master rows) |
| VII. Spec-first | ✅ This run |
| VIII. Rules not weakened | ✅ |

Post-design re-check: unchanged — all gates pass.

## Design

### 1. `ArrangementView` — footer rows ([contract](contracts/ui-footer-row.md))

- `BuildSlimRow` gains a meter, a fader and a live read-out:
  - Top line: name (`*`) + dB `TextBlock` (Mono 9, 30 px, right-aligned) — the same
    `ShowDb` formatting as `BuildTrackCard` (factor it into one static helper both use).
  - Bottom line: `MiniFader { Default = 1.0 }`.
  - Right: `MeterBar { Width = 8, VerticalAlignment = Stretch, Margin = (0,6,3,6) }`,
    docked like the track header meter.
- Return rows: fader `ValueChanged → _engine.SetTrackVolume(id, v)`; initial value
  from the return's `TrackVM`/`TrackInfo.Volume`.
- Master row: fader reads `GetMasterVolume()` and writes `SetMasterVolume(v)`.
- New fields, cleared at the start of `RebuildFooterHeaders`:
  `_footerMeters : Dictionary<int, MeterBar>`, `_footerFaders : Dictionary<int,
  MiniFader>`, `_footerDb : Dictionary<int, TextBlock>`, `_masterRowMeter`,
  `_masterRowFader`, `_masterRowDb`.
- `UpdateMeters()`:
  - push `TryGetTrackMeter(id)` into each `_footerMeters` entry (Reset on failure);
  - push `MasterMeter()` into `_masterRowMeter`;
  - in the existing per-track loop, also sync `_footerFaders` / `_footerDb` from
    `ti.Volume` (skip while dragging) — this makes return automation and Mixer/MCP
    changes show;
  - sync `_masterRowFader` / `_masterRowDb` from `GetMasterVolume()` (skip while
    dragging).
- New public callbacks `GetMasterVolume` / `SetMasterVolume`.
- Unchanged: selection on card press (the fader handles its own press), context menu,
  drop target, `FooterRowH`.

### 2. `MixerView` — master volume single owner (FR-007)

- Add the same `GetMasterVolume` / `SetMasterVolume` callbacks; remove
  `_masterVolume`. The master strip's fader starts from `GetMasterVolume()` on
  `Refresh()`, writes through `SetMasterVolume`, and in `UpdateMeters()` follows
  `GetMasterVolume()` when not dragging (MST / Master row / Remote changes).

### 3. `MainWindow` wiring

- `Timeline.GetMasterVolume = () => (float)vm.Transport.MasterVolume;`
  `Timeline.SetMasterVolume = v => vm.Transport.MasterVolume = v;` next to the other
  `Timeline` hooks; same for `_mixer` at construction.

### 4. Docs

- `CHANGELOG.md` `[Unreleased]`: Added — level meter and volume fader on Return and
  Master rows in the arrangement; Fixed — Return rows always showed −∞ and Master 0.0;
  master volume set in the Mixer was not saved and not shown in the transport.
- `nota-features`, `nota-docs` skills.

## Project Structure

### Documentation (this feature)

```text
specs/005-return-master-meters/
├── spec.md
├── plan.md              # this file
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── ui-footer-row.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/managed/Nota.App/
├── ArrangementView.cs     # BuildSlimRow, RebuildFooterHeaders, UpdateMeters, callbacks, ShowDb helper
├── MixerView.cs           # master volume through callbacks
└── MainWindow.axaml.cs    # wire Timeline + _mixer to vm.Transport.MasterVolume
CHANGELOG.md
FEATURES.md                # via nota-features
```

**Structure Decision**: Managed UI only, inside `Nota.App`; no new projects or files.

## Out of Scope / Known Gaps

- Pan on Return/Master rows (clarified: option B).
- Recording automation from footer faders; return automation lanes in the footer.
- Live automated master volume on any master fader (needs an engine read-out).
- MIDI-learn of Master Volume writes the engine directly, bypassing
  `Transport.MasterVolume` (`MidiLearnService`) — same class of bug as the Mixer one;
  candidate for a follow-up.

## Complexity Tracking

No constitution violations.
