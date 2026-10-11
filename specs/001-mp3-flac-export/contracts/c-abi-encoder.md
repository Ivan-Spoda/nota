# Contract: C ABI streaming encoder (`nota_engine.h`)

**Feature**: specs/001-mp3-flac-export | **Boundary**: AR-7 (pure C ABI) | **Status**: planned

New section in `include/nota/nota_engine.h`, implemented in a new
`src/nota_encoder.cpp` (flat engine layout), wrapping vendored libFLAC and
LAME. All functions are `extern "C"`, marked `NOTA_API`, namespaced
`nota_encoder_*` (matches the `nota_*` glob in `nota_exports.txt` /
`nota_exports.map`, so macOS/Linux export lists need no edit).

```c
/* ---- Audio file encoders (export) -------------------------------------- */

/* Opaque encoder instance. One per output file; single-threaded use only. */
typedef struct NotaEncoder NotaEncoder;

/* Opens a FLAC file for streaming stereo encode. bitsPerSample = 16 or 24.
 * dither != 0 applies TPDF dither on the float->int conversion (16-bit only,
 * mirrors the WAV writer). Compression level is the library default (5).
 * Returns null on failure (bad path, unsupported bits, I/O error). */
NOTA_API NotaEncoder* nota_encoder_open_flac(const char* path, int sampleRate,
                                             int bitsPerSample, int dither);

/* Opens an MP3 file for streaming stereo encode at constant bitrate.
 * bitrateKbps is one of 128 / 192 / 256 / 320. Sample rate must be one the
 * format can carry (44100 or 48000; MPEG-1). Returns null on failure. */
NOTA_API NotaEncoder* nota_encoder_open_mp3(const char* path, int sampleRate,
                                            int bitrateKbps);

/* Encodes `frames` of interleaved stereo float samples (outBuffer length >=
 * 2*frames, values nominally in [-1, 1]). Chunk sizes are the caller's choice;
 * the encoder buffers internally. Returns NotaResult_Ok or NotaResult_Error
 * after which the encoder is errored: further writes no-op, close reports the
 * error and frees the handle. */
NOTA_API NotaResult nota_encoder_write(NotaEncoder* enc, const float* samples,
                                       int frames);

/* Flushes and finishes the stream (FLAC seektable/MD5 finalization, LAME
 * encoder-delay/padding flush + Xing frame), closes the file and frees the
 * handle. Returns the final status; enc must not be used afterwards. */
NOTA_API NotaResult nota_encoder_close(NotaEncoder* enc);

/* Frees an errored or abandoned encoder without finishing the stream. The
 * partial output file is NOT deleted (the caller owns the path and removes
 * it). Null is accepted as a no-op. */
NOTA_API void nota_encoder_discard(NotaEncoder* enc);
```

## Semantics

- **Ownership**: caller opens, writes, and must `close` (success) or
  `discard` (failure/abandon) exactly once. `close`/`discard` free the handle;
  double-close is a caller bug (documented, not defended).
- **Threading**: called from the managed export background thread while the
  audio backend is stopped (same discipline as `nota_engine_render_offline_at`).
  No audio-thread contact (AR-6 untouched — encoding never runs on the audio
  thread). Handles are independent; no global state beyond library-internal
  tables (libFLAC/LAME are both instance-based).
- **Memory**: bufferss are copied/consumed synchronously inside `write`; no
  allocations happen that outlive the call except the encoder's internal FIFO.
- **Errors**: open failure → null (managed wrapper throws IOException-style);
  write failure → `NotaResult_Error`, sticky until close; the managed caller
  `Discard()`s and deletes the partial file, surfacing the failure through the
  existing export error path.
- **Fidelity**: FLAC is lossless at the chosen bit depth (sample-accurate
  length). MP3 carries LAME's encoder delay/padding (~50 ms) — duration
  differs from the render by that tolerance; gapless metadata (LAME tag) is
  written so capable players compensate.
- **Reuse of result codes**: `NotaResult` as declared in the header today
  (`Ok = 0`, errors as defined); no new enum values required.

## Managed mirror (Nota.Infrastructure)

`NativeMethods.Encode.cs` (LibraryImport, `Lib = "nota_engine"`,
`StringMarshalling = StringMarshalling.Utf8` on path, `[Out]`-free):
`EncoderOpenFlac/EncoderOpenMp3 → IntPtr`, `EncoderWrite(IntPtr, float[], int)
→ NotaResult`, `EncoderClose(IntPtr)`, `EncoderDiscard(IntPtr)`. Wrapped by
`NativeEncoder` (see `data-model.md`), which enforces the exactly-once
close/discard via `Dispose`/`Discard`.
