// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// Snap grid (specs/003-adaptive-snap-grid): the step ladder, adaptive resolution from the
// zoom, bar-anchored snapping and line enumeration (what you see is what you snap to),
// the ⌘1–⌘5 transitions, triplets, labels, and the app-wide settings round trip.
// Also runnable alone: `dotnet run --project tests/Nota.SmokeTest -- --grid`.

using System.Text.Json;
using Nota.Application;
using Nota.Application.Editing;

namespace Nota.SmokeTest;

internal static class SnapGridTests
{
    private static bool Near(double a, double b) => Math.Abs(a - b) < 1e-9;

    private static ResolvedGrid At(GridSetting s, double ppb, int bpb = 4) => SnapGrid.Resolve(s, ppb, bpb);

    // Every enumerated line snaps to itself, and every snapped beat is an enumerated line.
    private static bool LinesAgree(double step, int bpb, double from, double to)
    {
        var lines = SnapGrid.GridLines(from, to, step, bpb).ToList();
        if (lines.Count == 0) return false;
        foreach (var l in lines)
            if (!Near(SnapGrid.SnapBeat(l, step, bpb), l)) return false;
        var set = lines.Select(l => Math.Round(l, 6)).ToHashSet();
        for (double b = from; b <= to; b += 0.0371)
        {
            double s = SnapGrid.SnapBeat(b, step, bpb);
            if (s < from - 1e-6 || s > to + 1e-6) continue;   // nearest line lies outside the window
            if (!set.Contains(Math.Round(s, 6))) return false;
        }
        return true;
    }

