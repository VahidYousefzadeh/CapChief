using System.Runtime.InteropServices;

namespace CapChief.Media;

/// <summary>
/// Writes an animated GIF to a stream one full-color frame at a time.
/// </summary>
public sealed class GifWriter
{
    private const int MinCodeSize = 8;

    private const int ClearCode = 1 << MinCodeSize;
    private const int EndCode = ClearCode + 1;
    private const int FirstCode = ClearCode + 2;

    private const int SlotBits = 14;

    private const int Slots = 1 << SlotBits;

    private readonly Stream _output;

    private readonly int[] _keys = new int[Slots];
    private readonly short[] _codes = new short[Slots];

    private readonly byte[] _block = new byte[255];
    private int _w, _h;
    private bool _open;

    private ColorTable? _fileTable;
    private ColorTable? _ownTable;

    private byte[] _previous = [];
    private bool _hasPrevious;
    private byte[] _indices = [];

    private long _written;

    private int _next, _codeSize, _top;

    private int _bits, _held;
    private int _blockLen;

    public GifWriter(Stream dst)
    {
        _output = dst;
    }

    public long Written
    {
        get
        {
            return _written;
        }
    }

    private ReadOnlySpan<uint> Previous
    {
        get
        {
            return MemoryMarshal.Cast<byte, uint>(_previous.AsSpan());
        }
    }

    private void Emit(byte v)
    {
        _output.WriteByte(v);
        _written++;
    }

    private void Emit(ReadOnlySpan<byte> b)
    {
        _output.Write(b);
        _written += b.Length;
    }

    public bool Begin(int w, int h, ReadOnlySpan<byte> firstFrame)
    {
        if (!Fits(w, h) || firstFrame.Length < w * h * 4)
        {
            return false;
        }
        Open(w, h);

        var table = new ColorTable();
        table.Build(firstFrame, w, h, w * 4);
        _fileTable = table;

        foreach (char c in Gif89a.Signature)
        {
            Emit((byte)c);
        }
        EmitShort(w);
        EmitShort(h);
        Emit((byte)(Gif89a.GlobalTableFlag | Gif89a.ColorResolution | Gif89a.TableSizeBits(table.Size)));
        Emit(0);
        Emit(0);
        Emit(table.Rgb);

        Emit(Gif89a.Extension);
        Emit(Gif89a.ApplicationLabel);
        Emit(Gif89a.ApplicationSize);
        foreach (char c in Gif89a.NetscapeId)
        {
            Emit((byte)c);
        }
        Emit(Gif89a.LoopBlockSize);
        Emit(Gif89a.LoopBlockId);
        EmitShort(0);
        Emit(Gif89a.BlockEnd);
        return true;
    }

    public bool Resume(int w, int h, long written)
    {
        if (!Fits(w, h))
        {
            return false;
        }
        Open(w, h);
        _written = written;
        return true;
    }

    public bool Frame(ReadOnlySpan<byte> bgra, int delayCs)
    {
        if (!_open || bgra.Length < _previous.Length)
        {
            return false;
        }
        Rect sub;
        if (_hasPrevious)
        {
            sub = Changed(bgra);
        }
        else
        {
            sub = new Rect { Right = _w, Bottom = _h };
        }

        ColorTable table;
        bool own;
        if (_fileTable is { } file && Map(file, bgra, sub, nearest: !_hasPrevious))
        {
            table = file;
            own = false;
        }
        else
        {
            table = BuildOwnTable(bgra, sub);
            own = true;
        }

        Emit(Gif89a.Extension);
        Emit(Gif89a.GraphicControlLabel);
        Emit(Gif89a.GraphicControlSize);
        Emit(Gif89a.FramePacked);
        EmitShort(Math.Clamp(delayCs, 0, 0xFFFF));
        Emit(table.Transparent);
        Emit(Gif89a.BlockEnd);

        Emit(Gif89a.ImageDescriptor);
        EmitShort(sub.Left);
        EmitShort(sub.Top);
        EmitShort(sub.Width);
        EmitShort(sub.Height);
        if (own)
        {
            Emit((byte)(Gif89a.LocalTableFlag | Gif89a.TableSizeBits(table.Size)));
            Emit(table.Rgb);
        }
        else
        {
            Emit(Gif89a.NoLocalTable);
        }

        Lzw(_indices, sub);
        Keep(bgra, sub);
        return true;
    }

