namespace CapChief.Shell;

/// <summary>
/// Paints the frozen capture onto the overlay window: the screen dimmed outside the selected area,
/// the marks, the grips, the size label, the mark being drawn and a toast.
/// </summary>
public sealed class OverlayPainter
{
    private readonly Desktop _desk;
    private readonly Metrics _metrics;
    private readonly LabelPainter _labelPainter;
    private readonly Selecting _selecting;
    private readonly Drawing _drawing;
    private readonly Document _document;
    private readonly Review _review;
    private readonly MarkPainter _markPainter;
    private readonly DimTile _dim = new(Palette.DimA);
    private readonly Shapes _shapes = new();

    private readonly Surface _dimmed = new();
    private readonly Surface _backdrop = new();
    private readonly Surface _buffer = new();
    private readonly Region _stale = new();

    private Surface? _selectedOn;
    private OverlayContent _content;
    private string _toast = "";
    private Rect _toastAt;

    public OverlayPainter(Desktop desk, Metrics metrics, LabelPainter labelPainter, Selecting selecting, Drawing drawing, Document document, Review review, MarkPainter markPainter)
    {
        _desk = desk;
        _metrics = metrics;
        _labelPainter = labelPainter;
        _selecting = selecting;
        _drawing = drawing;
        _document = document;
        _review = review;
        _markPainter = markPainter;
    }

    private Rect Bounds
    {
        get
        {
            return _desk.Bounds;
        }
    }

    public bool IsOpen
    {
        get
        {
            return _buffer.IsOpen;
        }
    }

    public DeviceContext Backdrop
    {
        get
        {
            return _backdrop.DeviceContext;
        }
    }

    public int CropFrameWidth
    {
        get
        {
            return Math.Max((int)_metrics.BorderWidth, _metrics.Tolerance.GripRadius) + 3;
        }
    }

    public bool Open(Surface screen, OverlayContent content)
    {
        Rect all = Bounds;
        if (!Allocate(_dimmed) || !Allocate(_backdrop) || !Allocate(_buffer))
        {
            Close();
            return false;
        }
        _dimmed.DeviceContext.Blit(all, screen.DeviceContext, 0, 0);
        _dim.Over(_dimmed.DeviceContext, all);
        _stale.Free();
        MarkStale(all);
        Show(content, screen);
        return true;
    }

    public void Show(OverlayContent content, Surface? selectedOn = null)
    {
        _content = content;
        if (content == OverlayContent.Selecting)
        {
            _selectedOn = selectedOn;
        }
        else
        {
            _selectedOn = null;
        }
    }

    private bool Allocate(Surface s)
    {
        Rect all = Bounds;
        return (s.IsOpen && s.Width == all.Width && s.Height == all.Height) || s.OpenDib(all.Width, all.Height);
    }

    public void Close()
    {
        _dimmed.Free();
        _backdrop.Free();
        _buffer.Free();
        _dim.Free();
        _stale.Free();
        EndToast();
        Show(OverlayContent.None);
    }

    public Rect ShowToast(Rect crop, string text)
    {
        _toast = text;
        _toastAt = _labelPainter.ToastRect(crop, text);
        return _toastAt;
    }

    public Rect EndToast()
    {
        Rect was = _toastAt;
        _toast = "";
        _toastAt = default;
        return was;
    }

    public void MarkStale(Rect r)
    {
        r = Rect.Intersect(r, Bounds);
        if (r.IsEmpty || !_stale.Open())
        {
            return;
        }
        _stale.Add(r);
    }

    public void Paint(DeviceContext window, Rect box, Region? update)
    {
        box = Rect.Intersect(box, Bounds);
        if (!IsOpen || box.IsEmpty)
        {
            return;
        }

        UpdateBackdrop();
        DeviceContext buffer = _buffer.DeviceContext;
        ClippedDc clipped;
        if (update is { Exists: true })
        {
            clipped = ClippedDc.ToRegion(buffer, update);
        }
        else
        {
            clipped = ClippedDc.ToRect(buffer, box);
        }
        using (clipped)
        {
            buffer.Blit(box, _backdrop.DeviceContext, box.Left, box.Top);
            DrawOnTop(buffer);
        }
        window.Blit(box, buffer, box.Left, box.Top);
    }

