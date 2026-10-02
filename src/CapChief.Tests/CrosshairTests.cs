using CapChief.Interop;

namespace CapChief.Tests;

/// <summary>
/// Tests that the drawing crosshair grows with the display scale, with its hot spot on its lines, and
/// is drawn as it always was at 100%.
/// </summary>
public sealed class CrosshairTests
{
    private const uint Accent = 0xFF10A070u;

    [Theory]
    [InlineData(1f, 32, 15, 1, 8)]
    [InlineData(1.25f, 32, 15, 1, 10)]
    [InlineData(1.5f, 32, 15, 2, 12)]
    [InlineData(1.75f, 48, 23, 2, 14)]
    [InlineData(2f, 48, 23, 2, 16)]
    [InlineData(2.5f, 64, 30, 3, 20)]
    [InlineData(3f, 64, 30, 3, 24)]
    public void ItGrowsWithTheScale(float scale, int size, int hot, int line, int arm)
    {
        var crosshair = new Crosshair(Accent);
        (int w, int h, int hotX, int hotY, uint[] px) = Read(crosshair.ShapeAt(scale));
        crosshair.Free();

        Eq(w, size, $"at {scale:P0} the cursor's width");
        Eq(h, size, $"at {scale:P0} the cursor's height");
        Eq(hotX, hot, $"at {scale:P0} the hot spot's x");
        Eq(hotY, hot, $"at {scale:P0} the hot spot's y");
        Eq(px[hot * w + hot], Accent, $"at {scale:P0} the hot spot is on the lines");

        int across = 2 * arm + line;
        Eq(px.Count(p => p == Accent), 2 * across * line - line * line, $"at {scale:P0} the lines are {line} px thick and {across} px long");
        Eq(px.Count(p => p == 0xFF000000u), 8 * arm * line, $"at {scale:P0} the dark edging runs along both sides of them");
        Eq(Array.IndexOf(px, Accent, hot * w), hot * w + hot - arm, $"at {scale:P0} the lines reach {arm} px left");
        Eq(Array.LastIndexOf(px, Accent, hot * w + w - 1), hot * w + hot + line - 1 + arm, $"at {scale:P0} and as far right");
    }

    [Fact]
    public void EachScaleHasItsOwnCursorMadeOnce()
    {
        var crosshair = new Crosshair(Accent);
        IntPtr one = crosshair.ShapeAt(1f);
        IntPtr two = crosshair.ShapeAt(2f);
        Assert.True(one != IntPtr.Zero && two != IntPtr.Zero && one != two, "100% and 200% get a cursor each");
        Assert.True(crosshair.ShapeAt(1f) == one, "and asking again gives the same one back");
        crosshair.Free();
    }

    private static unsafe (int W, int H, int HotX, int HotY, uint[] Pixels) Read(IntPtr cursor)
    {
        var info = default(Win32.ICONINFO);
        Assert.True(Win32.GetIconInfo(cursor, ref info), "the cursor's bitmaps can be read");
        var bm = default(Win32.BITMAP);
        Win32.GetObjectW(info.hbmColor, sizeof(Win32.BITMAP), ref bm);

        var px = new uint[bm.bmWidth * bm.bmHeight];
        Win32.BITMAPINFOHEADER hdr = Surface.Hdr(bm.bmWidth, -bm.bmHeight);
        IntPtr dc = Win32.CreateCompatibleDC(IntPtr.Zero);
        fixed (uint* p = px)
        {
            Win32.GetDIBits(dc, info.hbmColor, 0, (uint)bm.bmHeight, (IntPtr)p, ref hdr, Win32.DIB_RGB_COLORS);
        }
        Win32.DeleteDC(dc);
        Win32.DeleteObject(info.hbmColor);
        Win32.DeleteObject(info.hbmMask);
        return (bm.bmWidth, bm.bmHeight, (int)info.xHotspot, (int)info.yHotspot, px);
    }
}
