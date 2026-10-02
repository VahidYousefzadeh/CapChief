using System.Reflection;

namespace CapChief.Lab;

/// <summary>
/// Provides methods for saving what the app drew as PNG files: an area of the screen, the toolbar,
/// the overlay's live layer and the drag image, each over a plain background where it is
/// see-through.
/// </summary>
public static class Pictures
{
    private const int Pad = 16;

    public static void Screen(Driver driver, Rect area, string path)
    {
        area = Rect.Intersect(area, driver.Desk.Bounds);
        Save(path, driver.Screen(area), area.Width, area.Height);
    }

    public static void Toolbar(Driver driver, string path)
    {
        Image(driver.App.Shell.Toolbar.Pixels, path);
    }

    public static void Pixels(byte[] bgra, int w, int h, string path)
    {
        Save(path, (byte[])bgra.Clone(), w, h);
    }

    public static void Image(Pixmap map, string path)
    {
        if (map.Width <= 0 || map.Height <= 0)
        {
            return;
        }
        int w = map.Width + Pad * 2;
        int h = map.Height + Pad * 2;
        var px = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int sx = x - Pad;
                int sy = y - Pad;
                bool inside = sx >= 0 && sx < map.Width && sy >= 0 && sy < map.Height;
                int i;
                if (inside)
                {
                    i = (sy * map.PixelsPerRow + sx) * 4;
                }
                else
                {
                    i = 0;
                }
                Span<byte> source;
                if (inside)
                {
                    source = map.Pixels.Slice(i, 4);
                }
                else
                {
                    source = default;
                }
                Over(px, (y * w + x) * 4, source, x, y);
            }
        }
        Save(path, px, w, h);
    }

    public static void DragImage(Driver driver, string path)
    {
        AlphaSurface image = driver.DragImage();
        if (!image.IsOpen)
        {
            return;
        }
        Image(new Pixmap(image.Surface.Pixels, image.Width, image.Height, image.Width), path);
    }

    public static void LiveLayer(Driver driver, Rect area, string path)
    {
        FieldInfo field = typeof(Overlay).GetField("_live", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Layer layer = ((LivePainter)field.GetValue(driver.App.Overlay)!).Layer;
        area = Rect.Intersect(area, driver.Desk.Bounds);
        if (layer.ReadPixels(area) is not { } px)
        {
            return;
        }
        Image(new Pixmap(px, area.Width, area.Height, area.Width), path);
    }

    private static void Over(byte[] dst, int at, ReadOnlySpan<byte> src, int x, int y)
    {
        int back;
        if (((x >> 3) + (y >> 3) & 1) == 0)
        {
            back = 96;
        }
        else
        {
            back = 72;
        }
        int a;
        if (src.IsEmpty)
        {
            a = 0;
        }
        else
        {
            a = src[3];
        }
        for (int c = 0; c < 3; c++)
        {
            int channel;
            if (src.IsEmpty)
            {
                channel = 0;
            }
            else
            {
                channel = src[c];
            }
            dst[at + c] = (byte)Math.Min(255, channel + back * (255 - a) / 255);
        }
        dst[at + 3] = 255;
    }

    private static void Save(string path, byte[] bgra, int w, int h)
    {
        if (w <= 0 || h <= 0 || bgra.Length < w * h * 4)
        {
            return;
        }
        for (int i = 3; i < bgra.Length; i += 4)
        {
            bgra[i] = 255;
        }
        using var file = new FileStream(path, FileMode.Create, FileAccess.Write);
        Png.Write(file, bgra, w, h, w * 4);
    }
}
