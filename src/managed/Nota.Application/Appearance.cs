// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// Which colour theme to wear, from what settings.json says (spec 011). Pure: no UI, no
// I/O, so the smoke test can walk every case. Themes themselves live in the app layer;
// this only needs their ids and whether each is dark. Old settings (Dark / Light /
// System) migrate here, and an unknown or wrong-kind id falls back to the Studio theme of
// the kind needed — resolving never throws and never writes.

using System.Collections.Generic;

namespace Nota.Application;

/// <summary>How Preferences → Appearance presents a choice: one dark theme, one light
/// theme, or follow the OS with a theme per mode.</summary>
public enum AppearanceMode { Dark, Light, FollowSystem }

/// <summary>A resolved appearance: one theme, or follow the OS with a theme per mode. Every
/// id is known and of the right kind.</summary>
public sealed record AppearanceChoice(bool FollowSystem, string Single, string Dark, string Light)
{
    /// <summary>The theme to wear now, given whether the OS is in dark mode.</summary>
    public string Active(bool osIsDark) => FollowSystem ? (osIsDark ? Dark : Light) : Single;
}

public static class Appearance
{
    public const string StudioDark = "studio-dark";
    public const string StudioLight = "studio-light";
    /// <summary>The <see cref="Settings.ColourTheme"/> value that means "follow the OS".</summary>
    public const string System = "system";

    /// <summary>Resolve the stored settings against the known themes (id → is it dark).</summary>
    public static AppearanceChoice Resolve(Settings s, IReadOnlyDictionary<string, bool> known)
    {
        var stored = s.ColourTheme;
        if (string.IsNullOrEmpty(stored))
            stored = s.Theme switch
            {
                AppTheme.Light => StudioLight,
                AppTheme.System => System,
                _ => StudioDark,
            };

        string dark = known.TryGetValue(s.SystemDarkTheme ?? "", out var d) && d ? s.SystemDarkTheme! : StudioDark;
        string light = known.TryGetValue(s.SystemLightTheme ?? "", out var l) && !l ? s.SystemLightTheme! : StudioLight;

        if (stored == System) return new AppearanceChoice(true, dark, dark, light);
        string single = known.ContainsKey(stored) ? stored : StudioDark;
        return new AppearanceChoice(false, single, dark, light);
    }

    /// <summary>The page's mode for a choice: following the system, or the kind of the
    /// single theme.</summary>
    public static AppearanceMode ModeOf(AppearanceChoice c, bool singleIsDark) =>
        c.FollowSystem ? AppearanceMode.FollowSystem : singleIsDark ? AppearanceMode.Dark : AppearanceMode.Light;

    /// <summary>Switch mode. Dark and Light wear that mode's remembered pick, so switching
    /// back and forth returns to the same theme; follow system keeps both picks.</summary>
    public static AppearanceChoice WithMode(AppearanceChoice c, AppearanceMode mode) => mode switch
    {
        AppearanceMode.FollowSystem => c with { FollowSystem = true },
        AppearanceMode.Dark => c with { FollowSystem = false, Single = c.Dark },
        _ => c with { FollowSystem = false, Single = c.Light },
    };

    /// <summary>Pick a theme of one kind. While following the system it becomes that
    /// mode's pick; otherwise it is worn and also remembered as that mode's pick.</summary>
    public static AppearanceChoice WithPick(AppearanceChoice c, string id, bool isDark)
    {
        var picked = isDark ? c with { Dark = id } : c with { Light = id };
        return c.FollowSystem ? picked : picked with { Single = id };
    }

    /// <summary>Write <paramref name="choice"/> into <paramref name="s"/>, keeping the legacy
    /// <see cref="Settings.Theme"/> in step (the active theme's kind, or System when
    /// following) so older versions still open in a sensible variant.</summary>
    public static void Store(Settings s, AppearanceChoice choice, bool singleIsDark)
    {
        s.ColourTheme = choice.FollowSystem ? System : choice.Single;
        s.SystemDarkTheme = choice.Dark;
        s.SystemLightTheme = choice.Light;
        s.Theme = choice.FollowSystem ? AppTheme.System : singleIsDark ? AppTheme.Dark : AppTheme.Light;
    }
}
