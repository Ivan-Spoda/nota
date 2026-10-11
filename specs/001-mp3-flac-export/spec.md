# Feature Specification: Export to MP3 and FLAC

**Feature Branch**: `feat/mp3-flac-export`

**Created**: 2026-10-11

**Status**: Draft

**Input**: User description: "Экспорт в MP3/FLAC" (Export to MP3/FLAC)

## Clarifications

### Session 2026-10-11

- Q: Where does FLAC/MP3 encoding live, and does an LGPL encoder (LAME) pass the dependency policy? → A: Vendored native: libFLAC (BSD) + LAME (LGPL, registered as an accounted dependency in `LICENSES/third-party.md`), encoding inside the engine behind the pure C ABI.
- Q: What quality-selection model does the MP3 export dialog use? → A: A CBR bitrate list (128 / 192 / 256 / 320 kbps), defaulting to 320 kbps.
- Q: How is the MP3 + 96 kHz combination handled? → A: Prevented: 96 kHz is not selectable while MP3 is chosen — no silent resampling.

## Nota Context *(fill in — drives plan and tasks)*

**Feature area** *(pick the main one)*:

- [x] Engine (C++, `src/native/nota.engine`) — real-time audio rules apply, see `ARCHITECTURE.md` AR table
- [x] Managed app (C#, `src/managed/*`) — domain, application, infrastructure, presentation
- [x] UI (Avalonia, `src/managed/Nota.Presentation`) — follow `DESIGN.md` (Ember)
- [ ] Plugin hosting (`pluginhost/`) — the only place JUCE is allowed
- [ ] Build / packaging / CI (`scripts/`, `.github/`) — see `BUILD.md`
- [ ] Docs only

Today export is WAV-only: the export dialog hardcodes a single format, and the
managed export pipeline (request → offline render → streaming file writer) has
exactly one writer. This feature adds two more output formats at that same seam,
with encoding implemented natively: libFLAC (BSD) and LAME (LGPL) vendored under
`vendor/`, exposed through the pure C ABI (AR-7) and registered in
`LICENSES/third-party.md`.

**Nota docs to keep in mind** *(links relative to `specs/<slug>/`, where this file lands)*:
[`ARCHITECTURE.md`](../../ARCHITECTURE.md) (AR rules),
[`CONTRIBUTING.md`](../../CONTRIBUTING.md) (licensing, dependency policy, PR
expectations), [`BUILD.md`](../../BUILD.md), [`DESIGN.md`](../../DESIGN.md),
[`FEATURES.md`](../../FEATURES.md).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Archive a finished track as FLAC (Priority: P1)

A producer finishes a song and wants a lossless archive that sounds exactly like
the WAV but takes roughly half the disk space. They hit the existing export
command, pick FLAC in the format list, choose bit depth, and get a file that
decodes back to the same audio.

**Why this priority**: FLAC is the natural next format after WAV — same
lossless guarantee, same bit-depth options, smaller files. It exercises the
"format is a choice" part of the flow with no lossy-specific complications.

**Independent Test**: Export a song as FLAC and re-import it into Nota (Nota
already decodes FLAC on import); the imported audio matches the equivalent WAV
export at the same bit depth.

**Acceptance Scenarios**:

1. **Given** a song with audio playing back, **When** the user opens the export
   dialog, **Then** the format list offers WAV, FLAC and MP3, with WAV still the
   default.
2. **Given** FLAC selected, **When** the user exports the master, **Then** the
   written file decodes to the same samples as a WAV export of the same render at
   the chosen bit depth.
3. **Given** FLAC selected, **When** bit depth is chosen, **Then** 16- and 24-bit
   are offered (no 32-bit float) and the file lands in the picker with a `.flac`
   extension.

---

### User Story 2 - Share a demo as MP3 (Priority: P2)

A songwriter wants to send a rough mix to a bandmate over a messenger or drop it
on a phone. They export as MP3 at a chosen quality and get a small file that
plays everywhere.

**Why this priority**: MP3 is the share-everywhere format and the main reason
this feature was asked for; it comes second only because its option model
(bitrate instead of bit depth) differs from the existing dialog.

**Independent Test**: Export a song as MP3 and play it back — in Nota's own
import and in a common OS player; audible quality at the default setting is
clean and the file is a small fraction of the WAV size.

**Acceptance Scenarios**:

1. **Given** MP3 selected, **When** the user picks quality, **Then** a bitrate
   choice is offered instead of bit depth, defaulting to the highest option.
2. **Given** MP3 selected, **When** the user exports the master, **Then** the
   file plays in common players and its size roughly matches the bitrate chosen.
3. **Given** MP3 selected, **When** the sample-rate list is shown, **Then** rates
   the format cannot represent (96 kHz) are not selectable, so no invalid file
   can be produced.

---

### User Story 3 - Stems in the chosen format (Priority: P3)

A mixing engineer wants lossless FLAC stems (or small MP3 reference stems) rather
than 24-bit WAV stems. They tick the stems block with FLAC or MP3 selected and
get one file per track in that format.

**Why this priority**: Stems ride on the master-export machinery; once the
pipeline writes a format, stems are the same render loop per track. Valuable but
not the entry point.

**Independent Test**: Export stems with FLAC (or MP3) selected; every produced
file is that format and each stem isolates its track exactly as WAV stems do
today.

**Acceptance Scenarios**:

1. **Given** FLAC or MP3 selected and stems enabled, **When** the user exports,
   **Then** each stem file uses the selected format and the existing naming
   scheme with the format's extension.
2. **Given** stems export with normalization on, **When** it completes, **Then**
   the shared make-up gain behaviour is unchanged (stems sum to the target).

---

### User Story 4 - Scripted export in the new formats (Priority: P4)

An automation user (MCP client, scripts, the stress-test harness) exports masters
or stems programmatically and wants the new formats without driving the UI.

**Why this priority**: The MCP export tools already mirror dialog options;
adding the format parameter keeps programmatic and interactive export in parity.

**Independent Test**: Call the MCP export tool with the new format value; the
written file is a valid FLAC/MP3 of the requested render.

**Acceptance Scenarios**:

1. **Given** an MCP client, **When** it calls master/stem export with a format
   of `flac` or `mp3` (default unchanged), **Then** the output file uses that
   format and honours the same option rules as the dialog.

### Edge Cases

- What happens when a combination the format cannot represent is selected —
  MP3 at 96 kHz, 32-bit float FLAC, dither with MP3, bit depth with MP3? The
  dialog must prevent the selection (grey out / adapt the option list) rather
  than silently substituting something else or writing an invalid file.
- MP3 is not sample-accurate: encoder delay and padding shift the audio by a few
  milliseconds and decoding is lossy. The exported MP3's duration may differ
  slightly from the render; this is inherent to the format and accepted (FLAC
  must be sample-accurate).
- Normalize (−1 dBTP) with MP3: the true peak is measured on the rendered audio
  before encoding; the encoded file's true peak may deviate slightly from the
  target. Small deviation is accepted and must not trip the user as an error.
- Exporting a range that renders to near-silence (or a very short custom range):
  the encoders must still produce valid, playable files.
- An encode that fails midway (disk full, write error): no corrupt file may be
  left presented as a success — the failure surfaces like today's export errors
  and temporary files are cleaned up.
- Overwriting an existing file keeps the picker's existing confirmation
  behaviour; the suggested filename carries the format's extension.
- The footer size/time estimate must not mislead: compressed formats show
  format-aware estimates, not WAV math.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The export dialog MUST offer WAV, FLAC and MP3 as the output
  format, with WAV remaining the default.
- **FR-002**: Option lists MUST adapt to the format: WAV keeps bit depth
  16/24/float32; FLAC offers bit depth 16/24 only; MP3 offers a CBR bitrate
  choice of 128 / 192 / 256 / 320 kbps (default 320) in place of bit depth.
- **FR-003**: The dialog MUST prevent combinations the selected format cannot
  represent (e.g. 96 kHz MP3, 32-bit float FLAC, dither with MP3), by disabling
  or hiding the inapplicable options.
- **FR-004**: The dither toggle MUST remain available only where it applies
  (16-bit targets), unchanged from today's rule.
- **FR-005**: "Normalize −1 dBTP" MUST work for FLAC and MP3 as it does for WAV:
  two-pass measure on the rendered audio, make-up gain applied before writing;
  stems keep the shared-gain behaviour.
- **FR-006**: Stems export MUST produce one file per non-return track in the
  selected format, keeping the existing naming scheme with the format's
  extension.
- **FR-007**: The save picker MUST filter by and default to the selected
  format's extension, and the suggested filename MUST carry it.
- **FR-008**: The footer estimate MUST reflect the selected format (compressed
  sizes for FLAC/MP3) and stay honest at default settings.
- **FR-009**: Export MUST remain a background, offline operation with overall
  progress covering render and encode, the app staying responsive, and the
  engine stop/restore sequence unchanged.
- **FR-010**: Written files MUST be valid for their format and playable in
  common players: FLAC decodes losslessly at the chosen bit depth; MP3 decodes
  with duration within normal codec padding tolerance. Nota's own import (which
  already decodes both) MUST accept the files.
