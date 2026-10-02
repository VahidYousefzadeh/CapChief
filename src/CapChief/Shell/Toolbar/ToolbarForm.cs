namespace CapChief.Shell;

/// <summary>
/// Represents the floating toolbar window that offers the capture and editing controls.
/// </summary>
public sealed class ToolbarForm : Form
{
    private const string ClassName = "CCPal";

    private const uint TipDelayMs = 550;
    private const uint SweepMs = 420;
    private const int Sweeps = 2;

    private readonly ToolbarPlacement _placement;
    private readonly ShadedPanel _panel;
    private readonly ToolTipForm _tip;
    private readonly Ticker _gloss = new(Timers.Sweep, SweepMs, Sweeps);

    private readonly ToastStrip _toast = new();
    private readonly Dictionary<Mode, ToolbarStrip> _strips = new();

    public event Action? ToastEnded;

    public event Action<Control?>? Pressed;

    private ToolbarStrip? _shown;
    private Rect? _beside;
    private bool _hiddenToTray;

    public ToolbarForm(AppIcon appIcon, ToolbarPlacement placement, Desktop desk)
        : base(new WindowKind(ClassName, appIcon, doubleClicks: false, redrawOnResize: false, ownsBackground: true, tracksLeaving: true))
    {
        _placement = placement;
        _tip = new ToolTipForm(appIcon);

        _panel = new ShadedPanel(Theme.BarRadius);
        Content = _panel;

        _panel.MouseDown += (_, e) =>
        {
            BeginDrag(MousePointer.At);
            e.Handled = true;
        };
        _panel.MouseMove += (_, _) => DragWindow();
        _panel.MouseUp += (_, _) => EndDragging();

        ToolTip = new ToolTip(_tip, desk.MonitorAt, Timers.Tip)
        {
            Content = _tip.Label,
            Delay = TipDelayMs,
            Gap = Theme.TipGap,
        };
    }

    public bool IsHiddenToTray
    {
        get
        {
            return _hiddenToTray;
        }
        set
        {
            if (_hiddenToTray == value)
            {
                return;
            }
            _hiddenToTray = value;
            if (_shown is not AnnotateStrip)
            {
                Show(Mode.Idle, default);
            }
        }
    }

    public Rect PanelOnScreen
    {
        get
        {
            return Rect.At(_placement.X, _placement.Y, _placement.Width, _placement.Height);
        }
    }

    private int PanelMargin
    {
        get
        {
            return Theme.PanelMargin.Px(Scale);
        }
    }

    public void Create(Window owner, params (Mode Mode, ToolbarStrip Strip)[] strips)
    {
        foreach ((Mode mode, ToolbarStrip strip) in strips)
        {
            _strips[mode] = strip;
            _panel.Add(strip);
        }
        _panel.Add(_toast);

        Open(FloatingSpec with { TakesFocus = false }, owner);

        _tip.Create(Window);
        Show(Mode.Idle, default);
    }

    public Rect ColumnOnScreen(Command command)
    {
        Rect panel = PanelOnScreen;
        if (_shown?.ButtonFor(command) is not { } button)
        {
            return panel;
        }
        Rect b = OnScreen(button.Bounds);
        return panel with { Left = b.Left, Right = b.Right };
    }

    public void Show(Mode mode, Rect crop)
    {
        ToolbarStrip? strip;
        if (_hiddenToTray && mode == Mode.Idle)
        {
            strip = null;
        }
        else
        {
            strip = _strips.GetValueOrDefault(mode);
        }
        Rect? beside;
        if (mode is Mode.Annotating or Mode.Reviewing)
        {
            beside = crop;
        }
        else
        {
            beside = null;
        }
        ShowStrip(strip, beside);
    }

    public void Hide()
    {
        ShowStrip(null, null);
    }

    public void Toast(string message)
    {
        _toast.Say(message);
        ShowStrip(_toast, null);
        Window.StartTimer(Timers.Toast, Timers.ToastMs);
    }

    public void Raise()
    {
        if (IsShown)
        {
            Window.Raise();
        }
    }

    public void ShowFromTray()
    {
        bool showing = _shown is not null;
        IsHiddenToTray = false;
        Raise();
        if (!showing)
        {
            return;
        }
        _gloss.Begin(Window);
        Invalidate();
    }

    public void HideForDialog()
    {
        CancelToolTip();
        Window.Hide();
    }

    public void ShowAfterDialog()
    {
        Window.Show();
    }

    public Pixmap ShowTipFor(Command command)
    {
        HoverOver(_shown?.ButtonFor(command));
        ShowToolTip();
        return _tip.Pixels;
    }

    public void HideTip()
    {
        CancelToolTip();
    }

    public void KeepTipsClearOf(Rect area)
    {
        ToolTip!.Avoid = area;
        CancelToolTip();
    }

    public void Close()
    {
        if (Window.Exists)
        {
            Window.Destroy();
        }
    }

    public override void Free()
    {
        base.Free();
        _tip.Free();
    }

    public override void OnMouseDown(Point at, bool shift)
    {
        Pressed?.Invoke(Content.HitTest(at));
        base.OnMouseDown(at, shift);
    }

    public override void OnTimer(int id)
    {
        if (id == Timers.Sweep)
        {
            _gloss.Advance(Window);
            Invalidate();
        }
        else if (id == Timers.Toast)
        {
            Window.StopTimer(Timers.Toast);
            ToastEnded?.Invoke();
        }
        else
        {
            base.OnTimer(id);
        }
    }

    private void ShowStrip(ToolbarStrip? strip, Rect? beside)
    {
        Window.StopTimer(Timers.Toast);
        _gloss.End(Window);
        CancelToolTip();
        _shown = strip;
        _beside = beside;
        ResetMouse();
        if (strip is null)
        {
            Window.Hide();
            return;
        }

        foreach (ToolbarStrip s in _strips.Values)
        {
            s.Visibility = Control.VisibleIf(s == strip);
        }
        _toast.Visibility = Control.VisibleIf(_toast == strip);
        Reposition(beside);
    }

    protected override void OnRendered(DrawContext draw)
    {
        if (!_gloss.IsRunning)
        {
            return;
        }
        GlossRaster.Sweep(draw.Pixels, PanelMargin, _gloss.Progress, Theme.GlossSweep);
    }

    protected override void Rescale()
    {
        if (_shown is not null)
        {
            Rect? beside;
            if (IsDragging)
            {
                beside = null;
            }
            else
            {
                beside = _beside;
            }
            Reposition(beside);
        }
    }

    private void Reposition(Rect? beside)
    {
        Size size = Layout(WidthLimit(_beside));
        _placement.Resize(ContentBounds.Width, ContentBounds.Height, Theme.CropGap.Px(Scale));
        _placement.Place(beside);

        int m = PanelMargin;
        PlaceAndShow(_placement.X - m, _placement.Y - m, size);
        Invalidate();
    }

    private void DragWindow()
    {
        int m = PanelMargin;
        Point at = DraggedTo(MousePointer.At);
        _placement.MoveTo(at.X + m, at.Y + m);
        MoveTo(_placement.X - m, _placement.Y - m);
    }

    private void EndDragging()
    {
        EndDrag();
        if (_beside is null && _shown is not null)
        {
            _placement.KeepAsIdleSpot();
        }
    }

    private Size WidthLimit(Rect? crop)
    {
        if (crop is { } c)
        {
            return new(c.Width + 2 * PanelMargin, Unbounded.Height);
        }
        else
        {
            return Unbounded;
        }
    }
}
