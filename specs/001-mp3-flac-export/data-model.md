# Data Model: Export to MP3 and FLAC

**Feature**: specs/001-mp3-flac-export | **Date**: 2026-10-11

No persisted data is introduced — export settings are not saved between runs
(unchanged from today; the dialog re-opens with defaults). The model below is
the in-memory vocabulary shared by the layers.

---

## Managed entities

### ExportFormat *(new, Nota.Application)*

Output container/codec choice.

| Value | Meaning | Option model | Extension |
|---|---|---|---|
| `Wav` (default) | PCM / IEEE float WAV, existing behaviour | `WavBitDepth` 16/24/Float32 | `.wav` |
| `Flac` | lossless FLAC, integer PCM only | `WavBitDepth` Pcm16/Pcm24 | `.flac` |
| `Mp3` | lossy MP3, CBR | `Mp3BitrateKbps` 128/192/256/320 | `.mp3` |

Validation rules (mirrors spec FR-002/FR-003):
- `Mp3` × `Float32` — invalid (no bit-depth option offered).
- `Mp3` × 96 000 Hz — invalid (rate not selectable).
- `Dither` — only valid with a 16-bit target (`Wav`+Pcm16 or `Flac`+Pcm16);
  ignored elsewhere, exactly as today.
- `Mp3BitrateKbps` — only meaningful for `Mp3`; default 320.

### ExportRequest *(extended, Nota.Application — was WAV-only)*

```
Path, TotalBeats, Bpm, SampleRate,
RestoreLoop, RestoreMetronome,
Normalize = false, Dither = false,
Format = ExportFormat.Wav,        // NEW
Mp3BitrateKbps = 320              // NEW (Format == Mp3 only)
```

`Depth` stays the `WavBitDepth` used by `Wav` and `Flac` (`Float32` reachable
only via `Wav`). Existing callers compile unchanged (new parameters default).

### ExportOptions *(extended dialog result, Nota.App)*

Gains `ExportFormat Format` and `int BitrateKbps`; `Depth` keeps its meaning.
Built by the dialog under the validation rules above; consumed by
`MainWindow.Files.DoExportAsync`.

### IAudioExporter / AudioExporter *(port unchanged, implementation renamed)*

`WavAudioExporter` → `AudioExporter` (same DI singleton, same two methods).
Internally selects the sink per request:

```
render loop ──► WavWriter        (Format == Wav)     [unchanged]
            └─► NativeEncoder    (Format == Flac|Mp3) [new]
normalize: RenderToTemp (float32 WAV) ──► gain in managed float buffer ──► same sinks
```

### NativeEncoder *(new, Nota.Infrastructure/Audio)*

Lifecycle wrapper over the C ABI encoder handle; shape copied from `WavWriter`:
constructed open (throws on failure), `WriteFrames(float[] interleaved, int
frames)`, `Dispose` = flush+finish+free, `Discard()` = free without finishing
(failure path; caller deletes the partial file). One instance per output file;
single-threaded use by design (called from the export `Task.Run`).

---

## Native entities

### NotaEncoder *(opaque handle, engine)*

Created by `nota_encoder_open_flac`/`_mp3`, destroyed by `nota_encoder_close`
(success) or `nota_encoder_discard` (failure). Full contract:
`contracts/c-abi-encoder.md`.

State transitions:

```
open ──► Open ──write*──► Open ──close──► Finished(handle freed)
              │                            
              └──write error──► Error ──discard──► (freed)
open failure ──► returns null (no handle)
```

- `write` after error: `NotaError` result, no-op.
- `close` on an errored encoder: returns the error, frees the handle.
- `discard`: always frees; idempotent-safe with null.

### Encoder configuration (native side)

| Format | Source of settings |
|---|---|
| FLAC | sample rate, bits (16/24), dither flag, compression level 5 (library default), 2 channels |
| MP3 | sample rate, CBR bitrate kbps, 2 channels, joint-stereo mode left at LAME default |

Float→integer conversion (FLAC, and LAME if fed ints) applies TPDF dither only
when the `dither` flag is set AND target is 16-bit — same rule as `WavWriter`.

---

## MCP entities

`export_master` / `export_stems` gain `format` (`"wav"` default | `"flac"` |
`"mp3"`) and `bitrateKbps` (int, default 320, `format == "mp3"` only).
`bitDepth` keeps applying to `wav`/`flac`. Contract:
`contracts/mcp-export-tools.md`.
