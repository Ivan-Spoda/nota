# Contract: MCP export tools (`export_master` / `export_stems`)

**Feature**: specs/001-mp3-flac-export | **Layer**: Nota.Mcp | **Status**: planned

Both existing tools gain one parameter and one conditional parameter. Defaults
keep every existing call working unchanged (FR-011 parity with the dialog).

```
export_master(path, totalBeats,
              bitDepth = "float32",     # unchanged: pcm16 | pcm24 | float32
              format = "wav",           # NEW: wav | flac | mp3
              bitrateKbps = 320)        # NEW: 128 | 192 | 256 | 320 (format=mp3 only)

export_stems(folder, totalBeats,
             bitDepth = "float32",
             format = "wav",            # NEW
             bitrateKbps = 320)         # NEW
```

## Semantics

- `format = "wav"` → behaviour byte-identical to today.
- `format = "flac"` → FLAC file at `path`/`folder`; `bitDepth` interpreted as
  `pcm16 | pcm24` (values `float32`/`float` fall back to `pcm24` — mirrors the
  existing `ParseDepth` fallback style and the format's option model).
- `format = "mp3"` → MP3 at `bitrateKbps`; `bitDepth` ignored.
- Sample rate derives from the engine device rate as today; an engine rate
  above 48 000 (unusual) with `format = "mp3"` is clamped to 48 000 and noted
  in the tool's return message, keeping FR-003 (no invalid file) without
  silently exporting a wrong-rate file — the tool result says what happened.
- Return strings keep the current shape (`"Exported <path>"` /
  `"<n> stems → <folder>"`), with the format appended when not WAV
  (e.g. `"Exported demo.flac (flac)"`).
- Validation failures (unknown format token, unknown bitrate) return the
  existing error-string convention used by the tool family rather than
  throwing.

## Registration

No wiring changes: attribute discovery (`WithToolsFromAssembly`) and the
`IAudioExporter` singleton shared via `McpAppServices` already carry the new
capability once the exporter implements it. Tool descriptions updated to name
the new parameters (they are part of the model-visible schema).
