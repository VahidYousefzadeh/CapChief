namespace CapChief.Workspace;

/// <summary>
/// Handles drawing on the captured screenshot: drawing a new mark, moving and resizing a mark or the
/// selected area, and typing the text of a mark and setting its size.
/// </summary>
public sealed class Drawing
{
    private const float MinTextBoxWidthDip = 50f, MinTextBoxHeightDip = 20f;
    private const float TextBoxWidthDip = 150f, TextBoxHeightDip = 40f;

    private readonly Document _document;
    private readonly Overlay _overlay;
    private readonly Metrics _metrics;
    private readonly Tools _tools;
    private readonly TextMarkBox _box;
    private readonly MarkDragImage _dragImage;

    public event Action? ClickedOutsideCrop;

    private Gesture? _gesture;

    private TextMark _textMark = new();
    private int _textMarkAt = -1;
    private TextMark _textMarkBefore = new();

    public Drawing(Document document, Tools tools, Overlay overlay, Metrics metrics, TextMarkBox box, MarkDragImage dragImage)
    {
        _document = document;
        _tools = tools;
        _overlay = overlay;
        _metrics = metrics;
        _box = box;
        _dragImage = dragImage;
        _tools.Changed += OnToolsChanged;
    }

    private Capture Capture
    {
        get
        {
            return _document.Capture!;
        }
    }

    private Marks Marks
    {
        get
        {
            return Capture.Marks;
        }
    }

    public int DraggedIndex
    {
        get
        {
            if (_gesture is DraggingMark drag)
            {
                return drag.Index;
            }
            else
            {
                return -1;
            }
        }
    }

    public int EditedIndex
    {
        get
        {
            if (_gesture is DraggingMark drag)
            {
                return drag.Index;
            }
            else
            {
                return _textMarkAt;
            }
        }
    }

    public Mark? DraggedMark
    {
        get
        {
            if (_gesture is DraggingMark drag)
            {
                return drag.Mark;
            }
            else
            {
                return null;
            }
        }
    }

    public Rect DraggedCrop
    {
        get
        {
            if (_gesture is DraggingCrop drag)
            {
                return drag.Crop;
            }
            else
            {
                return default;
            }
        }
    }

    public Mark? MarkBeingDrawn
    {
        get
        {
            if (_gesture is DrawingMark { Mark.HasExtent: true } drawing)
            {
                return drawing.Mark;
            }
            else
            {
                return null;
            }
        }
    }

    public bool IsDraggingCrop
    {
        get
        {
            return _gesture is DraggingCrop;
        }
    }

    public Point MarksOffset
    {
        get
        {
            if (_gesture is DraggingCrop drag)
            {
                return drag.Before.OffsetTo(drag.Crop);
            }
            else
            {
                return default;
            }
        }
    }

    public bool IsDragImageShown
    {
        get
        {
            return _gesture is DraggingMark { IsOnImage: true };
        }
    }

    public bool HasMarksOnImage
    {
        get
        {
            return _gesture is DraggingCrop { IsOnImage: true };
        }
    }

    public bool IsEditingText
    {
        get
        {
            return _box.IsOpen;
        }
    }

    public Color TextColor
    {
        get
        {
            return _textMark.Color;
        }
    }

    public void Clear()
    {
        _gesture = null;
        _textMarkAt = -1;
    }

    public Cursor CursorAt(Point p)
    {
        Grip g;
        int target;
        switch (_gesture)
        {
            case DraggingMark drag:
                g = drag.Grip;
                target = drag.Index;
                break;
            case DraggingCrop crop:
                g = crop.Grip;
                target = -1;
                break;
            default:
                g = GripAt(p, out target);
                break;
        }

        if (Marks.HasIndex(target) && g != Grip.Move && Marks[target] is ArrowMark)
        {
            return Cursor.Cross;
        }
        Cursor c = g.CursorFor();
        if (c != Cursor.Arrow)
        {
            return c;
        }
        bool offCrop = g == Grip.None && !Capture.Crop.Contains(p);
        if (_gesture is not null || !offCrop)
        {
            return Cursor.Draw;
        }
        else
        {
            return Cursor.Arrow;
        }
    }

    public Cursor CropCursorAt(Point p)
    {
        Grip grip;
        if (_gesture is DraggingCrop drag)
        {
            grip = drag.Grip;
        }
        else
        {
            grip = CropGripAt(p);
        }
        return grip.CursorFor();
    }

