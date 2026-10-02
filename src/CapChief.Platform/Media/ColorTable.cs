namespace CapChief.Media;

/// <summary>
/// Builds a GIF palette of up to 255 colors and maps image pixels to it.
/// </summary>
public sealed class ColorTable
{
    public const int MaxColors = 255;

    private const int Levels = 32;
    private const int RedShift = 10;
    private const int GreenShift = 5;
    private const int BlueShift = 0;

    private const int Cells = Levels * Levels * Levels;

    private const int SlotBits = 10;

    private const int Slots = 1 << SlotBits;

    private readonly long[] _tally = new long[Cells * 4];
    private readonly int[] _cells = new int[Cells];
    private readonly int[] _sorted = new int[Cells];
    private readonly byte[] _rgb = new byte[256 * 3];
    private readonly byte[] _boxOf = new byte[Cells];
    private readonly byte[] _byRed = new byte[MaxColors + 1];
    private readonly byte[] _nearest = new byte[Cells];
    private readonly int[] _nearestFound = new int[Cells];
    private readonly int[] _holdsEntry = new int[Cells];
    private readonly int[] _keys = new int[Slots];
    private readonly byte[] _vals = new byte[Slots];
    private readonly int[] _lo = new int[MaxColors + 1];
    private readonly int[] _hi = new int[MaxColors + 1];
    private readonly long[] _weight = new long[MaxColors + 1];
    private int _builds;
    private uint _lastAdded;

    public int Count { get; private set; }

    public bool IsExact { get; private set; }

    public int Size
    {
        get
        {
            int entries = 2;
            while (entries <= Count)
            {
                entries <<= 1;
            }
            return entries;
        }
    }

    public byte Transparent
    {
        get
        {
            return (byte)(Size - 1);
        }
    }

    public ReadOnlySpan<byte> Rgb
    {
        get
        {
            return _rgb.AsSpan(0, Size * 3);
        }
    }

    private static int CellOf(int r, int g, int b)
    {
        return ((r >> 3) << RedShift) | ((g >> 3) << GreenShift) | (b >> 3);
    }

    private static int CellOf(uint rgb)
    {
        return CellOf((int)(rgb >> 16) & 0xFF, (int)(rgb >> 8) & 0xFF, (int)rgb & 0xFF);
    }

    private static void LevelsOf(int cell, out int r, out int g, out int b)
    {
        r = cell >> RedShift;
        g = (cell >> GreenShift) & (Levels - 1);
        b = cell & (Levels - 1);
    }

    private static void CenterOf(int cell, out int r, out int g, out int b)
    {
        LevelsOf(cell, out r, out g, out b);
        r = (r << 3) | 4;
        g = (g << 3) | 4;
        b = (b << 3) | 4;
    }

    public void Build(ReadOnlySpan<byte> bgra, int w, int h, int stride)
    {
        Clear();
        for (int y = 0; y < h; y++)
        {
            int row = y * stride;
            for (int x = 0; x < w; x++)
            {
                int q = row + x * 4;
                Add(bgra[q + 2], bgra[q + 1], bgra[q]);
            }
        }
        Build();
    }

    public void Clear()
    {
        Array.Clear(_tally);
        Array.Clear(_keys);
        Array.Clear(_rgb);
        Count = 0;
        IsExact = true;
        _builds++;
        _lastAdded = uint.MaxValue;
    }

    public void Add(int r, int g, int b)
    {
        int t = CellOf(r, g, b) * 4;
        _tally[t]++;
        _tally[t + 1] += r;
        _tally[t + 2] += g;
        _tally[t + 3] += b;

        if (!IsExact)
        {
            return;
        }
        uint rgb = ((uint)r << 16) | ((uint)g << 8) | (uint)b;
        if (rgb == _lastAdded)
        {
            return;
        }
        _lastAdded = rgb;
        if (Find(rgb) >= 0)
        {
            return;
        }
        if (Count == MaxColors)
        {
            IsExact = false;
            return;
        }
        SetEntry(Count, r, g, b);
        Seed(rgb, (byte)Count);
        Count++;
    }

