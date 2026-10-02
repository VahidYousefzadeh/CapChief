namespace CapChief.Graphics;

/// <summary>
/// Draws a text string centered within a rectangle using its exact pixel extents.
/// </summary>
public static class CenteredText
{
    public static void Draw(IntPtr dc, Rect box, string s, IntPtr font, uint colorRef)
    {
        if (s.Length == 0)
        {
            return;
        }
        using var selected = new FontScope(dc, font);
        Win32.SetTextColor(dc, colorRef);

        Place(dc, box, s, out int x, out int baseline, out _);

        uint oa = Win32.SetTextAlign(dc, Win32.TA_LEFT | Win32.TA_BASELINE);
        Win32.TextOutW(dc, x, baseline, s, s.Length);
        Win32.SetTextAlign(dc, oa);
    }

    public static bool InkRect(TextRuler ruler, Rect box, string s, IntPtr font, out Rect ink)
    {
        ink = default;
        if (s.Length == 0)
        {
            return false;
        }
        IntPtr dc = ruler.Dc;
        using var selected = new FontScope(dc, font);
        return Place(dc, box, s, out _, out _, out ink);
    }

    private static bool Place(IntPtr dc, Rect box, string s, out int x, out int baseline, out Rect ink)
    {
        if (InkBox(dc, s, out int inkL, out int inkW, out int inkTop, out int inkH))
        {
            x = box.Left + (box.Width - inkW + 1) / 2 - inkL;
            baseline = box.Top + (box.Height - inkH + 1) / 2 + inkTop;
            ink = Rect.At(x + inkL, baseline - inkTop, inkW, inkH);
            return true;
        }

        Win32.GetTextMetricsW(dc, out Win32.TEXTMETRICW tm);
        Win32.GetTextExtentPoint32W(dc, s, s.Length, out Size ext);
        int cap = Math.Max(1, tm.tmAscent - tm.tmInternalLeading);
        x = box.Left + (box.Width - ext.Width + 1) / 2;
        baseline = box.Top + (box.Height + cap) / 2;
        ink = Rect.At(x, baseline - cap, ext.Width, cap);
        return false;
    }

    private static bool InkBox(IntPtr dc, string s, out int inkL, out int inkW, out int inkTop, out int inkH)
    {
        inkL = inkW = inkTop = inkH = 0;
        Win32.MAT2 mat = Win32.MAT2.Identity;
        if (Win32.GetGlyphOutlineW(dc, s[0], Win32.GGO_METRICS, out Win32.GLYPHMETRICS first, 0, IntPtr.Zero, ref mat)
            == Win32.GDI_ERROR || first.BlackBoxX == 0)
        {
            return false;
        }

        inkL = first.Origin.X;
        inkW = (int)first.BlackBoxX;
        int top = first.Origin.Y;
        int bottom = first.Origin.Y - (int)first.BlackBoxY;

        int pen = 0;
        Win32.GLYPHMETRICS last = first;
        for (int i = 1; i < s.Length; i++)
        {
            pen += last.CellIncX;
            if (Win32.GetGlyphOutlineW(dc, s[i], Win32.GGO_METRICS, out last, 0, IntPtr.Zero, ref mat)
                == Win32.GDI_ERROR)
            {
                return false;
            }
            if (last.BlackBoxX == 0)
            {
                continue;
            }
            top = Math.Max(top, last.Origin.Y);
            bottom = Math.Min(bottom, last.Origin.Y - (int)last.BlackBoxY);
        }
        inkTop = top;
        inkH = top - bottom;
        if (s.Length > 1)
        {
            inkW = pen + last.Origin.X + (int)last.BlackBoxX - inkL;
        }
        return inkW > 0 && inkH > 0;
    }
}
