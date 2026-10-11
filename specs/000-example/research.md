# Research: Document the stress-test script in BUILD.md

## What the script actually does (read from source)

- **Decision**: Document it as "ramp from 1 to 40 tracks (max 80), each with the Volt
  instrument + EQ-8 + Compressor + Auto Filter, recording DSP load and process CPU at
  each step while the transport plays".
- **Rationale**: Taken directly from `scripts/stress-test.py` module docstring and
  constants (`MAX_TRACKS = 80`, device chain, `SETTLE_SECONDS = 3.0`); FR-005 forbids
  inventing behaviour.
- **Alternatives considered**: Describing only "measures DSP load" (too vague to act
  on); restating the full algorithm (violates SC-002's ~30-line budget).

## Prerequisite and invocation

- **Decision**: State that the app must be running with MCP enabled on
  `127.0.0.1:3900`, launched via `dotnet run --project src/managed/Nota.App`, then
  `python3 scripts/stress-test.py`.
- **Rationale**: The script's docstring names the requirement; the MCP server is off by
  default, so a reader hitting "connection refused" needs the prerequisite stated up
  front (spec edge case).
- **Alternatives considered**: None — this is the only supported path.

## Output

- **Decision**: Mention `scripts/stress-test-results.csv` (constant `RESULTS_CSV`) and
  that measurements accumulate there per run.
- **Rationale**: FR-004; the CSV is the artifact a reader ultimately wants.
- **Alternatives considered**: None.

## Placement and tone inside BUILD.md

- **Decision**: Add a `### Stress test` subsection at the end of the section that
  covers build/test tooling (after the smoke-test material), in BUILD.md's existing
  terse imperative style with a fenced code block for the commands.
- **Rationale**: BUILD.md already walks prerequisites → build → test → packaging; the
  stress test is developer tooling that logically follows testing. Matching the local
  style satisfies CONTRIBUTING's "match the style of the code around you".
- **Alternatives considered**: A separate PERFORMANCE.md (overkill for ~30 lines);
  documenting in CONTRIBUTING.md (wrong audience — contributors' gate, not tooling
  manual).

## NEEDS CLARIFICATION resolution

None were raised in Technical Context; all facts were verified against the script
source on 2026-10-11.
