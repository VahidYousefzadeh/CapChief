using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using static CapChief.Tests.Fixtures;

namespace CapChief.Tests;

/// <summary>
/// Tests that a saved or copied capture holds what it should: redaction hides everything under it and nothing
/// else, and the PNG it is saved as reads back as the pixels it was written from.
/// </summary>
public sealed class ExportTests : IDisposable
{
    private const int PictureWidth = 240;
    private const int PictureHeight = 180;

    private static readonly Rect Area = R(20, 20, 220, 160);
    private static readonly Rect Bar = R(140, 50, 200, 110);
    private static readonly Color BarColor = Color.FromArgb(0xFF1E6EF0u);

    private readonly Metrics _metrics = MetricsFor(Desk());

    private int Block
    {
        get
        {
            return _metrics.Sizes.RedactBlock;
        }
    }

    private Rect MosaicArea
    {
        get
        {
            return Rect.At(40, 40, 8 * Block, 6 * Block);
        }
    }

    public void Dispose()
    {
        _metrics.Free();
    }

    [Fact]
    public void AMosaicLeavesOneColorInEachBlock()
    {
        Rect mosaic = MosaicArea;
        byte[] got = Render(DrawnOver(MarkKind.Mosaic, mosaic));

        int mixed = 0;
        for (int y = mosaic.Top; y < mosaic.Bottom; y += Block)
        {
            for (int x = mosaic.Left; x < mosaic.Right; x += Block)
            {
                if (!IsFlat(got, R(x, y, x + Block, y + Block)))
                {
                    mixed++;
                }
            }
        }
        Eq(mixed, 0, "blocks of the mosaic in which the checkers under it still show");
    }

    [Fact]
    public void ABlackoutCoversItsWholeAreaInItsColor()
    {
        byte[] got = Render(Blackout());
        Eq(Count(got, Bar, i => Pixels.ColorRefAt(got, i) != BarColor.ColorRef), 0, "pixels under the blackout that are not its color");
    }

    [Fact]
    public void RedactionLeavesTheRestOfTheCaptureAsItWas()
    {
        Rect mosaic = MosaicArea;
        byte[] got = Render(DrawnOver(MarkKind.Mosaic, mosaic), Blackout());

        int changed = Count(got, Area, i =>
        {
            Point at = P(Area.Left + i % Area.Width, Area.Top + i / Area.Width);
            return !mosaic.Contains(at) && !Bar.Contains(at) && Pixels.ColorRefAt(got, i) != CheckerAt(at.X, at.Y);
        });
        Eq(changed, 0, "pixels outside the marks that changed");
    }

