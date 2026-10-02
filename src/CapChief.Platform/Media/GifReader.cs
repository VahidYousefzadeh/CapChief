namespace CapChief.Media;

/// <summary>
/// Reads an animated GIF written by GifWriter, through the frame index kept while writing, and
/// composes the full-color image shown at any frame. Like GifWriter, it knows no interlacing and
/// leaves each frame in place. Damaged data makes it return false or null instead of throwing.
/// </summary>
public sealed class GifReader
{
    private const int SnapBudget = 64 << 20;
    private const long MaxPx = 64L << 20;

    private readonly byte[] _fileTable = new byte[256 * 3];
    private readonly byte[] _ownTable = new byte[256 * 3];

    private readonly short[] _prefix = new short[Gif89a.MaxCodes];
    private readonly byte[] _suffix = new byte[Gif89a.MaxCodes];
    private readonly byte[] _run = new byte[Gif89a.MaxCodes];

    private byte[] _data = [];
    private GifFrame[] _frames = [];
    private int _w, _h;
    private bool _open;

    private byte[] _canvas = [];
    private int _shown = -1;

    private byte[]?[] _snaps = [];
    private int _snapEvery = 1;

    public int Width
    {
        get
        {
            return _w;
        }
    }

    public int Height
    {
        get
        {
            return _h;
        }
    }

    public bool IsOpen
    {
        get
        {
            return _open;
        }
    }

    public byte[] Bytes
    {
        get
        {
            return _data;
        }
    }

    public IReadOnlyList<GifFrame> Index
    {
        get
        {
            return _frames;
        }
    }

    public bool Open(byte[] data, IReadOnlyList<GifFrame> frames)
    {
        Close();
        _data = data;

        if (frames.Count == 0 || !ParseHeader())
        {
            Close();
            return false;
        }

        _frames = new GifFrame[frames.Count];
        for (int i = 0; i < frames.Count; i++)
        {
            _frames[i] = frames[i];
            if (_frames[i].Offset < 0 || _frames[i].Length <= 0 || _frames[i].Offset + _frames[i].Length > _data.Length)
            {
                Close();
                return false;
            }
        }

        _canvas = new byte[_w * _h * 4];

        long each = (long)_w * _h * 4;
        int most = (int)Math.Max(1, SnapBudget / Math.Max(1, each));
        _snapEvery = Math.Max(1, (_frames.Length + most - 1) / most);
        _snaps = new byte[]?[_frames.Length / _snapEvery + 1];

        _shown = -1;
        _open = true;
        return true;
    }

    public void Close()
    {
        _data = [];
        _frames = [];
        _canvas = [];
        _snaps = [];
        _shown = -1;
        _w = _h = 0;
        _open = false;
    }

    public byte[]? Composite(int upto)
    {
        if (!_open || upto < 0 || upto >= _frames.Length)
        {
            return null;
        }
        if (_shown == upto)
        {
            return _canvas;
        }

        if (_shown < 0 || _shown > upto)
        {
            int k = upto / _snapEvery;
            while (k >= 0 && _snaps[k] is null)
            {
                k--;
            }
            if (k >= 0)
            {
                Array.Copy(_snaps[k]!, _canvas, _canvas.Length);
                _shown = k * _snapEvery;
            }
            else
            {
                Array.Clear(_canvas);
                _shown = -1;
            }
        }

        for (int i = _shown + 1; i <= upto; i++)
        {
            if (!Apply(i))
            {
                _shown = -1;
                return null;
            }
            _shown = i;
            if (i % _snapEvery == 0 && _snaps[i / _snapEvery] is null)
            {
                _snaps[i / _snapEvery] = (byte[])_canvas.Clone();
            }
        }
        return _canvas;
    }

    private bool ParseHeader()
    {
        if (_data.Length < Gif89a.HeaderBytes)
        {
            return false;
        }
        for (int i = 0; i < Gif89a.Signature.Length; i++)
        {
            if (_data[i] != Gif89a.Signature[i])
            {
                return false;
            }
        }

        _w = ShortAt(6);
        _h = ShortAt(8);
        if (_w <= 0 || _h <= 0 || (long)_w * _h > MaxPx)
        {
            return false;
        }

        int packed = _data[10];
        if ((packed & Gif89a.GlobalTableFlag) == 0)
        {
            return false;
        }
        int n = 3 * Gif89a.TableEntries(packed);
        if (Gif89a.HeaderBytes + n > _data.Length || n > _fileTable.Length)
        {
            return false;
        }

        Array.Clear(_fileTable);
        Array.Copy(_data, Gif89a.HeaderBytes, _fileTable, 0, n);
        return true;
    }

