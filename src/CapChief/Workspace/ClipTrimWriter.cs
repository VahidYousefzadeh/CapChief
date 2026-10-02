namespace CapChief.Workspace;

/// <summary>
/// Writes a selected range of frames from a GIF recording to a new file.
/// </summary>
public sealed class ClipTrimWriter
{
    public const int TrailerBytes = 1;

    private readonly GifReader _reader;
    private int _basedAt = -1;
    private long _baseBytes;

    public ClipTrimWriter(GifReader reader)
    {
        _reader = reader;
    }

    public static long HeaderBytes(IReadOnlyList<GifFrame> index)
    {
        if (index.Count > 0)
        {
            return index[0].Offset;
        }
        else
        {
            return 0;
        }
    }

    public static int CopyFrom(Clip clip)
    {
        if (clip.In == 0)
        {
            return 0;
        }
        else
        {
            return clip.In + 1;
        }
    }

    public static long CopyBytes(Clip clip, IReadOnlyList<GifFrame> index)
    {
        long n = 0;
        for (int i = CopyFrom(clip); i <= clip.Out; i++)
        {
            n += index[i].Length;
        }
        return n;
    }

    public long SizeOf(Clip clip)
    {
        if (!_reader.IsOpen)
        {
            return 0;
        }
        if (clip.In != _basedAt)
        {
            _baseBytes = MeasureBase(clip);
            _basedAt = clip.In;
        }
        IReadOnlyList<GifFrame> index = _reader.Index;
        return HeaderBytes(index) + _baseBytes + CopyBytes(clip, index) + TrailerBytes;
    }

    public bool Write(string path, Clip clip)
    {
        if (!_reader.IsOpen || clip.Count != _reader.Index.Count)
        {
            return false;
        }
        byte[] data = _reader.Bytes;
        try
        {
            using var dst = new FileStream(path, FileMode.Create, FileAccess.Write);
            long head = HeaderBytes(_reader.Index);
            dst.Write(data, 0, (int)head);

            var gif = new GifWriter(dst);
            if (!gif.Resume(_reader.Width, _reader.Height, head))
            {
                return false;
            }

            if (clip.In > 0)
            {
                if (_reader.Composite(clip.In) is not { } composite)
                {
                    return false;
                }
                if (!gif.Frame(composite, clip.ScaledDelayCs(clip.In)))
                {
                    return false;
                }
            }

            for (int i = CopyFrom(clip); i <= clip.Out; i++)
            {
                CopyBlock(dst, data, i, clip.ScaledDelayCs(i));
            }
            return gif.End();
        }
        catch (Exception e) when (Log.Tolerated("write the trimmed clip", e))
        {
            return false;
        }
    }

    private long MeasureBase(Clip clip)
    {
        if (clip.In <= 0)
        {
            return 0;
        }
        if (_reader.Composite(clip.In) is not { } composite)
        {
            return 0;
        }

        var gif = new GifWriter(Stream.Null);
        if (!gif.Resume(_reader.Width, _reader.Height, 0))
        {
            return 0;
        }
        gif.Frame(composite, clip.ScaledDelayCs(clip.In));
        return gif.Written;
    }

    private void CopyBlock(Stream dst, byte[] data, int i, int delayCs)
    {
        GifFrame f = _reader.Index[i];
        int at = (int)f.Offset;
        int n = f.Length;
        int want = Math.Clamp(delayCs, 0, 0xFFFF);

        bool gce = n >= 8 && data[at] == Gif89a.Extension && data[at + 1] == Gif89a.GraphicControlLabel && data[at + 2] == Gif89a.GraphicControlSize;
        if (!gce || (data[at + 4] | (data[at + 5] << 8)) == want)
        {
            dst.Write(data, at, n);
            return;
        }
        dst.Write(data, at, 4);
        dst.WriteByte((byte)want);
        dst.WriteByte((byte)(want >> 8));
        dst.Write(data, at + 6, n - 6);
    }
}