    public bool Frame(ReadOnlySpan<byte> bgra, int delayCs, out GifFrame written)
    {
        long at = _written;
        if (!Frame(bgra, delayCs))
        {
            written = default;
            return false;
        }
        written = new GifFrame(at, (int)(_written - at), delayCs);
        return true;
    }

    public bool End()
    {
        if (!_open)
        {
            return false;
        }
        Emit(Gif89a.Trailer);
        _open = false;
        return true;
    }

    private bool Fits(int w, int h)
    {
        return !_open && w > 0 && h > 0 && w <= 0xFFFF && h <= 0xFFFF && (long)w * h * 4 <= Array.MaxLength;
    }

    private void Open(int w, int h)
    {
        _w = w;
        _h = h;
        _previous = new byte[w * h * 4];
        _hasPrevious = false;
        _indices = new byte[w * h];
        _open = true;
    }

    private Rect Changed(ReadOnlySpan<byte> bgra)
    {
        ReadOnlySpan<uint> now = MemoryMarshal.Cast<byte, uint>(bgra);
        ReadOnlySpan<uint> was = Previous;
        int left = _w;
        int top = _h;
        int right = 0;
        int bottom = 0;
        for (int y = 0; y < _h; y++)
        {
            ReadOnlySpan<uint> before = was.Slice(y * _w, _w);
            ReadOnlySpan<uint> after = now.Slice(y * _w, _w);
            int first = before.CommonPrefixLength(after);
            if (first == _w)
            {
                continue;
            }

            int from = Math.Max(first, right);
            if (!before[from..].SequenceEqual(after[from..]))
            {
                int last = _w - 1;
                while (before[last] == after[last])
                {
                    last--;
                }
                right = last + 1;
            }
            left = Math.Min(left, first);
            top = Math.Min(top, y);
            bottom = y + 1;
        }
        if (right <= left)
        {
            return new Rect { Right = 1, Bottom = 1 };
        }
        else
        {
            return new Rect { Left = left, Top = top, Right = right, Bottom = bottom };
        }
    }

    private bool Map(ColorTable table, ReadOnlySpan<byte> bgra, Rect sub, bool nearest)
    {
        ReadOnlySpan<uint> now = MemoryMarshal.Cast<byte, uint>(bgra);
        ReadOnlySpan<uint> was = Previous;
        bool compare = _hasPrevious;
        byte transparent = table.Transparent;
        uint lastColor = 0;
        byte lastIndex = transparent;
        for (int y = sub.Top; y < sub.Bottom; y++)
        {
            for (int i = y * _w + sub.Left, end = y * _w + sub.Right; i < end; i++)
            {
                uint c = now[i];
                if (compare && c == was[i])
                {
                    _indices[i] = transparent;
                    continue;
                }
                if (c == lastColor && lastIndex != transparent)
                {
                    _indices[i] = lastIndex;
                    continue;
                }
                int r = (int)(c >> 16) & 0xFF;
                int g = (int)(c >> 8) & 0xFF;
                int b = (int)c & 0xFF;
                if (nearest)
                {
                    lastIndex = table.IndexOf(r, g, b);
                }
                else if (!table.TryIndexOf(r, g, b, out lastIndex))
                {
                    return false;
                }
                lastColor = c;
                _indices[i] = lastIndex;
            }
        }
        return true;
    }

