namespace CapChief.Shell;

/// <summary>
/// Represents the rounded, shaded panel behind the toolbar, its drop-downs and its tooltips.
/// </summary>
public sealed class ShadedPanel : Panel
{
    public ShadedPanel(Length radius)
    {
        _radius = radius;
        Margin = Thickness.All(Theme.PanelMargin);
        Cursor = CursorShape.Arrow;
        Font = Theme.Label;
        Foreground = Color.FromArgb(Palette.OnPanelArgb);
    }

    private readonly Length _radius;

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        PanelRaster.Gradient(e.Draw.Pixels, Theme.PanelMargin.Px(Scale), _radius.Exact(Scale), Theme.PanelRim.Exact(Scale), Palette.Panel);
    }
}