- **FR-011**: The MCP export tools MUST accept a format argument (`wav` default,
  `flac`, `mp3`) applying the same option rules as the dialog.
- **FR-012**: Any new codec dependency MUST satisfy the project's dependency
  policy (AGPL-compatible, registered in the third-party license list) before it
  ships. Resolved route: vendored native libFLAC (BSD) and LAME (LGPL — accepted
  as an accounted copyleft dependency with a `LICENSES/third-party.md` row),
  encoding behind the pure C ABI.

### Key Entities *(include if feature involves data)*

- **Export format**: a choice of WAV, FLAC or MP3; carries the option model
  (bit depth vs quality) and the file extension.
- **Export request**: the existing export description (range, tail, stems,
  normalize, dither) extended with the format and its format-specific settings.
- **Encoded export file**: master or stem written in the chosen format, subject
  to the format's fidelity guarantees (FLAC lossless, MP3 lossy).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can export a FLAC or MP3 master through the same dialog and
  the same number of steps as a WAV export today — no extra dialogs or
  preparatory settings.
- **SC-002**: A FLAC export re-imported into Nota matches the equivalent WAV
  export's audio at the same bit depth; an MP3 export imports and plays in Nota
  and at least one common OS player.
- **SC-003**: For the same render, the FLAC file is noticeably smaller than the
  24-bit WAV, and the MP3 at default quality is under ~15% of the WAV size.
