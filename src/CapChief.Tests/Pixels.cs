namespace CapChief.Tests;

/// <summary>
/// Provides methods for building test images and color tables for the GIF checks.
/// </summary>
public static class Pixels
{
    public static byte[] Bgra(int w, int h, Func<int, (int r, int g, int b)> at)
    {
        var px = new byte[w * h * 4];
        for (int i = 0; i < w * h; i++)
        {
            Paint(px, i, at(i));
        }
        return px;
    }

    public static void Paint(byte[] bgra, int i, (int r, int g, int b) c)
    {
        bgra[i * 4] = (byte)c.b;
        bgra[i * 4 + 1] = (byte)c.g;
        bgra[i * 4 + 2] = (byte)c.r;
        bgra[i * 4 + 3] = 255;
    }

    public static uint ColorRefAt(byte[] bgra, int i)
    {
        return ((uint)bgra[i * 4] << 16) | ((uint)bgra[i * 4 + 1] << 8) | bgra[i * 4 + 2];
    }

    public static (int, int, int) Spread(int i)
    {
        return (i * 7 % 256, i * 13 % 256, i * 29 % 256);
    }

    public static (int, int, int) Dull(int i)
    {
        return (i % 8 * 18, i / 8 % 8 * 18, i / 64 * 30);
    }

    public static ColorTable TableOf(byte[] bgra, int w, int h)
    {
        var t = new ColorTable();
        t.Build(bgra, w, h, w * 4);
        return t;
    }

    public static double MeanError(ColorTable t, byte[] bgra, int w, int h)
    {
        long sum = 0;
        for (int i = 0; i < w * h; i++)
        {
            int b = bgra[i * 4];
            int g = bgra[i * 4 + 1];
            int r = bgra[i * 4 + 2];
            t.Entry(t.IndexOf(r, g, b), out int er, out int eg, out int eb);
            sum += Math.Abs(er - r) + Math.Abs(eg - g) + Math.Abs(eb - b);
        }
        return sum / (3.0 * w * h);
    }

    public static double MeanError(byte[] want, byte[] got)
    {
        long sum = 0;
        for (int i = 0; i < want.Length; i += 4)
        {
            for (int c = 0; c < 3; c++)
            {
                sum += Math.Abs(want[i + c] - got[i + c]);
            }
        }
        return sum / (want.Length / 4 * 3.0);
    }

    public static int Differs(byte[] want, byte[]? got)
    {
        if (got is null || got.Length != want.Length)
        {
            return want.Length / 4;
        }
        int n = 0;
        for (int i = 0; i < want.Length; i += 4)
        {
            if (got[i] != want[i] || got[i + 1] != want[i + 1] || got[i + 2] != want[i + 2])
            {
                n++;
            }
        }
        return n;
    }

    public static byte[] Encode(int w, int h, params (byte[] bgra, int cs)[] frames)
    {
        using var ms = new MemoryStream();
        var gif = new GifWriter(ms);
        gif.Begin(w, h, frames[0].bgra);
        foreach ((byte[] bgra, int cs) f in frames)
        {
            gif.Frame(f.bgra, f.cs);
        }
        gif.End();
        return ms.ToArray();
    }
}
