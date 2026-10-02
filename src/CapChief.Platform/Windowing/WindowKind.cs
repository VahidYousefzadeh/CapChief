using System.Runtime.InteropServices;

namespace CapChief.Windowing;

/// <summary>
/// Represents a registered window class that creates windows and dispatches their messages.
/// </summary>
public sealed class WindowKind
{
    private readonly string _name;
    private readonly Win32.WndProc _proc;
    private readonly AppIcon? _icon;
    private readonly uint _classStyle;
    private readonly bool _ownsBackground;
    private readonly bool _tracksLeaving;

    private readonly Dictionary<IntPtr, IWindowHandler> _open = new();
    private readonly HashSet<IntPtr> _tracking = new();

    private bool _registered;

    public WindowKind(string name, AppIcon? icon, bool doubleClicks, bool redrawOnResize, bool ownsBackground, bool tracksLeaving)
    {
        _name = name;
        _icon = icon;
        _ownsBackground = ownsBackground;
        _tracksLeaving = tracksLeaving;
        uint style = 0;
        if (doubleClicks)
        {
            style |= Win32.CS_DBLCLKS;
        }
        if (redrawOnResize)
        {
            style |= Win32.CS_HREDRAW | Win32.CS_VREDRAW;
        }
        _classStyle = style;
        _proc = Dispatch;
    }

    public Window Open(WindowSpec spec, Window? owner, IWindowHandler? handler)
    {
        Register();

        Rect b = spec.Bounds;
        IntPtr h = Win32.CreateWindowExW(ExStyle(spec), _name, spec.Title, Win32.WS_POPUP, b.Left, b.Top, b.Width, b.Height, owner?.Handle ?? IntPtr.Zero, IntPtr.Zero, Win32.GetModuleHandleW(null), IntPtr.Zero);

        if (h != IntPtr.Zero && handler is not null)
        {
            _open[h] = handler;
        }
        return new(h);
    }