- **SC-004**: No invalid combination is selectable in the dialog: attempting
  every format × sample-rate × depth × dither combination via the UI yields only
  producible configurations.
- **SC-005**: A typical song export in each format completes with visible
  progress and a responsive UI, at speeds comparable to today's WAV export plus
  a reasonable encode overhead.

## Assumptions

- Defaults: FLAC uses 24-bit (matching the WAV default); MP3 quality is the CBR
  bitrate list's top option, 320 kbps; VBR presets are out of scope for this
  iteration.
- 96 kHz with MP3 is prevented at the dialog level (option unavailable), not
  solved by silent resampling.
- Range (loop/full), release tail, exclusivity and engine restore behaviour are
  unchanged; only the output format dimension is added.
- The existing WAV path keeps its behaviour byte-for-byte — this feature adds
  formats, it does not rework export.
- Encoding is implemented natively (vendored libFLAC + LAME, pure C ABI); the
  exact ABI shape and engine integration are plan-level details.
- MP3's inherent codec delay/padding (a few ms) and lossy true-peak drift under
  normalization are accepted; they are documented, not compensated.
- Small peripheral WAV exports (clip drag-out, Lens scope capture, Strata) stay
  WAV — only the master/stems export flow gains formats.

## Nota Compliance Checklist *(review before implementation; re-check in tasks.md)*

- [ ] Every new source file will carry the AGPL-3.0-only SPDX header (two lines, see CONTRIBUTING.md) — new encoder/wrapper files expected
- [ ] No new dependency — or: license checked, AGPL-compatible, `LICENSES/third-party.md` row planned — a codec dependency is the core of this feature; the license gate is FR-012
- [ ] Audio-thread impact considered: no alloc / lock / I/O on the audio thread (AR-6), command queue for data in (AR-4), immutable snapshots for structural edits (AR-5) — export renders offline on a background thread with the audio backend stopped; encoding happens post-render, no audio-thread code
- [ ] Managed/native crossings go through the pure C ABI in `nota_engine.h` only (AR-7); no JUCE outside `pluginhost/` — applies if the encoder is vendored native
- [ ] Smoke-test impact assessed: engine behaviour change → extend `tests/Nota.SmokeTest` — add export tests: FLAC lossless round-trip, MP3 validity/size, invalid-combination guards
- [ ] User-visible change → `CHANGELOG.md` `[Unreleased]` entry planned (and FEATURES.md / user docs via the nota-features / nota-docs skills when working with an agent that has them) — new export formats, new dialog options, MCP format argument
- [ ] UI change → follows `DESIGN.md` (Ember design language) — format combo + adaptive option rows in the export dialog
