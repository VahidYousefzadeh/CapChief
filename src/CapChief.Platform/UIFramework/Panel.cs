namespace CapChief.UIFramework;

/// <summary>
/// Provides a base class for controls that position and arrange child controls.
/// </summary>
public class Panel : Control
{
    private readonly List<Control> _children = new();

    protected IReadOnlyList<Control> Children
    {
        get
        {
            return _children;
        }
    }

    public void Add(Control child)
    {
        child.Parent = this;
        _children.Add(child);
    }

    public override void Refresh()
    {
        foreach (Control c in _children)
        {
            c.Refresh();
        }
    }

    protected override Size MeasureOverride(Size available)
    {
        int w = 0;
        int h = 0;
        foreach (Control c in _children)
        {
            c.Measure(available, Draw);
            w = Math.Max(w, c.DesiredSize.Width);
            h = Math.Max(h, c.DesiredSize.Height);
        }
        return new Size(w, h);
    }

    protected override void ArrangeOverride(Rect content)
    {
        foreach (Control c in _children)
        {
            c.Arrange(content);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        foreach (Control c in _children)
        {
            c.Render(e);
        }
    }

    protected override Control? ChildAt(Point p)
    {
        for (int i = _children.Count - 1; i >= 0; i--)
        {
            if (_children[i].HitTest(p) is { } hit)
            {
                return hit;
            }
        }
        return null;
    }
}
