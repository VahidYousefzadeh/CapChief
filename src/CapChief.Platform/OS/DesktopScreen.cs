using System.Runtime.InteropServices;

namespace CapChief.OS;

/// <summary>
/// Captures the contents of the physical screen, optionally including the mouse pointer.
/// </summary>
public sealed class DesktopScreen : ScreenSource
{
    private readonly Desktop _desk;
    private readonly ScaledPointer _pointer = new();

    public DesktopScreen(Desktop desk)
    {
        _desk = desk;
    }

    public override bool Capture(Surface dst)
    {
        using var screen = ScreenDc.Borrow();
        return Win32.BitBlt(dst.Dc, 0, 0, _desk.Size.Width, _desk.Size.Height, screen.Dc, _desk.Origin.X, _desk.Origin.Y, Win32.SRCCOPY);
    }

    public override void CaptureWithPointer(Surface dst, Rect area)
    {
        using (var screen = ScreenDc.Borrow())
        {
            Win32.BitBlt(dst.Dc, 0, 0, area.Width, area.Height, screen.Dc, area.Left + _desk.Origin.X, area.Top + _desk.Origin.Y, Win32.SRCCOPY);
        }
        DrawCursor(dst, area);
        Win32.GdiFlush();
    }

    public override int ElapsedMs(long from, long now)
    {
        return (int)(now - from);
    }

    private void DrawCursor(Surface dst, Rect area)
    {
        var ci = new Win32.CURSORINFO { cbSize = (uint)Marshal.SizeOf<Win32.CURSORINFO>() };
        if (!Win32.GetCursorInfo(ref ci))
        {
            return;
        }
        if ((ci.flags & Win32.CURSOR_SHOWING) == 0 || ci.hCursor == IntPtr.Zero)
        {
            return;
        }

        IntPtr cursor = _pointer.At(ci.hCursor, _desk.ScaleAt(ci.ptScreenPos));
        using var bits = PointerBitmaps.Of(cursor);
        if (!bits.IsRead)
        {
            return;
        }
        Win32.DrawIconEx(dst.Dc, ci.ptScreenPos.X - _desk.Origin.X - area.Left - bits.HotX, ci.ptScreenPos.Y - _desk.Origin.Y - area.Top - bits.HotY, cursor, 0, 0, 0, IntPtr.Zero, Win32.DI_NORMAL);
    }
}