    public void Build()
    {
        if (!IsExact)
        {
            Array.Clear(_keys);
            MedianCut();
            for (int i = 0; i < Count; i++)
            {
                Seed(Packed(i), (byte)i);
            }
        }
        if (Count == 0)
        {
            Count = 1;
            Seed(0, 0);
        }
        SortByRed();
    }

    private void SortByRed()
    {
        Span<int> starts = stackalloc int[257];
        for (int i = 0; i < Count; i++)
        {
            starts[_rgb[i * 3] + 1]++;
        }
        for (int red = 1; red <= 256; red++)
        {
            starts[red] += starts[red - 1];
        }
        for (int i = 0; i < Count; i++)
        {
            _byRed[starts[_rgb[i * 3]]++] = (byte)i;
        }
    }

    public void Entry(int i, out int r, out int g, out int b)
    {
        r = _rgb[i * 3];
        g = _rgb[i * 3 + 1];
        b = _rgb[i * 3 + 2];
    }

    private void SetEntry(int i, int r, int g, int b)
    {
        _rgb[i * 3] = (byte)r;
        _rgb[i * 3 + 1] = (byte)g;
        _rgb[i * 3 + 2] = (byte)b;
    }

    private uint Packed(int i)
    {
        return ((uint)_rgb[i * 3] << 16) | ((uint)_rgb[i * 3 + 1] << 8) | _rgb[i * 3 + 2];
    }

    public byte IndexOf(int r, int g, int b)
    {
        if (TryIndexOf(r, g, b, out byte index))
        {
            return index;
        }
        else
        {
            return Nearest(CellOf(r, g, b));
        }
    }

    public bool TryIndexOf(int r, int g, int b, out byte index)
    {
        index = 0;
        if (_holdsEntry[CellOf(r, g, b)] != _builds)
        {
            return false;
        }
        int at = Find(((uint)r << 16) | ((uint)g << 8) | (uint)b);
        if (at < 0)
        {
            return false;
        }
        index = _vals[at];
        return true;
    }

    private static int SlotOf(uint rgb)
    {
        return Hash.Slot(rgb, SlotBits);
    }

    private void Seed(uint rgb, byte index)
    {
        int i = SlotOf(rgb);
        while (_keys[i] != 0)
        {
            i = (i + 1) & (Slots - 1);
        }
        _keys[i] = (int)rgb + 1;
        _vals[i] = index;
        _holdsEntry[CellOf(rgb)] = _builds;
    }

    private int Find(uint rgb)
    {
        int want = (int)rgb + 1;
        for (int i = SlotOf(rgb); _keys[i] != 0; i = (i + 1) & (Slots - 1))
        {
            if (_keys[i] == want)
            {
                return i;
            }
        }
        return -1;
    }

    private byte Nearest(int cell)
    {
        if (_nearestFound[cell] == _builds)
        {
            return _nearest[cell];
        }
        CenterOf(cell, out int r, out int g, out int b);
        int best = Math.Min(_boxOf[cell], Count - 1);
        int bestD = Distance(best, r, g, b);

        int up = FirstWithRedAtLeast(r);
        int down = up - 1;
        while (up < Count || down >= 0)
        {
            if (up < Count)
            {
                int i = _byRed[up];
                int dr = _rgb[i * 3] - r;
                if (dr * dr > bestD)
                {
                    up = Count;
                }
                else
                {
                    Consider(i, r, g, b, ref best, ref bestD);
                    up++;
                }
            }
            if (down >= 0)
            {
                int i = _byRed[down];
                int dr = r - _rgb[i * 3];
                if (dr * dr > bestD)
                {
                    down = -1;
                }
                else
                {
                    Consider(i, r, g, b, ref best, ref bestD);
                    down--;
                }
            }
        }
        _nearestFound[cell] = _builds;
        _nearest[cell] = (byte)best;
        return _nearest[cell];
    }

    private void Consider(int i, int r, int g, int b, ref int best, ref int bestD)
    {
        int d = Distance(i, r, g, b);
        if (d > bestD || (d == bestD && i > best))
        {
            return;
        }
        bestD = d;
        best = i;
    }

