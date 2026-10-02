using System.Runtime.InteropServices;

namespace CapChief.OS;

/// <summary>
/// Finds the window, monitor or user interface element under a point on the desktop.
/// </summary>
public sealed class WindowFinder
{
    private readonly Desktop _desk;
    private readonly ElementScan _scan;
    private readonly int _minSize;

    private readonly List<WindowOnScreen> _windows = new();

    private readonly Win32.WindowEnumProc _onWindow;
    private readonly char[] _className = new char[64];
    private readonly uint _ourProcess = Win32.GetCurrentProcessId();

    public WindowFinder(Desktop desk, ElementScan scan, int minSize)
    {
        _desk = desk;
        _scan = scan;
        _minSize = minSize;
        _onWindow = OnWindow;
    }

    public void FindWindows()
    {
        _windows.Clear();
        Win32.EnumWindows(_onWindow, IntPtr.Zero);
        foreach (Rect monitor in _desk.ScreenMonitors)
        {
            Add(monitor, IntPtr.Zero);
        }
    }

    public Rect WindowAt(Point p)
    {
        return TopmostAt(p)?.Bounds ?? default;
    }

    public Rect ElementAt(Point p)
    {
        return TopmostAt(p)?.ElementAt(p) ?? default;
    }

    public void ScanElementsAt(Point p)
    {
        if (TopmostAt(p) is not { } window || window.IsMonitor)
        {
            return;
        }
        if (window.Innermost.Contains(p))
        {
            return;
        }
        _scan.Request(window.Handle, _desk.ToScreen(p));
    }

    public bool TakeScannedElements()
    {
        if (_scan.TakeResult() is not { } scanned)
        {
            return false;
        }

        int i = _windows.FindIndex(w => w.Handle == scanned.Window);
        if (i < 0)
        {
            return false;
        }

        var elements = new Rect[scanned.Elements.Count];
        for (int n = 0; n < elements.Length; n++)
        {
            elements[n] = Rect.Intersect(_desk.FromScreen(scanned.Elements[n]), _desk.Bounds);
        }
        _windows[i] = _windows[i] with
        {
            Elements = elements,
            Innermost = _desk.FromScreen(scanned.Innermost),
        };
        return true;
    }

    public void CancelScan()
    {
        _scan.Cancel();
    }

    private WindowOnScreen? TopmostAt(Point p)
    {
        return _windows.Find(w => w.Bounds.Contains(p));
    }

    private bool OnWindow(IntPtr h, IntPtr _)
    {
        if (!Win32.IsWindowVisible(h) || Win32.IsIconic(h))
        {
            return true;
        }
        Win32.GetWindowThreadProcessId(h, out uint process);
        if (process == _ourProcess)
        {
            return true;
        }

        if (Win32.DwmGetWindowAttribute(h, Win32.DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0)
        {
            return true;
        }

        if (ClassOf(h) is "Progman" or "WorkerW")
        {
            return true;
        }

        if (Win32.DwmGetWindowAttribute(h, Win32.DWMWA_EXTENDED_FRAME_BOUNDS, out Rect r, Marshal.SizeOf<Rect>()) != 0 && !Win32.GetWindowRect(h, out r))
        {
            return true;
        }

        Add(r, h);
        return true;
    }

    private ReadOnlySpan<char> ClassOf(IntPtr h)
    {
        return _className.AsSpan(0, Win32.GetClassNameW(h, _className, _className.Length));
    }

    private void Add(Rect screen, IntPtr handle)
    {
        Rect r = Rect.Intersect(_desk.FromScreen(screen), _desk.Bounds);
        if (r.Width < _minSize || r.Height < _minSize)
        {
            return;
        }
        _windows.Add(new WindowOnScreen(handle, r));
    }
}

/// <summary>
/// Represents a window or monitor on the desktop and the elements found inside it.
/// </summary>
public sealed record WindowOnScreen(IntPtr Handle, Rect Bounds)
{
    public Rect[] Elements { get; init; } = [];

    public Rect Innermost { get; init; }

    public bool IsMonitor
    {
        get
        {
            return Handle == IntPtr.Zero;
        }
    }

    public Rect ElementAt(Point p)
    {
        Rect element = Rect.SmallestContaining(Elements, p);
        if (element.IsEmpty)
        {
            return Bounds;
        }
        else
        {
            return element;
        }
    }
}
