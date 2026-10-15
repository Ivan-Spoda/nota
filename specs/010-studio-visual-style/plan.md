# Implementation Plan: Studio Visual Style

**Branch**: `010-studio-visual-style` | **Date**: 2026-10-13 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/010-studio-visual-style/spec.md`

## Summary

Replace Nota's Ember design language (warm graphite, brass) with the Studio design system
(neutral greys stepped on dark seams, one orange accent for live-or-chosen state, 2/3/4 px
corners, sentence-case labels, flat docked panels, shadows only on floating layers),
without moving or removing any feature.

Nota's theme is already token-driven: every colour a view paints comes from a
`Brush.*` key in `Theme/NotaTheme.axaml` (XAML) or its mirror slot in
`Theme/NotaPalette.cs` (custom-drawn views), and the smoke test's `DesignTokenCheck`
keeps both in sync and bans literals. So the restyle is done in four layers, cheapest
and widest first:

1. **Token remap** — re-point every existing slot (both variants) to its Studio role
   value. Slot *names* stay, so the ~200 files that reference them repaint with no edit.
   Add the few roles Studio has and Ember lacked (seam, rule, faint text, solo blue,
   viz ink). Geometry keys are remapped to Studio's 2/3/4 radius scale the same way.
2. **Shared styles and controls** — `NotaTheme.axaml` control styles (buttons, toggles,
   segments, fields, tabs, menus, tooltips, scrollbars, dialogs) and the shared
   custom-drawn controls (`Knob`, faders, switches, meters, `NotaGraph`, `DeviceCardKit`
   builders) adopt Studio's state rules: toggled-on is a solid accent fill with derived
   ink, resting values neutral, accent only while focused/dragged, bevel + seam ring on
   raised controls, popover/modal shadows, sentence-case labels through the label
   builders.
3. **Shell** — `MainWindow.axaml` (and its partials) trade the floating islands and
   12 px gutter for docked panels on 1 px seams, Studio panel toolbars and a flat
   transport row; region order and contents unchanged.
4. **Custom views and docs** — arrangement / piano roll / audio editor / mixer /
   session / modular follow-ups that tokens alone cannot express (clip title bands,
   mute-amber / solo-blue / arm-red, mixer top colour bar); then DESIGN.md,
   DESIGN.html (`scripts/design-html.py`), the `nota-design` skill, `DesignTokenCheck`
   rules, CHANGELOG and FEATURES.

## Nota: Read Before Planning

- **UI work** → [`DESIGN.md`](../../DESIGN.md) (currently Ember; rewritten by this
  feature) and the `nota-design` skill (rewritten by this feature).
- **Design rules** → the Studio direction in [spec.md](spec.md) (§ Studio, in short)
  and the token map in [data-model.md](data-model.md) (dark + light).
- **Anything** → [`CONTRIBUTING.md`](../../CONTRIBUTING.md).

## Technical Context

**Language/Version**: C# / .NET 10 (managed app) and Avalonia XAML. No C++ is touched.

**Primary Dependencies**: Avalonia 12.0.3 + Fluent theme (existing). Geist / Geist Mono
fonts already bundled in `assets/fonts`. **No new dependency**, and no third-party code
or assets are added.

**Storage**: N/A. No stored format changes; track colour indices keep their meaning
(see [data-model.md](data-model.md)).

**Audio-Thread Impact**: None: UI-only change. No engine code, no C ABI change, no
command-queue traffic added.

**Testing**: `tests/Nota.SmokeTest` (run by `scripts/build-linux.sh`), including
`DesignTokenCheck` (updated to the new rules). Visual verification by running the app
under Xvfb and capturing screenshots of each main surface in both appearances
([quickstart.md](quickstart.md)).

**Target Platform**: Desktop — macOS, Windows, Linux (same as today).

**Project Type**: Desktop application (UI layer only).

**Performance Goals**: No regression in repaint cost; appearance switch < 1 s (SC-006),
already met by the slot re-tint mechanism, which this plan keeps.

**Constraints**: Hit-test geometry unchanged (FR-019): arrangement row heights, header
column 228, ruler/overview/section strips, piano-roll keys/rows, device card
700 × 260 frame, control hit areas. No literal colours in views (existing bans stay).

**Scale/Scope**: ~120 C# UI files and `NotaTheme.axaml` (1,213 lines, 146 styles);
~45 custom-drawn visualisers; 14 device-card files; 82 controls.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Note |
|---|---|---|
| I. Real-time audio safety | ✅ Pass | No engine or audio-thread code touched. |
| II. Boundary discipline | ✅ Pass | No C ABI change; JUCE untouched. |
| III. Licensing hygiene | ✅ Pass | No new dependency, no copied code. New files (if any) get the two-line SPDX header. |
| IV. Verification gate | ✅ Pass (planned) | `scripts/build-linux.sh` + smoke test must pass; `DesignTokenCheck` is updated, not weakened: every Ember ban that still applies stays, new Studio rules are added. |
| V. One logical change per PR | ⚠️ Justified | One logical change — "adopt the Studio design language" — but a large diff. It is delivered as one PR because a half-converted app (Studio shell, Ember devices) would be shipped in an inconsistent state; commits are layered (tokens → styles/controls → shell → views → docs) so review can go commit by commit. See Complexity Tracking. |
| VI. Documentation in sync | ✅ Planned | CHANGELOG `[Unreleased]`, DESIGN.md, DESIGN.html, `nota-design` skill, FEATURES.md (design language line). The user manual lives in `../nota-docs`, which is not available in this environment — flagged as a follow-up. |
| VII. Spec-first | ✅ Pass | spec → clarify → plan (this) → tasks → implement → converge. |
| VIII. Safety rules not weakened | ✅ Pass | Nothing in I–III is affected. |

**Post-design re-check**: unchanged — the design (token remap + style/control updates +
shell re-layout) stays entirely in `src/managed/Nota.App`, `tests/Nota.SmokeTest`,
and docs.

## Project Structure

### Documentation (this feature)

```text
specs/010-studio-visual-style/
├── plan.md              # This file
├── research.md          # Phase 0: decisions on mapping, shell, labels, knobs, tests
├── data-model.md        # Phase 1: the token map (Ember slot → Studio role, dark + light)
├── quickstart.md        # Phase 1: build + visual verification guide
├── contracts/
│   └── ui-contract.md   # Phase 1: the visual/behavioural contract the views must meet
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
src/managed/Nota.App/
├── Theme/
│   ├── NotaTheme.axaml        # tokens (Dark + Light), geometry, type classes, control styles
│   ├── NotaPalette.cs         # slot mirror for custom-drawn views; track palette
│   ├── NotaGeometry.cs        # radius / size / spacing mirror
│   └── NotaFonts.cs           # type scale for custom drawing
├── MainWindow.axaml (+ MainWindow.*.cs)   # shell: islands → docked panels
├── DeviceCardKit.cs           # card header/sections, KnobCell / SliderRow label builders
├── Controls/                  # Knob, faders, switches, meters, NotaGraph, Glyph, …
├── ArrangementView*.cs, ClipEditorView.cs, AudioClipEditorView.cs,
│   MainWindow.Mixer.cs, Session/Modular views   # custom-drawn follow-ups
└── *Window.cs                 # dialogs: modal treatment

tests/Nota.SmokeTest/DesignTokenCheck.cs   # rules updated to the Studio system
scripts/design-html.py                     # regenerates DESIGN.html
DESIGN.md, DESIGN.html, CHANGELOG.md, FEATURES.md
.claude/skills/nota-design/SKILL.md
```

**Structure Decision**: UI-only; everything lives in `src/managed/Nota.App` (where the
Avalonia views actually are), the smoke test's design check, and docs. No other
project is touched.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Large single PR (Principle V) | The request is one logical change — the app's design language — and must not ship half-applied. | Splitting into several PRs (tokens first, devices later) would merge an app whose shell and devices follow two different design systems; the spec's SC-001/SC-004 cannot pass until all layers land. Commits stay layered for review. |