    private Grip CropGripAt(Point p)
    {
        return Capture.Crop.GripAt(p, _metrics.Tolerance.EdgeBand, _metrics.Tolerance.GripRadius);
    }

    private Grip GripAt(Point p, out int target)
    {
        HitTolerance tolerance = _metrics.Tolerance;
        if (_document.SelectedMark is { } selected)
        {
            Grip onSelected = selected.GripAt(p, tolerance.GripRadius);
            if (onSelected != Grip.None)
            {
                target = _document.SelectedIndex;
                return onSelected;
            }
        }

        Grip onCrop = CropGripAt(p);
        if (onCrop is not (Grip.None or Grip.Move))
        {
            target = -1;
            return onCrop;
        }

        target = Marks.IndexAt(p, tolerance.HitBand, _textMarkAt);
        if (target >= 0)
        {
            return Grip.Move;
        }
        else
        {
            return onCrop;
        }
    }

    public void Press(Point p, bool shift)
    {
        if (GripAt(p, out _) == Grip.None && !Capture.Crop.Contains(p))
        {
            _gesture = new PressOutsideCrop(p);
            return;
        }
        Start(p, shift);
    }

    public void PressCrop(Point p)
    {
        _gesture = null;
        Grip g = CropGripAt(p);
        if (g != Grip.None)
        {
            StartDraggingCrop(p, g);
        }
    }

    private void Start(Point p, bool shift)
    {
        _gesture = null;
        Grip g = GripAt(p, out int target);
        if (g == Grip.None)
        {
            _document.Select(-1);
            _gesture = DrawingMark.Start(_tools.CurrentTool, _tools.Color, p, shift, _metrics.Sizes);
            return;
        }

        if (target < 0)
        {
            StartDraggingCrop(p, g);
            return;
        }

        _document.Select(target);
        Mark mark = Marks[target];
        bool onImage = g == Grip.Move && mark.CanMoveAsImage && _dragImage.Show(Capture, mark);
        _gesture = new DraggingMark(p, target, g, mark) { IsOnImage = onImage };
        _overlay.InvalidateMark(mark, selected: true, backdrop: true);
    }

    private void StartDraggingCrop(Point p, Grip g)
    {
        LineFinder? lines;
        bool onImage;
        if (g == Grip.Move)
        {
            lines = null;
            onImage = ShowMarksOnImage();
        }
        else
        {
            lines = LineFinder.Of(Capture.Picture, _metrics.Scale);
            onImage = false;
        }
        _gesture = new DraggingCrop(p, g, Capture.Crop) { Lines = lines, IsOnImage = onImage };
        _overlay.InvalidateSizeLabel(Capture.Crop);
        if (onImage)
        {
            InvalidateMarksOnImage();
        }
    }

    /// <summary>
    /// Shows the drag image with the marks that can move as a picture, as when a mark is moved on
    /// its own, so they ride along with the area rather than being drawn again at every step.
    /// </summary>
    private bool ShowMarksOnImage()
    {
        var onImage = new List<Mark>();
        foreach (Mark mark in Marks.All)
        {
            if (mark.CanMoveAsImage)
            {
                onImage.Add(mark);
            }
        }
        Mark? selected = _document.SelectedMark;
        if (selected is { CanMoveAsImage: false })
        {
            selected = null;
        }
        return onImage.Count > 0 && _dragImage.Show(Capture, onImage, selected);
    }

    private void InvalidateMarksOnImage()
    {
        foreach (Mark mark in Marks.All)
        {
            if (mark.CanMoveAsImage)
            {
                _overlay.InvalidateMark(mark, selected: true, backdrop: true);
            }
        }
    }

    public void Drag(Point p, bool shift, bool alt)
    {
        if (_gesture is PressOutsideCrop outside)
        {
            if (!Gesture.MovedPastDragThreshold(outside.PressedAt, p))
            {
                return;
            }
            Start(outside.PressedAt, shift);
        }

        switch (_gesture)
        {
            case DraggingMark mark:
                DragMark(mark, p, shift);
                break;
            case DraggingCrop crop:
                DragCrop(crop, p, shift, alt);
                break;
            case DrawingMark drawing:
                DragDrawing(drawing, p, shift);
                break;
        }
    }

    public void Release()
    {
        Gesture? gesture = _gesture;
        FinishPress();
        if (gesture is PressOutsideCrop)
        {
            ClickedOutsideCrop?.Invoke();
        }
    }

