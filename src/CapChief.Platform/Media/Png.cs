using System.Buffers.Binary;
using System.IO.Compression;

namespace CapChief.Media;

/// <summary>
/// Provides methods for writing an image to a stream in PNG format.
/// </summary>
public static class Png
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private const int HeaderBytes = 13;

    private const byte BitDepth = 8;

    private const byte ColorTypeRgb = 2;

    private const byte PaethFilter = 4;

    private const uint CrcPolynomial = 0xEDB88320u;

    private const uint CrcSeed = 0xFFFFFFFFu;

    private static readonly uint[] CrcLut = CrcTable();

    public static bool Write(Stream to, byte[] bgra, int w, int h, int stride)
    {
        if (w <= 0 || h <= 0)
        {
            return false;
        }

        byte[] idat = Deflate(Filter(bgra, w, h, stride));

        var ihdr = new byte[HeaderBytes];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr, (uint)w);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4), (uint)h);
        ihdr[8] = BitDepth;
        ihdr[9] = ColorTypeRgb;

        to.Write(Signature);
        Chunk(to, "IHDR"u8, ihdr);
        Chunk(to, "IDAT"u8, idat);
        Chunk(to, "IEND"u8, []);
        return true;
    }

    private static byte[] Filter(byte[] bgra, int w, int h, int stride)
    {
        int line = w * 3;
        long need = (long)h * (line + 1);
        if (need > int.MaxValue)
        {
            throw new IOException("image too large");
        }

        var outp = new byte[need];
        var cur = new byte[line];
        var prev = new byte[line];
        int o = 0;
        for (int y = 0; y < h; y++)
        {
            int src = y * stride;
            for (int x = 0; x < w; x++)
            {
                cur[x * 3 + 0] = bgra[src + x * 4 + 2];
                cur[x * 3 + 1] = bgra[src + x * 4 + 1];
                cur[x * 3 + 2] = bgra[src + x * 4 + 0];
            }

            outp[o++] = PaethFilter;
            for (int i = 0; i < line; i++)
            {
                byte a = 0;
                byte c = 0;
                if (i >= 3)
                {
                    a = cur[i - 3];
                    c = prev[i - 3];
                }
                byte b = prev[i];
                outp[o++] = (byte)(cur[i] - Paeth(a, b, c));
            }
            (prev, cur) = (cur, prev);
        }
        return outp;
    }

    private static byte Paeth(byte a, byte b, byte c)
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

    private static byte[] Deflate(byte[] raw)
    {
        using var ms = new MemoryStream();
        using (var zlib = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(raw);
        }
        return ms.ToArray();
    }

    private static void Chunk(Stream s, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> word = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(word, (uint)data.Length);
        s.Write(word);
        s.Write(type);
        s.Write(data);

        uint crc = Crc(Crc(CrcSeed, type), data) ^ CrcSeed;
        BinaryPrimitives.WriteUInt32BigEndian(word, crc);
        s.Write(word);
    }

    private static uint[] CrcTable()
    {
        var t = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
            {
                if ((c & 1) != 0)
                {
                    c = CrcPolynomial ^ (c >> 1);
                }
                else
                {
                    c >>= 1;
                }
            }
            t[n] = c;
        }
        return t;
    }

    private static uint Crc(uint crc, ReadOnlySpan<byte> buf)
    {
        for (int i = 0; i < buf.Length; i++)
        {
            crc = CrcLut[(crc ^ buf[i]) & 0xFF] ^ (crc >> 8);
        }
        return crc;
    }
}
