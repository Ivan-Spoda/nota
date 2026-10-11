// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// Managed face of the engine's streaming FLAC/MP3 encoders (nota_encoder_* C ABI):
// same shape as WavWriter — construct (opens), WriteFrames(float[] interleaved),
// Dispose closes/flushes — so the exporter treats both sinks alike. Encoding runs
// on the export background thread; one instance per output file.

using System;
using System.IO;

namespace Nota.Infrastructure;

/// <summary>Streaming FLAC / MP3 file encoder for export. 16-bit FLAC targets can
/// carry TPDF dither (same rule as WavWriter); MP3 has no integer target.</summary>
public sealed class NativeEncoder : IDisposable
{
    private IntPtr _enc;

    public NativeEncoder(string path, int sampleRate, int channels, WavBitDepth depth,
                         bool dither = false)
    {
        if (channels != 2)
            throw new NotSupportedException("Native encoders are stereo-only (export).");
        if (depth == WavBitDepth.Float32)
            throw new NotSupportedException("FLAC has no 32-bit float target.");
        _enc = NativeMethods.EncoderOpenFlac(path, sampleRate,
            depth == WavBitDepth.Pcm16 ? 16 : 24, dither ? 1 : 0);
        if (_enc == IntPtr.Zero)
            throw new IOException($"FLAC encoder failed to open '{path}'.");
    }

    public NativeEncoder(string path, int sampleRate, int channels, int bitrateKbps)
    {
        if (channels != 2)
            throw new NotSupportedException("Native encoders are stereo-only (export).");
        _enc = NativeMethods.EncoderOpenMp3(path, sampleRate, bitrateKbps);
        if (_enc == IntPtr.Zero)
            throw new IOException($"MP3 encoder failed to open '{path}'.");
    }

    /// <summary>Appends <paramref name="frames"/> interleaved stereo frames.</summary>
    public void WriteFrames(float[] interleaved, int frames)
    {
        var r = NativeMethods.EncoderWrite(Handle(), interleaved, frames);
        if (r != NativeMethods.NotaResult.Ok)
            throw new IOException($"encoder write failed ({r}).");
    }

    /// <summary>Flushes, finishes and closes the stream. Safe to call once; the
    /// handle is freed either way.</summary>
    public void Close()
    {
        if (_enc == IntPtr.Zero) return;
        var e = _enc;
        _enc = IntPtr.Zero;
        var r = NativeMethods.EncoderClose(e);
        if (r != NativeMethods.NotaResult.Ok)
            throw new IOException($"encoder close failed ({r}).");
    }

    /// <summary>Frees an errored/abandoned encoder WITHOUT finishing the stream —
    /// the partial file stays on disk for the caller to delete.</summary>
    public void Discard()
    {
        if (_enc == IntPtr.Zero) return;
        NativeMethods.EncoderDiscard(_enc);
        _enc = IntPtr.Zero;
    }

    public void Dispose() => Close();

    private IntPtr Handle()
        => _enc != IntPtr.Zero ? _enc : throw new ObjectDisposedException(nameof(NativeEncoder));
}