    public void FinishPress()
    {
        Gesture? gesture = _gesture;
        _gesture = null;
        switch (gesture)
        {
            case DraggingMark mark:
                ReleaseMark(mark);
                break;
            case DraggingCrop crop:
                ReleaseCrop(crop);
                break;
            case DrawingMark drawing:
                FinishDrawing(drawing);
                break;
        }
    }

    private void DragMark(DraggingMark drag, Point p, bool shift)
    {
        Mark now = drag.Mark;
        Mark moved = drag.Before.Dragged(drag.Grip, p - drag.PressedAt, shift, _metrics.Sizes.MinSize);
        if (moved.A == now.A && moved.B == now.B)
        {
            return;
        }

        _gesture = drag with { Mark = moved };
        if (drag.IsOnImage)
        {
            _dragImage.Move(moved.Bounds.TopLeft - drag.Before.Bounds.TopLeft);
            return;
        }
        _overlay.InvalidateMark(now, selected: true, backdrop: false);
        _overlay.InvalidateMark(moved, selected: true, backdrop: false);
    }

    private void DragCrop(DraggingCrop drag, Point p, bool shift, bool alt)
    {
        Crop now = drag.Crop;
        Crop to = drag.Before.Dragged(drag.Grip, p - drag.PressedAt, shift, Capture.CropBounds);
        if (drag.Lines is { } lines && !shift && !alt)
        {
            to = to.SnapTo(lines, drag.Grip.EdgesOf(), _metrics.SnapDistance);
        }
        if (to == now)
        {
            return;
        }

        Point wasOffset = MarksOffset;
        _gesture = drag with { Crop = to };
        _overlay.InvalidateCrop(now, to);
        if (MarksOffset == wasOffset)
        {
            return;
        }
        if (drag.IsOnImage)
        {
            _dragImage.Move(MarksOffset);
        }
        foreach (Mark mark in Marks.All)
        {
            if (drag.IsOnImage && mark.CanMoveAsImage)
            {
                continue;
            }
            _overlay.InvalidateMark(mark.Moved(wasOffset), selected: true, backdrop: true);
            _overlay.InvalidateMark(mark.Moved(MarksOffset), selected: true, backdrop: true);
        }
    }

    private void DragDrawing(DrawingMark drawing, Point p, bool shift)
    {
        if (drawing.DragTo(p, shift, _metrics.Sizes) is not { } next)
        {
            return;
        }

        _gesture = next;
        if (next.Mark is PenMark)
        {
            _overlay.Invalidate(next.PenDirtyRect.Inflated((int)(_metrics.Sizes.StrokeWidth * 2) + 18));
            return;
        }
        if (drawing.Mark.HasExtent)
        {
            _overlay.InvalidateMark(drawing.Mark, selected: false, backdrop: false);
        }
        _overlay.InvalidateMark(next.Mark, selected: false, backdrop: false);
    }

    private void ReleaseMark(DraggingMark drag)
    {
        if (ReferenceEquals(drag.Mark, drag.Before))
        {
            _overlay.InvalidateMark(drag.Before, selected: true, backdrop: true);
        }
        else
        {
            _document.ChangeMark(drag.Index, drag.Mark);
        }
        if (drag.IsOnImage)
        {
            _dragImage.Hide();
        }
    }

    private void ReleaseCrop(DraggingCrop drag)
    {
        _overlay.InvalidateSizeLabel(drag.Crop);
        if (drag.Crop != drag.Before)
        {
            _document.ChangeCrop(drag.Crop);
        }
        if (drag.IsOnImage)
        {
            InvalidateMarksOnImage();
            _dragImage.Hide();
        }
    }

    private void FinishDrawing(DrawingMark drawing)
    {
        if (drawing.Mark.HasExtent)
        {
            _overlay.InvalidateMark(drawing.Mark, selected: false, backdrop: false);
        }
        if (!drawing.ShouldKeep)
        {
            return;
        }
        if (drawing.Mark is TextMark textMark)
        {
            BeginTextEdit(WithMinimumSize(textMark with { Size = _tools.TextSize }), -1);
        }
        else
        {
            _document.AddMark(drawing.Mark);
        }
    }

    private TextMark WithMinimumSize(TextMark textMark)
    {
        Rect r = textMark.Bounds;
        float scale = _metrics.Scale;
        float factor = Metrics.TextFactor(textMark.Size);
        if (r.Width < (int)(MinTextBoxWidthDip * scale))
        {
            r.Right = r.Left + (int)(TextBoxWidthDip * scale * factor);
        }
        if (r.Height < (int)(MinTextBoxHeightDip * scale))
        {
            r.Bottom = r.Top + (int)(TextBoxHeightDip * scale * factor);
        }
        return textMark.WithBounds(r);
    }

