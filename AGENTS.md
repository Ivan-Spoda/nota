<!-- SPDX-License-Identifier: AGPL-3.0-only -->
<!-- Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms. -->

# AGENTS.md

Nota is a real-time audio application: a C++20 engine behind a pure C ABI, driven by a
.NET 10 / Avalonia app. Read [`CONTRIBUTING.md`](CONTRIBUTING.md) before changing
anything — the rules there are binding (AGPL headers, dependency policy, audio-thread
restrictions).

- The project constitution for agents lives at `.specify/memory/constitution.md`.
- Non-trivial work is spec-driven: the speckit commands (`specify`, `clarify`, `plan`,
  `tasks`, `implement`, `converge` — see your integration's command list) produce
  artifacts in `specs/<slug>/`; see [`CONTRIBUTING.md`](CONTRIBUTING.md) §
  Spec-driven workflow. `specs/000-example/` shows a completed run.
- Bug reports go through the speckit `bug-assess` → `bug-fix` → `bug-test` commands.
- The `nota-*` skills in `.claude/skills/` and `.agents/skills/` (design, docs,
  features, instrument, shortcuts, release) encode project-specific workflows — prefer
  them over improvising.
- Before any pull request: `scripts/build.sh` (or the Linux/Windows siblings) must pass;
  it runs the smoke test that drives the engine end-to-end.
