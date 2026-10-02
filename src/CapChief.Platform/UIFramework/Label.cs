namespace CapChief.UIFramework;

/// <summary>
/// Represents a control that displays a single line of text.
/// </summary>
public sealed class Label : Control
{
    public string Text { get; set; } = "";

    protected override Size MeasureOverride(Size available)
    {
        return Draw.Measure(Text, Font);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Text.Length > 0)
        {
            e.Draw.Text(Text, Bounds, Foreground, Font);
        }
    }
}
