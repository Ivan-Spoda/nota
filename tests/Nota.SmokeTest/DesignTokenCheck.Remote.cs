// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// Nota Remote's page (src/web/remote, spec 014) held as the desktop is: no colour value of
// its own — every colour is a theme token as a custom property, its first-paint fallback
// built from the theme files — and the Studio rules a source scan can see: sentence case
// with no tracking, radii 0–4, every custom property it reads defined. A failure names the
// file and what it found.

using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nota.SmokeTest;

internal static partial class DesignTokenCheck
{
    private static readonly Regex HexColour = new(@"#(?:[0-9a-fA-F]{8}|[0-9a-fA-F]{6}|[0-9a-fA-F]{4}|[0-9a-fA-F]{3})(?![0-9a-zA-Z_-])");
    private static readonly Regex CssColourFn = new(@"\b(?:rgba?|hsla?)\(\s*[\d.]");
    private static readonly Regex Tracking = new(@"letter-spacing\s*:\s*(?<v>[^;}""]+)");
    private static readonly Regex Radius = new(@"border-radius\s*:\s*(?<v>[^;}""]+)");
    private static readonly Regex VarUse = new(@"var\(\s*(?<n>--[a-z0-9-]+)");
    private static readonly Regex VarDef = new(@"(?<![\w-])(?<n>--[a-z0-9-]+)\s*:|style:(?<n>--[a-z0-9-]+)|['""](?<n>--[a-z0-9-]+)['""]");
    private static readonly HashSet<string> RadiusOk = new() { "0", "1px", "2px", "3px", "4px", "50%" };

    public static IEnumerable<(bool Ok, string Label)> RunRemote()
    {
        var root = RepoRoot();
        if (root is null) { yield return (false, "repo root located"); yield break; }
        var web = Path.Combine(root, "src/web/remote");
        var src = Path.Combine(web, "src");
        var files = Files(src, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".svelte") || f.EndsWith(".ts") || f.EndsWith(".css"))
            .OrderBy(f => f, StringComparer.Ordinal).ToList();
        yield return (files.Count > 10, $"phone sources found ({files.Count})");
        var text = files.ToDictionary(f => f, File.ReadAllText);

        // No colour value in the page: hex, rgb() or hsl() — index.html included.
        var withColour = text.Append(new(Path.Combine(web, "index.html").Replace('\\', '/'), File.ReadAllText(Path.Combine(web, "index.html"))))
            .SelectMany(kv => HexColour.Matches(kv.Value).Concat(CssColourFn.Matches(kv.Value)).Select(m => $"{Rel(web, kv.Key)}: {m.Value}"))
            .ToArray();
        yield return (withColour.Length == 0, withColour.Length == 0 ? "phone: no colour literal (every colour is a theme token)"
            : $"phone: colour literals — use a theme token: {string.Join(", ", withColour.Take(8))}");

        // The fallback comes from the theme files, not from values typed into the plugin.
        var plugin = File.ReadAllText(Path.Combine(web, "theme-plugin.ts"));
        var config = File.ReadAllText(Path.Combine(web, "vite.config.ts"));
        yield return (plugin.Contains("Theme/Themes/") && !HexColour.IsMatch(plugin) && config.Contains("notaTheme()"),
            "phone: the first-paint fallback is built from the theme files by the Vite plugin");

        // Sentence case, no tracking: no letter-spacing but 0, no forced capitals.
        var tracked = text.SelectMany(kv => Tracking.Matches(kv.Value).Where(m => m.Groups["v"].Value.Trim() is not ("0" or "normal"))
            .Select(m => $"{Rel(src, kv.Key)}: {m.Value.Trim()}")).ToArray();
        yield return (tracked.Length == 0, tracked.Length == 0 ? "phone: no tracked labels" : $"phone: letter-spacing — labels are not tracked: {string.Join(", ", tracked)}");
        var caps = text.Where(kv => kv.Value.Contains("text-transform: uppercase") || kv.Value.Contains("text-transform:uppercase")
                                    || (kv.Key.EndsWith(".svelte") && kv.Value.Contains(".toUpperCase()")) || Regex.IsMatch(kv.Value, @"class=""[^""]*\bcaps\b"))
            .Select(kv => Rel(src, kv.Key)).ToArray();
        yield return (caps.Length == 0, caps.Length == 0 ? "phone: no forced capitals" : $"phone: forced capitals (uppercase, toUpperCase, .caps): {string.Join(", ", caps)}");

        // Radii 2 / 3 / 4 (0 docked, 1 for bars, 50 % for round dots).
        var radii = text.SelectMany(kv => Radius.Matches(kv.Value)
                .Where(m => m.Groups["v"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(v => !RadiusOk.Contains(v.Trim())))
                .Select(m => $"{Rel(src, kv.Key)}: {m.Value.Trim()}"))
            .ToArray();
        yield return (radii.Length == 0, radii.Length == 0 ? "phone: radii are 0 / 1 / 2 / 3 / 4" : $"phone: off-scale radii: {string.Join(", ", radii.Take(8))}");

        // Every custom property read is a theme token or one the page defines — a renamed or
        // removed token fails here, not as a silently transparent fill on the phone.
        var themeFile = Path.Combine(root, "src/managed/Nota.App/Theme/Themes/studio-dark.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(themeFile));
        var tokens = doc.RootElement.GetProperty("colors").EnumerateObject()
            .Select(p => "--" + Regex.Replace(p.Name, "([a-z0-9])([A-Z])", "$1-$2").ToLowerInvariant()).ToHashSet();
        var defined = text.Values.SelectMany(t => VarDef.Matches(t).Select(m => m.Groups["n"].Value)).ToHashSet();
        var unknown = text.SelectMany(kv => VarUse.Matches(kv.Value).Select(m => m.Groups["n"].Value)
                .Where(n => !tokens.Contains(n) && !defined.Contains(n)).Select(n => $"{Rel(src, kv.Key)}: {n}"))
            .Distinct().ToArray();
        yield return (unknown.Length == 0, unknown.Length == 0 ? $"phone: every custom property read is a theme token ({tokens.Count}) or defined"
            : $"phone: custom properties that are neither a theme token nor defined: {string.Join(", ", unknown.Take(8))}");
    }
}
