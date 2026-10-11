<!-- SPDX-License-Identifier: AGPL-3.0-only -->
<!-- Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms. -->

# Contributing to Nota

Thanks for wanting to help. This document covers the things that are specific to this
project — the licensing agreement, the architecture rules you must not break, and how to
get a change reviewed.

Start with [`ARCHITECTURE.md`](ARCHITECTURE.md). Nota is a real-time audio application,
and a change that looks harmless in most codebases (allocating in a callback, taking a
lock, logging from the wrong thread) will produce audible dropouts here.

## Licensing your contribution

Nota is licensed under `AGPL-3.0-only` (see [`LICENSES/`](LICENSES/)).

> By submitting a contribution, you agree that your work is licensed under
> **AGPL-3.0-only**.

If you are not comfortable with that, please open an issue to discuss instead of sending
code.

**If you are employed as a programmer**, check your employment agreement before
contributing. Many contracts assign copyright in your code to your employer, in which
case they — not you — must grant the license above.

Every new source file needs the standard two-line header:

```c
// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
```

## Adding a dependency

Don't add one without checking it first. Every dependency must be AGPL-compatible.

1. Confirm the license qualifies.
2. Add a row to [`LICENSES/third-party.md`](LICENSES/third-party.md).
3. Say so in the pull request.

A copyleft dependency that is not already accounted for will be rejected — the JUCE
situation (which is why this project is AGPL rather than GPL) is the one exception, and
it took a deliberate architectural decision to contain it.

## Rules you must not break

These are described in full in [`ARCHITECTURE.md`](ARCHITECTURE.md#architecture-rules-ar-).
The short version:

- **The audio thread never allocates, locks, or does I/O** (AR-6). No `new`, no `malloc`,
  no `std::string`, no mutex, no file access, no logging. If you need to get data to the
  audio thread, use the lock-free command queue (AR-4).
- **Structural edits publish a new immutable graph snapshot** (AR-5). Do not mutate a
  live graph in place.
- **Only a pure C ABI crosses the managed/native boundary** (AR-7). No C++ types, no
  exceptions. Extend `nota_engine.h` rather than working around it.
- **No JUCE outside `pluginhost/`.** The core engine's JUCE-freedom is both an
  architectural and a licensing constraint.
- **`Transport` is the only source of musical time** (AR-9).

## Spec-driven workflow

For anything non-trivial — a feature, a behaviour change, a hard bug — don't start with
code. Start with a spec. This repository runs [Spec Kit](https://github.com/github/spec-kit),
and the AI agents it supports (Claude Code, Copilot, Cursor, Gemini, Codex) already have
the commands installed. The workflow:

1. `/speckit-specify` — write `spec.md`: what and why, user stories, requirements.
2. `/speckit-clarify` *(optional)* — tighten underspecified areas.
3. `/speckit-plan` — write `plan.md`: technical approach, audio-thread impact.
4. `/speckit-tasks` — break it into dependency-ordered `tasks.md`.
5. `/speckit-implement` — execute the tasks.
6. `/speckit-converge` — verify nothing was missed; repeat until it reports
   **Converged**.

Artifacts live in `specs/<slug>/` and are committed alongside the code. A completed
example is kept at [`specs/000-example/`](specs/000-example/) — read it before your
first run. Bug reports can take the analogous `/speckit-bug-assess` → `/speckit-bug-fix`
→ `/speckit-bug-test` path; the report directory lands in `.specify/bugs/<slug>/`.

Two things govern agents working here: the project constitution at
`.specify/memory/constitution.md` (principles distilled from this document and
[`ARCHITECTURE.md`](ARCHITECTURE.md)), and this document — which remains canonical for
humans. You don't need an AI agent to contribute: writing `spec.md` and `plan.md` by
hand from the templates in `.specify/templates/` is equally welcome. Typos, small fixes
and doc edits don't need a spec — just open the pull request.

## Building

See [`README.md`](README.md) for prerequisites and per-platform build commands, and
[`BUILD.md`](BUILD.md) for details. In short:

```bash
scripts/build.sh          # macOS   — native + managed + smoke test
pwsh scripts/build-win.ps1  # Windows (Developer PowerShell for VS 2022)
scripts/build-linux.sh    # Linux
dotnet run --project src/managed/Nota.App
```

The build scripts run the smoke test (`tests/Nota.SmokeTest`), which drives the engine
end-to-end through the C ABI. **It must pass before you open a pull request.**

## Pull requests

- One logical change per pull request.
- Match the style of the code around you. Comment headers explain *why* a file exists,
  not what each line does — keep that convention.
- If you change engine behaviour, extend the smoke test to cover it.
- If you change user-visible behaviour, add a `CHANGELOG.md` entry.
- Say which platforms you actually built and tested on.

## Reporting bugs

Include your OS and version, the Nota version (Help -> About), your audio device and
buffer size, and — for audio problems — whether it reproduces with plugins disabled.
Crash and dropout reports are much more useful with a project that reproduces them.
