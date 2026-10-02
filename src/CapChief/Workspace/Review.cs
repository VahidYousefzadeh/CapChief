using System.Diagnostics;

namespace CapChief.Workspace;

/// <summary>
/// Handles reviewing a recording before it is saved: playing it in the area it was recorded from,
/// stepping through it, cutting it down to a smaller area, trimming its ends and changing its
/// speed.
/// </summary>
public sealed class Review
{
    private const uint PlayTickMs = 30;

    private readonly Document _document;
    private readonly GifReader _reader = new();
    private readonly GifReader _cut = new();
    private readonly Surface _frame = new();
    private readonly Stopwatch _playClock = new();

    public event Action? Changed;
    public event Action<uint>? TickMsChanged;
    public event Action? FrameChanged;

    private readonly object _gate = new();

    private ClipTrimWriter? _trim;
    private Thread? _cutter;
    private CancellationTokenSource? _stopCutting;
    private CutResult? _cutMade;
    private bool _cutEnded;
    private Action? _whenCut;
    private Rect _kept;
    private long _playedFromMs;
    private TrimGrip _grip;
    private int _gripFirst, _gripLast;

    public Review(Document document)
    {
        _document = document;
    }

    public Clip? Clip { get; private set; }

    public Surface Frame
    {
        get
        {
            return _frame;
        }
    }

    public bool IsPlaying { get; private set; }

    public long? SaveSize { get; private set; }

    public void WhenCut(Action post)
    {
        _whenCut = post;
    }

    public bool Open(Clip clip, Rect crop)
    {
        Close();
        if (clip.FilePath is not { } path)
        {
            return false;
        }

        byte[] data;
        try
        {
            data = File.ReadAllBytes(path);
        }
        catch (Exception e) when (Log.Tolerated("read the clip back", e))
        {
            return false;
        }
        if (!_reader.Open(data, clip.Frames))
        {
            return false;
        }
        if (!_frame.OpenDib(_reader.Width, _reader.Height))
        {
            _reader.Close();
            return false;
        }

        Clip = clip;
        Cut(clip, crop);
        Paint(clip);
        Play(clip);
        return true;
    }

    public void Close()
    {
        Pause();
        _playClock.Reset();
        _grip = TrimGrip.None;
        Clip = null;
        StopCutting();
        _trim = null;
        _kept = default;
        _reader.Close();
        _cut.Close();
        _frame.Free();
    }

    public void CropTo(Rect crop)
    {
        if (Clip is not { } clip)
        {
            return;
        }
        Cut(clip, crop);
        Changed?.Invoke();
    }

    public void TakeCut()
    {
        CutResult? made;
        lock (_gate)
        {
            if (!_cutEnded)
            {
                return;
            }
            made = _cutMade;
            _cutMade = null;
            _cutEnded = false;
        }
        _cutter = null;
        _stopCutting = null;
        if (Clip is not { } clip)
        {
            return;
        }

        if (made is { } cut && _cut.Open(cut.Bytes, cut.Frames))
        {
            _trim = new ClipTrimWriter(_cut);
        }
        SaveSize = _trim?.SizeOf(clip);
        Changed?.Invoke();
    }

    public void Tick()
    {
        if (Clip is not { } clip || !IsPlaying)
        {
            return;
        }
        int frame = clip.FrameAtMs(_playedFromMs + _playClock.ElapsedMilliseconds);
        if (frame == clip.Playhead)
        {
            return;
        }
        clip.Seek(frame);
        Show(clip);
    }

    public void TogglePlay()
    {
        if (Clip is not { } clip)
        {
            return;
        }
        if (IsPlaying)
        {
            Pause();
        }
        else
        {
            Play(clip);
        }
        Changed?.Invoke();
    }

    public void Pause()
    {
        if (!IsPlaying)
        {
            return;
        }
        IsPlaying = false;
        _playClock.Stop();
        TickMsChanged?.Invoke(0);
        Changed?.Invoke();
    }

    public void Advance(int frames)
    {
        if (Clip is not { } clip)
        {
            return;
        }
        Pause();
        clip.Advance(frames);
        Show(clip);
    }

    public void Jump(bool toEnd)
    {
        if (Clip is not { } clip)
        {
            return;
        }
        Pause();
        int target;
        if (toEnd)
        {
            target = clip.Out;
        }
        else
        {
            target = clip.In;
        }
        clip.Seek(target);
        Show(clip);
    }

    public void Trim(bool head)
    {
        if (Clip is not { } clip)
        {
            return;
        }
        if (head)
        {
            _document.TrimClip(clip.Playhead, clip.Out);
        }
        else
        {
            _document.TrimClip(clip.In, clip.Playhead);
        }
    }

    public void Untrim()
    {
        if (Clip is { } clip)
        {
            _document.TrimClip(0, clip.Count - 1);
        }
    }

    public void CycleSpeed()
    {
        if (Clip is { } clip)
        {
            _document.ChangeClipSpeed(clip.NextSpeed);
        }
    }