    public bool ReopenTextAt(Point p)
    {
        if (_box.IsOpen)
        {
            return false;
        }
        int i = Marks.TextIndexAt(p, _metrics.Tolerance.HitBand);
        if (i < 0 || Marks[i] is not TextMark textMark)
        {
            return false;
        }

        _gesture = null;
        BeginTextEdit(textMark, i);
        return true;
    }

    /// <summary>
    /// Makes text a size bigger or smaller, counting from the size of the text being typed, else of
    /// the selected text mark, else of the next text.
    /// </summary>
    public void StepTextSize(int step)
    {
        TextSize from;
        if (_box.IsOpen)
        {
            from = _textMark.Size;
        }
        else if (_document.SelectedMark is TextMark selected)
        {
            from = selected.Size;
        }
        else
        {
            from = _tools.TextSize;
        }
        PickTextSize((TextSize)Math.Clamp((int)from + step, (int)TextSize.Small, (int)TextSize.Huge));
    }

    /// <summary>
    /// Sets the size the next text is typed in, and gives it to the text being typed or else to the
    /// selected text mark.
    /// </summary>
    private void PickTextSize(TextSize size)
    {
        _tools.SetTextSize(size);
        if (_box.IsOpen)
        {
            _textMark = _textMark with { Size = size };
            _box.Resize(size);
        }
        else if (_document.SelectedMark is TextMark selected && selected.Size != size)
        {
            Rect bounds = _box.Resized(selected, size, Capture.Crop);
            _document.ChangeMark(_document.SelectedIndex, (selected with { Size = size }).WithBounds(bounds), Merge.Resize);
        }
    }

    private void OnToolsChanged()
    {
        if (!_box.IsOpen || _textMark.Color == _tools.Color)
        {
            return;
        }
        _textMark = _textMark with { Color = _tools.Color };
        _box.Refresh();
    }

    private void BeginTextEdit(TextMark textMark, int at)
    {
        _textMarkAt = at;
        _textMarkBefore = textMark;
        _textMark = textMark;

        if (at >= 0)
        {
            _document.Select(-1);
            _overlay.InvalidateMark(textMark, selected: true, backdrop: true);
        }
        _box.OpenAt(textMark.Bounds, Capture.Crop, textMark.Text, textMark.Size);
    }

    public void EndTextEdit(bool cancel)
    {
        if (!_box.IsOpen)
        {
            return;
        }

        string? text;
        if (cancel)
        {
            text = null;
        }
        else
        {
            text = _box.Text();
        }
        Rect bounds = _box.Bounds;
        int at = _textMarkAt;
        TextMark before = _textMarkBefore;

        _textMarkAt = -1;
        _box.Close();

        if (at < 0)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            _textMark = (_textMark with { Text = text }).WithBounds(bounds);
            _document.AddMark(_textMark);
            return;
        }

        if (cancel)
        {
            _overlay.InvalidateMark(before, selected: false, backdrop: true);
            return;
        }
        if (string.IsNullOrEmpty(text))
        {
            _document.DeleteMark(at);
            return;
        }

        _textMark = (_textMark with { Text = text }).WithBounds(bounds);
        if (_textMark != before)
        {
            _document.ChangeMark(at, _textMark);
        }
        else
        {
            _overlay.InvalidateMark(before, selected: false, backdrop: true);
        }
    }
}

/// <summary>
/// Represents what a held mouse button is doing on the screenshot: drawing a new mark, dragging a
/// mark or the selected area, or a press outside the area that counts as a click until it moves.
/// </summary>
public abstract record Gesture(Point PressedAt)
{
    private const int DragThreshold = 4;

    public static bool MovedPastDragThreshold(Point from, Point to)
    {
        Point moved = to - from;
        return Math.Abs(moved.X) >= DragThreshold || Math.Abs(moved.Y) >= DragThreshold;
    }
}

/// <summary>
/// Represents a press outside the selected area that has not moved: released where it is, it is a
/// click; moved past the drag threshold, it starts drawing where it was pressed.
/// </summary>
public sealed record PressOutsideCrop(Point PressedAt) : Gesture(PressedAt);