    private ColorTable BuildOwnTable(ReadOnlySpan<byte> bgra, Rect sub)
    {
        ColorTable table = _ownTable ??= new ColorTable();
        ReadOnlySpan<uint> now = MemoryMarshal.Cast<byte, uint>(bgra);
        ReadOnlySpan<uint> was = Previous;
        bool compare = _hasPrevious;
        table.Clear();
        for (int y = sub.Top; y < sub.Bottom; y++)
        {
            for (int i = y * _w + sub.Left, end = y * _w + sub.Right; i < end; i++)
            {
                uint c = now[i];
                if (compare && c == was[i])
                {
                    continue;
                }
                table.Add((int)(c >> 16) & 0xFF, (int)(c >> 8) & 0xFF, (int)c & 0xFF);
            }
        }
        table.Build();
        Map(table, bgra, sub, nearest: true);
        return table;
    }

    private void Keep(ReadOnlySpan<byte> bgra, Rect sub)
    {
        for (int y = sub.Top; y < sub.Bottom; y++)
        {
            int at = (y * _w + sub.Left) * 4;
            int n = sub.Width * 4;
            bgra.Slice(at, n).CopyTo(_previous.AsSpan(at, n));
        }
        _hasPrevious = true;
    }

    private void EmitShort(int v)
    {
        Emit((byte)v);
        Emit((byte)(v >> 8));
    }

    private void Lzw(byte[] px, Rect r)
    {
        Emit(MinCodeSize);
        ResetTable();
        Put(ClearCode);

        int prefix = -1;
        for (int y = r.Top; y < r.Bottom; y++)
        {
            int row = y * _w;
            for (int x = r.Left; x < r.Right; x++)
            {
                int c = px[row + x];
                if (prefix < 0)
                {
                    prefix = c;
                    continue;
                }

                int slot = Slot(prefix, c);
                if (slot >= 0 && _keys[slot] != 0)
                {
                    prefix = _codes[slot];
                    continue;
                }

                Put(prefix);
                if (slot >= 0 && _next < Gif89a.MaxCodes)
                {
                    _keys[slot] = Key(prefix, c);
                    _codes[slot] = (short)_next++;
                }
                else
                {
                    Put(ClearCode);
                    ResetTable();
                }
                prefix = c;
            }
        }
        if (prefix >= 0)
        {
            Put(prefix);
        }
        Put(EndCode);

        FlushBits();
        FlushBlock();
        Emit(Gif89a.BlockEnd);
    }

    private void ResetTable()
    {
        Array.Clear(_keys);
        _next = FirstCode;
        _codeSize = MinCodeSize + 1;
        _top = (1 << _codeSize) - 1;
    }

    private static int Key(int prefix, int suffix)
    {
        return ((prefix << 8) | suffix) + 1;
    }

    private int Slot(int prefix, int suffix)
    {
        int want = Key(prefix, suffix);
        int i = Hash.Slot((uint)want, SlotBits);
        for (int probes = 0; probes < Slots; probes++)
        {
            if (_keys[i] == 0 || _keys[i] == want)
            {
                return i;
            }
            i = (i + 1) & (Slots - 1);
        }
        return -1;
    }

    private void Put(int code)
    {
        _bits |= code << _held;
        _held += _codeSize;
        while (_held >= 8)
        {
            AddToBlock((byte)_bits);
            _bits >>= 8;
            _held -= 8;
        }
        if (_next > _top && _codeSize < Gif89a.MaxCodeBits)
        {
            _codeSize++;
            _top = (1 << _codeSize) - 1;
        }
    }

    private void FlushBits()
    {
        if (_held > 0)
        {
            AddToBlock((byte)_bits);
        }
        _bits = 0;
        _held = 0;
    }

    private void AddToBlock(byte b)
    {
        _block[_blockLen++] = b;
        if (_blockLen == _block.Length)
        {
            FlushBlock();
        }
    }

    private void FlushBlock()
    {
        if (_blockLen == 0)
        {
            return;
        }
        Emit((byte)_blockLen);
        Emit(_block.AsSpan(0, _blockLen));
        _blockLen = 0;
    }
}
