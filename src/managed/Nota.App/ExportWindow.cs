// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// Export audio (mockup 1i). Range (Loop / Full song are real; Custom is M7+),
// format (WAV / FLAC / MP3) / sample-rate / bit-depth-or-bitrate (M6-4 + M7
// codecs), release tail, normalize (−1 dBTP true peak) and dither (16-bit
// targets only), plus stem export. The option rows adapt to the format: FLAC
// offers 16/24-bit integer only, MP3 swaps bit depth for a CBR bitrate list and
// caps the sample rate at 48 kHz (the format's limit). The dialog returns
// ExportOptions; MainWindow runs the offline render.

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Nota.Application;

namespace Nota.App;

public sealed record ExportOptions(int SampleRate, WavBitDepth Depth, double TailBeats, double RangeBeats,
                                   bool Stems, bool Normalize, bool Dither,
                                   ExportFormat Format = ExportFormat.Wav, int BitrateKbps = 320);

public sealed class ExportWindow : NotaWindow
{
    private static readonly IBrush Panel = NotaPalette.SurfaceCard;
    private static readonly IBrush Sunken = NotaPalette.BgSunken;
    private static readonly IBrush Raised = NotaPalette.SurfaceRaised;
    private static readonly IBrush BorderDef = NotaPalette.BorderDefault;
    private static readonly IBrush BorderStrong = NotaPalette.BorderStrong;
    private static readonly IBrush Brass = NotaPalette.Accent;
    private static readonly IBrush OnAccent = NotaPalette.TextOnAccent;
    private static readonly IBrush TextPrimary = NotaPalette.TextPrimary;
    private static readonly IBrush TextSecondary = NotaPalette.TextSecondary;
    private static readonly IBrush TextTertiary = NotaPalette.TextTertiary;

    private static readonly int[] Rates = { 44100, 48000, 96000 };
    private static readonly string[] RateLabels = { "44 100", "48 000", "96 000" };
    private static readonly int[] Bitrates = { 320, 256, 192, 128 };   // MP3 CBR, best first
    private const int BeatsPerBar = 4;

    private readonly double _bpm;
    private double _rangeBeats;
    private TextBlock _rangeReadout = null!;
    private TextBlock _estimate = null!;
    private ComboBox _format = null!;
    private ComboBox _rate = null!;
    private ComboBoxItem[] _rateItems = null!;
    private ComboBox _depth = null!;
    private ComboBox _quality = null!;
    private TextBlock _depthTitle = null!;
    private bool _tailOn = true;
    private bool _stems;
    private bool _normalize;
    private bool _dither;
    private Action? _refreshDitherEnabled;
    private TextBlock _exportLabel = null!;

