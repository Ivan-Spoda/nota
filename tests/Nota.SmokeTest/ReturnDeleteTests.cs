// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// Deleting a return track (.specify/bugs/return-track-delete): the surviving returns move
// down to bus slots 0..n-1, every send follows its bus, the survivor keeps sounding, undo
// restores the old numbering and sends, a new return takes a free slot, and the result
// survives save/load. Also runnable alone: `dotnet run --project tests/Nota.SmokeTest -- --returns`.

using Nota.Application;
using Nota.Infrastructure;

namespace Nota.SmokeTest;

internal static class ReturnDeleteTests
{
    private const int Utility = 4;   // unity gain, no tail: a bus is either fed or silent

    public static IEnumerable<(bool ok, string label)> Run()
    {
        using var eng = new NotaEngine();
        int src = eng.AddInstrumentTrack();
        int clip = eng.AddMidiClip(src, 0.0, 4.0);
        eng.SetClipNotes(src, clip, new[] { new NotaNote(60, 0.0, 4.0, 0.9f) });
        int r0 = eng.AddReturnTrack(), r1 = eng.AddReturnTrack();
        eng.AddBuiltinDevice(r0, Utility);
        eng.AddBuiltinDevice(r1, Utility);
        eng.SetTrackSend(src, 0, 0.5f);
        eng.SetTrackSend(src, 1, 0.7f);
        yield return (eng.TrackReturnIndex(r0) == 0 && eng.TrackReturnIndex(r1) == 1, "returns: two returns on buses 0 and 1");

        yield return (eng.RemoveTracks(new[] { r0 }), "returns: delete the first return");
        yield return (eng.ReturnTrackCount == 1 && eng.TrackReturnIndex(r1) == 0,
            $"returns: the survivor moves to bus 0 (index {eng.TrackReturnIndex(r1)})");
        yield return (Near(eng.GetTrackSend(src, 0), 0.7f) && eng.GetTrackSend(src, 1) == 0f,
            $"returns: the send follows its bus ({eng.GetTrackSend(src, 0):F2}, {eng.GetTrackSend(src, 1):F2})");
        float peak = Peak(eng, r1);
        yield return (peak > 0.001f, $"returns: the surviving return still sounds (peak={peak:F3})");

        yield return (eng.Undo(), "returns: undo the delete");
        yield return (eng.TrackReturnIndex(r0) == 0 && eng.TrackReturnIndex(r1) == 1
                      && Near(eng.GetTrackSend(src, 0), 0.5f) && Near(eng.GetTrackSend(src, 1), 0.7f),
            "returns: undo restores both buses and both sends");

        // The single-track path does the same; a new return then takes the free slot.
        yield return (eng.RemoveTrack(r0) && eng.TrackReturnIndex(r1) == 0, "returns: RemoveTrack compacts too");
        int r2 = eng.AddReturnTrack();
        yield return (eng.TrackReturnIndex(r2) == 1 && eng.GetTrackSend(src, 1) == 0f,
            $"returns: a new return takes bus 1, unfed (index {eng.TrackReturnIndex(r2)})");

        // Save/load: the reloaded returns line up with the saved sends.
        eng.SetTrackSend(src, 1, 0.3f);
        eng.SetTrackName(r1, "Kept");
        string dir = Path.Combine(Path.GetTempPath(), "nota-returns-" + Guid.NewGuid().ToString("N"));
        try
        {
            var doc = ProjectService.Capture(eng, new TransportState(120, 1, false, false), new List<string>());
            ProjectService.Save(doc, dir, eng);
            using var eng2 = new NotaEngine();
            ProjectService.Apply(ProjectService.Load(dir), eng2, dir);
            int kept = -1, inst = -1;
            for (int i = 0; i < eng2.TrackCount; i++)
                if (eng2.TryGetTrackInfo(i, out var t))
                {
                    if (t.IsReturn && eng2.GetTrackName(t.Id) == "Kept") kept = t.Id;
                    else if (t.IsInstrument) inst = t.Id;
                }
            yield return (kept > 0 && eng2.TrackReturnIndex(kept) == 0
                          && Near(eng2.GetTrackSend(inst, 0), 0.7f) && Near(eng2.GetTrackSend(inst, 1), 0.3f),
                "returns: save/load keeps each send on its return");
        }
        finally
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch (IOException) { }
        }
    }

    private static bool Near(float a, float b) => Math.Abs(a - b) < 1e-6f;

    private static float Peak(NotaEngine eng, int trackId)
    {
        var buf = new float[4096 * 2];
        eng.Seek(0);
        eng.Play();
        eng.RenderOffline(buf, 4096);
        eng.StopTransport();
        eng.TryGetTrackMeter(trackId, out var m);
        return m.Peak;
    }
}