    private bool Apply(int i)
    {
        int at = (int)_frames[i].Offset;
        int end = at + _frames[i].Length;
        int transparent = -1;

        while (at < end && _data[at] == Gif89a.Extension)
        {
            at++;
            if (at >= end)
            {
                return false;
            }
            int label = _data[at++];
            bool control = label == Gif89a.GraphicControlLabel && at + Gif89a.GraphicControlSize < end;
            if (control && (_data[at + 1] & Gif89a.TransparentFlag) != 0)
            {
                transparent = _data[at + 4];
            }
            at = SkipBlocks(at, end);
            if (at < 0)
            {
                return false;
            }
        }

        if (at >= end || _data[at] != Gif89a.ImageDescriptor || at + 10 > end)
        {
            return false;
        }
        at++;
        Rect sub = Rect.At(ShortAt(at), ShortAt(at + 2), ShortAt(at + 4), ShortAt(at + 6));
        int imgPacked = _data[at + 8];
        at += 9;
        byte[] table = _fileTable;
        if ((imgPacked & Gif89a.LocalTableFlag) != 0)
        {
            int n = 3 * Gif89a.TableEntries(imgPacked);
            if (at + n > end)
            {
                return false;
            }
            Array.Clear(_ownTable);
            Array.Copy(_data, at, _ownTable, 0, n);
            table = _ownTable;
            at += n;
        }
        if (at >= end)
        {
            return false;
        }
        if (sub.Left < 0 || sub.Top < 0 || sub.Right > _w || sub.Bottom > _h || sub.IsEmpty)
        {
            return false;
        }

        int minCodeSize = _data[at++];
        return Inflate(at, end, minCodeSize, sub, transparent, table);
    }

    private int ShortAt(int at)
    {
        return _data[at] | (_data[at + 1] << 8);
    }

    private int SkipBlocks(int at, int end)
    {
        while (at < end)
        {
            int n = _data[at++];
            if (n == 0)
            {
                return at;
            }
            at += n;
        }
        return -1;
    }

    private bool Inflate(int at, int end, int minCodeSize, Rect sub, int transparent, byte[] table)
    {
        if (minCodeSize < 2 || minCodeSize > 8)
        {
            return false;
        }
        int clear = 1 << minCodeSize;
        int stop = clear + 1;
        int next = clear + 2;
        int size = minCodeSize + 1;
        int top = (1 << size) - 1;
        int bits = 0;
        int held = 0;
        int prev = -1;

        int x = sub.Left;
        int y = sub.Top;
        int blockLeft = 0;

        while (true)
        {
            while (held < size)
            {
                if (blockLeft == 0)
                {
                    if (at >= end)
                    {
                        return false;
                    }
                    blockLeft = _data[at++];
                    if (blockLeft == 0 || at + blockLeft > end)
                    {
                        return false;
                    }
                }
                bits |= _data[at++] << held;
                held += 8;
                blockLeft--;
            }

            int code = bits & top;
            bits >>= size;
            held -= size;

            if (code == clear)
            {
                next = clear + 2;
                size = minCodeSize + 1;
                top = (1 << size) - 1;
                prev = -1;
                continue;
            }
            if (code == stop)
            {
                return true;
            }

            int n = 0;
            int walk = code;
            if (code == next && prev >= 0)
            {
                _run[n++] = FirstOf(prev, clear);
                walk = prev;
            }
            else if (code >= next)
            {
                return false;
            }

            while (walk >= clear + 2)
            {
                if (n >= _run.Length)
                {
                    return false;
                }
                _run[n++] = _suffix[walk];
                walk = _prefix[walk];
            }
            if (walk >= clear || n >= _run.Length)
            {
                return false;
            }
            _run[n++] = (byte)walk;

            for (int k = n - 1; k >= 0; k--)
            {
                if (y >= sub.Bottom)
                {
                    return true;
                }
                byte v = _run[k];
                if (v != transparent)
                {
                    int o = (y * _w + x) * 4;
                    int e = v * 3;
                    _canvas[o] = table[e + 2];
                    _canvas[o + 1] = table[e + 1];
                    _canvas[o + 2] = table[e];
                    _canvas[o + 3] = 0xFF;
                }
                if (++x >= sub.Right)
                {
                    x = sub.Left;
                    y++;
                }
            }

            if (prev >= 0 && next < Gif89a.MaxCodes)
            {
                _prefix[next] = (short)prev;
                _suffix[next] = _run[n - 1];
                next++;
                if (next > top && size < Gif89a.MaxCodeBits)
                {
                    size++;
                    top = (1 << size) - 1;
                }
            }
            prev = code;
        }
    }

    private byte FirstOf(int code, int clear)
    {
        for (int guard = 0; code >= clear + 2 && guard < _prefix.Length; guard++)
        {
            code = _prefix[code];
        }
        if (code < clear)
        {
            return (byte)code;
        }
        else
        {
            return 0;
        }
    }
}