    private int FirstWithRedAtLeast(int r)
    {
        int lo = 0;
        int hi = Count;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (_rgb[_byRed[mid] * 3] < r)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }
        return lo;
    }

    private int Distance(int i, int r, int g, int b)
    {
        int dr = r - _rgb[i * 3];
        int dg = g - _rgb[i * 3 + 1];
        int db = b - _rgb[i * 3 + 2];
        return dr * dr + dg * dg + db * db;
    }

    private void MedianCut()
    {
        int occupied = 0;
        long total = 0;
        for (int cell = 0; cell < Cells; cell++)
        {
            long n = _tally[cell * 4];
            if (n == 0)
            {
                continue;
            }
            _cells[occupied++] = cell;
            total += n;
        }

        int boxes = 1;
        _lo[0] = 0;
        _hi[0] = occupied;
        _weight[0] = total;
        while (boxes < MaxColors)
        {
            int pick = -1;
            long most = 0;
            for (int i = 0; i < boxes; i++)
            {
                if (_hi[i] - _lo[i] < 2 || _weight[i] <= most)
                {
                    continue;
                }
                most = _weight[i];
                pick = i;
            }
            if (pick < 0)
            {
                break;
            }

            int split = Split(_lo[pick], _hi[pick], _weight[pick], out long below);
            _lo[boxes] = split;
            _hi[boxes] = _hi[pick];
            _weight[boxes] = _weight[pick] - below;
            _hi[pick] = split;
            _weight[pick] = below;
            boxes++;
        }

        Count = 0;
        for (int i = 0; i < boxes; i++)
        {
            long n = 0;
            long r = 0;
            long g = 0;
            long b = 0;
            for (int k = _lo[i]; k < _hi[i]; k++)
            {
                int t = _cells[k] * 4;
                n += _tally[t];
                r += _tally[t + 1];
                g += _tally[t + 2];
                b += _tally[t + 3];
            }
            if (n == 0)
            {
                continue;
            }
            for (int k = _lo[i]; k < _hi[i]; k++)
            {
                _boxOf[_cells[k]] = (byte)Count;
            }
            SetEntry(Count, (int)Math.Clamp(r / n, 0, 255), (int)Math.Clamp(g / n, 0, 255), (int)Math.Clamp(b / n, 0, 255));
            Count++;
        }
    }

    private int Split(int lo, int hi, long weight, out long below)
    {
        int rLo = Levels - 1;
        int rHi = 0;
        int gLo = Levels - 1;
        int gHi = 0;
        int bLo = Levels - 1;
        int bHi = 0;
        for (int k = lo; k < hi; k++)
        {
            int cell = _cells[k];
            LevelsOf(cell, out int r, out int g, out int b);
            rLo = Math.Min(rLo, r);
            rHi = Math.Max(rHi, r);
            gLo = Math.Min(gLo, g);
            gHi = Math.Max(gHi, g);
            bLo = Math.Min(bLo, b);
            bHi = Math.Max(bHi, b);
        }
        int shift;
        if (rHi - rLo >= gHi - gLo && rHi - rLo >= bHi - bLo)
        {
            shift = RedShift;
        }
        else if (gHi - gLo >= bHi - bLo)
        {
            shift = GreenShift;
        }
        else
        {
            shift = BlueShift;
        }

        Span<int> starts = stackalloc int[Levels + 1];
        for (int k = lo; k < hi; k++)
        {
            starts[((_cells[k] >> shift) & (Levels - 1)) + 1]++;
        }
        for (int level = 1; level <= Levels; level++)
        {
            starts[level] += starts[level - 1];
        }
        for (int k = lo; k < hi; k++)
        {
            _sorted[lo + starts[(_cells[k] >> shift) & (Levels - 1)]++] = _cells[k];
        }
        Array.Copy(_sorted, lo, _cells, lo, hi - lo);

        long half = weight / 2;
        long acc = 0;
        int at = lo;
        while (at < hi - 1 && acc + _tally[_cells[at] * 4] < half)
        {
            acc += _tally[_cells[at++] * 4];
        }
        int split = Math.Clamp(at + 1, lo + 1, hi - 1);
        if (split == at + 1)
        {
            below = acc + _tally[_cells[at] * 4];
        }
        else
        {
            below = acc;
        }
        return split;
    }
}
