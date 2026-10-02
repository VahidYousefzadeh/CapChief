namespace CapChief.Tests;

/// <summary>
/// Provides methods for decoding a GIF file so its frames can be checked.
/// </summary>
public static class GifProbe
{
    public static GifHeader Decode(byte[] d)
    {
        var r = new GifHeader();
        try
        {
            Parse(d, r);
        }
        catch (Exception e)
        {
            r.Error ??= e.GetType().Name;
        }
        return r;
    }

    private static void Parse(byte[] d, GifHeader r)
    {
        var fileTable = new byte[256 * 3];
        int at = ReadHeader(d, r, fileTable);
        if (at < 0)
        {
            return;
        }

        var canvas = new byte[r.W * r.H * 4];
        int delay = 0;
        int transparent = -1;

        while (at < d.Length)
        {
            int block = d[at++];
            if (block == 0x3B)
            {
                return;
            }
            if (block == 0x21)
            {
                at = Extension(d, at, r, ref delay, ref transparent);
                continue;
            }
            if (block != 0x2C)
            {
                r.Error = $"unknown block 0x{block:X2} at {at - 1}";
                return;
            }

            if (!Frame(d, ref at, r, canvas, fileTable, delay, transparent))
            {
                return;
            }
        }
        r.Error = "ran off the end without a trailer";
    }

    private static int ReadHeader(byte[] d, GifHeader r, byte[] fileTable)
    {
        if (d.Length < 13)
        {
            r.Error = "shorter than a header";
            return -1;
        }
        for (int i = 0; i < 6; i++)
        {
            if (d[i] != "GIF89a"[i])
            {
                r.Error = "not a GIF89a";
                return -1;
            }
        }

        r.W = d[6] | (d[7] << 8);
        r.H = d[8] | (d[9] << 8);
        int packed = d[10];
        int at = 13;
        if ((packed & 0x80) != 0)
        {
            r.TableSize = 1 << ((packed & 7) + 1);
            Array.Copy(d, at, fileTable, 0, r.TableSize * 3);
            at += r.TableSize * 3;
        }
        return at;
    }

    private static int Extension(byte[] d, int at, GifHeader r, ref int delay, ref int transparent)
    {
        int label = d[at++];
        if (label == 0xF9)
        {
            int len = d[at++];
            int flags = d[at];
            delay = d[at + 1] | (d[at + 2] << 8);
            if ((flags & 1) != 0)
            {
                transparent = d[at + 3];
            }
            else
            {
                transparent = -1;
            }
            at += len;
        }
        else if (label == 0xFF)
        {
            int len = d[at++];
            if (d.AsSpan(at, len).SequenceEqual("NETSCAPE2.0"u8))
            {
                r.Loops = true;
            }
            at += len;
        }
        return SkipBlocks(d, at);
    }

    private static bool Frame(byte[] d, ref int at, GifHeader r, byte[] canvas, byte[] fileTable, int delay, int transparent)
    {
        var sub = Rect.At(d[at] | (d[at + 1] << 8), d[at + 2] | (d[at + 3] << 8), d[at + 4] | (d[at + 5] << 8), d[at + 6] | (d[at + 7] << 8));
        int imgPacked = d[at + 8];
        at += 9;

        byte[] table = fileTable;
        int own = 0;
        if ((imgPacked & 0x80) != 0)
        {
            own = 1 << ((imgPacked & 7) + 1);
            table = new byte[256 * 3];
            Array.Copy(d, at, table, 0, own * 3);
            at += own * 3;
        }

        int minCodeSize = d[at++];
        byte[] data = Gather(d, ref at);

        var cell = new byte[sub.Width * sub.Height];
        string? bad = Inflate(data, minCodeSize, cell, r);
        if (bad is not null)
        {
            r.Error = bad;
            return false;
        }

        for (int y = 0; y < sub.Height; y++)
        {
            for (int x = 0; x < sub.Width; x++)
            {
                int v = cell[y * sub.Width + x];
                if (v == transparent)
                {
                    continue;
                }
                int o = ((sub.Top + y) * r.W + sub.Left + x) * 4;
                canvas[o] = table[v * 3 + 2];
                canvas[o + 1] = table[v * 3 + 1];
                canvas[o + 2] = table[v * 3];
                canvas[o + 3] = 0xFF;
            }
        }

        r.Frames.Add((byte[])canvas.Clone());
        r.Subs.Add(sub);
        r.Delays.Add(delay);
        r.OwnTables.Add(own);
        r.Cells.Add(cell);
        r.SeeThrough.Add(transparent);
        return true;
    }