    [Fact]
    public void APngReadsBackAsThePixelsItWasWrittenFrom()
    {
        const int w = 37;
        const int h = 23;
        byte[] bgra = Pixels.Bgra(w, h, Pixels.Spread);
        using var file = new MemoryStream();
        Assert.True(Png.Write(file, bgra, w, h, w * 4), "it writes");
        byte[] png = file.ToArray();

        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        Assert.True(png.AsSpan(0, 8).SequenceEqual(signature), "it starts with the PNG signature");

        using var idat = new MemoryStream();
        int at = 8;
        string type = "";
        while (type != "IEND")
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(at));
            type = Encoding.ASCII.GetString(png, at + 4, 4);
            ReadOnlySpan<byte> data = png.AsSpan(at + 8, length);
            Eq(BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(at + 8 + length)), Crc32(png.AsSpan(at + 4, 4 + length)), $"the CRC of the {type} chunk");
            if (type == "IHDR")
            {
                Eq(BinaryPrimitives.ReadInt32BigEndian(data), w, "the width in the header");
                Eq(BinaryPrimitives.ReadInt32BigEndian(data[4..]), h, "the height in the header");
                Assert.True(data[8] == 8 && data[9] == 2 && data[12] == 0, "8-bit RGB without interlacing");
            }
            if (type == "IDAT")
            {
                idat.Write(data);
            }
            at += 12 + length;
        }
        Eq(at, png.Length, "nothing follows the IEND chunk");

        idat.Position = 0;
        using var zlib = new ZLibStream(idat, CompressionMode.Decompress);
        using var raw = new MemoryStream();
        zlib.CopyTo(raw);
        Eq(raw.Length, h * (1 + w * 3), "one filter byte and a row of RGB per line");

        byte[] rgb = Unfilter(raw.ToArray(), w, h);
        int differ = 0;
        for (int i = 0; i < w * h; i++)
        {
            if (rgb[i * 3] != bgra[i * 4 + 2] || rgb[i * 3 + 1] != bgra[i * 4 + 1] || rgb[i * 3 + 2] != bgra[i * 4])
            {
                differ++;
            }
        }
        Eq(differ, 0, "pixels that read back differently");
    }

    private static Mark Blackout()
    {
        return DrawnOver(MarkKind.Blackout, Bar) with { Color = BarColor };
    }

    private static Mark DrawnOver(MarkKind kind, Rect r)
    {
        return Drawn(kind, r.Left, r.Top, r.Right, r.Bottom);
    }

    private byte[] Render(params Mark[] marks)
    {
        using var picture = new Surface();
        Assert.True(picture.OpenDib(PictureWidth, PictureHeight), "the picture opens");
        Span<byte> px = picture.Pixels;
        for (int y = 0; y < PictureHeight; y++)
        {
            for (int x = 0; x < PictureWidth; x++)
            {
                int o = (y * PictureWidth + x) * 4;
                px[o] = px[o + 1] = px[o + 2] = (byte)CheckerAt(x, y);
                px[o + 3] = 0xFF;
            }
        }

        var document = new Document();
        document.Open(new Capture(picture, CropOf(Area.Left, Area.Top, Area.Right, Area.Bottom)));
        foreach (Mark mark in marks)
        {
            document.AddMark(mark);
        }

        using var image = new Surface();
        bool rendered = document.Capture!.Render(image, new MarkPainter(_metrics));
        Assert.True(rendered, "the capture renders");
        return image.ReadPixels(image.Bounds) ?? throw new InvalidOperationException("no pixels");
    }

    private static uint CheckerAt(int x, int y)
    {
        if ((x + y) % 2 == 0)
        {
            return 0xFAFAFAu;
        }
        else
        {
            return 0x0A0A0Au;
        }
    }

    private static int IndexOf(int x, int y)
    {
        return (y - Area.Top) * Area.Width + x - Area.Left;
    }

    private static bool IsFlat(byte[] got, Rect r)
    {
        uint first = Pixels.ColorRefAt(got, IndexOf(r.Left, r.Top));
        return Count(got, r, i => Pixels.ColorRefAt(got, i) != first) == 0;
    }

    private static int Count(byte[] got, Rect r, Func<int, bool> wrong)
    {
        int n = 0;
        for (int y = r.Top; y < r.Bottom; y++)
        {
            for (int x = r.Left; x < r.Right; x++)
            {
                if (wrong(IndexOf(x, y)))
                {
                    n++;
                }
            }
        }
        return n;
    }

    private static byte[] Unfilter(byte[] raw, int w, int h)
    {
        int line = w * 3;
        var px = new byte[h * line];
        for (int y = 0; y < h; y++)
        {
            int filter = raw[y * (line + 1)];
            for (int i = 0; i < line; i++)
            {
                int a;
                if (i >= 3)
                {
                    a = px[y * line + i - 3];
                }
                else
                {
                    a = 0;
                }
                int b;
                if (y > 0)
                {
                    b = px[(y - 1) * line + i];
                }
                else
                {
                    b = 0;
                }
                int c;
                if (i >= 3 && y > 0)
                {
                    c = px[(y - 1) * line + i - 3];
                }
                else
                {
                    c = 0;
                }
                int predicted;
                switch (filter)
                {
                    case 0:
                        predicted = 0;
                        break;
                    case 1:
                        predicted = a;
                        break;
                    case 2:
                        predicted = b;
                        break;
                    case 3:
                        predicted = (a + b) / 2;
                        break;
                    case 4:
                        predicted = Paeth(a, b, c);
                        break;
                    default:
                        throw new InvalidDataException($"filter {filter} on line {y}");
                }
                px[y * line + i] = (byte)(raw[y * (line + 1) + 1 + i] + predicted);
            }
        }
        return px;
    }

    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc)
        {
            return a;
        }
        else if (pb <= pc)
        {
            return b;
        }
        else
        {
            return c;
        }
    }

    private static uint Crc32(ReadOnlySpan<byte> bytes)
    {
        uint crc = 0xFFFFFFFFu;
        foreach (byte b in bytes)
        {
            crc ^= b;
            for (int k = 0; k < 8; k++)
            {
                if ((crc & 1) != 0)
                {
                    crc = (crc >> 1) ^ 0xEDB88320u;
                }
                else
                {
                    crc = crc >> 1;
                }
            }
        }
        return ~crc;
    }
}
