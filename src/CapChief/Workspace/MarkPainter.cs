namespace CapChief.Workspace;

/// <summary>
/// Draws marks onto a device context, putting the shapes each mark says it is made of on the
/// surface with the line widths, fonts and screenshot pixels of its capture.
/// </summary>
public sealed class MarkPainter
{
    public const uint TextFormat = TextLayout.Wrap;

    private const float HighlighterLightening = 0.5f;

    private readonly Metrics _metrics;
    private readonly Shapes _shapes = new();
    private readonly Dictionary<Mark, Ink> _inks = new(ReferenceEqualityComparer.Instance);

    private DeviceContext _dc;
    private Point _shift;
    private Capture? _capture;
    private int _step;
    private bool _drawsEmptyText;

    public MarkPainter(Metrics metrics)
    {
        _metrics = metrics;
    }

    private MarkSizes Sizes
    {
        get
        {
            return _metrics.Sizes;
        }
    }

    public void DrawMarks(DeviceContext dc, Capture capture, Rect area, int skip, Point offset, bool marksOnImage)
    {
        Begin(dc, capture, default, drawsEmptyText: true);
        if (offset == default)
        {
            Free();
        }
        Marks marks = capture.Marks;
        for (int i = 0; i < marks.Count; i++)
        {
            if (i == skip || (marksOnImage && marks[i].CanMoveAsImage))
            {
                continue;
            }
            Mark mark;
            if (offset == default)
            {
                mark = marks[i];
            }
            else
            {
                mark = marks[i].Moved(offset);
            }
            if (!mark.Extent(Sizes).Overlaps(area))
            {
                continue;
            }
            if (offset != default && marks[i] is HighlighterMark highlighter)
            {
                DrawMovedHighlight(highlighter, offset);
                continue;
            }
            _step = marks.StepNumberOf(i);
            mark.Draw(this);
        }
    }

    public void DrawCrop(DeviceContext dc, Capture capture)
    {
        Begin(dc, capture, -capture.Crop.TopLeft, drawsEmptyText: false);
        Marks marks = capture.Marks;
        for (int i = 0; i < marks.Count; i++)
        {
            _step = marks.StepNumberOf(i);
            marks[i].Draw(this);
        }
    }

    public void DrawMark(DeviceContext dc, Capture capture, Mark mark, int step, Point shift)
    {
        Begin(dc, capture, shift, drawsEmptyText: true);
        _step = step;
        mark.Draw(this);
    }

    public void DrawSelectedMark(DeviceContext dc, Mark mark)
    {
        if (mark is TextMark)
        {
            _shapes.DrawBox(dc, mark.Bounds, Palette.AccentRef, Sizes.HintWidth);
        }
        foreach (Grip g in mark.Grips)
        {
            DrawGrip(dc, mark.GripRect(g, _metrics.Tolerance.GripRadius));
        }
    }

    public static void DrawGrip(DeviceContext dc, Rect r)
    {
        dc.FillRect(r, Palette.DarkRef);
        dc.FillRect(r.Inflated(-1), Palette.AccentRef);
    }

    public void Free()
    {
        foreach (Ink ink in _inks.Values)
        {
            ink.Free();
        }
        _inks.Clear();
    }

    private void Begin(DeviceContext dc, Capture capture, Point shift, bool drawsEmptyText)
    {
        _dc = dc;
        _capture = capture;
        _shift = shift;
        _drawsEmptyText = drawsEmptyText;
    }

    /// <summary>
    /// Draws a highlighter while the marks move with the area: it keeps the ink it was first drawn
    /// with, and only tints what is under it at each step.
    /// </summary>
    private void DrawMovedHighlight(HighlighterMark mark, Point offset)
    {
        if (mark.Path is not { } path)
        {
            return;
        }
        if (!_inks.TryGetValue(mark, out Ink? ink))
        {
            ink = new Ink();
            _shapes.DrawInk(ink, path.Points, InkColor(mark.Color), Sizes.HighlighterWidth);
            _inks.Add(mark, ink);
        }
        _shapes.Tint(_dc, ink, offset);
    }

    private static uint InkColor(Color color)
    {
        return color.Lightened(HighlighterLightening).ColorRef;
    }

    private Rect At(Rect r)
    {
        return r.Moved(_shift);
    }

    public void Box(Rect r, Color color)
    {
        _shapes.DrawBox(_dc, At(r), color.ColorRef, Sizes.StrokeWidth);
    }

    public void Ellipse(Rect r, Color color)
    {
        _shapes.DrawEllipse(_dc, At(r), color.ColorRef, Sizes.StrokeWidth);
    }

    public void Arrow(Point a, Point b, Color color)
    {
        Point tail = a + _shift;
        Point head = b + _shift;
        _shapes.DrawArrow(_dc, tail.X, tail.Y, head.X, head.Y, color.ColorRef, Sizes.StrokeWidth, Sizes.ArrowHead);
    }

    public void Stroke(Polyline path, Color color)
    {
        _shapes.DrawStroke(_dc, path.Points, _shift, color.ColorRef, Sizes.StrokeWidth);
    }

    public void Highlight(Polyline path, Color color)
    {
        _shapes.DrawHighlight(_dc, path.Points, _shift, InkColor(color), Sizes.HighlighterWidth);
    }

    public void Fill(Rect r, Color color)
    {
        Rect box = At(r);
        if (!box.IsEmpty)
        {
            _dc.FillRect(box, color.ColorRef);
        }
    }

    public void Pixelate(Rect r)
    {
        if (_capture is not { } capture)
        {
            return;
        }
        _shapes.Pixelate(_dc, capture.Picture.DeviceContext, At(r), r, Sizes.RedactBlock);
    }

    public void Disc(Rect r, Color color)
    {
        Rect box = At(r);
        string stepText;
        if (_step > 0)
        {
            stepText = _step.ToString();
        }
        else
        {
            stepText = "";
        }
        _shapes.DrawDisc(_dc, box, color.ColorRef, stepText, color.ContrastingTextColor().ColorRef, _metrics.Step(Math.Min(box.Width, box.Height)));
    }

    public void Text(string? text, Rect r, Color color, TextSize size)
    {
        if (!string.IsNullOrEmpty(text))
        {
            _dc.DrawText(text, At(r), color.ColorRef, _metrics.Text(size), TextFormat);
        }
        else if (_drawsEmptyText)
        {
            _shapes.DrawBox(_dc, At(r), color.ColorRef, Sizes.HintWidth);
        }
    }
}
