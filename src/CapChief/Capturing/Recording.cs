using System.Diagnostics;

namespace CapChief.Capturing;

/// <summary>
/// Records the selected area of the screen to a GIF file, one frame at a time, until it is stopped
/// or reaches its length limit.
/// </summary>
public sealed class Recording
{
    public const int MaxSeconds = 60;

    public const uint FrameMs = 100;

    private readonly ScreenSource _screen;
    private readonly Stopwatch _clock = new();
    private readonly List<GifFrame> _frames = new();
    private readonly object _gate = new();

    private Surface _queued = new();
    private Surface _incoming = new();
    private GifWriter? _gif;
    private FileStream? _file;
    private string? _temp;
    private Crop _crop;
    private Centiseconds _cadence;
    private long _queuedAt;

    private Thread? _writer;
    private Surface? _toWrite;
    private int _toWriteCs;
    private bool _writerEnding;
    private bool _writeFailed;

    public Recording(ScreenSource screen)
    {
        _screen = screen;
    }

    public Crop Crop
    {
        get
        {
            return _crop;
        }
    }

    public int Seconds
    {
        get
        {
            return (int)(_clock.ElapsedMilliseconds / 1000);
        }
    }

    public bool Start(Crop crop)
    {
        Abort();
        if (!_queued.OpenDib(crop.Width, crop.Height) || !_incoming.OpenDib(crop.Width, crop.Height))
        {
            Abort();
            return false;
        }
        _crop = crop;
        Take(_queued);

        try
        {
            _temp = Path.Combine(Path.GetTempPath(), $"{Product.FilePrefix}{Guid.NewGuid().ToString("N")}.gif");
            _file = new FileStream(_temp, FileMode.Create, FileAccess.Write);
        }
        catch (Exception e) when (Log.Tolerated("open the recording file", e))
        {
            Abort();
            return false;
        }

        var gif = new GifWriter(_file);
        if (!gif.Begin(crop.Width, crop.Height, _queued.Pixels))
        {
            Abort();
            return false;
        }
        _gif = gif;

        _writeFailed = false;
        _writerEnding = false;
        _writer = new Thread(WriteFrames) { IsBackground = true, Name = "record" };
        _writer.Start();

        _cadence = default;
        _queuedAt = 0;
        _clock.Restart();
        return true;
    }

    public bool Frame()
    {
        if (_gif is null || !WaitForWriter())
        {
            return false;
        }
        long now = _clock.ElapsedMilliseconds;
        if (now >= MaxSeconds * 1000L)
        {
            return false;
        }

        Write(_queued, _screen.ElapsedMs(_queuedAt, now));
        Take(_incoming);
        (_queued, _incoming) = (_incoming, _queued);
        _queuedAt = now;
        return true;
    }

    public Clip? Stop()
    {
        if (_gif is not { } gif)
        {
            return null;
        }
        string? path = _temp;
        if (WaitForWriter())
        {
            Write(_queued, _screen.ElapsedMs(_queuedAt, _clock.ElapsedMilliseconds));
        }
        bool written = WaitForWriter();
        EndWriter();
        try
        {
            if (!written || !gif.End())
            {
                path = null;
            }
            else
            {
                _file?.Flush();
            }
        }
        catch (Exception e) when (Log.Tolerated("finish the recording", e))
        {
            path = null;
        }

        if (path is null)
        {
            Abort();
            return null;
        }
        CloseOutput();
        _clock.Stop();

        var clip = new Clip(path, _frames.ToArray(), _crop);
        _temp = null;
        _frames.Clear();
        return clip;
    }

    public void Abort()
    {
        CloseOutput();
        _clock.Reset();
        _frames.Clear();
        if (_temp is not { } temp)
        {
            return;
        }
        Clip.DeleteTemporary(temp);
        _temp = null;
    }

    private void CloseOutput()
    {
        EndWriter();
        _gif = null;
        _file?.Dispose();
        _file = null;
        FreeFrames();
    }

    private void Write(Surface frame, int elapsedMs)
    {
        lock (_gate)
        {
            _toWrite = frame;
            _toWriteCs = _cadence.Next(elapsedMs);
            Monitor.PulseAll(_gate);
        }
    }

    private bool WaitForWriter()
    {
        lock (_gate)
        {
            while (_toWrite is not null)
            {
                Monitor.Wait(_gate);
            }
            return !_writeFailed;
        }
    }

    private void EndWriter()
    {
        if (_writer is not { } writer)
        {
            return;
        }
        lock (_gate)
        {
            _writerEnding = true;
            Monitor.PulseAll(_gate);
        }
        writer.Join();
        _writer = null;
    }

    private void WriteFrames()
    {
        while (true)
        {
            Surface frame;
            int cs;
            lock (_gate)
            {
                while (_toWrite is null && !_writerEnding)
                {
                    Monitor.Wait(_gate);
                }
                if (_toWrite is not { } toWrite)
                {
                    return;
                }
                frame = toWrite;
                cs = _toWriteCs;
            }
            bool written = WriteNow(frame, cs);
            lock (_gate)
            {
                _toWrite = null;
                _writeFailed |= !written;
                Monitor.PulseAll(_gate);
            }
        }
    }

    private bool WriteNow(Surface frame, int cs)
    {
        try
        {
            if (!_gif!.Frame(frame.Pixels, cs, out GifFrame written))
            {
                return false;
            }
            _frames.Add(written);
            return true;
        }
        catch (Exception e) when (Log.Tolerated("write a recorded frame", e))
        {
            return false;
        }
    }

    private void Take(Surface frame)
    {
        _screen.CaptureWithPointer(frame, _crop);
    }

    private void FreeFrames()
    {
        _queued.Free();
        _incoming.Free();
    }
}