    private static int SkipBlocks(byte[] d, int at)
    {
        while (at < d.Length && d[at] != 0)
        {
            at += d[at] + 1;
        }
        return at + 1;
    }

    private static byte[] Gather(byte[] d, ref int at)
    {
        using var ms = new MemoryStream();
        while (at < d.Length && d[at] != 0)
        {
            int len = d[at++];
            ms.Write(d, at, len);
            at += len;
        }
        at++;
        return ms.ToArray();
    }

    private static string? Inflate(byte[] data, int minCodeSize, byte[] outp, GifHeader r)
    {
        if (minCodeSize < 2 || minCodeSize > 8)
        {
            return $"a minimum code size of {minCodeSize}";
        }
        int clear = 1 << minCodeSize;
        int eoi = clear + 1;
        var prefix = new int[4096];
        var suffix = new byte[4096];
        var stack = new byte[4096];

        int codeSize = minCodeSize + 1;
        int top = (1 << codeSize) - 1;
        int next = clear + 2;
        int prev = -1;
        int bit = 0;
        int wrote = 0;

        while (true)
        {
            if (bit + codeSize > data.Length * 8)
            {
                return "the code stream ended without an EOI";
            }
            int code = 0;
            for (int i = 0; i < codeSize; i++)
            {
                int b = bit + i;
                code |= ((data[b >> 3] >> (b & 7)) & 1) << i;
            }
            bit += codeSize;

            if (code == clear)
            {
                codeSize = minCodeSize + 1;
                top = (1 << codeSize) - 1;
                next = clear + 2;
                prev = -1;
                r.Clears++;
                continue;
            }
            if (code == eoi)
            {
                if (wrote == outp.Length)
                {
                    return null;
                }
                else
                {
                    return $"{wrote} of {outp.Length} pixels decoded";
                }
            }
            if (code > next || (code == next && prev < 0))
            {
                return $"code {code} with only {next} defined";
            }

            int sp = 0;
            int cur;
            if (code == next)
            {
                cur = prev;
            }
            else
            {
                cur = code;
            }
            while (cur >= clear + 2)
            {
                stack[sp++] = suffix[cur];
                cur = prefix[cur];
            }
            byte root = (byte)cur;
            stack[sp++] = root;

            for (int i = sp - 1; i >= 0 && wrote < outp.Length; i--)
            {
                outp[wrote++] = stack[i];
            }
            if (code == next && wrote < outp.Length)
            {
                outp[wrote++] = root;
            }

            if (prev >= 0 && next < 4096)
            {
                prefix[next] = prev;
                suffix[next] = root;
                next++;
                if (next > top && codeSize < 12)
                {
                    codeSize++;
                    if (codeSize == 12)
                    {
                        top = 4096;
                    }
                    else
                    {
                        top = (1 << codeSize) - 1;
                    }
                }
            }
            prev = code;
        }
    }

    /// <summary>
    /// Represents the decoded contents of a GIF file, including its frames, color tables and
    /// delays.
    /// </summary>
    public sealed class GifHeader
    {
        public int W;
        public int H;
        public int TableSize;
        public bool Loops;
        public int Clears;
        public string? Error;

        public readonly List<byte[]> Frames = new();
        public readonly List<Rect> Subs = new();
        public readonly List<int> Delays = new();
        public readonly List<int> OwnTables = new();
        public readonly List<byte[]> Cells = new();
        public readonly List<int> SeeThrough = new();
    }
}
