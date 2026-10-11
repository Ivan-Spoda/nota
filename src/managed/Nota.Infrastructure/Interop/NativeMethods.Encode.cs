// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.

using System.Runtime.InteropServices;

namespace Nota.Infrastructure;

internal static partial class NativeMethods
{
    // --- Audio file encoders (export) — nota_engine.h "Audio file encoders" ---

    [LibraryImport(Lib, EntryPoint = "nota_encoder_open_flac", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr EncoderOpenFlac(string path, int sampleRate, int bitsPerSample, int dither);

    [LibraryImport(Lib, EntryPoint = "nota_encoder_open_mp3", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial IntPtr EncoderOpenMp3(string path, int sampleRate, int bitrateKbps);

    [LibraryImport(Lib, EntryPoint = "nota_encoder_write")]
    internal static partial NotaResult EncoderWrite(IntPtr encoder, float[] samples, int frames);

    [LibraryImport(Lib, EntryPoint = "nota_encoder_close")]
    internal static partial NotaResult EncoderClose(IntPtr encoder);

    [LibraryImport(Lib, EntryPoint = "nota_encoder_discard")]
    internal static partial void EncoderDiscard(IntPtr encoder);
}
