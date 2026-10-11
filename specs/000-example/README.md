# Completed example spec — kept as documentation

This folder is a finished, real run of Nota's spec-driven workflow, kept so contributors
can see what the artifacts look like before writing their own. Read in this order:

1. [`spec.md`](spec.md) — what and why (user stories, requirements, success criteria)
2. [`plan.md`](plan.md) — technical approach, audio-thread impact, constitution check
3. [`research.md`](research.md) — the decisions behind the plan
4. [`tasks.md`](tasks.md) — the executable checklist, all boxes checked
5. [`quickstart.md`](quickstart.md) — how the result was validated end-to-end

The feature itself was deliberately small — documenting `scripts/stress-test.py` in
`BUILD.md` — so the artifacts stay readable. A larger feature produces the same
artifacts with more content, not a different process.

Start your own with `/speckit-specify <description>` (see
[CONTRIBUTING.md](../../CONTRIBUTING.md) § Spec-driven workflow). Your spec gets the
next number: `specs/001-…`, `specs/002-…`, and so on.
