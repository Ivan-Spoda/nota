# Implementation Plan: Document the stress-test script in BUILD.md

**Branch**: `001-document-stress-test` | **Date**: 2026-10-11 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-document-stress-test/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Add a "Stress test" subsection to BUILD.md documenting `scripts/stress-test.py`: what it
measures (DSP load and process CPU while ramping tracks with an instrument and an effect
chain), its prerequisite (Nota running with MCP enabled on 127.0.0.1:3900), how to run
it, and where results land (`scripts/stress-test-results.csv`). Documentation-only; the
script itself is untouched.

## Nota: Read Before Planning

Consult the matching source of truth before designing:

- **Engine work** → [`ARCHITECTURE.md`](../../ARCHITECTURE.md): the AR rules table is binding (AR-4 lock-free queue, AR-5 immutable snapshots, AR-6 audio-thread restrictions, AR-7 pure C ABI, AR-9 `Transport` is the only source of musical time).
- **UI work** → [`DESIGN.md`](../../DESIGN.md) (Ember design language) and the `nota-design` skill.
- **A built-in instrument / audio or MIDI effect** → the `nota-instrument` skill (DSP class, kind registration across C++ and C#, device-card UI).
- **Build / packaging** → [`BUILD.md`](../../BUILD.md).
- **Anything** → [`CONTRIBUTING.md`](../../CONTRIBUTING.md): licensing, dependency policy, PR expectations.

For this feature only the last two apply; the change is a subsection inside BUILD.md
itself, matching its existing tone and structure (see research.md).

## Technical Context

**Language/Version**: Markdown documentation; references a Python 3 standard-library script.

**Primary Dependencies**: N/A — no new dependencies.

**Storage**: N/A.

**Audio-Thread Impact** *(required — state explicitly)*: None — documentation-only change; the script under documentation itself only drives the app through its public MCP interface.

**Testing**: Manual — the subsection is validated by following it (quickstart.md).

**Target Platform**: All (BUILD.md is platform-agnostic documentation).

**Project Type**: Developer documentation in a desktop-app repository.

**Performance Goals**: N/A.

**Constraints**: ~30 lines maximum (SC-002); content must match the script's actual behaviour (FR-005).

**Scale/Scope**: One subsection in BUILD.md.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Note |
|---|---|---|
| I. Real-Time Audio Safety | Pass | No code touched. |
| II. Boundary Discipline | Pass | No code touched. |
| III. Licensing Hygiene | Pass | BUILD.md already carries the AGPL header; no new files. |
| IV. Verification Gate | Pass | Smoke test unaffected; doc-only change still requires `scripts/build.sh` green before PR. |
| V. One Logical Change per PR | Pass | Single documentation subsection. |
| VI. Documentation Stays in Sync | Pass | This change *is* documentation sync. |
| VII. Spec-First | Pass | This spec/plan/tasks run. |
| VIII. Safety Rules Never Weakened | Pass | Not applicable — nothing weakened. |

No violations. Complexity Tracking not needed.

## Project Structure

### Documentation (this feature)

```text
specs/001-document-stress-test/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

*data-model.md and contracts/ are skipped: the feature has no entities and exposes no
interfaces — it is a documentation edit.*

### Source Code (repository root)

```text
BUILD.md                     # ← the only file modified (new "Stress test" subsection)
scripts/stress-test.py       # documented, NOT modified
scripts/stress-test-results.csv  # mentioned as the output location
```

**Structure Decision**: Single-file edit in the repository's build manual, placed in the
tooling area of BUILD.md (exact placement per research.md).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

None — no violations.
