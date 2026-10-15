# Implementation Plan: UI Polish Pass

**Branch**: `012-ui-polish` | **Date**: 2026-10-14 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/012-ui-polish/spec.md`

## Summary

This is a polish pass driven by a screenshot critique ([critique.md](critique.md)) and
code research ([research.md](research.md)). There are six workstreams, each mapped to a
user story. All are UI-only, and none changes a feature, shortcut, menu or saved format:

1. **Defects (US1)**:
   - **New track at the arrangement's bottom edge.** It isn't revealed and has no
     boundary against the pinned returns. Add a seam, scroll the track into view, and
     paint the lane side below the tracks in the lane colour (R1).
   - **Session toolbar.** It has no overflow; it gets priority collapse and a "⋯" menu.
     The inspector becomes collapsible and narrower (R2).
   - **Modular sidebar.** It is never built when nothing is selected. Always refresh it,
     auto-select a track, and add empty states (R3).
2. **Floating windows (US2)**: the mixer sizes to its strips; Escape closes the mixer and
   export; window titles are in sentence case. They stay windows: the critique's "no
   edge" was the test display's missing window manager (R4).
3. **Labels (US3)**:
   - fix the all-caps and Title Case sources (palette search headers, Drum Rack, mixer,
     session inspector, Preferences, native menu, command titles, window titles);
   - route label builders through `NotaText.Label`;
   - add the `RunLabels` check, starting as a shrinking ratchet (R5).
4. **Device cards (US4)**:
   - one header order in the shared `BuildCardShell` (name · preset · badges · power,
     with a reserved power slot);
   - move Instrument Rack, `SimpleCard` and Sends onto it (R6);
   - add one `DeviceCardKit.Section`, which draws rule-divided sections instead of framed
     boxes, and sweep about 90 helpers and inline boxes onto it (R7).
5. **Icons (US5)**:
   - extend `Glyph` into the single icon set (about 35 new kinds, including Arrangement,
     Session and Modular);
   - replace about 45 inline paths;
   - make the view switcher three icon segments with tooltips;
   - add the `RunIcons` check (R8).
6. **Chrome (US6)**:
   - drop the "Nota" prefix in browser rows and quieten the type tags;
   - make the add-device target slim, dashed only while dragging;
   - add minus signs on the mixer scale, the range-dash rule, and stronger number and
     radius checks (R9);
   - leave the arrangement header rows untouched (clarified).

## Nota: Read Before Planning

- **UI work** → [`DESIGN.md`](../../DESIGN.md) (Studio) and the `nota-design` skill.
- **Anything** → [`CONTRIBUTING.md`](../../CONTRIBUTING.md): SPDX headers on new files,
  no new dependency.

## Technical Context

**Language/Version**: C# / .NET 10 (managed app only).

**Primary Dependencies**: Avalonia 12.0.3 (existing). No new dependency.

**Storage**: `settings.json` gains one bool, `SessionInspectorOpen` (default true). No
project-format change.

**Audio-Thread Impact**: None. UI-only; nothing crosses the C ABI.

**Testing**:
- `tests/Nota.SmokeTest`:
  - new `RunLabels` and `RunIcons` checks;
  - `RunNumbers` gains hyphen-minus literals;
  - `RunGeometry` gains `Rectangle.RadiusX`;
  - `PaletteTests` expectations are updated for sentence-case headers.
- Screenshots under Xvfb in all four themes:
  - every built-in card, before and after, with an edge-overlay diff;
  - the critique tour re-run.

**Target Platform**: macOS 13+, Windows 10+, Linux, as today. Window chrome is verified
on macOS (Xvfb has no window manager).

**Project Type**: Desktop app (Avalonia).

**Performance Goals**: no regression. Section and header changes remove elements rather
than add them.

**Constraints**:
- Card frames (700 × 260 and the documented exceptions), section positions and content,
  and arrangement row heights stay unchanged.
- Arrangement header rows are untouched (FR-012).
- Every theme stays within the readability bar.

**Scale/Scope**:
- 51 built-in cards plus the plugin card;
- about 90 section helpers and inline boxes;
- about 45 inline icons;
- about 60 label literals;
- about 20 files for defects and chrome.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Note |
|---|---|---|
| I. Real-time audio safety | ✅ N/A | UI-only. |
| II. Boundary discipline | ✅ | No C ABI change. No JUCE. |
| III. Licensing hygiene | ✅ | New files get the SPDX header. No dependency. Icons are drawn in-house (no icon font, per DESIGN.md). |
| IV. Verification gate | ✅ | The smoke test is extended (labels, icons, numbers, geometry). Build scripts must pass. |
| V. One logical change per PR | ⚠️ | Six workstreams in one feature. **Mitigation**: one commit per workstream, each buildable and screenshot-verified, so the PR can be split along those lines if the maintainer asks. Recorded in Complexity Tracking. |
| VI. Docs in sync | ✅ | DESIGN.md (floating windows, header anatomy, sections, icon rule, range rule), the nota-design skill, CHANGELOG. FEATURES.md only where a described label changes (e.g. view switcher). nota-docs is noted as a follow-up (that repo is unavailable here). |
| VII. Spec-first | ✅ | Spec plus 4 clarifications and plan findings; tasks next. |
| VIII. Safety rules not weakened | ✅ | Design checks only get stricter. Allow-lists are data-only exemptions with reasons. |

**Post-design re-check**: unchanged. The R4 correction removes the only item that would
have changed behaviour (turning windows into overlays).

## Project Structure

### Documentation (this feature)

```text
specs/012-ui-polish/
├── plan.md              # this file
├── critique.md          # the input: findings F1–F12
├── research.md          # R1–R10
├── data-model.md        # icon set, card-header slots, label rules, settings
├── quickstart.md        # verification runs
├── contracts/
│   └── ui-contract.md   # P1–P6 visual and behavioural contract
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/managed/
├── Nota.Application/
│   ├── Palette/PaletteSearch.cs          # sentence-case group/suggested/recent headers
│   ├── Palette/CommandCatalog.cs         # command titles → sentence case
│   └── Settings.cs                       # + SessionInspectorOpen
└── Nota.App/
    ├── ArrangementView.cs                # footer seam, reveal new/selected track, lane backdrop (R1)
    ├── SessionView.Toolbar.cs / SessionView.cs / SessionView.Inspector.cs   # overflow, inspector collapse, labels (R2, R5)
    ├── ModularView.cs, MainWindow.axaml.cs                                  # sidebar refresh, auto-select, empty states (R3)
    ├── MixerWindow.cs, MixerView.cs, ExportWindow.cs, NotaWindow.cs         # size-to-content, Escape, titles, minus signs (R4, R9)
    ├── CommandPaletteView.cs             # header via NotaText.Label; icons → Glyph
    ├── DeviceChainView.Shell.cs          # header order + reserved power slot (R6)
    ├── DeviceCardKit.cs                  # + Section(body, edges) (R7)
    ├── DeviceCards/**                    # SectionBox/TabFrame/IslandBox → Section; inline boxes swept; Rack onto the shell
    ├── Controls/Glyph.cs                 # + ~35 kinds incl. Arrangement/Session/Modular (R8)
    ├── MainWindow.axaml                  # view switcher → icon segments; transport toggle icons → Glyph; menu labels
    ├── BrowserView.cs, PreferencesWindow*.cs, HistoryView.cs, PreviewPlayer.cs, NotaIcons.cs   # icons → Glyph; labels
    ├── DeviceChainView.cs, RackCardView.cs   # slim add-device target (R9)
    └── Theme/NotaText.cs                 # Fixed list readable by the check (internal)

tests/Nota.SmokeTest/
├── DesignTokenCheck.cs                   # RunLabels, RunIcons; RunNumbers/RunGeometry extended
├── PaletteTests.cs                       # header expectations
└── Program.cs                            # "-- design: labels --", "-- design: icons --"

DESIGN.md, DESIGN.html (regenerate), .claude/skills/nota-design/SKILL.md, CHANGELOG.md
```

**Structure Decision**: every change stays in the files that own the behaviour today. The
only new shared pieces are `DeviceCardKit.Section` and the extended `Glyph` set. No new
project or layer.

## Order of work

Each step is buildable, screenshot-verified and one commit:

1. **Labels (US3)**: lowest risk, and it unblocks the `RunLabels` ratchet that guards
   everything after it.
2. **Defects (US1)**: F1 → F3 → F2.
3. **Floating windows (US2).**
4. **Icons (US5)**: extend `Glyph`, then replace by file, then the view switcher, then
   `RunIcons`.
5. **Card headers (US4a)**: shell order, then Rack, `SimpleCard` and Sends onto the shell.
6. **Card sections (US4b)**:
   - `DeviceCardKit.Section`, then helpers delegated, then inline boxes swept;
   - batch by folder (instruments, effects, MIDI, racks), with a before/after edge diff
     per card;
   - largest diff, so it comes last.
7. **Chrome (US6)**: browser prefix and tags, add-device target, numbers.
8. **Docs, tour re-run, converge.**

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Six workstreams in one PR (Constitution V) | The critique is one coherent polish pass, and the user asked for it as one spec. The work shares one verification harness (the tour and the card diffs). | Six PRs would each need the same screenshot baseline and review context. Instead, one commit per workstream keeps it splittable on request. |
