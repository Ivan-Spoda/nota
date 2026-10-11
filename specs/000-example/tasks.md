# Tasks: Document the stress-test script in BUILD.md

**Input**: Design documents from `/specs/001-document-stress-test/`

**Prerequisites**: plan.md, spec.md, research.md, quickstart.md

**Tests**: Not requested — validation is manual, via quickstart.md.

**Organization**: Single user story (documentation-only feature).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to
- Include exact file paths in descriptions

## Phase 1: Setup

**Purpose**: Confirm the facts the subsection will state

- [x] T001 Verify the script's behaviour claims against `scripts/stress-test.py`: ramp bounds (1→40, `MAX_TRACKS = 80`), device chain (Volt + EQ-8 + Compressor + Auto Filter), settle time, `RESULTS_CSV = scripts/stress-test-results.csv`, MCP URL `127.0.0.1:3900`

---

## Phase 2: User Story 1 - A contributor measures DSP headroom (Priority: P1) 🎯 MVP

**Goal**: BUILD.md gains a "Stress test" subsection a reader can follow without opening the script.

**Independent Test**: Follow quickstart.md — the documented steps run the script and produce CSV rows.

### Implementation for User Story 1

- [x] T002 [US1] Add a `### Stress test` subsection to `BUILD.md` after the smoke-test/testing material: what the script measures, the MCP prerequisite (`dotnet run --project src/managed/Nota.App`, MCP on 127.0.0.1:3900), the run command (`python3 scripts/stress-test.py`), and the output location (`scripts/stress-test-results.csv`) — keep it within ~30 lines per SC-002
- [x] T003 [US1] Cross-read the new subsection against `scripts/stress-test.py` (FR-005: no invented flags or behaviour)

> Cross-read findings (T001/T003): the code ramps 1..80 (`MAX_TRACKS = 80`) while the
> script's own docstring still says "1 to 40" — the documentation states the actual
> behaviour. MCP enablement is Preferences → Appearance → AI control.

**Checkpoint**: BUILD.md alone is enough to discover and run the stress test.

---

## Phase 3: Polish & Cross-Cutting Concerns

**Purpose**: Nota PR gates

### Nota closing tasks

- [x] T004 Run `scripts/build.sh` until native + managed + smoke test are green — SMOKE TEST PASSED (exit 0), 2026-10-11
- [x] T005 AGPL-3.0-only SPDX two-line header on every new source file — N/A, no new source files (BUILD.md already carries it)
- [x] T006 New dependency, if any: license checked + row added to `LICENSES/third-party.md` — N/A
- [x] T007 User-visible change → `CHANGELOG.md` `[Unreleased]` entry — N/A, contributor docs only
- [x] T008 User-visible capability change → FEATURES.md / user docs — N/A
- [x] T009 Engine behaviour change → extend `tests/Nota.SmokeTest` — N/A

---

## Dependencies & Execution Order

- T001 → T002 → T003 → T004 (T005–T009 are verification checkboxes, no file work)

## Implementation Strategy

Single-story feature: T001 (facts) → T002 (write) → T003 (verify) → T004 (gate). MVP equals the whole feature.

## Notes

- No parallel tasks: one file is modified.
