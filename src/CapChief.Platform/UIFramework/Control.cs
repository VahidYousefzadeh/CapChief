namespace CapChief.UIFramework;

/// <summary>
/// Represents the base class for all controls that participate in layout and painting.
/// </summary>
public abstract class Control
{
    private DrawContext _draw = null!;
    private Font _font;
    private Color _foreground;

    public event EventHandler<MouseEventArgs>? MouseDown;

    public event EventHandler<MouseEventArgs>? MouseMove;

    public event EventHandler<MouseEventArgs>? MouseUp;

    public Thickness Margin { get; set; }

    public Thickness Padding { get; set; }

    public Length Width { get; set; } = Length.Auto;

    public Length Height { get; set; } = Length.Auto;

    public Length MinWidth { get; set; } = Length.Auto;

    public Length MinHeight { get; set; } = Length.Auto;

    public Length MaxWidth { get; set; } = Length.Auto;

    public HorizontalAlignment HorizontalAlignment { get; set; }

    public VerticalAlignment VerticalAlignment { get; set; }

    public Visibility Visibility { get; set; }

    public static Visibility VisibleIf(bool on)
    {
        if (on)
        {
            return Visibility.Visible;
        }
        else
        {
            return Visibility.Collapsed;
        }
    }

    public Color Background { get; set; }

    public Color Foreground
    {
        get
        {
            for (Control? c = this; c is not null; c = c.Parent)
            {
                if (!c._foreground.IsTransparent)
                {
                    return c._foreground;
                }
            }
            return default;
        }
        set
        {
            _foreground = value;
        }
    }

    public CornerRadius CornerRadius { get; set; } = CornerRadius.None;

    public string ToolTip { get; set; } = "";

    public CursorShape? Cursor { get; set; }

    public Font Font
    {
        get
        {
            for (Control? c = this; c is not null; c = c.Parent)
            {
                if (!c._font.IsUnset)
                {
                    return c._font;
                }
            }
            return default;
        }
        set
        {
            _font = value;
        }
    }

    public Control? Parent { get; internal set; }

    public Size DesiredSize { get; private set; }

    public Rect Bounds { get; private set; }

    protected bool IsMouseOver { get; private set; }

    protected DrawContext Draw
    {
        get
        {
            return _draw;
        }
    }

    protected float Scale
    {
        get
        {
            return _draw.Scale;
        }
    }

    private Size LaidOutSize
    {
        get
        {
            return new(DesiredSize.Width - Margin.Width(Scale), DesiredSize.Height - Margin.Height(Scale));
        }
    }

    public void Measure(Size available, DrawContext draw)
    {
        _draw = draw;
        float scale = draw.Scale;
        if (Visibility == Visibility.Collapsed)
        {
            DesiredSize = default;
            return;
        }

        Size inner = Padding.Shrink(Margin.Shrink(available, scale), scale);
        Size desiredSize = Padding.Grow(MeasureOverride(inner), scale);

        int w = Math.Clamp(Width.Px(scale, desiredSize.Width), MinWidth.Px(scale, 0), MaxWidth.Px(scale, int.MaxValue));
        int h = Math.Max(Height.Px(scale, desiredSize.Height), MinHeight.Px(scale, 0));
        DesiredSize = Margin.Grow(new Size(w, h), scale);
    }

    public void Arrange(Rect final)
    {
        if (Visibility == Visibility.Collapsed)
        {
            Bounds = default;
            return;
        }

        Rect slot = Margin.Shrink(final, Scale);
        int w;
        if (HorizontalAlignment == HorizontalAlignment.Stretch && Width.IsAuto)
        {
            w = slot.Width;
        }
        else
        {
            w = LaidOutSize.Width;
        }
        int h;
        if (VerticalAlignment == VerticalAlignment.Stretch && Height.IsAuto)
        {
            h = slot.Height;
        }
        else
        {
            h = LaidOutSize.Height;
        }

        Bounds = Rect.At(slot.Left, slot.Top, w, h);
        ArrangeOverride(Padding.Shrink(Bounds, Scale));
    }

    public void Render(PaintEventArgs e)
    {
        if (Visibility == Visibility.Visible)
        {
            OnPaint(e);
        }
    }

    public void RenderBackground(PaintEventArgs e)
    {
        if (Visibility == Visibility.Visible)
        {
            OnPaintBackground(e);
        }
    }

    public Control? HitTest(Point p)
    {
        if (Visibility != Visibility.Visible || !Bounds.Contains(p))
        {
            return null;
        }
        return ChildAt(p) ?? this;
    }

    public virtual void Refresh() { }

    public virtual CursorShape? CursorAt(Point p)
    {
        return Cursor;
    }

    public virtual void OnMouseEnter(EventArgs e)
    {
        IsMouseOver = true;
    }

    public virtual void OnMouseLeave(EventArgs e)
    {
        IsMouseOver = false;
    }

    public virtual void OnMouseDown(MouseEventArgs e)
    {
        MouseDown?.Invoke(this, e);
    }

    public virtual void OnMouseMove(MouseEventArgs e)
    {
        MouseMove?.Invoke(this, e);
    }

    public virtual void OnMouseUp(MouseEventArgs e)
    {
        MouseUp?.Invoke(this, e);
    }

    public virtual void OnClick(EventArgs e) { }

    protected virtual Size MeasureOverride(Size available)
    {
        return default;
    }

    protected virtual void ArrangeOverride(Rect content) { }

    protected virtual void OnPaint(PaintEventArgs e)
    {
        e.Draw.Fill(Bounds, Background, CornerRadius);
    }

    protected virtual void OnPaintBackground(PaintEventArgs e) { }

    protected virtual Control? ChildAt(Point p)
    {
        return null;
    }
}

/// <summary>
/// Specifies how a control is positioned horizontally within the space available to it.
/// </summary>
public enum HorizontalAlignment
{
    Left,
    Stretch,
}

/// <summary>
/// Specifies how a control is positioned vertically within the space available to it.
/// </summary>
public enum VerticalAlignment
{
    Top,
    Stretch,
}

/// <summary>
/// Specifies whether a control is displayed or left out of layout.
/// </summary>
public enum Visibility
{
    Visible,
    Collapsed,
}
