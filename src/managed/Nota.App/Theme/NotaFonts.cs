// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// Two typefaces and one hard rule between them: Geist for names and words, Geist Mono
// for anything that is a measurement — position, tempo, dB, Hz, ms, percentages,
// counters, format badges. Mono is monospaced, so digits keep their width as a knob turns.
//
// This is the C# side of Font.UI / Font.Mono in Theme/NotaTheme.axaml, for the
// custom-drawn layer that builds FormattedText itself. Never construct a Typeface from a
// family name in a view: a name the app does not bundle ("Inter", "Menlo", "monospace")
// silently resolves to whatever the OS has, which is how half the visualisers ended up
// in a different face from the chrome around them.
//
// Both families are bundled (assets/fonts, SIL OFL 1.1). Glyphs they lack — the thin
// space, ■ ▾ ✓ ★ — fall back to the system font per character.

using Avalonia;
using Avalonia.Media;

namespace Nota.App;

public static class NotaFonts
{
    public const string UiUri = "avares://Nota.App/Assets/Fonts#Geist";
    public const string MonoUri = "avares://Nota.App/Assets/Fonts#Geist Mono";

    public static readonly FontFamily UiFamily = new(UiUri);
    public static readonly FontFamily MonoFamily = new(MonoUri);

    // Geist 400 · 500 · 600 · 700
    public static readonly Typeface Sans = new(UiFamily);
    public static readonly Typeface SansMedium = new(UiFamily, FontStyle.Normal, FontWeight.Medium);
    public static readonly Typeface SansSemiBold = new(UiFamily, FontStyle.Normal, FontWeight.SemiBold);
    public static readonly Typeface SansBold = new(UiFamily, FontStyle.Normal, FontWeight.Bold);

    // Geist Mono 400 · 500 (· 700 for the rare bold readout)
    public static readonly Typeface Mono = new(MonoFamily);
    public static readonly Typeface MonoMedium = new(MonoFamily, FontStyle.Normal, FontWeight.Medium);
    public static readonly Typeface MonoBold = new(MonoFamily, FontStyle.Normal, FontWeight.Bold);

    /// <summary>Make Geist the default family, so a control that never names a font — and
    /// <c>FontFamily.Default</c> — still lands on it rather than on the OS face.</summary>
    public static AppBuilder WithNotaFonts(this AppBuilder builder)
        => builder.With(new FontManagerOptions { DefaultFamilyName = UiUri });
}

/// <summary>The type scales (the shell hierarchy, and a device scale held to the 260px card), for
/// views that set FontSize in code. Mirrors the TextBlock role classes in NotaTheme.axaml.
/// Labels are sentence case with no tracking: the *Tracking constants are 0 and stay only
/// so older call sites compile — new code should not set LetterSpacing at all.</summary>
internal static class NotaType
{
    // Shell scale
    public const double Title = 13, Heading = 13, Name = 12, Body = 12, Caption = 11,
                        SectionLabel = 11, Readout = 13, Value = 11, Eyebrow = 10;
    public const double SectionLabelTracking = 0, EyebrowTracking = 0;

    // Device scale — 7 is the floor (plot tick labels only); a label is 8 or more.
    public const double DeviceName = 12, DeviceSection = 10.5, RowLabel = 9, KnobLabel = 8,
                        KnobValue = 8, Axis = 7.5;
    public const double DeviceSectionTracking = 0, RowLabelTracking = 0, KnobLabelTracking = 0;

    public const double Floor = 7;
}
