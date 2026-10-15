# Implementation Plan: Selectable Colour Themes

**Branch**: `011-colour-themes` | **Date**: 2026-10-13 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/011-colour-themes/spec.md`

## Summary

Turn Nota's two fixed palette variants into a list of named, single-look colour themes:
Studio Dark, Studio Light, Ember Graphite and Ember Paper. The user picks one in
Preferences → Appearance, or uses **follow system**, which pairs a dark-mode theme with
a light-mode theme. Only colour changes. Layout, type and behaviour stay Studio's.

The core move is to give colour **one home**: one JSON file per theme. Today every
colour exists twice, as a `T("#dark","#light")` slot in `NotaPalette.cs` and as a
`Brush.*` entry in both XAML `ThemeDictionaries`. Afterwards:

1. **Theme files** (`Theme/Themes/*.json`, embedded) hold one value per token for one
   theme (research R2, [contracts/theme-file.md](contracts/theme-file.md)).
2. **`NotaPalette`** declares slots by token name. `Apply(theme)` re-tints the
   long-lived slot brushes in place, as variant switches do today.
3. **XAML** stops holding colours. At startup the slot brushes themselves are published
   into `Application.Resources` under their `Brush.*` keys, so every
   `{DynamicResource Brush.*}` follows a switch with no XAML change (R1, R6). Shadows
   and the Fluent accent ramp are set as resources from theme tokens on each switch.
4. **`NotaThemeService`** applies a theme by id, sets `RequestedThemeVariant` from the
   theme's kind, and in follow-system mode swaps themes on the OS's
   `ActualThemeVariantChanged` (R4).
5. **Settings** gain `ColourTheme`, `SystemDarkTheme` and `SystemLightTheme`. One pure
   resolver in `Nota.Application` handles migration and fallbacks, and the smoke test
   covers it (R5).
6. **Preferences** gets a theme list with previews and a follow-system row with two
   per-mode pickers ([contracts/ui-contract.md](contracts/ui-contract.md) § T1).
7. **Checks**: `DesignTokenCheck` validates every theme file against the token catalogue
   and the readability pairings (T3), keeps the Studio rules on the Studio themes, and
   tests the appearance resolver.
8. **Ember** is rebuilt from release 0.52.1 by slot name. The 8 Studio-only tokens are
   derived by role, and values failing T3 are nudged and documented (R8).

## Nota: Read Before Planning

- **UI work** → [`DESIGN.md`](../../DESIGN.md) (Studio) and the `nota-design` skill. This
  feature adds a "Colour themes" section there.
- **Anything** → [`CONTRIBUTING.md`](../../CONTRIBUTING.md): SPDX headers on new files,
  no new dependency (`System.Text.Json` is in the base library).

## Technical Context

**Language/Version**: C# / .NET 10 (managed app only). Python 3 for the design-page
generator script.

**Primary Dependencies**: Avalonia 12.0.3 (existing), `System.Text.Json` (BCL). No new
dependency.

**Storage**:
- `settings.json` (per machine): three new string fields; the legacy `Theme` field is
  kept and written in step.
- Theme files are embedded resources.
- Project files: no change.

**Audio-Thread Impact**: None. This is a UI-only change; nothing crosses the C ABI, and
the engine is untouched.

**Testing**:
- `tests/Nota.SmokeTest`: `DesignTokenCheck` gains `RunThemes`, and `Program.cs` gains
  the "design: themes" and "design: appearance" sections. Both read files as text and
  call the pure resolver; no UI needed.
- Screenshot comparison per [quickstart.md](quickstart.md).

**Target Platform**: macOS 13+, Windows 10+, Linux (x64/arm64), as today.

**Project Type**: Desktop app (Avalonia).

**Performance Goals**: a theme switch repaints every window within 0.5 s (SC-001).
Follow system reacts within 1 s (SC-008).

**Constraints**:
- Studio Dark and Studio Light render pixel-identical to 010 (SC-003). Their values are
  copied, not re-derived.
- No frame is drawn in the wrong theme at launch (SC-007).
- Slot brush identity is preserved across switches; existing aliases and caches keep
  working.

**Scale/Scope**:
- 106 tokens × 4 themes;
- ~10 files changed in `Nota.App/Theme`, `PreferencesWindow.cs`, `App.axaml.cs`,
  `Nota.Application/Settings.cs` (+ new `Appearance.cs`);
- 4 new JSON files;
- `DesignTokenCheck`, `Program.cs`, `scripts/design-html.py`, docs.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Note |
|---|---|---|
| I. Real-time audio safety | ✅ N/A | UI-only. No engine or audio-thread code touched. |
| II. Boundary discipline | ✅ | No C ABI change. No JUCE. |
| III. Licensing hygiene | ✅ | New `.cs` files carry the SPDX header. JSON files have none (JSON has no comments); they are data covered by the repo licence, like `settings.json`'s schema. No new dependency. |
| IV. Verification gate | ✅ | The smoke test is extended (themes + appearance). The build scripts must pass. |
| V. One logical change per PR | ✅ | A single feature PR, stacked on 010 (PR #1). It doesn't merge before 006. |
| VI. Docs in sync | ✅ | CHANGELOG `[Unreleased]` Added entry, FEATURES.md (nota-features), DESIGN.md § Colour themes + Ember nudges, DESIGN.html regenerated, nota-design skill, nota-docs (Preferences page; the docs repo is unavailable in this environment, as for 010 T069). |
| VII. Spec-first | ✅ | spec + clarify (3 Q) done; this plan; tasks next. |
| VIII. Safety rules not weakened | ✅ | No rule touched. |

**Post-design re-check**: unchanged. The design adds no engine surface. The design-check
rules are **not** loosened for Studio: the Studio rule (no Ember value) keeps applying to
both Studio themes. Ember's exemption from that rule is required by the spec (FR-016).

## Project Structure

### Documentation (this feature)

```text
specs/011-colour-themes/
├── plan.md              # this file
├── research.md          # R1–R10
├── data-model.md        # theme, token catalogue, appearance setting, resolution
├── quickstart.md        # gate, manual flows, screenshots, migration
├── contracts/
│   ├── theme-file.md    # theme JSON format v1 (F1–F8)
│   └── ui-contract.md   # Preferences UI (T1), switching (T2), readability (T3), Ember (T4)
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
src/managed/
├── Nota.Application/
│   ├── Settings.cs                 # + ColourTheme, SystemDarkTheme, SystemLightTheme; legacy Theme kept
│   └── Appearance.cs               # NEW — pure resolver: migration, fallbacks, built-in ids
└── Nota.App/
    ├── App.axaml.cs                # apply the resolved appearance before the first window
    ├── Nota.App.csproj             # EmbeddedResource Theme/Themes/*.json
    ├── PreferencesWindow.cs        # theme list + follow-system row + per-mode pickers (T1)
    └── Theme/
        ├── Themes/
        │   ├── studio-dark.json    # NEW — today's dark values, verbatim
        │   ├── studio-light.json   # NEW — today's light values, verbatim
        │   ├── ember-graphite.json # NEW — 0.52.1 dark + R8 derivations, T3 nudges
        │   └── ember-paper.json    # NEW — 0.52.1 light + R8 derivations, T3 nudges
        ├── NotaColourTheme.cs      # NEW — theme record, JSON loader + validation, built-in registry
        ├── NotaPalette.cs          # slots by token name; Apply(theme); ColorIn(slot, theme); full KeyMap; washes
        ├── NotaThemeService.cs     # apply by id/mode; publish brushes, shadows, accent ramp; follow system
        └── NotaTheme.axaml         # colour entries removed from ThemeDictionaries; header explains where colour lives

tests/Nota.SmokeTest/
├── DesignTokenCheck.cs             # RunThemes (F1–F8, T3); Run reworked: keys → KeyMap; Studio checks read studio-*.json
└── Program.cs                      # "-- design: themes --", "-- design: appearance --"

scripts/design-html.py              # palette from theme JSON; one section per built-in theme
DESIGN.md, DESIGN.html              # § Colour themes, token roles, Ember nudge table
.claude/skills/nota-design/SKILL.md # where colour lives now; how to add a theme
CHANGELOG.md, FEATURES.md           # Added: colour themes + follow system
```

**Structure Decision**: all code stays where theming already lives (`Nota.App/Theme`,
`PreferencesWindow.cs`). The only Application-layer addition is the pure resolver, which
lives next to `Settings`. That keeps it free of Avalonia and directly testable from the
smoke test, which does not reference `Nota.App`.

## Approach notes for tasks

- **Extract, don't retype**: a one-off script (scratch, not committed) generates
  `studio-dark.json` and `studio-light.json` from the current `T()` pairs and XAML-only
  values, and `ember-*.json` from `git show f37f51e:…`. The smoke test then proves the
  Studio files match the 010 values token for token, before the old pairs are deleted.
- **Order**:
  1. theme files + loader;
  2. `NotaPalette` by name, with Studio applied, still on today's ThemeDictionaries;
  3. publish brushes and remove XAML colours;
  4. resolver + settings;
  5. service;
  6. Preferences UI;
  7. Ember tuning;
  8. checks/docs.

  Studio must look identical after each of steps 1–5 (screenshot diff).
- **Risk: styles that read a `Color` resource rather than a brush** (e.g.
  `SystemAccentColor*` in Fluent templates). These are set as `Color` resources on every
  switch. A grep for `{DynamicResource` keys that resolve to non-brush types is part of
  step 3.
- **Risk: `StaticResource Brush.*`**: none today (verified: 0 uses). The check keeps it
  that way.
- **Risk: popups already open during a switch**: they are separate windows and are
  covered by `RepaintEverything` walking `desktop.Windows`. Popup roots are added to the
  walk if the screenshot pass shows a stale one.

## Complexity Tracking

No constitution violations to justify.
