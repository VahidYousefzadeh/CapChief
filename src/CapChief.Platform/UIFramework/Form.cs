namespace CapChief.UIFramework;

/// <summary>
/// Represents a window that hosts a tree of controls and routes input to them.
/// </summary>
public class Form : IMouseHandler, IMouseLeaveHandler, ITimerHandler, IHitTestHandler, IDpiChangeHandler
{
    private const int InitialSidePx = 10;

    private readonly WindowKind _kind;
    private readonly BackBuffer _surface = new();
    private readonly DrawContext _draw;
    private readonly PaintEventArgs _paint;

    private Control? _hovered;
    private Control? _pressed;
    private Point _origin;
    private Point _grab;
    private bool _rescaling;
    private bool _dirty;
    private bool _painting;

    public Form(WindowKind kind)
    {
        _kind = kind;
        _draw = new DrawContext(_surface);
        _paint = new PaintEventArgs(_draw);
    }

    protected Control Content { get; set; } = new Panel();

    public float Scale { get; private set; } = 1f;

    public Window Window { get; private set; } = Window.None;

    public bool IsShown
    {
        get
        {
            return Window.Exists && Window.IsVisible;
        }
    }

    public ToolTip? ToolTip { get; set; }

    public Rect ContentBounds
    {
        get
        {
            return Content.Bounds;
        }
    }

    protected Control? Hovered
    {
        get
        {
            return _hovered;
        }
    }

    public static Size Unbounded
    {
        get
        {
            return new(int.MaxValue / 4, int.MaxValue / 4);
        }
    }

    private static Rect InitialBounds
    {
        get
        {
            return new() { Right = InitialSidePx, Bottom = InitialSidePx };
        }
    }

    protected static WindowSpec FloatingSpec
    {
        get
        {
            return new()
            {
                Bounds = InitialBounds,
                AlwaysOnTop = true,
                ShowsInTaskbar = false,
                PerPixelAlpha = true,
            };
        }
    }

    public bool IsDragging { get; private set; }

    public Pixmap Pixels
    {
        get
        {
            return _draw.Pixels;
        }
    }

    public void Open(WindowSpec spec, Window? owner)
    {
        Window = _kind.Open(spec, owner, this);
        Scale = Window.Scale;
    }

    public Size Layout(Size budget)
    {
        _draw.Scale = Scale;
        Content.Measure(budget, _draw);
        Size desiredSize = Content.DesiredSize;
        Content.Arrange(new Rect { Right = desiredSize.Width, Bottom = desiredSize.Height });

        _surface.Resize(desiredSize.Width, desiredSize.Height);
        Content.RenderBackground(_paint);
        _surface.Rebase();
        return desiredSize;
    }

    public void PlaceAt(int x, int y)
    {
        _origin = new Point(x, y);
    }

    public void MoveTo(int x, int y)
    {
        PlaceAt(x, y);
        Window.MoveTo(x, y);
    }

    protected void BeginDrag(Point cursor)
    {
        _grab = cursor - _origin;
        IsDragging = true;
    }

    protected Point DraggedTo(Point cursor)
    {
        return cursor - _grab;
    }

    protected void EndDrag()
    {
        IsDragging = false;
    }

    public void OnDpiChanged(float scale, Rect suggested)
    {
        if (_rescaling || scale == Scale)
        {
            return;
        }
        _rescaling = true;
        Scale = scale;
        PlaceAt(suggested.Left, suggested.Top);
        Rescale();
        _rescaling = false;
    }

    protected virtual void Rescale()
    {
        Size size = Layout(Unbounded);
        Window.PlaceAt(Rect.At(_origin.X, _origin.Y, size.Width, size.Height));
        Render();
    }

    protected void PlaceAndShow(int x, int y, Size size)
    {
        PlaceAt(x, y);
        Window.PlaceAndShow(Rect.At(x, y, size.Width, size.Height));
    }

    protected void ShowUnder(Rect anchor, int left, int gap, Rect screen, Size size)
    {
        Rect body = ContentBounds;
        int top = anchor.Bottom + gap;
        if (top + body.Height > screen.Bottom)
        {
            top = anchor.Top - gap - body.Height;
        }
        Rect at = Rect.Within(Rect.At(left, top, body.Width, body.Height), screen);
        PlaceAndShow(at.Left - body.Left, at.Top - body.Top, size);
        Invalidate();
    }

