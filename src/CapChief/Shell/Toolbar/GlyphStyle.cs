namespace CapChief.Shell;

/// <summary>
/// Provides the shared color and line width used for drawing toolbar icons.
/// </summary>
public static class GlyphStyle
{
    public static readonly Color Foreground = Color.FromArgb(Palette.OnPanelArgb);

    public static readonly Color PanelMiddle = Color.FromArgb(Palette.PanelAt(0.5f));

    public static StrokeStyle IconStroke(DrawContext d)
    {
        return new(Foreground, Theme.IconPen.Exact(d.Scale));
    }
}
