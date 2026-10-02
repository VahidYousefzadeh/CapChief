namespace CapChief.Shell;

/// <summary>
/// Draws what shows over the live screen before it is frozen, on a layer of its own: the crop
/// outline and size label, the countdown and the key that cancels it, and the frame and label of a
/// recording.
/// </summary>
public sealed class LivePainter
{
    private const int RecFrameFloorPx = 2;
    private const float RecFrameDip = 3f;

    private const int DirtyInitial = 16;

    private const byte HighlightAlpha = 255;
    private const byte RecordingAlpha = 235;
    private const byte CountdownAlpha = 215;

    private readonly LabelPainter _labelPainter;
    private readonly ClockBadge _clockBadge;
    private readonly Metrics _metrics;
    private readonly Rect[] _border = new Rect[RectBands.OutsideBandCount];

    private int _borderN;
    private Rect _label;
    private Rect _badge;
    private Rect _cropDrawn;
    private Rect _countdownRect;
    private Rect[] _dirty = new Rect[DirtyInitial];
    private int _dirtyN;

    public LivePainter(LabelPainter labelPainter, Metrics metrics)
    {
        _labelPainter = labelPainter;
        _metrics = metrics;
        _clockBadge = new ClockBadge(metrics);
    }

    public Layer Layer { get; } = new();

    public Rect StopButton { get; private set; }

    public ReadOnlySpan<Rect> Dirty
    {
        get
        {
            return new(_dirty, 0, _dirtyN);
        }
    }

    private ReadOnlySpan<Rect> Border
    {
        get
        {
            return new(_border, 0, _borderN);
        }
    }

    public void Free()
    {
        Layer.Close();
        _clockBadge.Free();
    }

    public void Clear()
    {
        _borderN = 0;
        _label = default;
        _badge = default;
        _cropDrawn = default;
        _countdownRect = default;
        StopButton = default;
    }

    public void ClearLayer(byte alpha)
    {
        Layer.ClearAll(alpha);
        Clear();
    }

    public void DrawSelecting(Rect crop, bool exists, bool highlighted, bool timed, TypedSize? typed)
    {
        _dirtyN = 0;
        Span<Rect> changed = stackalloc Rect[RectBands.ChangeBandCount];
        int nc = RectBands.ChangeBands(_cropDrawn, crop, changed);
        AddDirty(changed[..nc]);
        AddDirty(Border);
        AddDirty(_label);
        AddDirty(_badge);

        foreach (Rect r in Dirty)
        {
            Layer.Restore(r, crop);
        }
        _borderN = 0;
        _label = default;
        _badge = default;
        StopButton = default;
        _cropDrawn = crop;

        if (!exists)
        {
            return;
        }

        if (highlighted)
        {
            _borderN = Layer.BorderBands(crop, (int)_metrics.BorderWidth, Palette.AccentRef, HighlightAlpha, _border);
            AddDirty(Border);
        }

        _label = _labelPainter.DrawSizeLabelInLayer(Layer, crop, typed);
        AddDirty(_label);

        if (!timed)
        {
            return;
        }
        _badge = _clockBadge.DrawInLayer(Layer, _clockBadge.Box(crop), crop);
        AddDirty(_badge);
    }

    public void DrawRecording(Rect crop, string label)
    {
        _dirtyN = 0;
        AddDirty(Border);
        AddDirty(_label);
        Layer.Erase(Dirty);

        int th = Length.AtLeast(RecFrameFloorPx, RecFrameDip).Px(_metrics.Scale);
        _borderN = Layer.BorderBands(crop.Inflated(th), th, Palette.AccentRef, RecordingAlpha, _border);
        AddDirty(Border);

        _label = _labelPainter.DrawRecInLayer(Layer, crop, label);
        StopButton = _label;
        AddDirty(_label);
    }

    public void DrawCountdown(Rect crop, int seconds, bool showsBorder, string tag)
    {
        Layer.Erase(Border);
        Layer.Erase(_countdownRect);
        Layer.Erase(_label);

        if (showsBorder)
        {
            _borderN = Layer.BorderBands(crop, Theme.CountdownRule.Px(_metrics.Scale), Palette.AccentRef, CountdownAlpha, _border);
        }
        else
        {
            _borderN = 0;
        }

        _countdownRect = _clockBadge.Box(crop);
        _labelPainter.DrawCountdownInLayer(Layer, _countdownRect, seconds.ToString());

        if (tag.Length > 0)
        {
            _label = _labelPainter.DrawCountdownTagInLayer(Layer, crop, tag);
        }
        else
        {
            _label = default;
        }
    }

    private void AddDirty(Rect r)
    {
        if (r.IsEmpty)
        {
            return;
        }
        if (_dirtyN == _dirty.Length)
        {
            Array.Resize(ref _dirty, _dirtyN * 2);
        }
        _dirty[_dirtyN++] = r;
    }

    private void AddDirty(ReadOnlySpan<Rect> rs)
    {
        foreach (Rect r in rs)
        {
            AddDirty(r);
        }
    }
}

/// <summary>
/// Specifies what the overlay shows over the live screen: an area being chosen, a countdown, or a
/// recording.
/// </summary>
public enum LiveContent
{
    None,
    Selection,
    Countdown,
    Recording,
}
