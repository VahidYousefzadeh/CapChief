namespace CapChief.Lab;

/// <summary>
/// Times the drags a user makes across the whole screen, from each pointer move to the repainted
/// screen, and writes the times beside checks that each drag did what it should.
/// </summary>
public sealed class Bench
{
    private const int Moves = 60;
    private const int ScribblePoints = 3000;

    private readonly Driver _d;
    private readonly Report _report = new("bench");

    public Bench(Driver driver)
    {
        _d = driver;
    }

    private App App
    {
        get
        {
            return _d.App;
        }
    }

    public int Run(string path)
    {
        int w = _d.Desk.Size.Width;
        int h = _d.Desk.Size.Height;
        _report.Note($"desktop {w}x{h}, {Moves} moves a drag, milliseconds from a move to the screen");
        Screenshot(w, h);
        _d.Escape();
        _d.Escape();
        App.Commands.Run(App.Commands.HideToTray);
        return _report.Write(path);
    }

    private void Time(string what, Timing timing)
    {
        _report.Note($"  {what,-40} {timing}");
    }

    private void Screenshot(int w, int h)
    {
        App.CaptureService.StartCapture(CaptureKind.Frozen);
        _d.Press(w / 20, h / 20);
        Time("select an area across the screen", _d.TimeMoves(i => At(w / 20 + i * (w * 9 / 10) / Moves, h / 20 + i * (h * 9 / 10) / Moves), Moves));
        _d.Release(w * 19 / 20, h * 19 / 20);
        if (App.Capture is not { } capture)
        {
            _report.Check(false, "the selection opens a capture");
            return;
        }

        Rect crop = capture.Crop;
        _d.Press(crop.Right, crop.Bottom);
        Time("resize the area by its corner", _d.TimeMoves(i => At(crop.Right - i * 5, crop.Bottom - i * 3), Moves));
        _d.Release(crop.Right - Moves * 5, crop.Bottom - Moves * 3);
        _report.Check(capture.Crop.Bounds.Right == crop.Right - Moves * 5, "the area was resized");
        MoveArea("move the area by its edge", 3, 2);

        Draw(MarkKind.Ellipse, "draw a screen-sized ellipse", w / 10, h / 10, w * 8 / 10, h * 8 / 10);
        Draw(MarkKind.Rectangle, "draw a screen-sized rectangle", w / 8, h / 8, w * 6 / 8, h * 6 / 8);
        Draw(MarkKind.Arrow, "draw a long arrow", w / 6, h / 6, w * 4 / 6, h * 4 / 6);

        _d.Pick(MarkKind.Pen);
        _d.Press(w / 5, h / 2);
        Time("draw a pen stroke across the screen", _d.TimeMoves(i => At(w / 5 + i * (w * 3 / 5) / (Moves * 2), h / 2 + (int)(Math.Sin(i / 6.0) * h / 4)), Moves * 2));
        _d.Release(w * 4 / 5, h / 2);
        _report.Check(capture.Marks.Count == 4, $"four marks drawn ({capture.Marks.Count})");

        Move(IndexOf<PenMark>(capture), "move the screen-wide pen stroke");
        Move(IndexOf<EllipseMark>(capture), "move the screen-sized ellipse");
        Move(IndexOf<RectangleMark>(capture), "move the screen-sized rectangle");

        int ellipse = IndexOf<EllipseMark>(capture);
        Point on = _d.PointOn(ellipse);
        _d.Click(on.X, on.Y);
        Rect before = capture.Marks[ellipse].Bounds;
        _report.Check(App.Workflow.CursorAt(At(before.Right, before.Bottom)) == Cursor.SizeNWSE, "the ellipse's corner grip is under the pointer");
        _d.Press(before.Right, before.Bottom);
        Time("resize the screen-sized ellipse", _d.TimeMoves(i => At(before.Right - i * 5, before.Bottom - i * 3), Moves));
        _d.Release(before.Right - Moves * 5, before.Bottom - Moves * 3);
        _report.Check(capture.Marks[ellipse].Bounds != before, "the ellipse was resized");

        MoveArea("move the area with the four marks", -3, -2);
        Scribble(MarkKind.Pen, 1.0);
        MoveArea("move the area with a pen scribble too", 3, 2);
        Scribble(MarkKind.Highlighter, 0.7);
        MoveArea("move the area with a highlighter scribble too", -3, -2);

        Rect area = capture.Crop;
        _d.Press(area.Right, area.Bottom);
        Time("resize the area with both scribbles", _d.TimeMoves(i => At(area.Right - i * 2, area.Bottom - i), Moves));
        _d.Release(area.Right - Moves * 2, area.Bottom - Moves);
        _report.Check(capture.Crop.Width < area.Width && capture.Crop.Height < area.Height, "the area with the scribbles was resized");
    }

