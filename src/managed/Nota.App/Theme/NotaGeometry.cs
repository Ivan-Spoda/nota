// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// Geometry for code-built views — the C# mirror of the Radius.* / Control.* / Space.*
// keys in Theme/NotaTheme.axaml, the way NotaPalette mirrors Brush.*. Geometry does not
// change with the variant, so these are plain constants. The smoke test fails if a value
// here and its XAML key drift apart.
//
// Radius follows one small scale: 2 for inner keys, rows, clips; 3 for controls, cards and
// popovers; 4 only for modal windows; 0 for docked panels; 1 for bars inside a plot.

using Avalonia;

namespace Nota.App;

internal static class NotaRadius
{
    public const double DockedValue = 0, BarValue = 1, ClipValue = 2, BadgeValue = 2, ControlValue = 3,
                        TileValue = 3, PanelValue = 3, BodyValue = 3, PillValue = 3, ModalValue = 4;

    /// <summary>0 — a docked panel; it meets its neighbours on a 1px seam.</summary>
    public static readonly CornerRadius Docked = new(DockedValue);
    /// <summary>1 — spectrum and level bars inside a plot.</summary>
    public static readonly CornerRadius Bar = new(BarValue);
    /// <summary>2 — clip, row highlight, menu item, slider track.</summary>
    public static readonly CornerRadius Clip = new(ClipValue);
    /// <summary>2 — inner key, badge inside a device.</summary>
    public static readonly CornerRadius Badge = new(BadgeValue);
    /// <summary>3 — button, field, dropdown, graph window, popover.</summary>
    public static readonly CornerRadius Control = new(ControlValue);
    /// <summary>3 — transport key, search field, tile.</summary>
    public static readonly CornerRadius Tile = new(TileValue);
    /// <summary>3 — device section, segmented group.</summary>
    public static readonly CornerRadius Panel = new(PanelValue);
    /// <summary>3 — device card.</summary>
    public static readonly CornerRadius Body = new(BodyValue);
    /// <summary>3 — chip, switch. Studio has no pill shapes; the name stays for its callers.</summary>
    public static readonly CornerRadius Pill = new(PillValue);
    /// <summary>4 — modal window, command palette, splash.</summary>
    public static readonly CornerRadius Modal = new(ModalValue);

    /// <summary>Rounded top only — a header strip or a stripe sitting on a body.</summary>
    public static CornerRadius Top(double r) => new(r, r, 0, 0);
}

internal static class NotaSize
{
    public const double Chip = 20;        // filter chip
    public const double Seg = 24;         // tab segment
    public const double Shell = 26;       // shell button, field, dropdown
    public const double SegGroup = 30;    // segment container
    public const double Transport = 26;   // transport key
    public const double Play = 36;        // Play is the one wide transport key
    public const double Console = 30;     // the recessed transport group inside the row
    public const double TransportBar = 44; // the docked transport row
    public const double KnobSecondary = 34;
    public const double KnobRegular = 36;
    public const double KnobMain = 44;
    public const double KnobInline = 24;  // a knob inside a table row (Consort oscillators, ADSR strip)
    public const double ParamCell = 53;   // knob 34 + label line 9 + value line 10
}

internal static class NotaSpace
{
    // Inside a device: 2px units.
    public const double DeviceHair = 2;
    public const double DeviceGap = 5;
    public const double DeviceInset = 6;
    public const double DeviceInsetWide = 8;
    // In the shell: 4px units.
    public const double Tile = 8;
    public const double Gutter = 1;       // between docked panels: the seam
    public const double Inset = 20;
}
