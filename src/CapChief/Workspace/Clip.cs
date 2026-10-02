namespace CapChief.Workspace;

/// <summary>
/// Represents a recording: the GIF file it was written to, the area of the screen it covers, its
/// frames, and its trim points, playback speed and current position.
/// </summary>
public sealed class Clip
{
    private const int MinimumDelayCs = 2;

    public const int SlowedDelayCs = 10;

    private readonly int[] _recordedDelays;
    private readonly long[] _recordedStarts;
    private readonly int[] _scaledDelays;
    // When each frame starts, in ms from the in point; frames before it start at 0.
    private readonly long[] _playedStarts;

    public Clip(IReadOnlyList<int> delaysCs)
    {
        _recordedDelays = [.. delaysCs];

        _recordedStarts = new long[_recordedDelays.Length + 1];
        for (int i = 0; i < _recordedDelays.Length; i++)
        {
            _recordedStarts[i + 1] = _recordedStarts[i] + _recordedDelays[i];
        }

        _scaledDelays = new int[_recordedDelays.Length];
        _playedStarts = new long[_recordedDelays.Length + 1];
        Out = _recordedDelays.Length - 1;
        RecomputeTimes();
    }

    public Clip(string filePath, IReadOnlyList<GifFrame> frames, Rect area)
        : this(GifFrame.DelaysOf(frames))
    {
        FilePath = filePath;
        Frames = frames;
        Area = area;
    }

    private Clip(Clip from)
        : this(from._recordedDelays)
    {
        FilePath = from.FilePath;
        Frames = from.Frames;
        Area = from.Area;
        In = from.In;
        Out = from.Out;
        Speed = from.Speed;
        RecomputeTimes();
        Playhead = from.Playhead;
    }

    public string? FilePath { get; private set; }

    public IReadOnlyList<GifFrame> Frames { get; } = [];

    public Rect Area { get; }

    public int Count
    {
        get
        {
            return _recordedDelays.Length;
        }
    }

    public int In { get; private set; }

    public int Out { get; private set; }

    public int Playhead { get; private set; }

    public ClipSpeed Speed { get; private set; }

    public bool IsUnedited
    {
        get
        {
            return In == 0 && Out == Count - 1 && Speed == ClipSpeed.Normal;
        }
    }

    public long LengthMs
    {
        get
        {
            return _playedStarts[Out + 1];
        }
    }

    public ClipSpeed NextSpeed
    {
        get
        {
            switch (Speed)
            {
                case ClipSpeed.Normal:
                    return ClipSpeed.Double;
                case ClipSpeed.Double:
                    return ClipSpeed.Half;
                default:
                    return ClipSpeed.Normal;
            }
        }
    }

    public Clip Copy()
    {
        return new(this);
    }

    public void DeleteFile()
    {
        if (FilePath is not { } path)
        {
            return;
        }
        FilePath = null;
        DeleteTemporary(path);
    }

    public static void DeleteTemporary(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception e) when (Log.Tolerated("delete the temporary clip", e)) { }
    }

    public bool HasSameEdits(Clip other)
    {
        return In == other.In && Out == other.Out && Speed == other.Speed;
    }

    public int ScaledDelayCs(int i)
    {
        return _scaledDelays[i];
    }

    public int PlayedDelayCs(int i)
    {
        if (_scaledDelays[i] < MinimumDelayCs)
        {
            return SlowedDelayCs;
        }
        else
        {
            return _scaledDelays[i];
        }
    }

    public long MsOf(int i)
    {
        return _playedStarts[i];
    }

    public int FrameAtMs(long ms)
    {
        long span = LengthMs;
        if (span <= 0)
        {
            return In;
        }
        long at = ms % span;
        if (at < 0)
        {
            at += span;
        }

        for (int i = In; i <= Out; i++)
        {
            if (at < _playedStarts[i + 1])
            {
                return i;
            }
        }
        return Out;
    }

    public void Seek(int frame)
    {
        Playhead = Math.Clamp(frame, In, Out);
    }

    public void Advance(int delta)
    {
        Seek(Playhead + delta);
    }

    public void SetIn(int frame)
    {
        In = Math.Clamp(frame, 0, Out);
        RecomputeTimes();
        Seek(Playhead);
    }

    public void SetOut(int frame)
    {
        Out = Math.Clamp(frame, In, Count - 1);
        RecomputeTimes();
        Seek(Playhead);
    }

    public void Trim(int first, int last)
    {
        In = Math.Clamp(first, 0, Count - 1);
        Out = Math.Clamp(last, In, Count - 1);
        RecomputeTimes();
        Seek(Playhead);
    }

    public void SetSpeed(ClipSpeed speed)
    {
        Speed = speed;
        RecomputeTimes();
    }

    private void RecomputeTimes()
    {
        _playedStarts[0] = 0;
        for (int i = 0; i < _recordedDelays.Length; i++)
        {
            if (i < In)
            {
                _scaledDelays[i] = _recordedDelays[i];
                _playedStarts[i + 1] = _playedStarts[i];
                continue;
            }
            long before = _recordedStarts[i] - _recordedStarts[In];
            _scaledDelays[i] = (int)(Scale(before + _recordedDelays[i]) - Scale(before));
            _playedStarts[i + 1] = _playedStarts[i] + PlayedDelayCs(i) * 10L;
        }
    }

    private long Scale(long cs)
    {
        switch (Speed)
        {
            case ClipSpeed.Double:
                return cs / 2;
            case ClipSpeed.Half:
                return cs * 2;
            default:
                return cs;
        }
    }
}

/// <summary>
/// Specifies the playback speed of a recording: normal, double or half.
/// </summary>
public enum ClipSpeed
{
    Normal,
    Double,
    Half,
}
