<!-- SPDX-License-Identifier: AGPL-3.0-only -->
<!-- Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms. -->

# CLAUDE.md

Nota is a real-time audio application: a C++20 engine behind a pure C ABI, driven by a
.NET 10 / Avalonia app. Read [`CONTRIBUTING.md`](CONTRIBUTING.md) before changing
anything — the rules there are binding (AGPL headers, dependency policy, audio-thread
restrictions).

- The project constitution for agents lives at `.specify/memory/constitution.md`.
- Non-trivial work is spec-driven: `/speckit-specify` → `/speckit-clarify` →
  `/speckit-plan` → `/speckit-tasks` → `/speckit-implement` → `/speckit-converge`.
  Artifacts land in `specs/<slug>/`; see [`CONTRIBUTING.md`](CONTRIBUTING.md) §
  Spec-driven workflow. `specs/000-example/` shows a completed run.
- Bug reports go through `/speckit-bug-assess` → `/speckit-bug-fix` → `/speckit-bug-test`.
- The `nota-*` skills in `.claude/skills/` (design, docs, features, instrument,
  shortcuts, release) encode project-specific workflows — prefer them over improvising.
- Before any pull request: `scripts/build.sh` (or the Linux/Windows siblings) must pass;
  it runs the smoke test that drives the engine end-to-end.
