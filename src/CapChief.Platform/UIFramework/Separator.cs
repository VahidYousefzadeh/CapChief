namespace CapChief.UIFramework;

/// <summary>
/// Represents a thin vertical line that divides groups of controls.
/// </summary>
public sealed class Separator : Control
{
    public Length LineWidth { get; set; } = Length.Of(1f);

    protected override void OnPaint(PaintEventArgs e)
    {
        Rect line = Padding.Shrink(Bounds, Scale);
        float w = LineWidth.Exact(Scale);
        e.Draw.Fill(line.Left + (line.Width - w) / 2f, line.Top, w, line.Height, Foreground);
    }
}