    public void Invalidate()
    {
        _dirty = true;
        if (_painting)
        {
            return;
        }

        _painting = true;
        while (_dirty)
        {
            _dirty = false;
            Render();
        }
        _painting = false;
    }

    private void Render()
    {
        if (!_surface.IsOpen)
        {
            return;
        }
        Content.Refresh();
        _surface.ResetToBase();
        Content.Render(_paint);
        OnRendered(_draw);
        _surface.Push(Window, _origin.X, _origin.Y);
    }

    public virtual void Free()
    {
        _draw.Free();
        _surface.Free();
    }

    protected virtual void OnRendered(DrawContext draw) { }

    public virtual HitZone OnHitTest()
    {
        if (Content.HitTest(Window.ToClient(MousePointer.At)) is null)
        {
            return HitZone.Transparent;
        }
        else
        {
            return HitZone.Client;
        }
    }

    public virtual CursorShape? OnCursorWanted()
    {
        return _hovered?.CursorAt(Window.ToClient(MousePointer.At)) ?? Content.Cursor;
    }

    public virtual void OnMouseMove(Point at, bool shift, bool ctrl)
    {
        var e = new MouseEventArgs(at);
        if (_pressed is { } held)
        {
            held.OnMouseMove(e);
            return;
        }
        if (!Hover(Content.HitTest(at)))
        {
            return;
        }

        ArmToolTip();
        Invalidate();
    }

    public virtual void OnMouseDown(Point at, bool shift)
    {
        CancelToolTip();

        var e = new MouseEventArgs(at);
        _pressed = null;
        for (Control? c = Content.HitTest(at); c is not null; c = c.Parent)
        {
            c.OnMouseDown(e);
            if (!e.Handled)
            {
                continue;
            }
            _pressed = c;
            break;
        }
        if (_pressed is null)
        {
            return;
        }

        Window.CaptureMouse();
        Invalidate();
    }

    public virtual void OnMouseUp(Point at)
    {
        if (_pressed is not { } held)
        {
            return;
        }
        _pressed = null;
        Window.ReleaseMouse();
        held.OnMouseUp(new MouseEventArgs(at));

        if (Under(Content.HitTest(at), held))
        {
            held.OnClick(EventArgs.Empty);
        }
        else
        {
            Invalidate();
        }
    }

    public virtual void OnMouseLeft()
    {
        CancelToolTip();
        if (Hover(null))
        {
            Invalidate();
        }
    }

    private static bool Under(Control? hit, Control held)
    {
        for (Control? c = hit; c is not null; c = c.Parent)
        {
            if (c == held)
            {
                return true;
            }
        }
        return false;
    }

    public virtual void OnTimer(int id)
    {
        if (ToolTip is { } tip && id == tip.TimerId)
        {
            ShowToolTip();
        }
    }

    protected void ResetMouse()
    {
        Hover(null);
        if (_pressed is not { } held)
        {
            return;
        }
        _pressed = null;
        Window.ReleaseMouse();
        held.OnMouseUp(new MouseEventArgs(Window.ToClient(MousePointer.At)));
    }

    protected void HoverOver(Control? c)
    {
        if (Hover(c))
        {
            Invalidate();
        }
    }

    protected void ShowToolTip()
    {
        if (ToolTip is not { } tip)
        {
            return;
        }
        Window.StopTimer(tip.TimerId);
        if (_hovered is { } c && c.ToolTip.Length > 0)
        {
            tip.Show(c.ToolTip, OnScreen(c.Bounds), OnScreen(Content.Bounds));
        }
    }

    protected void CancelToolTip()
    {
        if (ToolTip is not { } tip)
        {
            return;
        }
        tip.Hide();
        Window.StopTimer(tip.TimerId);
    }

    private void ArmToolTip()
    {
        if (ToolTip is not { } tip)
        {
            return;
        }
        CancelToolTip();
        if (_hovered is { ToolTip.Length: > 0 })
        {
            Window.StartTimer(tip.TimerId, tip.Delay);
        }
    }

    private bool Hover(Control? c)
    {
        if (c == _hovered)
        {
            return false;
        }
        _hovered?.OnMouseLeave(EventArgs.Empty);
        _hovered = c;
        _hovered?.OnMouseEnter(EventArgs.Empty);
        return true;
    }

    protected Rect OnScreen(Rect client)
    {
        return client.Moved(_origin);
    }
}
