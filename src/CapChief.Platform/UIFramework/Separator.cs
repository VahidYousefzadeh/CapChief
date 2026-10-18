namespace CapChief.UIFramework;

/// <summary>
/// Represents a thin line that divides groups of controls: upright by default, or lying flat between
/// rows.
/// </summary>
public sealed class Separator : Control
{
    public Length LineWidth { get; set; } = Length.Of(1f);

    public Orientation Orientation { get; set; } = Orientation.Vertical;

    protected override void OnPaint(PaintEventArgs e)
    {
        Rect line = Padding.Shrink(Bounds, Scale);
        float w = LineWidth.Exact(Scale);
        if (Orientation == Orientation.Horizontal)
        {
            e.Draw.Fill(line.Left, line.Top + (line.Height - w) / 2f, line.Width, w, Foreground);
        }
        else
        {
            e.Draw.Fill(line.Left + (line.Width - w) / 2f, line.Top, w, line.Height, Foreground);
        }
    }
}