    public void UpdateBackdrop()
    {
        if (!IsOpen || !_stale.Exists || !_stale.Box(out Rect box))
        {
            return;
        }

        DeviceContext backdrop = _backdrop.DeviceContext;
        (Surface? picture, Point at, Rect crop) = Inside();
        using (ClippedDc.ToRegion(backdrop, _stale))
        {
            backdrop.Blit(box, _dimmed.DeviceContext, box.Left, box.Top);
            if (_content == OverlayContent.Review && picture is { IsOpen: true })
            {
                DimRecordingAround(backdrop, box, picture, picture.Bounds.Moved(at), crop);
            }

            Rect inside = Rect.Intersect(crop, box);
            if (!inside.IsEmpty)
            {
                using (ClippedDc.ToRect(backdrop, inside))
                {
                    if (picture is { IsOpen: true })
                    {
                        backdrop.Blit(inside, picture.DeviceContext, inside.Left - at.X, inside.Top - at.Y);
                    }
                    else
                    {
                        backdrop.FillRect(inside, 0);
                    }
                }
            }

            if (_content == OverlayContent.Annotating && _document.Capture is { } capture)
            {
                _markPainter.DrawMarks(backdrop, capture, box, _drawing.EditedIndex, _drawing.MarksOffset);
            }
        }
        _stale.Free();
    }

    private void DimRecordingAround(DeviceContext backdrop, Rect box, Surface frame, Rect recorded, Rect crop)
    {
        Span<Rect> bands = stackalloc Rect[RectBands.OutsideBandCount];
        int n = RectBands.Outside(recorded, crop, bands);
        for (int i = 0; i < n; i++)
        {
            Rect band = Rect.Intersect(bands[i], box);
            if (band.IsEmpty)
            {
                continue;
            }
            backdrop.Blit(band, frame.DeviceContext, band.Left - recorded.Left, band.Top - recorded.Top);
            _dim.Over(backdrop, band);
        }
    }

    private (Surface? Picture, Point At, Rect Crop) Inside()
    {
        switch (_content)
        {
            case OverlayContent.Selecting:
                return (_selectedOn, default, _selecting.Selection);
            case OverlayContent.Annotating when _document.Capture is { } c:
                return (c.Picture, default, CropOf(c));
            case OverlayContent.Review when _document.Capture is { Clip: { } clip } c:
                return (_review.Frame, clip.Area.TopLeft, CropOf(c));
            default:
                return (null, default, default);
        }
    }

    private void DrawOnTop(DeviceContext dc)
    {
        switch (_content)
        {
            case OverlayContent.Selecting:
                if (_selecting.Selection.IsEmpty)
                {
                    return;
                }
                if (_selecting.ShowsTarget)
                {
                    DrawCropBorder(dc, _selecting.Selection, grips: false);
                }
                _labelPainter.DrawSizeLabel(dc, _selecting.Selection, _selecting.Typed);
                break;
            case OverlayContent.Annotating or OverlayContent.Review when _document.Capture is { } capture:
                DrawCropBorder(dc, CropOf(capture), grips: true);
                DrawLiveMarks(dc, capture);
                if (_drawing.IsDraggingCrop)
                {
                    _labelPainter.DrawSizeLabel(dc, _drawing.DraggedCrop, null);
                }
                if (_toast.Length > 0)
                {
                    _labelPainter.DrawToast(dc, _toastAt, _toast);
                }
                break;
        }
    }

    private void DrawLiveMarks(DeviceContext dc, Capture capture)
    {
        Marks marks = capture.Marks;
        if (!_drawing.IsDragImageShown)
        {
            if (_drawing.DraggedMark is { } dragged)
            {
                _markPainter.DrawMark(dc, capture, dragged, marks.StepNumberOf(_drawing.DraggedIndex), default);
            }
            if ((_drawing.DraggedMark ?? _document.SelectedMark?.Moved(_drawing.MarksOffset)) is { } selected)
            {
                _markPainter.DrawSelectedMark(dc, selected);
            }
        }
        if (_drawing.MarkBeingDrawn is { } drawn)
        {
            int step;
            if (drawn is StepMark)
            {
                step = marks.NextStepNumber;
            }
            else
            {
                step = 0;
            }
            _markPainter.DrawMark(dc, capture, drawn, step, default);
        }
    }

    private Rect CropOf(Capture capture)
    {
        if (_drawing.IsDraggingCrop)
        {
            return _drawing.DraggedCrop;
        }
        else
        {
            return capture.Crop;
        }
    }

    private void DrawCropBorder(DeviceContext dc, Rect crop, bool grips)
    {
        _shapes.DrawBox(dc, crop, Palette.AccentRef, _metrics.BorderWidth);
        if (!grips)
        {
            return;
        }
        foreach (Grip g in RectGrips.All)
        {
            MarkPainter.DrawGrip(dc, crop.GripRect(g, _metrics.Tolerance.GripRadius));
        }
    }

    public Rect SizeLabelArea(Rect crop)
    {
        if (crop.IsEmpty)
        {
            return default;
        }
        else
        {
            return _labelPainter.SizeLabelRect(crop).Inflated(2);
        }
    }
}

/// <summary>
/// Specifies what the overlay window shows over the frozen screen: an area being chosen, a capture
/// being drawn on, or a recording being reviewed.
/// </summary>
public enum OverlayContent
{
    None,
    Selecting,
    Annotating,
    Review,
}