    private void MoveArea(string what, int dx, int dy)
    {
        Capture capture = App.Capture!;
        Rect before = capture.Crop;
        Rect[] marks = capture.Marks.All.Select(m => m.Bounds).ToArray();
        Point on = At(before.Left + before.Width / 4, before.Top);
        _d.Press(on.X, on.Y);
        _report.Check(App.Drawing.IsDraggingCrop, "the press took hold of the area's edge");
        Time(what, _d.TimeMoves(i => At(on.X + i * dx, on.Y + i * dy), Moves));
        _d.Release(on.X + Moves * dx, on.Y + Moves * dy);
        var moved = new Point(Moves * dx, Moves * dy);
        bool marksMoved = capture.Marks.All.Select(m => m.Bounds).SequenceEqual(marks.Select(r => r.Moved(moved)));
        _report.Check(capture.Crop.Bounds == before.Moved(moved) && marksMoved, $"the area moved with its {marks.Length} marks");
    }

    private void Scribble(MarkKind kind, double spread)
    {
        Capture capture = App.Capture!;
        Rect area = capture.Crop;
        int count = capture.Marks.Count;
        int first = 0;
        while (first < ScribblePoints && App.Workflow.CursorAt(ScribblePoint(area, first, spread)) != Cursor.Draw)
        {
            first++;
        }
        _d.Pick(kind);
        Point start = ScribblePoint(area, first, spread);
        _d.Press(start.X, start.Y);
        for (int i = first + 1; i <= first + ScribblePoints; i++)
        {
            Point p = ScribblePoint(area, i, spread);
            _d.Move(p.X, p.Y);
        }
        Point end = ScribblePoint(area, first + ScribblePoints, spread);
        _d.Release(end.X, end.Y);
        App.Overlay.Window.PaintNow();
        _report.Check(capture.Marks.Count == count + 1 && capture.Marks[^1] is PenMark { Samples: > ScribblePoints / 2 } pen && pen.Kind == kind, $"a long {kind} scribble drawn");
    }

    /// <summary>
    /// Gives point i of a path that loops round and round the middle of the area, the way the pen
    /// goes in a busy scribble.
    /// </summary>
    private static Point ScribblePoint(Rect area, int i, double spread)
    {
        double t = 2 * Math.PI * i / ScribblePoints;
        int x = area.Center.X + (int)(area.Width * spread * (0.3 * Math.Sin(3 * t) + 0.1 * Math.Sin(41 * t)));
        int y = area.Center.Y + (int)(area.Height * spread * (0.3 * Math.Cos(2 * t) + 0.1 * Math.Cos(37 * t)));
        return new(x, y);
    }

    private void Draw(MarkKind kind, string what, int x1, int y1, int x2, int y2)
    {
        _d.Pick(kind);
        _d.Press(x1, y1);
        Time(what, _d.TimeMoves(i => At(x1 + i * (x2 - x1) / Moves, y1 + i * (y2 - y1) / Moves), Moves));
        _d.Release(x2, y2);
    }

    private void Move(int index, string what)
    {
        Marks marks = App.Capture!.Marks;
        Point on = _d.PointOn(index);
        _d.Click(on.X, on.Y);
        on = _d.PointOn(index);
        Rect before = marks[index].Bounds;
        _d.Press(on.X, on.Y);
        bool lifted = App.Drawing.IsDragImageShown;
        string suffix;
        if (lifted)
        {
            suffix = " (on the drag image)";
        }
        else
        {
            suffix = "";
        }
        Time(what + suffix, _d.TimeMoves(i => At(on.X + i * 3, on.Y + i * 2), Moves));
        _d.Release(on.X + Moves * 3, on.Y + Moves * 2);
        _report.Check(marks[index].Bounds == before.Moved(new Point(Moves * 3, Moves * 2)), $"{marks[index].Kind} moved with the pointer");
    }

    private static int IndexOf<T>(Capture capture) where T : Mark
    {
        for (int i = 0; i < capture.Marks.Count; i++)
        {
            if (capture.Marks[i] is T)
            {
                return i;
            }
        }
        return -1;
    }

    private static Point At(int x, int y)
    {
        return new(x, y);
    }
}
