// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// Wall-clock budgets (DSP stress, per-keystroke search) hold on a developer machine but
// not on shared CI runners, whose speed swings run to run. Under CI (the `CI` variable
// GitHub Actions sets) a missed budget is printed and not failed; the functional half of
// such a check — voice counts and the like — is enforced everywhere.

namespace Nota.SmokeTest;

internal static class TimingBudget
{
    private static readonly bool Lenient =
        Environment.GetEnvironmentVariable("CI") is { Length: > 0 } ci && ci != "false";

    public static (bool Ok, string Label) Check(bool functional, bool withinBudget, string label)
    {
        if (withinBudget || !functional) return (functional && withinBudget, label);
        return (Lenient, Lenient ? $"{label} — over budget, not enforced under CI" : label);
    }
}
