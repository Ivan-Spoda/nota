// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 Egor Khindikaynen (Nota). See LICENSES/ for license terms.
//
// A device-card section (spec 012): no fill and no frame — it sits on the card surface —
// and a 1px Rule only on an edge it shares with a neighbouring section, so a card reads as
// one surface divided by rules instead of a stack of boxes. It keeps the 1px inset the old
// framed box's border gave its content (inside any Padding), so nothing moves.
// Graph windows and wells keep their own frame; this is only for sections.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Nota.App;

/// <summary>Which edges of a section carry the dividing rule.</summary>
[Flags]
internal enum SectionEdge
{
    None = 0,
    Top = 1,
    Left = 2,
    /// <summary>Find them: a rule on the top or left edge that faces a neighbouring section.</summary>
    Auto = 4,
}

internal sealed class RuleSection : Decorator
{
    // How far apart two sections may be and still count as neighbours (the card gutters are
    // 4–10), and how much they must overlap along the shared edge.
    private const double MaxGap = 16, MinOverlap = 4;
    // How far up the tree to look for neighbours: sections sit in a grid or stack, sometimes
    // inside a column of their own.
    private const int SearchDepth = 4;

    // Where the old box's 1px border was.
    private static readonly Thickness Inset = new(1);

    public SectionEdge Edges { get; init; } = SectionEdge.Auto;

    public RuleSection() => ClipToBounds = true;

    protected override Size MeasureOverride(Size availableSize)
    {
        var pad = Padding + Inset;
        if (Child is not { } c) return new Size(pad.Left + pad.Right, pad.Top + pad.Bottom);
        c.Measure(availableSize.Deflate(pad));
        return c.DesiredSize.Inflate(pad);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(finalSize).Deflate(Padding + Inset));
        return finalSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty) InvalidateVisual();   // neighbours are found from the layout
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var edges = Edges.HasFlag(SectionEdge.Auto) ? FindEdges() : Edges;
        if (edges == SectionEdge.None) return;
        var r = new Rect(Bounds.Size);
        if (edges.HasFlag(SectionEdge.Top)) context.FillRectangle(NotaPalette.Rule, new Rect(0, 0, r.Width, 1));
        if (edges.HasFlag(SectionEdge.Left)) context.FillRectangle(NotaPalette.Rule, new Rect(0, 0, 1, r.Height));
    }

    private SectionEdge FindEdges()
    {
        Visual? root = this;
        for (int i = 0; i < SearchDepth && root?.GetVisualParent() is { } p; i++) root = p;
        if (root is null || ReferenceEquals(root, this)) return SectionEdge.None;

        var edges = SectionEdge.None;
        double w = Bounds.Width, h = Bounds.Height;
        foreach (var v in root.GetVisualDescendants())
        {
            if (v is not RuleSection other || ReferenceEquals(other, this) || !other.IsVisible) continue;
            if (other.IsVisualAncestorOf(this) || this.IsVisualAncestorOf(other)) continue;
            if (other.TranslatePoint(default, this) is not { } o) continue;
            double ow = other.Bounds.Width, oh = other.Bounds.Height;
            double gapLeft = -(o.X + ow), gapTop = -(o.Y + oh);
            double vOverlap = Math.Min(o.Y + oh, h) - Math.Max(o.Y, 0);
            double hOverlap = Math.Min(o.X + ow, w) - Math.Max(o.X, 0);
            if (gapLeft >= -0.5 && gapLeft <= MaxGap && vOverlap >= MinOverlap) edges |= SectionEdge.Left;
            if (gapTop >= -0.5 && gapTop <= MaxGap && hOverlap >= MinOverlap) edges |= SectionEdge.Top;
        }
        return edges;
    }
}
