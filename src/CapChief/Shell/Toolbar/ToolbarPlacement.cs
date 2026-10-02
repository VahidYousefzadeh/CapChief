namespace CapChief.Shell;

/// <summary>
/// Calculates where the toolbar should be placed on the screen: beside the open capture, or where it
/// stands while the app is idle, which only a drag of the idle toolbar changes.
/// </summary>
public sealed class ToolbarPlacement
{
    private const double IdleYFraction = 0.38;

    private const int OnMonitorMargin = 8;

    private readonly Desktop _desk;

    private Point? _idleAt;
    private int _gap;

    public ToolbarPlacement(Desktop desk)
    {
        _desk = desk;
    }

    public int X { get; private set; }
    public int Y { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public void Resize(int w, int h, int gap)
    {
        Width = w;
        Height = h;
        _gap = gap;
    }

    public void Place(Rect? beside)
    {
        Point at;
        if (beside is { } c)
        {
            at = Beside(c);
        }
        else
        {
            at = IdleSpot();
        }
        MoveTo(at.X, at.Y);
    }

    public void KeepAsIdleSpot()
    {
        _idleAt = new Point(X, Y);
    }

    private Point IdleSpot()
    {
        if (_idleAt is { } at && OnAMonitor(at))
        {
            return at;
        }
        Rect primary = _desk.ScreenPrimary;
        var spot = new Point(primary.Left + (primary.Width - Width) / 2, primary.Top + (int)((primary.Height - Height) * IdleYFraction));
        _idleAt = spot;
        return spot;
    }

    private bool OnAMonitor(Point at)
    {
        Point corner = at + new Point(OnMonitorMargin, OnMonitorMargin);
        foreach (Rect monitor in _desk.ScreenMonitors)
        {
            if (monitor.Contains(corner))
            {
                return true;
            }
        }
        return false;
    }

    private Point Beside(Rect c)
    {
        Rect crop = _desk.ToScreen(c);
        ReadOnlySpan<Point> tries =
        [
            new(crop.Right + _gap, crop.Bottom - Height),
            new(crop.Right - Width, crop.Bottom + _gap),
            new(crop.Right - Width, crop.Top - _gap - Height),
            new(crop.Left - _gap - Width, crop.Bottom - Height),
        ];
        foreach (Point t in tries)
        {
            Rect at = OnMonitor(t.X, t.Y);
            if (!at.Overlaps(crop))
            {
                return at.TopLeft;
            }
        }
        return OnMonitor(crop.Right - _gap - Width, crop.Top + _gap).TopLeft;
    }

    private Rect OnMonitor(int x, int y)
    {
        Rect at = Rect.At(x, y, Width, Height);
        return Rect.Within(at, _desk.MonitorAt(at.Center));
    }

    public void MoveTo(int x, int y)
    {
        Rect at = OnScreen(x, y);
        X = at.Left;
        Y = at.Top;
    }

    private Rect OnScreen(int x, int y)
    {
        return Rect.Within(Rect.At(x, y, Width, Height), _desk.ScreenBounds);
    }
}
