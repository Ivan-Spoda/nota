// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// Filter knob glide (.specify/bugs/filter-param-zipper): a sudden Freq / Morph / Gain step on
// Auto Filter, EQ-8 and Dynamic EQ-8 must not click. Steady tones (100 Hz + 1 kHz, a
// deterministic WAV) play through the device, one knob jumps at a block boundary, and the
// peak 4th difference of the output (a high-pass that a click lights up and the tones barely
// do) right after the jump is compared with the device's settled sound before and after.
// The tones are quiet so a resonance sweeping across them stays clear of the ±1 clip.
// Also runnable alone: `dotnet run --project tests/Nota.SmokeTest -- --glide`.

using Nota.Infrastructure;

namespace Nota.SmokeTest;

internal static class FilterGlideTests
{
    private const int Chunk = 512, Chunks = 100, StepAt = 50;
    // Before the fix such jumps measured ×400 – ×17 000 over the settled sound (a standalone
    // render); with the glide they stay within ~×20 — what is left is a resonant cutoff
    // sweeping across the 1 kHz tone, not a click.
    private const double MaxOvershoot = 150.0;

    public static IEnumerable<(bool Ok, string Label)> Run()
    {
        string wav = Path.Combine(Path.GetTempPath(), "nota_smoke_glide_" + Guid.NewGuid().ToString("N") + ".wav");
        WavWriter.WriteTones(wav, seconds: 3.0, freqs: new[] { 100.0, 1000.0 }, amp: 0.05, sampleRate: 44100);
        try
        {
            // Auto Filter (kind 7, params 0..1): Freq 0 · Res 1 · Morph 4.
            yield return Case(wav, "Auto Filter Freq jump", 7,
                new() { [1] = 0.5f, [0] = 0.2f }, (0, 0.8f));
            yield return Case(wav, "Auto Filter Morph jump", 7,
                new() { [1] = 0.3f, [0] = 0.5f }, (4, 1.0f));
            // EQ-8 (kind 0, raw units, 5 per band): band 8 high cut at Q 4; band 2 bell at Q 8.
            yield return Case(wav, "EQ-8 high cut Freq jump", 0,
                new() { [35] = 1f, [37] = 600f, [39] = 4f }, (37, 8000f));
            yield return Case(wav, "EQ-8 bell Freq jump", 0,
                new() { [5] = 1f, [6] = 2f, [7] = 1000f, [8] = 15f, [9] = 8f }, (7, 4000f));
            yield return Case(wav, "EQ-8 48 dB low cut Freq jump", 0,
                new() { [0] = 1f, [1] = 0f, [2] = 40f, [4] = 6f, [40] = 2f }, (2, 300f));
            // Dynamic EQ-8 (kind 13, 10 per band): band 1 high cut at Q 4; band 2 bell gain.
            yield return Case(wav, "Dynamic EQ-8 high cut Freq jump", 13,
                new() { [0] = 1f, [1] = 5f, [2] = 600f, [4] = 4f }, (2, 8000f));
            yield return Case(wav, "Dynamic EQ-8 bell Gain jump", 13,
                new() { [10] = 1f, [11] = 2f, [12] = 3000f, [13] = 0f, [14] = 8f }, (13, 15f));
        }
        finally { try { File.Delete(wav); } catch { } }
    }

    private static (bool, string) Case(string wav, string name, int kind, Dictionary<int, float> setup, (int Param, float Value) jump)
    {
        using var e = new NotaEngine();
        e.SetBpm(120);
        int t = e.AddAudioTrack();
        e.AddAudioClip(t, wav, startBeat: 0.0);
        int d = e.AddBuiltinDevice(t, kind);
        if (d < 0) return (false, $"{name}: device added");
        foreach (var (p, v) in setup) e.DeviceSetParam(t, d, p, v);

        var outL = new float[Chunk * Chunks];
        var buf = new float[Chunk * 2];
        e.Seek(0); e.Play();
        for (int k = 0; k < Chunks; k++)
        {
            if (k == StepAt) e.DeviceSetParam(t, d, jump.Param, jump.Value);
            e.RenderOffline(buf, Chunk);
            for (int i = 0; i < Chunk; i++) outL[k * Chunk + i] = buf[i * 2];
        }
        e.StopTransport();

        double Peak4(int fromChunk, int toChunk)
        {
            double m = 0;
            for (int i = Math.Max(4, fromChunk * Chunk); i < toChunk * Chunk; i++)
                m = Math.Max(m, Math.Abs(outL[i] - 4.0 * outL[i - 1] + 6.0 * outL[i - 2] - 4.0 * outL[i - 3] + outL[i - 4]));
            return m;
        }
        double settled = Math.Max(Peak4(StepAt - 20, StepAt), Peak4(Chunks - 20, Chunks));
        double jumpPk = Peak4(StepAt, StepAt + 10);
        bool finite = outL.All(float.IsFinite);
        double ratio = jumpPk / Math.Max(settled, 1e-9);
        return (finite && settled > 0 && ratio <= MaxOvershoot,
            $"{name} glides without a click (jump ×{ratio:F1} the settled sound, limit ×{MaxOvershoot:F0})");
    }
}