    public void OnClipEdited()
    {
        if (Clip is { } clip)
        {
            Trimmed(clip);
        }
    }

    public bool PressTimeline(Scrub at)
    {
        if (Clip is not { } clip)
        {
            return false;
        }
        _grip = at.Grip;
        if (_grip == TrimGrip.None)
        {
            return false;
        }
        (_gripFirst, _gripLast) = (clip.In, clip.Out);
        Pause();
        Scrubbed(clip, at);
        return true;
    }

    public void DragTimeline(Scrub at)
    {
        if (Clip is { } clip && _grip != TrimGrip.None)
        {
            Scrubbed(clip, at);
        }
    }

    public void ReleaseTimeline()
    {
        if (_grip is TrimGrip.In or TrimGrip.Out)
        {
            _document.RecordTrim(_gripFirst, _gripLast);
        }
        _grip = TrimGrip.None;
    }

    public bool SaveAs(string path)
    {
        if (_cutter is { } cutter)
        {
            cutter.Join();
            TakeCut();
        }
        if (Clip is not { FilePath: { } recorded } clip || _trim is not { } trim)
        {
            return false;
        }
        if (!clip.IsUnedited || _cut.IsOpen)
        {
            return trim.Write(path, clip);
        }
        try
        {
            File.Copy(recorded, path, overwrite: true);
            return true;
        }
        catch (Exception e) when (Log.Tolerated("copy the recording into place", e))
        {
            return false;
        }
    }

    private void Cut(Clip clip, Rect crop)
    {
        Rect kept = crop.Moved(-clip.Area.TopLeft);
        if (kept == _kept && (_trim is not null || _cutter is not null))
        {
            return;
        }

        _kept = kept;
        StopCutting();
        _trim = null;
        _cut.Close();
        if (kept == new Rect { Right = _reader.Width, Bottom = _reader.Height })
        {
            _trim = new ClipTrimWriter(_reader);
            SaveSize = _trim.SizeOf(clip);
            return;
        }

        SaveSize = null;
        byte[] recording = _reader.Bytes;
        IReadOnlyList<GifFrame> frames = _reader.Index;
        Action? done = _whenCut;
        var stop = new CancellationTokenSource();
        _stopCutting = stop;
        _cutter = new Thread(() =>
        {
            CutResult? made = CutClip(recording, frames, kept, stop.Token);
            lock (_gate)
            {
                if (stop.IsCancellationRequested)
                {
                    return;
                }
                _cutMade = made;
                _cutEnded = true;
            }
            done?.Invoke();
        }) { IsBackground = true, Name = "cut" };
        _cutter.Start();
    }

    private static CutResult? CutClip(byte[] bytes, IReadOnlyList<GifFrame> frames, Rect kept, CancellationToken stop)
    {
        var reader = new GifReader();
        if (!reader.Open(bytes, frames))
        {
            return null;
        }
        var copy = new MemoryStream();
        if (GifCrop.Write(reader, kept, copy, stop) is not { } cut)
        {
            return null;
        }
        return new CutResult(copy.ToArray(), cut);
    }

    private void StopCutting()
    {
        lock (_gate)
        {
            _stopCutting?.Cancel();
            _cutMade = null;
            _cutEnded = false;
        }
        _stopCutting = null;
        _cutter = null;
    }

    private void Scrubbed(Clip clip, Scrub at)
    {
        switch (_grip)
        {
            case TrimGrip.In:
                clip.SetIn(at.Boundary);
                break;
            case TrimGrip.Out:
                clip.SetOut(at.Boundary - 1);
                break;
            default:
                clip.Seek(at.Frame);
                break;
        }
        Trimmed(clip);
    }

    private void Trimmed(Clip clip)
    {
        SaveSize = _trim?.SizeOf(clip);
        RestartPlayClock(clip);
        Show(clip);
    }

    private void Show(Clip clip)
    {
        Paint(clip);
        FrameChanged?.Invoke();
        Changed?.Invoke();
    }

    private void Paint(Clip clip)
    {
        if (_reader.Composite(clip.Playhead) is { } pixels)
        {
            _frame.WriteFrom(pixels);
        }
    }

    private void Play(Clip clip)
    {
        IsPlaying = true;
        RestartPlayClock(clip);
        TickMsChanged?.Invoke(PlayTickMs);
    }

    private void RestartPlayClock(Clip clip)
    {
        _playedFromMs = clip.MsOf(clip.Playhead);
        if (IsPlaying)
        {
            _playClock.Restart();
        }
    }

    /// <summary>
    /// Represents the cut-down copy of a clip: its GIF bytes and the frames it holds.
    /// </summary>
    private readonly record struct CutResult(byte[] Bytes, List<GifFrame> Frames);
}

/// <summary>
/// Specifies which part of the review timeline the user is dragging.
/// </summary>
public enum TrimGrip
{
    None,
    In,
    Out,
    Bar,
}

/// <summary>
/// Represents a pointer position on the review timeline.
/// </summary>
public readonly record struct Scrub(TrimGrip Grip, int Frame, int Boundary);
