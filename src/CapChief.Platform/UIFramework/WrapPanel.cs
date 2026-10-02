namespace CapChief.UIFramework;

/// <summary>
/// Arranges child controls from left to right in rows, starting a new row when the next one would not
/// fit or the row already holds as many as it may.
/// </summary>
public sealed class WrapPanel : Panel
{
    private readonly List<Point> _at = new();

    public Length Spacing { get; set; }

    public int MaxPerRow { get; set; } = int.MaxValue;

    protected override Size MeasureOverride(Size available)
    {
        int gap = Spacing.Px(Scale, 0);
        int x = 0;
        int y = 0;
        int inRow = 0;
        int rowHeight = 0;
        int width = 0;
        int height = 0;
        _at.Clear();

        foreach (Control c in Children)
        {
            c.Measure(available, Draw);
            _at.Add(default);
            if (c.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            Size s = c.DesiredSize;
            if (inRow > 0 && (inRow == MaxPerRow || x + gap + s.Width > available.Width))
            {
                y += rowHeight + gap;
                x = inRow = rowHeight = 0;
            }
            if (inRow > 0)
            {
                x += gap;
            }

            _at[^1] = new Point(x, y);
            x += s.Width;
            inRow++;
            rowHeight = Math.Max(rowHeight, s.Height);
            width = Math.Max(width, x);
            height = Math.Max(height, y + s.Height);
        }

        return new Size(width, height);
    }

    protected override void ArrangeOverride(Rect content)
    {
        for (int i = 0; i < Children.Count; i++)
        {
            Control c = Children[i];
            if (c.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            int x = content.Left + _at[i].X;
            int y = content.Top + _at[i].Y;
            c.Arrange(Rect.At(x, y, c.DesiredSize.Width, c.DesiredSize.Height));
        }
    }
}
