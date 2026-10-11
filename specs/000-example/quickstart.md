# Quickstart: Document the stress-test script in BUILD.md

How to validate this feature end-to-end after implementation.

## Prerequisites

- A working Nota build (see BUILD.md § prerequisites for your platform).
- `python3` available (standard library only — no pip installs).

## Steps

1. Open `BUILD.md` and find the new **Stress test** subsection.

2. Follow it verbatim:

   ```bash
   dotnet run --project src/managed/Nota.App   # app must run with MCP on 127.0.0.1:3900
   python3 scripts/stress-test.py
   ```

3. **Expected outcome**: the script prints progress as it ramps tracks, and appends
   rows to `scripts/stress-test-results.csv` (DSP load + process CPU per step).

## Validation checks

- The subsection is discoverable from BUILD.md's structure (acceptance scenario 1).
- A reader who never opened the script can run it using only the subsection (SC-001).
- Everything the subsection claims matches `scripts/stress-test.py` (FR-005):
  track ramp bounds, device chain, MCP prerequisite, CSV path.
- `scripts/build.sh` still passes (doc-only change, but the gate applies regardless).
