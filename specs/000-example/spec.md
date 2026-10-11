# Feature Specification: Document the stress-test script in BUILD.md

**Feature Branch**: `001-document-stress-test`

**Created**: 2026-10-11

**Status**: Draft

**Input**: User description: "Document scripts/stress-test.py in BUILD.md — the stress-test script exists in scripts/ but is not mentioned anywhere in BUILD.md; contributors don't know it exists or how to run it."

## Nota Context *(fill in — drives plan and tasks)*

**Feature area** *(pick the main one)*:

- [ ] Engine (C++, `src/native/nota.engine`) — real-time audio rules apply, see `ARCHITECTURE.md` AR table
- [ ] Managed app (C#, `src/managed/*`) — domain, application, infrastructure, presentation
- [ ] UI (Avalonia, `src/managed/Nota.Presentation`) — follow `DESIGN.md` (Ember)
- [ ] Plugin hosting (`pluginhost/`) — the only place JUCE is allowed
- [ ] Build / packaging / CI (`scripts/`, `.github/`) — see `BUILD.md`
- [x] Docs only

**Nota docs to keep in mind** *(links relative to `specs/<slug>/`, where this file lands)*:
[`ARCHITECTURE.md`](../../ARCHITECTURE.md) (AR rules),
[`CONTRIBUTING.md`](../../CONTRIBUTING.md) (licensing, dependency policy, PR
expectations), [`BUILD.md`](../../BUILD.md), [`DESIGN.md`](../../DESIGN.md),
[`FEATURES.md`](../../FEATURES.md).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A contributor measures DSP headroom (Priority: P1)

A contributor (or the maintainer) wants to check how much DSP load the engine takes
as a project grows, for example before and after an engine change. They open
`BUILD.md`, find a short "Stress test" subsection, and follow it: launch the app with
MCP enabled, run the script, and read the resulting CSV.

**Why this priority**: This is the only story — the entire feature is one documentation
subsection that makes an existing, invisible tool discoverable.

**Independent Test**: BUILD.md contains a subsection that names the script, states its
prerequisites (app running with MCP on 127.0.0.1:3900), the command to run it, and where
the results land. A reader who has never seen the script can run it without reading its
source.

**Acceptance Scenarios**:

1. **Given** BUILD.md, **When** a contributor scans the table of sections, **Then** they
   find a stress-test subsection alongside the other tooling documentation.
2. **Given** a Nota build on this machine, **When** the contributor follows the
   documented steps (launch app, run the script), **Then** the script runs and appends
   measurements to `scripts/stress-test-results.csv`.
3. **Given** the subsection, **When** reading it, **Then** it is clear what the script
   does (ramps tracks with instrument + effects while measuring DSP load and process
   CPU) and what its prerequisites are.

### Edge Cases

- The app is not running, or MCP is disabled: the docs must state the prerequisite up
  front so the failure mode is obvious.
- Python version: the script uses only the standard library; the docs should not
  impose requirements beyond a working `python3`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: BUILD.md MUST contain a subsection documenting `scripts/stress-test.py`.
- **FR-002**: The subsection MUST state the prerequisite: Nota running with MCP enabled
  on 127.0.0.1:3900, including how to launch it
  (`dotnet run --project src/managed/Nota.App`).
- **FR-003**: The subsection MUST state what the script does: ramp up tracks, each with
  an instrument and a chain of effects, recording DSP load and process CPU per step.
- **FR-004**: The subsection MUST state where results are written
  (`scripts/stress-test-results.csv`) and how to run the script
  (`python3 scripts/stress-test.py`).
- **FR-005**: The documentation MUST match the script's actual behaviour (no invented
  flags or options).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A contributor who has never opened the script can run it successfully
  following only BUILD.md.
- **SC-002**: The new subsection adds no more than ~30 lines to BUILD.md — it documents
  an existing tool, it does not restate its implementation.

## Assumptions

- The script itself is not modified; this is documentation-only work.
- `scripts/stress-test-results.csv` being committed is pre-existing repository state and
  out of scope here.

## Nota Compliance Checklist *(review before implementation; re-check in tasks.md)*

- [x] Every new source file will carry the AGPL-3.0-only SPDX header (two lines, see CONTRIBUTING.md) — no new source files; BUILD.md already carries it
- [x] No new dependency — or: license checked, AGPL-compatible, `LICENSES/third-party.md` row planned — none
- [x] Audio-thread impact considered: no alloc / lock / I/O on the audio thread (AR-6), command queue for data in (AR-4), immutable snapshots for structural edits (AR-5) — docs only
- [x] Managed/native crossings go through the pure C ABI in `nota_engine.h` only (AR-7); no JUCE outside `pluginhost/` — not touched
- [x] Smoke-test impact assessed: engine behaviour change → extend `tests/Nota.SmokeTest` — no engine change
- [x] User-visible change → `CHANGELOG.md` `[Unreleased]` entry planned — contributor docs only, not user-visible
- [x] UI change → follows `DESIGN.md` (Ember design language) — no UI change
