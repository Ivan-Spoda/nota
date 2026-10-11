# Contract: Export dialog option matrix (`ExportWindow`)

**Feature**: specs/001-mp3-flac-export | **Layer**: Nota.App (code-built Avalonia, Ember) | **Status**: planned

The dialog keeps its current structure (Range / Format+options / Toggles /
Stems / footer estimate). Only the option block and estimate become
format-driven. FR-002/FR-003 expressed as the matrix the UI must guarantee:

## Option matrix (the contract)

| Control | WAV (default) | FLAC | MP3 |
|---|---|---|---|
| Format combo | `WAV · FLAC · MP3`, default WAV, same for all three columns | | |
| Sample rate | 44 100 / 48 000 / 96 000 | 44 100 / 48 000 / 96 000 | 44 100 / 48 000; **96 000 disabled (greyed, unselectable)** |
| Depth combo | 16-bit / 24-bit / 32-bit float (default 24) | 16-bit / 24-bit (default 24); no float32 row | hidden; replaced by Quality combo |
| Quality combo | hidden | hidden | 320 / 256 / 192 / 128 kbps (default 320) |
| Dither pill | enabled ⇔ depth = 16-bit (today's rule) | enabled ⇔ depth = 16-bit | disabled + greyed ("lossy format" hint) |
| Normalize pill | unchanged | unchanged | unchanged |
| Release tail / Range / Stems | unchanged for all formats | | |

Switching format re-applies the matrix instantly (same `Sync()` refresh path
the dither pill uses today). No combination outside this matrix is reachable,
and the footer estimate always reflects the currently reachable selection.

## Footer estimate formulas

`sec = (rangeBeats + tail) * 60 / bpm`; `sr` and `bytesPerSample` as today.

| Format | Estimate |
|---|---|
| WAV | `sec * sr * 2 * bytesPerSample` (exact, unchanged) |
| FLAC | `0.65 * sec * sr * 2 * (2 or 3)` — shown with the existing `≈` qualifier |
| MP3 | `bitrateKbps * 125 * sec` bytes (exact for CBR) |

Stems wording unchanged ("one file per track · … MB each").

## Export action

- Master: save picker per format — filter `"FLAC audio" *.flac` / `"MP3 audio"
  *.mp3` / existing WAV row; `DefaultExtension` = `flac|mp3|wav`;
  `SuggestedFileName` = `"Nota Export"` + the extension.
- Stems: folder picker unchanged; produced files use the format's extension,
  same `NN <Kind> <track>.<ext>` naming.
- Button label: `Export FLAC` / `Export MP3` / `Export WAV` / `Export stems`.
- `ExportOptions` returned from the dialog carries `Format` + `BitrateKbps` +
  the existing fields, already validated by the matrix.

## Visual language

No new control types: the adaptive block is built from the existing `Combo`,
`WorkingToggle(enabled:)`, `Section`, `Segmented` helpers, following
`DESIGN.md` (Ember) — `nota-design` skill consulted at implementation time.
