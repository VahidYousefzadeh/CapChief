using System.Runtime.InteropServices;
using CapChief.Interop;

namespace CapChief.Tests;

/// <summary>
/// Tests that a recording gets the pointer at the size Windows draws it on the pointer's monitor:
/// Windows' own pointers resized for that monitor's scale, a program's own pointer left as it is.
/// </summary>
public sealed class ScaledPointerTests
{
    private static IntPtr Arrow
    {
        get
        {
            return Win32.LoadCursorW(IntPtr.Zero, Win32.IDC_ARROW);
        }
    }

    private static IntPtr Beam
    {
        get
        {
            return Win32.LoadCursorW(IntPtr.Zero, Win32.IDC_IBEAM);
        }
    }

    [Fact]
    public void WindowsOwnPointerTakesTheMonitorsSize()
    {
        var pointer = new ScaledPointer();
        int at100 = Read(pointer.At(Arrow, 1f)).Width;
        int at200 = Read(pointer.At(Arrow, 2f)).Width;
        int at150 = Read(pointer.At(Arrow, 1.5f)).Width;
        pointer.Free();

        Eq(at200, 2 * at100, "at 200% the arrow is twice as wide as at 100%");
        Eq(at150, (int)MathF.Round(1.5f * at100), "and at 150% half as wide again");
    }

    [Fact]
    public void AResizedPointerStillPointsWithTheSameSpot()
    {
        var pointer = new ScaledPointer();
        (int w1, int x1, int y1) = Read(pointer.At(Beam, 1f));
        (int w2, int x2, int y2) = Read(pointer.At(Beam, 2f));
        pointer.Free();

        Assert.True(w2 == 2 * w1 && Math.Abs(x2 - 2 * x1) <= 1 && Math.Abs(y2 - 2 * y1) <= 1, $"the I-beam's hot spot at {x1},{y1} of {w1} px moves with it to about twice that at {w2} px, not to {x2},{y2}");
    }

    [Fact]
    public void AProgramsOwnPointerIsLeftAsItIs()
    {
        var crosshair = new Crosshair(0xFF10A070u);
        IntPtr own = crosshair.ShapeAt(1f);
        var pointer = new ScaledPointer();
        Eq(pointer.At(own, 2f), own, "Windows does not resize a program's own pointer, so a recording does not either");
        pointer.Free();
        crosshair.Free();
    }

    [Fact]
    public void ThePointerMadeForAScaleIsKeptUntilEitherChanges()
    {
        var pointer = new ScaledPointer();
        IntPtr first = pointer.At(Arrow, 2f);
        Assert.True(first != Arrow && pointer.At(Arrow, 2f) == first, "asking again for the same pointer and scale gives the same copy");
        pointer.Free();
    }

    private static (int Width, int HotX, int HotY) Read(IntPtr cursor)
    {
        var ii = default(Win32.ICONINFO);
        Assert.True(Win32.GetIconInfo(cursor, ref ii), "the pointer can be read");
        var bm = default(Win32.BITMAP);
        IntPtr bitmap;
        if (ii.hbmColor != IntPtr.Zero)
        {
            bitmap = ii.hbmColor;
        }
        else
        {
            bitmap = ii.hbmMask;
        }
        Win32.GetObjectW(bitmap, Marshal.SizeOf<Win32.BITMAP>(), ref bm);
        if (ii.hbmColor != IntPtr.Zero)
        {
            Win32.DeleteObject(ii.hbmColor);
        }
        if (ii.hbmMask != IntPtr.Zero)
        {
            Win32.DeleteObject(ii.hbmMask);
        }
        return (bm.bmWidth, (int)ii.xHotspot, (int)ii.yHotspot);
    }
}