    public static IEnumerable<(bool Ok, string Label)> Run()
    {
        // ---- ladder ---------------------------------------------------------------------
        double[] want4 = { 0.125, 0.25, 0.5, 1, 2, 4, 8, 16 };
        yield return (Enumerable.Range(0, 8).All(i => Near(GridStep.Of(i, false).Beats(4), want4[i])),
            "the straight ladder in 4/4 is 1/32 … 1/2, 1, 2, 4 bars");
        yield return (Near(GridStep.Of(5, false).Beats(3), 3) && Near(GridStep.Of(6, false).Beats(3), 6) && Near(GridStep.Of(7, false).Beats(3), 12),
            "bar steps follow the time signature (3/4: 3, 6, 12 beats)");
        yield return (GridStep.Of(42, false).Index == 7 && GridStep.Of(-3, false).Index == 0, "the step index is clamped to 0..7");
        string[] labels = { "1/32", "1/16", "1/8", "1/4", "1/2", "1 bar", "2 bars", "4 bars" };
        yield return (Enumerable.Range(0, 8).All(i => GridStep.Of(i, false).Label == labels[i]), "straight labels read 1/32 … 4 bars");

        // ---- adaptive resolution --------------------------------------------------------
        var def = GridSetting.DefaultArrangement;
        var r = At(def, 28);
        yield return (r.Snap.Index == 3 && r.Draw == r.Snap && r.SnapOn, $"defaults at 28 px/beat resolve to 1/4 (got {r.Snap.Label})");
        yield return (SnapGrid.Label(def, r) == "Auto · 1/4", $"the adaptive label reads \"Auto · 1/4\" (got \"{SnapGrid.Label(def, r)}\")");
        int n = At(def with { Density = GridDensity.Narrow }, 60).Snap.Index;
        int m = At(def with { Density = GridDensity.Medium }, 60).Snap.Index;
        int w = At(def with { Density = GridDensity.Wide }, 60).Snap.Index;
        yield return (n < m && m < w, $"Narrow < Medium < Wide at the same zoom ({n} {m} {w})");
        yield return (At(def with { Density = GridDensity.Narrow }, 240).Snap.Index == 0, "zoomed far in, adaptive stops at 1/32");
        yield return (At(def with { Density = GridDensity.Wide }, 0.5).Snap.Index == 7, "zoomed far out, adaptive stops at 4 bars");
        yield return (At(def, 4).Snap.Index == 5, "the arrangement's widest zoom (4 px/beat) resolves to 1 bar");
        bool finer = true;
        for (double ppb = 2; ppb < 300; ppb *= 1.17)
            if (At(def, ppb * 1.17).Snap.Index > At(def, ppb).Snap.Index) finer = false;
        yield return (finer, "zooming in never makes the adaptive step coarser");

        // ---- snapping and lines ---------------------------------------------------------
        yield return (Near(SnapGrid.SnapBeat(1.3, 0.5, 4), 1.5) && Near(SnapGrid.SnapBeat(1.2, 0.5, 4), 1.0), "SnapBeat picks the nearest position");
        yield return (Near(SnapGrid.SnapBeat(-0.4, 1, 4), 0), "SnapBeat never goes below zero");
        yield return (Near(SnapGrid.SnapBeat(13, 16, 4), 16) && Near(SnapGrid.SnapBeat(7, 16, 4), 0), "4-bar steps snap to multiples of 4 bars");
        bool agree = true;
        foreach (int bpb in new[] { 4, 3, 7 })
            for (int i = 0; i < 8; i++)
                foreach (bool t in new[] { false, true })
                    if (!LinesAgree(GridStep.Of(i, t).Beats(bpb), bpb, 0, 40)) agree = false;
        yield return (agree, "lines and snapping agree for every step, straight and triplet, in 4/4, 3/4 and 7/4 (SC-001)");
        var bars34 = SnapGrid.GridLines(0, 12, 2, 3).ToList();
        yield return (new[] { 0.0, 2, 3, 5, 6, 8, 9, 11, 12 }.All(b => bars34.Any(l => Near(l, b))) && bars34.Count == 9,
            "3/4 with 1/2: lines restart at every bar (0 2 | 3 5 | 6 8 | …)");
        yield return (Near(SnapGrid.SnapBeat(2.8, 2, 3), 3), "3/4 with 1/2: the bar line is a snap point (2.8 → 3)");

        // ---- fixed mode, finer than drawn -----------------------------------------------
        var f32 = def with { Adaptive = false, FixedIndex = 0 };
        var far = At(f32, 4);
        yield return (far.Snap.Index == 0 && far.Draw.Index > 0 && far.FinerThanVisible, $"fixed 1/32 at 4 px/beat draws coarser lines ({far.Draw.Label}) and is marked");
        var near = At(f32, 240);
        yield return (near.Draw == near.Snap && !near.FinerThanVisible, "fixed 1/32 at 240 px/beat draws its own lines, unmarked");
        var f8 = def with { Adaptive = false, FixedIndex = 2 };
        yield return (At(f8, 4).Snap.Index == 2 && At(f8, 200).Snap.Index == 2 && SnapGrid.Label(f8, At(f8, 50)) == "1/8",
            "a fixed step ignores the zoom and reads \"1/8\"");

        // ---- transitions (⌘1 ⌘2 ⌘4 ⌘5, menu picks) ------------------------------------
        var fx = def with { Adaptive = false, FixedIndex = 2 };
        yield return (SnapGrid.Narrower(fx).FixedIndex == 1 && SnapGrid.Wider(fx).FixedIndex == 3, "⌘1 / ⌘2 halve / double a fixed step");
        yield return (SnapGrid.Narrower(fx with { FixedIndex = 0 }).FixedIndex == 0 && SnapGrid.Wider(fx with { FixedIndex = 7 }).FixedIndex == 7,
            "⌘1 / ⌘2 stop at 1/32 and 4 bars");
        yield return (SnapGrid.Narrower(def with { Density = GridDensity.Wide }).Density == GridDensity.Medium
                      && SnapGrid.Narrower(def with { Density = GridDensity.Narrow }).Density == GridDensity.Narrow
                      && SnapGrid.Wider(def).Density == GridDensity.Wide
                      && SnapGrid.Wider(def with { Density = GridDensity.Wide }).Density == GridDensity.Wide,
            "in Auto, ⌘1 / ⌘2 move the density one notch and stop at the ends");
        var mixed = def with { Density = GridDensity.Wide, FixedIndex = 1 };
        var toFixed = SnapGrid.ToggleAdaptive(mixed);
        var back = SnapGrid.ToggleAdaptive(toFixed);
        yield return (!toFixed.Adaptive && toFixed.FixedIndex == 1 && back.Adaptive && back.Density == GridDensity.Wide,
            "⌘5 twice restores both the density and the fixed step");
        var off = SnapGrid.ToggleSnap(def);
        yield return (!off.SnapOn && off with { SnapOn = true } == def && SnapGrid.ToggleSnap(off) == def, "⌘4 flips only snap on/off");
        yield return (!At(off, 28).SnapOn, "snap off is carried into the resolved grid");
        yield return (SnapGrid.Narrower(off).SnapOn == false, "⌘1 leaves snap off as it is");
        yield return (SnapGrid.PickFixed(off, 4) is { Adaptive: false, FixedIndex: 4, SnapOn: true }
                      && SnapGrid.PickAdaptive(off with { Adaptive = false }, GridDensity.Narrow) is { Adaptive: true, Density: GridDensity.Narrow, SnapOn: true },
            "picking a value from the menu turns snap back on");

        // ---- triplets -------------------------------------------------------------------
        yield return (Near(GridStep.Of(2, true).Beats(4), 1.0 / 3) && Near(GridStep.Of(3, true).Beats(4), 2.0 / 3) && Near(GridStep.Of(4, true).Beats(4), 4.0 / 3),
            "triplet steps are 2/3 of straight ones (1/8T = 1/3 beat)");
        yield return (Near(GridStep.Of(5, true).Beats(4), 4) && !GridStep.Of(6, true).Triplet && GridStep.Of(5, true).Label == "1 bar", "bar steps ignore triplets");
        yield return (GridStep.Of(2, true).Label == "1/8T", "triplet labels add T (1/8T)");
        var trip = def with { Triplets = true };
        var rt = At(trip, 60);
        yield return (rt.Snap.Triplet && SnapGrid.Label(trip, rt) == $"Auto · {rt.Snap.Label}" && rt.Snap.Label.EndsWith('T'),
            $"adaptive with triplets resolves to a triplet step ({rt.Snap.Label})");
        yield return (At(trip, 240).Snap.Triplet && At(trip, 4).Snap.Index >= 5, "adaptive triplets move through the triplet family, then to bars");
        var tt = SnapGrid.ToggleTriplets(fx);
        yield return (tt.Triplets && tt with { Triplets = false } == fx && SnapGrid.ToggleTriplets(tt) == fx, "⌘3 flips only triplets");
        var t34 = SnapGrid.GridLines(0, 6, 4.0 / 3, 3).ToList();
        yield return (t34.Any(l => Near(l, 3)) && t34.Any(l => Near(l, 6)), "3/4 with 1/2T keeps every bar start a line");

        // ---- settings (app-wide, FR-016 / FR-017) ---------------------------------------
        var fresh = JsonSerializer.Deserialize<Settings>("{}")!;
        yield return (fresh.ArrangementGrid == GridSetting.DefaultArrangement && fresh.PianoRollGrid == GridSetting.DefaultPianoRoll
                      && fresh.QuantizeStep == GridStep.Of(1, false) && fresh.ArrangementGrid.Adaptive && fresh.ArrangementGrid.SnapOn,
            "settings without grid fields load Auto · Medium, snap on, Quantize 1/16");
        var s = new Settings
        {
            ArrangementGrid = def with { Adaptive = false, FixedIndex = 6 },
            PianoRollGrid = GridSetting.DefaultPianoRoll with { Triplets = true, Density = GridDensity.Narrow },
            QuantizeStep = GridStep.Of(2, true),
        };
        var rs = JsonSerializer.Deserialize<Settings>(JsonSerializer.Serialize(s))!;
        yield return (rs.ArrangementGrid == s.ArrangementGrid && rs.PianoRollGrid == s.PianoRollGrid && rs.QuantizeStep == s.QuantizeStep,
            "a settings round trip keeps both editors' grids and the Quantize step separate");
        var wild = JsonSerializer.Deserialize<Settings>("{\"ArrangementGrid\":{\"FixedIndex\":42,\"Adaptive\":false}}")!;
        yield return (At(wild.ArrangementGrid, 28).Snap.Index == 7 && wild.ArrangementGrid.SnapOn, "an out-of-range stored step is clamped, missing fields keep defaults");
    }
}
