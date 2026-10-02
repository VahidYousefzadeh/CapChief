namespace CapChief.Windowing;

/// <summary>
/// Represents a native window and exposes its position, visibility and behavior.
/// </summary>
public sealed class Window
{
    private readonly IntPtr _handle;

    public Window(IntPtr handle)
    {
        _handle = handle;
    }

    public IntPtr Handle
    {
        get
        {
            return _handle;
        }
    }

    public static Window None
    {
        get
        {
            return new(IntPtr.Zero);
        }
    }

    public bool Exists
    {
        get
        {
            return _handle != IntPtr.Zero;
        }
    }

    public bool IsVisible
    {
        get
        {
            return Win32.IsWindowVisible(_handle);
        }
    }

    public float Scale
    {
        get
        {
            uint dpi = Win32.GetDpiForWindow(_handle);
            if (dpi == 0)
            {
                dpi = Win32.USER_DEFAULT_SCREEN_DPI;
            }
            return Win32.ScaleFromDpi(dpi);
        }
    }

    public bool ClickThrough
    {
        set
        {
            Set(Win32.WS_EX_TRANSPARENT, value);
        }
    }

    public bool PerPixelAlpha
    {
        set
        {
            Set(Win32.WS_EX_LAYERED, value);
        }
    }

    public void ExcludeFromCapture()
    {
        if (!Exists)
        {
            return;
        }
        IntPtr ex = Win32.GetWindowLongPtr(_handle, Win32.GWL_EXSTYLE);
        bool layered = (ex.ToInt64() & Win32.WS_EX_LAYERED) != 0;
        if (layered)
        {
            Win32.SetWindowLongPtr(_handle, Win32.GWL_EXSTYLE, (IntPtr)(ex.ToInt64() & ~Win32.WS_EX_LAYERED));
        }
        Win32.SetWindowDisplayAffinity(_handle, Win32.WDA_NONE);
        Win32.SetWindowDisplayAffinity(_handle, Win32.WDA_EXCLUDEFROMCAPTURE);
        if (layered)
        {
            Win32.SetWindowLongPtr(_handle, Win32.GWL_EXSTYLE, ex);
        }
    }

    public void Show()
    {
        Win32.ShowWindow(_handle, Win32.SW_SHOWNOACTIVATE);
    }

    public void Hide()
    {
        Win32.ShowWindow(_handle, Win32.SW_HIDE);
    }

    public Point ToClient(Point p)
    {
        Win32.ScreenToClient(_handle, ref p);
        return p;
    }

    public bool AddHotkey(int id, Hotkey key)
    {
        return key.RegisterOn(_handle, id);
    }

    public void DropHotkey(int id)
    {
        Win32.UnregisterHotKey(_handle, id);
    }

    public void Reveal()
    {
        Win32.SetWindowPos(_handle, Win32.HWND_TOPMOST, 0, 0, 0, 0, Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE | Win32.SWP_SHOWWINDOW);
    }

    public void ShowKeepingOrder()
    {
        Win32.SetWindowPos(_handle, IntPtr.Zero, 0, 0, 0, 0, Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOZORDER | Win32.SWP_SHOWWINDOW);
    }

    public void Raise()
    {
        Win32.SetWindowPos(_handle, Win32.HWND_TOPMOST, 0, 0, 0, 0, Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE);
    }

    public void Activate()
    {
        Win32.SetForegroundWindow(_handle);
    }

    public void SetIcon(AppIcon icon)
    {
        icon.Apply(_handle);
    }

    public Action Poster(uint message)
    {
        IntPtr h = _handle;
        return () => Win32.PostMessageW(h, message, IntPtr.Zero, IntPtr.Zero);
    }

    public void Focus()
    {
        Win32.SetForegroundWindow(_handle);
        Win32.SetFocus(_handle);
    }

    public void PlaceAt(Rect r)
    {
        Win32.SetWindowPos(_handle, Win32.HWND_TOPMOST, r.Left, r.Top, r.Width, r.Height, Win32.SWP_NOACTIVATE);
    }

    public void PlaceAndShow(Rect r)
    {
        Win32.SetWindowPos(_handle, Win32.HWND_TOPMOST, r.Left, r.Top, r.Width, r.Height, Win32.SWP_NOACTIVATE | Win32.SWP_SHOWWINDOW);
    }

    public void MoveTo(int x, int y)
    {
        Win32.SetWindowPos(_handle, IntPtr.Zero, x, y, 0, 0, Win32.SWP_NOSIZE | Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
    }

    public void Destroy()
    {
        Win32.DestroyWindow(_handle);
    }

    public void CaptureMouse()
    {
        Win32.SetCapture(_handle);
    }

    public void ReleaseMouse()
    {
        Win32.ReleaseCapture();
    }

    public void PaintNow()
    {
        Win32.UpdateWindow(_handle);
    }

    public void PushLayer(Layer layer)
    {
        layer.Push(_handle);
    }

    public void PushLayer(Layer layer, ReadOnlySpan<Rect> dirty)
    {
        layer.Push(_handle, dirty);
    }

    public bool PushPixels(Surface surf, int x, int y, byte opacity = 255)
    {
        return PushPixels(surf, surf.Bounds, x, y, opacity);
    }

    public bool PushPixels(Surface surf, Rect part, int x, int y, byte opacity = 255)
    {
        var dst = new Point(x, y);
        Size sz = part.Size;
        Point src = part.TopLeft;
        Win32.BLENDFUNCTION bf = Win32.BLENDFUNCTION.Over;
        bf.SourceConstantAlpha = opacity;
        using var screen = ScreenDc.Borrow();
        return Win32.UpdateLayeredWindow(_handle, screen.Dc, ref dst, ref sz, surf.Dc, ref src, 0, ref bf, Win32.ULW_ALPHA);
    }

    public PaintScope Paint()
    {
        return new(_handle);
    }

    public bool CollectUpdate(IntPtr region)
    {
        return Win32.GetUpdateRgn(_handle, region, false) > Win32.NULLREGION;
    }

    public void RepaintNow()
    {
        Win32.RedrawWindow(_handle, IntPtr.Zero, IntPtr.Zero, Win32.RDW_INVALIDATE | Win32.RDW_UPDATENOW);
    }

    public void Invalidate(Rect r)
    {
        if (_handle != IntPtr.Zero && !r.IsEmpty)
        {
            Win32.InvalidateRect(_handle, ref r, false);
        }
    }

    public void InvalidateAll()
    {
        if (_handle != IntPtr.Zero)
        {
            Win32.InvalidateRect(_handle, IntPtr.Zero, false);
        }
    }

    public void StartTimer(int id, uint everyMs)
    {
        Win32.SetTimer(_handle, (UIntPtr)id, everyMs, IntPtr.Zero);
    }

    public void StopTimer(int id)
    {
        Win32.KillTimer(_handle, (UIntPtr)id);
    }

    private void Set(long bit, bool on)
    {
        IntPtr ex = Win32.GetWindowLongPtr(_handle, Win32.GWL_EXSTYLE);
        long want;
        if (on)
        {
            want = ex.ToInt64() | bit;
        }
        else
        {
            want = ex.ToInt64() & ~bit;
        }
        if (want != ex.ToInt64())
        {
            Win32.SetWindowLongPtr(_handle, Win32.GWL_EXSTYLE, (IntPtr)want);
        }
    }
}