/// <summary>
/// Represents a mark being moved or resized by one of its grips, with the mark as it was before the
/// drag and as the drag shows it now, so the release can make the whole drag one change.
/// </summary>
public sealed record DraggingMark(Point PressedAt, int Index, Grip Grip, Mark Before) : Gesture(PressedAt)
{
    public Mark Mark { get; init; } = Before;

    public bool IsOnImage { get; init; }
}

/// <summary>
/// Represents the selected area being moved or resized by one of its grips, with where it was before
/// and where the drag shows it now.
/// </summary>
public sealed record DraggingCrop(Point PressedAt, Grip Grip, Crop Before) : Gesture(PressedAt)
{
    public Crop Crop { get; init; } = Before;

    public LineFinder? Lines { get; init; }

    public bool IsOnImage { get; init; }
}

/// <summary>
/// Represents a new mark being drawn: a drag moves a shape's far corner or end, or adds a point to a
/// pen stroke, whose tail runs straight from where Shift was pressed while it is held.
/// </summary>
public sealed record DrawingMark(Point PressedAt, Mark Mark) : Gesture(PressedAt)
{
    private const int MinMarkLengthSquared = 9;

    private int StraightRunStart { get; init; }

    private bool ShiftHeld { get; init; }

    public Rect PenDirtyRect { get; init; }

    public bool ShouldKeep
    {
        get
        {
            return Mark.LengthSquared > MinMarkLengthSquared || Mark is TextMark || Mark is PenMark { Samples: > 1 };
        }
    }

    public static DrawingMark Start(MarkKind kind, Color color, Point at, bool shift, MarkSizes sizes)
    {
        Mark mark = Mark.Create(kind) with { Color = color, A = at, B = at };
        if (mark is StepMark)
        {
            mark = mark.WithBounds(Rect.Around(at, sizes.BadgeRadius));
        }
        if (mark is PenMark pen)
        {
            mark = pen with { Path = new Polyline(at) };
        }
        return new(at, mark) { ShiftHeld = shift };
    }

    public DrawingMark? DragTo(Point pointer, bool shift, MarkSizes sizes)
    {
        if (Mark is PenMark pen)
        {
            return AddPenPoint(pen, pointer, shift, sizes.RaySpacing);
        }
        else
        {
            return ResizeTo(pointer, shift, sizes.BadgeRadius);
        }
    }

    private DrawingMark? ResizeTo(Point pointer, bool shift, int badgeRadius)
    {
        if (Mark is StepMark)
        {
            Rect badge = Rect.Around(pointer, badgeRadius);
            if (badge.TopLeft == Mark.A)
            {
                return null;
            }
            else
            {
                return this with { Mark = Mark.WithBounds(badge) };
            }
        }
        if (shift)
        {
            pointer = Mark.Constrained(PressedAt, pointer);
        }
        if (pointer == Mark.B)
        {
            return null;
        }
        else
        {
            return this with { Mark = Mark with { B = pointer } };
        }
    }

    private DrawingMark? AddPenPoint(PenMark pen, Point pointer, bool shift, int spacing)
    {
        if (pen.Path is not { Count: > 0 } path)
        {
            return null;
        }

        int runStart;
        if (shift != ShiftHeld)
        {
            runStart = path.Count - 1;
        }
        else
        {
            runStart = StraightRunStart;
        }
        Rect dirty;
        if (shift)
        {
            Rect before = path.Box(runStart);
            Point from = path[runStart];
            Point to = pointer.Snap45(from);
            int steps = StepCount(from, to, spacing);
            var run = new Point[steps];
            for (int i = 1; i <= steps; i++)
            {
                run[i - 1] = Point.Interpolate(from, to, i, steps);
            }
            path = path.Take(runStart + 1).AddRange(run);
            dirty = Rect.UnionIgnoringEmpty(before, path.Box(runStart));
        }
        else
        {
            if (path[^1] == pointer)
            {
                return null;
            }
            path = path.Add(pointer);
            dirty = path.Box(Math.Max(0, path.Count - 3));
        }

        return this with
        {
            Mark = (pen with { Path = path }).WithBounds(path.Box(0)),
            StraightRunStart = runStart,
            ShiftHeld = shift,
            PenDirtyRect = dirty,
        };
    }

    private static int StepCount(Point from, Point to, int spacing)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from.Y;
        if (dx == 0 && dy == 0)
        {
            return 0;
        }
        return Math.Max(1, (int)Math.Round(Math.Sqrt((double)dx * dx + (double)dy * dy)) / spacing);
    }
}
