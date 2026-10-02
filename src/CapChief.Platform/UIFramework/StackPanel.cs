namespace CapChief.UIFramework;

/// <summary>
/// Arranges child controls in a single horizontal or vertical line.
/// </summary>
public class StackPanel : Panel
{
    private readonly List<int> _runs = new();
    private readonly List<int> _lead = new();

    public Orientation Orientation { get; set; }

    public Length Spacing { get; set; }

    private bool IsHorizontal
    {
        get
        {
            return Orientation == Orientation.Horizontal;
        }
    }

    protected virtual bool Spaced(Control before, Control after)
    {
        return true;
    }

    protected override Size MeasureOverride(Size available)
    {
        _runs.Clear();
        _lead.Clear();
        int run = 0;
        int cross = 0;

        foreach (Control c in Children)
        {
            _runs.Add(0);
            _lead.Add(0);
            if (Filling(c))
            {
                continue;
            }

            c.Measure(available, Draw);
            if (c.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            _runs[^1] = Along(c.DesiredSize);
            run += _runs[^1];
            cross = Math.Max(cross, Across(c.DesiredSize));
        }

        run += Gaps();

        for (int i = 0; i < Children.Count; i++)
        {
            Control c = Children[i];
            if (!Filling(c))
            {
                continue;
            }

            c.Measure(available, Draw);
            if (c.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            _runs[i] = Math.Clamp(Along(available) - run, c.MinWidth.Px(Scale, 0), c.MaxWidth.Px(Scale, int.MaxValue));
            run += _runs[i];
            cross = Math.Max(cross, Across(c.DesiredSize));
        }

        if (IsHorizontal)
        {
            return new Size(run, cross);
        }
        else
        {
            return new Size(cross, run);
        }
    }

    protected override void ArrangeOverride(Rect content)
    {
        int at;
        if (IsHorizontal)
        {
            at = content.Left;
        }
        else
        {
            at = content.Top;
        }

        for (int i = 0; i < Children.Count; i++)
        {
            Control c = Children[i];
            if (c.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            at += _lead[i];
            int run = _runs[i];
            Rect cell;
            if (IsHorizontal)
            {
                cell = Rect.At(at, content.Top, run, content.Height);
            }
            else
            {
                cell = Rect.At(content.Left, at, content.Width, run);
            }

            c.Arrange(cell);
            at += run;
        }
    }

    private int Gaps()
    {
        int gap = Spacing.Px(Scale, 0);
        if (gap <= 0)
        {
            return 0;
        }

        int total = 0;
        Control? before = null;
        for (int i = 0; i < Children.Count; i++)
        {
            Control c = Children[i];
            if (c.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            if (before is not null && Inset(c) == 0 && Spaced(before, c))
            {
                _lead[i] = gap;
                total += gap;
            }
            before = c;
        }
        return total;
    }

    private int Inset(Control c)
    {
        if (IsHorizontal)
        {
            return c.Margin.Width(Scale);
        }
        else
        {
            return c.Margin.Height(Scale);
        }
    }

    private bool Filling(Control c)
    {
        return IsHorizontal && c.HorizontalAlignment == HorizontalAlignment.Stretch && c.Width.IsAuto;
    }

    private int Along(Size s)
    {
        if (IsHorizontal)
        {
            return s.Width;
        }
        else
        {
            return s.Height;
        }
    }

    private int Across(Size s)
    {
        if (IsHorizontal)
        {
            return s.Height;
        }
        else
        {
            return s.Width;
        }
    }
}

/// <summary>
/// Specifies whether controls are laid out horizontally or vertically.
/// </summary>
public enum Orientation
{
    Horizontal,
    Vertical,
}
