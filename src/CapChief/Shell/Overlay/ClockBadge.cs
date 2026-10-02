namespace CapChief.Shell;

/// <summary>
/// Draws the small clock badge that marks a delayed capture.
/// </summary>
public sealed class ClockBadge
{
    private const int SideFloorPx = 26;
    private const float SideDip = 36f;

    private const int GapFloorPx = 6;
    private const float GapDip = 10f;

    private const float GlyphShare = 0.26f;

    private readonly AlphaSurface _surf = new(highQualityPixelOffset: true);
    private readonly Metrics _metrics;

    private float _builtAt;

    public ClockBadge(Metrics metrics)
    {
        _metrics = metrics;
    }

    private int Size
    {
        get
        {
            return Length.AtLeast(SideFloorPx, SideDip).Px(_metrics.Scale);
        }
    }

    private int Gap
    {
        get
        {
            return Length.AtLeast(GapFloorPx, GapDip).Px(_metrics.Scale);
        }
    }

    public Rect Box(Rect crop)
    {
        int w = Size;
        int gap = Gap;
        if (crop.Width < w + 2 * gap || crop.Height < w + 2 * gap)
        {
            return default;
        }
        return Rect.At(crop.Right - w - gap, crop.Top + gap, w, w);
    }

    public void Free()
    {
        _surf.Free();
    }

    public Rect DrawInLayer(Layer layer, Rect r, Rect crop)
    {
        if (!layer.IsOpen || r.Width <= 0 || !Build())
        {
            return default;
        }
        Rect at = Rect.At(r.Left, r.Top, _surf.Width, _surf.Width);
        layer.DrawOver(_surf.Surface, at, crop, false);
        return at;
    }

    private bool Build()
    {
        int w = Size;
        if (_surf.IsOpen && _surf.Width == w && _builtAt == _metrics.Scale)
        {
            return true;
        }
        if (!_surf.Open(w, w))
        {
            return false;
        }
        _builtAt = _metrics.Scale;

        int m = _metrics.LabelMargin;
        var target = new Pixmap(_surf.Surface.Pixels, w, w, w);
        PanelRaster.Gradient(target, m, Theme.PanelRadius.Exact(_metrics.Scale), Theme.PanelRim.Exact(_metrics.Scale), Palette.Panel);

        float h2 = w * 0.5f;
        ClockGlyph.Draw(_surf.DeviceContext, h2, h2, (w - 2 * m) * GlyphShare, Palette.OnPanelArgb, Theme.IconPen.Exact(_metrics.Scale));
        _surf.Seal();
        return true;
    }
}
