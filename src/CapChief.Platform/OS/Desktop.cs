using System.Runtime.InteropServices;

namespace CapChief.OS;

/// <summary>
/// Represents the virtual desktop and its monitors. Bounds is measured from the desktop's top-left
/// corner, like every point on the overlay. Origin, ScreenPrimary, ScreenMonitors, ScreenBounds and
/// the points that MonitorAt and ScaleAt take are in screen coordinates, measured from the primary
/// monitor's top-left corner.
/// </summary>
public sealed class Desktop
{
    private readonly bool _measured;
    private readonly float _scale;

    public Desktop(Rect bounds, Rect primary, IReadOnlyList<Rect> monitors, float scale = 0f)
        : this(bounds, primary, monitors, false)
    {
        _scale = scale;
    }

    private Desktop(Rect bounds, Rect primary, IReadOnlyList<Rect> monitors, bool measured)
    {
        _measured = measured;
        ScreenMonitors = monitors;
        Take(bounds, primary, monitors);
    }

    public Point Origin { get; private set; }

    public Size Size { get; private set; }

    public Rect ScreenPrimary { get; private set; }

    public IReadOnlyList<Rect> ScreenMonitors { get; private set; }

    public Rect Bounds { get; private set; }

    public Rect ScreenBounds
    {
        get
        {
            return Bounds.Moved(Origin);
        }
    }

    public static void MakeProcessDpiAware()
    {
        if (!Win32.SetProcessDpiAwarenessContext(Win32.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
        {
            Win32.SetProcessDPIAware();
        }
    }

    public static Desktop Measure()
    {
        List<Rect> monitors = MonitorsNow();
        return new Desktop(BoundsOf(monitors), PrimaryMonitor(), monitors, measured: true);
    }

    public bool Remeasure()
    {
        if (!_measured)
        {
            return false;
        }
        List<Rect> monitors = MonitorsNow();
        Rect bounds = BoundsOf(monitors);
        Rect primary = PrimaryMonitor();
        if (bounds == ScreenBounds && primary == ScreenPrimary && monitors.SequenceEqual(ScreenMonitors))
        {
            return false;
        }
        Take(bounds, primary, monitors);
        return true;
    }

    public Point ToScreen(Point p)
    {
        return p + Origin;
    }

    public Rect ToScreen(Rect r)
    {
        return r.Moved(Origin);
    }

    public Point FromScreen(Point p)
    {
        return p - Origin;
    }

    public Rect FromScreen(Rect r)
    {
        return r.Moved(-Origin);
    }

    public Rect MonitorAt(Point screen)
    {
        Rect nearest = ScreenBounds;
        long best = long.MaxValue;
        foreach (Rect m in ScreenMonitors)
        {
            long dx = Math.Max(0, Math.Max(m.Left - screen.X, screen.X - (m.Right - 1)));
            long dy = Math.Max(0, Math.Max(m.Top - screen.Y, screen.Y - (m.Bottom - 1)));
            long distance = dx * dx + dy * dy;
            if (distance >= best)
            {
                continue;
            }
            best = distance;
            nearest = m;
        }
        return nearest;
    }

    public float ScaleAt(Point screen)
    {
        if (_scale > 0f)
        {
            return _scale;
        }
        IntPtr monitor = Win32.MonitorFromPoint(screen, Win32.MONITOR_DEFAULTTONEAREST);
        if (Win32.GetDpiForMonitor(monitor, Win32.MDT_EFFECTIVE_DPI, out uint dpi, out _) != 0 || dpi == 0)
        {
            return 1f;
        }
        return Win32.ScaleFromDpi(dpi);
    }

    private void Take(Rect bounds, Rect primary, IReadOnlyList<Rect> monitors)
    {
        Origin = bounds.TopLeft;
        Size = bounds.Size;
        ScreenPrimary = primary;
        ScreenMonitors = monitors;
        Bounds = new Rect { Right = bounds.Width, Bottom = bounds.Height };
    }

    private static List<Rect> MonitorsNow()
    {
        var monitors = new List<Rect>();
        Win32.MonitorEnumProc onMonitor = (IntPtr monitor, IntPtr dc, ref Rect rc, IntPtr data) =>
        {
            monitors.Add(rc);
            return true;
        };
        Win32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, onMonitor, IntPtr.Zero);
        return monitors;
    }

    private static Rect BoundsOf(List<Rect> monitors)
    {
        Rect all = default;
        foreach (Rect m in monitors)
        {
            all = Rect.UnionIgnoringEmpty(all, m);
        }
        if (!all.IsEmpty)
        {
            return all;
        }

        return Rect.At(Win32.GetSystemMetrics(Win32.SM_XVIRTUALSCREEN), Win32.GetSystemMetrics(Win32.SM_YVIRTUALSCREEN), Win32.GetSystemMetrics(Win32.SM_CXVIRTUALSCREEN), Win32.GetSystemMetrics(Win32.SM_CYVIRTUALSCREEN));
    }

    private static Rect PrimaryMonitor()
    {
        IntPtr primary = Win32.MonitorFromPoint(new Point(), Win32.MONITOR_DEFAULTTOPRIMARY);
        var info = new Win32.MONITORINFO { cbSize = (uint)Marshal.SizeOf<Win32.MONITORINFO>() };
        if (Win32.GetMonitorInfoW(primary, ref info))
        {
            return info.rcMonitor;
        }
        else
        {
            return default;
        }
    }
}