    private void Register()
    {
        if (_registered)
        {
            return;
        }

        var wc = new Win32.WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<Win32.WNDCLASSEX>(),
            style = _classStyle,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_proc),
            hInstance = Win32.GetModuleHandleW(null),
            hIcon = _icon?.Big ?? IntPtr.Zero,
            hIconSm = _icon?.Small ?? IntPtr.Zero,
            hCursor = Win32.LoadCursorW(IntPtr.Zero, (IntPtr)Win32.IDC_ARROW),
            hbrBackground = IntPtr.Zero,
            lpszClassName = _name,
        };

        Win32.RegisterClassExW(ref wc);
        _registered = true;
    }

    private static uint ExStyle(WindowSpec spec)
    {
        long ex = 0;
        if (spec.AlwaysOnTop)
        {
            ex |= Win32.WS_EX_TOPMOST;
        }
        if (!spec.TakesFocus)
        {
            ex |= Win32.WS_EX_NOACTIVATE;
        }
        if (!spec.ShowsInTaskbar)
        {
            ex |= Win32.WS_EX_TOOLWINDOW;
        }
        if (spec.PerPixelAlpha)
        {
            ex |= Win32.WS_EX_LAYERED;
        }
        if (spec.ClickThrough)
        {
            ex |= Win32.WS_EX_TRANSPARENT;
        }
        return (uint)ex;
    }

    private IntPtr Dispatch(IntPtr h, uint m, IntPtr w, IntPtr l)
    {
        if (m == Win32.WM_ERASEBKGND && _ownsBackground)
        {
            return (IntPtr)1;
        }
        if (m == Win32.WM_MOUSEACTIVATE && TakesNoFocus(h))
        {
            return (IntPtr)Win32.MA_NOACTIVATE;
        }
        if (!_open.TryGetValue(h, out IWindowHandler? e))
        {
            return Win32.DefWindowProcW(h, m, w, l);
        }

        if (Pointing(h, m, w, l, e, out IntPtr pointed))
        {
            return pointed;
        }

        switch (m)
        {
            case Win32.WM_NCHITTEST:
                if (e is IHitTestHandler hit)
                {
                    switch (hit.OnHitTest())
                    {
                        case HitZone.Client:
                            return (IntPtr)Win32.HTCLIENT;
                        case HitZone.Transparent:
                            return (IntPtr)Win32.HTTRANSPARENT;
                    }
                }
                break;

            case Win32.WM_PAINT:
                if (e is IPaintHandler paint)
                {
                    paint.OnPaint();
                }
                return IntPtr.Zero;

            case Win32.WM_KEYDOWN:
            case Win32.WM_SYSKEYDOWN when w.ToInt32() == Keys.Alt:
                if (e is IKeyDownHandler down && down.OnKeyDown(w.ToInt32()))
                {
                    return (IntPtr)1;
                }
                break;

            case Win32.WM_KEYUP:
            case Win32.WM_SYSKEYUP when w.ToInt32() == Keys.Alt:
                if (e is IKeyUpHandler up && up.OnKeyUp(w.ToInt32()))
                {
                    return (IntPtr)1;
                }
                break;

            case Win32.WM_TIMER:
                if (e is ITimerHandler timer)
                {
                    timer.OnTimer(w.ToInt32());
                }
                return IntPtr.Zero;

            case Win32.WM_HOTKEY:
                if (e is IHotkeyHandler hotkey)
                {
                    hotkey.OnHotkey(w.ToInt32());
                }
                return IntPtr.Zero;

            case Win32.WM_DPICHANGED:
                if (e is IDpiChangeHandler dpi)
                {
                    dpi.OnDpiChanged(Win32.ScaleFromDpi((uint)Win32.LoWord(w)), Win32.RectOf(l));
                }
                return IntPtr.Zero;

            case Win32.WM_DISPLAYCHANGE:
                if (e is IDisplayChangeHandler display)
                {
                    display.OnDisplayChanged();
                }
                break;

            case Win32.WM_ACTIVATE:
                if (Win32.LoWord(w) == Win32.WA_INACTIVE)
                {
                    if (e is IDeactivateHandler deactivate)
                    {
                        deactivate.OnDeactivated();
                    }
                }
                else
                {
                    if (e is IActivateHandler activate)
                    {
                        activate.OnActivated();
                    }
                }
                break;

            case Win32.WM_CTLCOLOREDIT:
                if (e is IEditBackdropHandler backdrop && backdrop.OnEditBackdrop(new DeviceContext(w), new EditRef(l)))
                {
                    return Win32.GetStockObject(Win32.NULL_BRUSH);
                }
                break;

            case Win32.WM_DESTROY:
                if (e is ICloseHandler close)
                {
                    close.OnClosed();
                }
                _open.Remove(h);
                _tracking.Remove(h);
                return IntPtr.Zero;
        }

        if (m >= Win32.WM_APP && e is IAppMessageHandler app && app.OnAppMessage(m, new AppSignal(w, l)))
        {
            return IntPtr.Zero;
        }
        return Win32.DefWindowProcW(h, m, w, l);
    }

    private bool Pointing(IntPtr h, uint m, IntPtr w, IntPtr l, IWindowHandler e, out IntPtr answer)
    {
        answer = IntPtr.Zero;
        switch (m)
        {
            case Win32.WM_LBUTTONDOWN:
                if (e is IMouseHandler down)
                {
                    down.OnMouseDown(Win32.PointOf(l), Shift(w));
                }
                return true;

            case Win32.WM_LBUTTONUP:
                if (e is IMouseHandler up)
                {
                    up.OnMouseUp(Win32.PointOf(l));
                }
                return true;

            case Win32.WM_LBUTTONDBLCLK:
                if (e is IDoubleClickHandler twice)
                {
                    twice.OnDoubleClick(Win32.PointOf(l), Shift(w));
                }
                return true;

            case Win32.WM_RBUTTONUP:
                if (e is not IRightClickHandler right)
                {
                    return false;
                }
                right.OnRightClick();
                return true;

            case Win32.WM_MOUSEMOVE:
                if (_tracksLeaving)
                {
                    TrackLeaving(h);
                }
                if (e is IMouseHandler move)
                {
                    move.OnMouseMove(Win32.PointOf(l), Shift(w), Ctrl(w));
                }
                return true;

            case Win32.WM_MOUSELEAVE:
                _tracking.Remove(h);
                if (e is IMouseLeaveHandler leave)
                {
                    leave.OnMouseLeft();
                }
                return true;

            case Win32.WM_SETCURSOR:
                if (Win32.LoWord(l) != Win32.HTCLIENT)
                {
                    return false;
                }
                if (e is not IMouseHandler mouse || mouse.OnCursorWanted() is not { } cursor)
                {
                    return false;
                }
                Win32.SetCursor(cursor);
                answer = (IntPtr)1;
                return true;
        }
        return false;
    }

    private static bool TakesNoFocus(IntPtr h)
    {
        return (Win32.GetWindowLongPtr(h, Win32.GWL_EXSTYLE).ToInt64() & Win32.WS_EX_NOACTIVATE) != 0;
    }

    private void TrackLeaving(IntPtr h)
    {
        if (!_tracking.Add(h))
        {
            return;
        }

        var tme = new Win32.TRACKMOUSEEVENT
        {
            cbSize = (uint)Marshal.SizeOf<Win32.TRACKMOUSEEVENT>(),
            dwFlags = Win32.TME_LEAVE,
            hwndTrack = h,
        };
        Win32.TrackMouseEvent(ref tme);
    }

    private static bool Shift(IntPtr w)
    {
        return (w.ToInt64() & Win32.MK_SHIFT) != 0;
    }

    private static bool Ctrl(IntPtr w)
    {
        return (w.ToInt64() & Win32.MK_CONTROL) != 0;
    }
}