    public ExportWindow(double fullBeats, double loopBeats, double bpm)
    {
        _bpm = bpm;
        _rangeBeats = loopBeats > 0 ? loopBeats : fullBeats;
        Title = "Export Audio";
        Width = 560;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Panel;

        var body = new StackPanel { Margin = new Thickness(18, 16), Spacing = 14 };

        // Range.
        _rangeReadout = Mono("", TextSecondary);
        var range = Segmented(new[] { ("Loop", true), ("Full song", true), ("Custom", false) }, 0, i =>
        {
            _rangeBeats = i == 1 ? fullBeats : loopBeats > 0 ? loopBeats : fullBeats;
            RefreshRange();
        });
        var rangeRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        rangeRow.Children.Add(range);
        var customNa = new NaBadge { Kind = NaBadgeKind.Future, VerticalAlignment = VerticalAlignment.Center };
        rangeRow.Children.Add(customNa);
        rangeRow.Children.Add(_rangeReadout);
        body.Children.Add(Section("RANGE", rangeRow));

        // Format / SR / bit-depth-or-quality. The third column swaps its control
        // (and title) with the format: depth for WAV/FLAC, bitrate for MP3.
        _format = Combo(new[] { "WAV", "FLAC", "MP3" }, 0);
        _format.SelectionChanged += (_, _) => ApplyFormat();

        _rate = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
        _rateItems = new ComboBoxItem[Rates.Length];
        for (int i = 0; i < Rates.Length; i++)
        {
            _rateItems[i] = new ComboBoxItem { Content = RateLabels[i] };
            _rate.Items.Add(_rateItems[i]);
        }
        _rate.SelectedIndex = 1;   // 48 000
        _rate.SelectionChanged += (_, _) => RefreshEstimate();

        _depth = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
        _quality = Combo(Array.ConvertAll(Bitrates, b => b.ToString()), 0);
        _quality.SelectionChanged += (_, _) => RefreshEstimate();

        var depthOrQuality = new StackPanel();
        depthOrQuality.Children.Add(_depth);
        depthOrQuality.Children.Add(_quality);
        var fmtGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*"), ColumnSpacing = 12 };
        var f0 = Section("FORMAT", _format); fmtGrid.Children.Add(f0);
        var f1 = Section("SAMPLE RATE", _rate); Grid.SetColumn(f1, 1); fmtGrid.Children.Add(f1);
        var f2 = Section("BIT DEPTH", depthOrQuality); Grid.SetColumn(f2, 2); fmtGrid.Children.Add(f2);
        _depthTitle = (TextBlock)((StackPanel)f2).Children[0];
        body.Children.Add(fmtGrid);
        _depth.SelectionChanged += (_, _) => { RefreshEstimate(); _refreshDitherEnabled?.Invoke(); };
        ApplyFormat();   // populates depth items + visibility for the default format

        // Options: normalize + dither (working toggles) and a release tail (checkbox).
        var normalize = WorkingToggle("Normalize −1 dBTP", () => _normalize, v => { _normalize = v; RefreshEstimate(); });
        var dither = WorkingToggle("Dither", () => _dither, v => { _dither = v; },
                                   enabled: () => SelectedFormat != ExportFormat.Mp3 && _depth.SelectedIndex == 0,
                                   hint: "16-bit only");
        body.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 22, Children = { normalize, dither } });
        body.Children.Add(TailCheck());

        // Stems (one file per track, in the selected format).
        body.Children.Add(StemsBlock());

        // Footer.
        _estimate = new TextBlock { FontSize = 10, Foreground = TextTertiary, VerticalAlignment = VerticalAlignment.Center };
        var export = new Border
        {
            Background = Brass, CornerRadius = NotaRadius.Tile, Padding = new Thickness(18, 0), Height = 30,
            Cursor = new Cursor(StandardCursorType.Hand), VerticalAlignment = VerticalAlignment.Center,
            Child = (_exportLabel = new TextBlock { Text = "Export WAV", FontSize = 12, FontWeight = FontWeight.SemiBold, Foreground = OnAccent, VerticalAlignment = VerticalAlignment.Center }),
        };
        export.PointerPressed += (_, _) =>
        {
            var fmt = SelectedFormat;
            int sr = Rates[_rate.SelectedIndex < 0 ? 1 : _rate.SelectedIndex];
            // MP3 has no bit-depth target; Pcm24 keeps Depth well-formed for the request.
            var d = fmt == ExportFormat.Mp3 ? WavBitDepth.Pcm24
                  : _depth.SelectedIndex switch { 0 => WavBitDepth.Pcm16, 2 => WavBitDepth.Float32, _ => WavBitDepth.Pcm24 };
            double tailBeats = _tailOn ? BeatsPerBar : 0.0;
            bool dither = _dither && d == WavBitDepth.Pcm16;   // only meaningful for 16-bit targets
            int kbps = Bitrates[_quality.SelectedIndex < 0 ? 0 : _quality.SelectedIndex];
            Close(new ExportOptions(sr, d, tailBeats, _rangeBeats, _stems, _normalize, dither, fmt, kbps));
        };
        var cancel = new TextBlock { Text = "Cancel", FontSize = 11, Foreground = TextSecondary, VerticalAlignment = VerticalAlignment.Center, Cursor = new Cursor(StandardCursorType.Hand) };
        cancel.PointerPressed += (_, _) => Close(null);
        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), Margin = new Thickness(0, 2, 0, 0) };
        footer.Children.Add(export);
        Grid.SetColumn(_estimate, 1); _estimate.Margin = new Thickness(10, 0, 0, 0); footer.Children.Add(_estimate);
        Grid.SetColumn(cancel, 2); footer.Children.Add(cancel);
        body.Children.Add(footer);

        SetBody(body);
        RefreshRange();
    }

    private ExportFormat SelectedFormat
        => _format.SelectedIndex switch { 1 => ExportFormat.Flac, 2 => ExportFormat.Mp3, _ => ExportFormat.Wav };

    // Re-applies the option matrix for the selected format (FR-002/FR-003): the
    // depth list, the third column's control + title, the 96 kHz gate for MP3,
    // the dither rule, and the dependent label/estimate.
    private void ApplyFormat()
    {
        if (_depth is null) return;   // during construction, before fields exist
        var fmt = SelectedFormat;

        _depth.Items.Clear();
        if (fmt == ExportFormat.Wav)
        {
            foreach (var it in new[] { "16-bit", "24-bit", "32-bit float" }) _depth.Items.Add(it);
            _depth.SelectedIndex = 1;
        }
        else
        {
            foreach (var it in new[] { "16-bit", "24-bit" }) _depth.Items.Add(it);
            _depth.SelectedIndex = 1;
        }

        bool mp3 = fmt == ExportFormat.Mp3;
        _depth.IsVisible = !mp3;
        _quality.IsVisible = mp3;
        _depthTitle.Text = mp3 ? "QUALITY" : "BIT DEPTH";

        // MP3 tops out at 48 kHz (MPEG-1): grey 96 kHz out, and move an existing
        // 96 kHz selection down to 48 kHz so no invalid pair is reachable.
        _rateItems[2].IsEnabled = !mp3;
        if (mp3 && _rate.SelectedIndex == 2) _rate.SelectedIndex = 1;

        _refreshDitherEnabled?.Invoke();
        PaintExportLabel();
        RefreshEstimate();
    }

    private void PaintExportLabel()
    {
        if (_exportLabel is null) return;   // during construction, before the footer exists
        _exportLabel.Text = _stems ? "Export stems" : $"Export {SelectedFormat.ToString().ToUpperInvariant()}";
    }

    private void RefreshRange()
    {
        double sec = _bpm > 0 ? _rangeBeats * 60.0 / _bpm : 0;
        int endBar = (int)(_rangeBeats / BeatsPerBar) + 1;
        _rangeReadout.Text = string.Format(NotaNum.Culture, "1.1.1 → {0}.1.1 · {1:0.0} s", endBar, sec);
        RefreshEstimate();
    }

    private void RefreshEstimate()
    {
        if (_estimate is null) return;
        double tail = _tailOn ? BeatsPerBar : 0;
        double sec = _bpm > 0 ? (_rangeBeats + tail) * 60.0 / _bpm : 0;
        int sr = Rates[_rate.SelectedIndex < 0 ? 1 : _rate.SelectedIndex];
        int bytesPerSample = _depth.SelectedIndex switch { 0 => 2, 2 => 4, _ => 3 };
        double mb = SelectedFormat switch
        {
            // MP3 CBR is exact: kbps → 125 bytes/s. FLAC is level-5-typical
            // compression on full-range music — honest ≈, not a promise.
            ExportFormat.Mp3 => sec * Bitrates[_quality.SelectedIndex < 0 ? 0 : _quality.SelectedIndex] * 125 / 1_000_000.0,
            ExportFormat.Flac => 0.65 * sec * sr * 2 * bytesPerSample / 1_000_000.0,
            _ => sec * sr * 2 * bytesPerSample / 1_000_000.0,
        };
        _estimate.Text = _stems
            ? string.Format(NotaNum.Culture, "one file per track · {0:0.0} MB each · offline render", mb)
            : string.Format(NotaNum.Culture, "≈ 1 file · {0:0.0} MB · offline render", mb);
    }

    // ---- helpers ----------------------------------------------------------

    private Control Section(string title, Control content) => new StackPanel
    {
        Spacing = 6,
        Children = { new TextBlock { Text = title, FontSize = 10, FontWeight = FontWeight.Bold, Foreground = TextTertiary }, content },
    };

    private ComboBox Combo(string[] items, int selected)
    {
        var c = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
        foreach (var it in items) c.Items.Add(it);
        c.SelectedIndex = selected;   // after Items are populated
        return c;
    }

    private TextBlock Mono(string text, IBrush fg)
    {
        var t = new TextBlock { Text = text, FontSize = 10, Foreground = fg, VerticalAlignment = VerticalAlignment.Center };
        t.BindResource(TextBlock.FontFamilyProperty, "Font.Mono");
        return t;
    }

    // A working pill toggle (label + optional "16-bit only" style hint). `enabled`
    // lets a toggle grey out and force off when it doesn't apply (dither vs format
    // and bit depth).
    private Control WorkingToggle(string label, Func<bool> get, Action<bool> set,
                                  Func<bool>? enabled = null, string? hint = null)
    {
        var knob = new Border { Width = 26, Height = 15, CornerRadius = NotaRadius.Body, Background = Raised, BorderBrush = BorderStrong, BorderThickness = new Thickness(1) };
        var dot = new Border { Width = 11, Height = 11, CornerRadius = NotaRadius.Panel, Background = TextTertiary, VerticalAlignment = VerticalAlignment.Center };
        knob.Child = dot;
        var text = new TextBlock { Text = label, FontSize = 11, Foreground = TextSecondary, VerticalAlignment = VerticalAlignment.Center };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center, Cursor = new Cursor(StandardCursorType.Hand), Children = { knob, text } };
        if (hint is not null)
            row.Children.Add(new TextBlock { Text = "· " + hint, FontSize = 9, Foreground = TextTertiary, VerticalAlignment = VerticalAlignment.Center });

        void Paint()
        {
            bool on = get();
            knob.Background = on ? Brass : Raised;
            dot.Background = on ? OnAccent : TextTertiary;
            dot.HorizontalAlignment = on ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            dot.Margin = on ? new Thickness(0, 0, 2, 0) : new Thickness(2, 0, 0, 0);
            text.Foreground = on ? TextPrimary : TextSecondary;
        }
        void Sync()   // reflect enabled-state (dither only for 16-bit targets)
        {
            bool en = enabled?.Invoke() ?? true;
            if (!en && get()) { set(false); }
            Paint();
            Inactive.Set(row, !en);
        }
        row.PointerPressed += (_, _) => { set(!get()); Paint(); };
        if (enabled is not null) _refreshDitherEnabled += Sync;
        Sync();
        return row;
    }

    // The release-tail option: the shell switch (a themed CheckBox), word to the right.
    private Control TailCheck()
    {
        var sw = new CheckBox { Content = "Add 1-bar release tail", IsChecked = _tailOn, VerticalAlignment = VerticalAlignment.Center };
        sw.IsCheckedChanged += (_, _) => { _tailOn = sw.IsChecked == true; RefreshEstimate(); };
        return sw;
    }

    private Control StemsBlock()
    {
        // A working pill toggle: one file per track (in the export format) into a
        // chosen folder (M6-5).
        var knob = new Border { Width = 26, Height = 15, CornerRadius = NotaRadius.Body, Background = Raised, BorderBrush = BorderStrong, BorderThickness = new Thickness(1) };
        var dot = new Border { Width = 11, Height = 11, CornerRadius = NotaRadius.Panel, Background = TextTertiary, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 0, 0) };
        knob.Child = dot;
        void Paint()
        {
            knob.Background = _stems ? Brass : Raised;
            dot.Background = _stems ? OnAccent : TextTertiary;
            dot.HorizontalAlignment = _stems ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            dot.Margin = _stems ? new Thickness(0, 0, 2, 0) : new Thickness(2, 0, 0, 0);
            PaintExportLabel();
            RefreshEstimate();
        }
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                knob,
                new TextBlock { Text = "Export stems", FontSize = 11, FontWeight = FontWeight.Medium, Foreground = TextPrimary, VerticalAlignment = VerticalAlignment.Center },
                new TextBlock { Text = "· one file per track, post-fader", FontSize = 9, Foreground = TextTertiary, VerticalAlignment = VerticalAlignment.Center },
            },
        };
        var headerBar = new Border { Height = 28, Background = NotaPalette.SurfaceInset, BorderBrush = BorderDef, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(10, 0), Cursor = new Cursor(StandardCursorType.Hand), Child = header };
        headerBar.PointerPressed += (_, _) => { _stems = !_stems; Paint(); };
        var box = new StackPanel { Children = { headerBar } };
        return new Border { BorderBrush = BorderDef, BorderThickness = new Thickness(1), CornerRadius = NotaRadius.Panel, ClipToBounds = true, Child = box };
    }

    private Control Segmented((string label, bool enabled)[] items, int active, Action<int> onSelect)
    {
        var inner = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        var cells = new Border[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            int idx = i;
            var t = new TextBlock { Text = items[i].label, FontSize = 10, VerticalAlignment = VerticalAlignment.Center };
            var cell = new Border { CornerRadius = NotaRadius.Badge, Padding = new Thickness(10, 2), Child = t };
            if (items[i].enabled) { cell.Cursor = new Cursor(StandardCursorType.Hand); cell.PointerPressed += (_, _) => { onSelect(idx); Paint(idx); }; }
            else t.Foreground = NotaPalette.TextDisabled;
            cells[i] = cell;
            inner.Children.Add(cell);
        }
        void Paint(int a)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                bool on = i == a;
                cells[i].Background = on ? Brass : Brushes.Transparent;
                ((TextBlock)cells[i].Child!).Foreground = on ? OnAccent : TextSecondary;
                ((TextBlock)cells[i].Child!).FontWeight = on ? FontWeight.SemiBold : FontWeight.Normal;
            }
        }
        Paint(active);
        return new Border { Background = Sunken, BorderBrush = BorderDef, BorderThickness = new Thickness(1), CornerRadius = NotaRadius.Tile, Padding = new Thickness(2), Child = inner, VerticalAlignment = VerticalAlignment.Center };
    }
}
