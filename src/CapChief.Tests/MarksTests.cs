using static CapChief.Tests.Fixtures;

namespace CapChief.Tests;

/// <summary>
/// Tests that marks are shaped, hit-tested and dragged correctly, that the list of marks keeps its
/// order, its selection and its step numbers, and that a pen stroke lays its points down as drawn.
/// </summary>
public sealed class MarksTests
{
    private readonly Metrics _metrics = MetricsFor(Desk());

    [Fact]
    public void AMarkNormalisesItsBoundsAndHasItsSizes()
    {
        Eq((int)_metrics.Sizes.StrokeWidth, 4, "StrokeWidth at 100 %");
        Eq(_metrics.Sizes.MinSize, 10, "MinSize at 100 %");
        Eq(_metrics.Sizes.BadgeRadius, 14, "BadgeRadius at 100 %");

        Mark a = Rectangle(100, 100, 20, 20);
        Eq(a.Bounds, R(20, 20, 100, 100), "Bounds normalises a backwards drag");

        Assert.False(new RectangleMark { A = P(5, 5), B = P(5, 5) }.HasExtent, "a bare click has no extent");
        Assert.True(Rectangle(0, 0, 10, 10).HasExtent, "a dragged box has one");
        Assert.False(new PenMark { Path = new Polyline(P(1, 1)) }.HasExtent, "a one-point stroke has none");
        Assert.True(new PenMark { Path = new Polyline(P(1, 1), P(9, 9)) }.HasExtent, "a two-point stroke has one");

        Eq(Rectangle(0, 0, 10, 10).Grips.Length, 8, "a box offers eight grips");
        Eq(new ArrowMark().Grips.Length, 2, "an arrow offers two");
        Eq(new StepMark().Grips.Length, 4, "a step offers four corners");

        var arrow = new ArrowMark { A = P(100, 10), B = P(10, 100) };
        Eq(arrow.GripCenter(Grip.NW), P(100, 10), "an arrow's NW grip is its tail");
        Eq(arrow.GripCenter(Grip.SE), P(10, 100), "an arrow's SE grip is its head");
        Eq(Rectangle(0, 0, 100, 100).GripCenter(Grip.SE), P(100, 100), "a box's SE grip is its corner");

        Rect ext = Rectangle(100, 100, 200, 200).Extent(_metrics.Sizes);
        Assert.True(ext.Left < 100 && ext.Right > 200, "Extent reaches outside the bounding box");

        var pen = new PenMark
        {
            A = P(0, 0),
            B = P(600, 0),
            Path = new Polyline(P(0, 0), P(600, 0)),
        };
        Assert.True(pen.Extent(_metrics.Sizes).Width > Rectangle(0, 0, 600, 0).Extent(_metrics.Sizes).Width, "a stroke's extent allows for the spline bowing out");

        Point sq = new RectangleMark().Constrained(P(0, 0), P(100, 40));
        Eq(Math.Abs(sq.X), Math.Abs(sq.Y), "Constrained squares a box");
        Point neg = new RectangleMark().Constrained(P(0, 0), P(-100, 40));
        Eq(neg, P(-100, 100), "Constrained keeps each sign");
        Eq(new ArrowMark().Constrained(P(0, 0), P(100, 3)).Y, 0, "Constrained snaps an arrow to a ray");
    }

    [Fact]
    public void EveryKindOfMarkIsMadeAsItsOwnKind()
    {
        foreach (MarkKind k in Enum.GetValues<MarkKind>())
        {
            Mark m = Drawn(k, 0, 0, 100, 100);
            Eq(m.Kind, k, $"{k} is made as a mark of its own kind");
            Assert.True(m.Grips.Length > 0, $"{k} offers grips");
            Assert.True(Tools.GroupOf(k) != ToolGroup.None, $"{k} sits in a toolbar group");
            Assert.True(m.IsHit(P(0, 0), 6) || m.IsHit(P(50, 0), 6) || m.IsHit(P(50, 50), 6), $"{k} is hit somewhere in the box it was drawn in");
            Assert.False(m.IsHit(P(900, 900), 6), $"{k} is not hit from far outside");
        }

        Assert.False(new MosaicMark().TakesColor, "a mosaic takes no color");
        Assert.True(Mark.Create(MarkKind.Arrow) is ArrowMark, "the arrow tool draws an arrow");
        Assert.True(Mark.Create(MarkKind.Pen) is PenMark, "the pen draws a freehand stroke");
        Assert.True(Mark.Create(MarkKind.Highlighter) is HighlighterMark, "the highlighter draws a highlighter stroke");
        Assert.True(Mark.Create(MarkKind.Step) is StepMark, "the step tool draws a numbered step");
        Assert.True(Mark.Create(MarkKind.Text) is TextMark, "the text tool draws text");

        Eq(Tools.FirstIn(ToolGroup.Arrow), MarkKind.Arrow, "the arrow button starts on the arrow");
        Eq(Tools.GroupOf(MarkKind.Pen), ToolGroup.Arrow, "and holds the pen too");
        Eq(Tools.GroupOf(MarkKind.Highlighter), ToolGroup.Arrow, "and the highlighter");
        Eq(Tools.FirstIn(ToolGroup.Rect), MarkKind.Rectangle, "the rect button starts on the rectangle");
        Eq(Tools.GroupOf(MarkKind.Ellipse), ToolGroup.Rect, "and holds the ellipse too");
        Eq(Tools.FirstIn(ToolGroup.Text), MarkKind.Text, "the text button starts on text");
        Eq(Tools.GroupOf(MarkKind.Step), ToolGroup.Text, "and holds the numbered step too");
        Eq(Tools.FirstIn(ToolGroup.Redact), MarkKind.Mosaic, "the redact button starts on the mosaic");
        Eq(Tools.GroupOf(MarkKind.Blackout), ToolGroup.Redact, "and holds the solid bar too");
        Eq(Tools.FirstIn(ToolGroup.None), MarkKind.Arrow, "a group with no kinds answers with the first kind");
    }

    [Fact]
    public void AMarkIsHitOnItsEdges()
    {
        int band = _metrics.Tolerance.HitBand;
        Mark b = Rectangle(100, 100, 300, 300);
        Assert.True(b.IsHit(P(100, 200), band), "a box is hit on its left edge");
        Assert.True(b.IsHit(P(300, 200), band), "a box is hit on its right edge");
        Assert.False(b.IsHit(P(200, 200), band), "a box is not hit in its empty middle");

        var solid = new BlackoutMark { A = P(100, 100), B = P(300, 300) };
        Assert.True(solid.IsHit(P(200, 200), band), "a blackout is hit in its middle");
        Assert.False(solid.IsHit(P(400, 400), band), "a blackout is not hit outside");

        var el = new EllipseMark { A = P(0, 0), B = P(200, 200) };
        Assert.True(el.IsHit(P(200, 100), band), "an ellipse is hit on its rim");
        Assert.False(el.IsHit(P(100, 100), band), "an ellipse is not hit at its center");

        var step = new StepMark { A = P(0, 0), B = P(100, 100) };
        Assert.True(step.IsHit(P(50, 50), band), "a step is hit at its center");
        Assert.False(step.IsHit(P(2, 2), band), "a step is not hit in its corner, where its grip sits");

        var arrow = new ArrowMark { A = P(0, 0), B = P(200, 200) };
        Assert.True(arrow.IsHit(P(100, 100), band), "an arrow is hit along its shaft");
        Assert.False(arrow.IsHit(P(100, 0), band), "an arrow is not hit off its shaft");

        var pen = new PenMark
        {
            A = P(0, 0),
            B = P(100, 100),
            Path = new Polyline(P(0, 0), P(50, 50), P(100, 100)),
        };
        Assert.True(pen.IsHit(P(50, 50), band), "a stroke is hit on a point");
        Assert.False(pen.IsHit(P(0, 100), band), "a stroke is not hit across its bounding box");
        Assert.False(new PenMark().IsHit(P(0, 0), band), "an empty stroke is hit by nothing");
    }

    [Fact]
    public void DraggingAMarkMovesItOrResizesIt()
    {
        int min = _metrics.Sizes.MinSize;
        Mark b = Rectangle(100, 100, 300, 300);

        Eq(b.Dragged(Grip.Move, P(10, -20), false, min).Bounds, R(110, 80, 310, 280), "Move translates the whole shape");
        Eq(b.Dragged(Grip.Move, P(10, -3), true, min).Bounds, R(110, 100, 310, 300), "Move with Shift keeps to one axis");
        Eq(b.Dragged(Grip.SE, P(50, 50), false, min).Bounds, R(100, 100, 350, 350), "SE resizes from the far corner");
        Eq(b.Dragged(Grip.SE, P(-1000, -1000), false, min).Bounds.Width, min, "a shape cannot be dragged below MinSize");
        Assert.True(b.Dragged(Grip.Move, P(-5000, -5000), false, min).Bounds.Left < 0, "a mark may be dragged off the screen");
        Eq(b.Moved(P(5, -5)).Bounds, R(105, 95, 305, 295), "Moved is a drag by the Move grip");

        var step = new StepMark { A = P(0, 0), B = P(100, 100) };
        Rect stretched = step.Dragged(Grip.SE, P(200, 0), false, min).Bounds;
        Eq(stretched.Width, stretched.Height, "a step stays square without Shift");

        var arrow = new ArrowMark { A = P(0, 0), B = P(100, 100) };
        Mark pulled = arrow.Dragged(Grip.SE, P(10, 20), false, min);
        Eq(pulled.B, P(110, 120), "an arrow's head follows the drag");
        Eq(pulled.A, P(0, 0), "and its tail stays put");
        Eq(arrow.Dragged(Grip.SE, P(10, -95), true, min).B.Y, 0, "an arrow's head snaps to a ray with Shift");

        var path = new Polyline(P(0, 0), P(50, 50));
        var pen = new PenMark { A = P(0, 0), B = P(50, 50), Path = path };
        var penMoved = (PenMark)pen.Dragged(Grip.Move, P(10, 10), false, min);
        Assert.False(ReferenceEquals(penMoved.Path, path), "a moved stroke is given a path of its own");
        Eq(path[0], P(0, 0), "the original points are left alone");
        Eq(penMoved.Path![0], P(10, 10), "and the new points carry the move");
    }

    [Fact]
    public void MarksKeepTheirOrder()
    {
        var marks = new Marks();
        Assert.True(marks.Count == 0 && !marks.HasIndex(0) && !marks.HasIndex(-1), "a new list is empty");

        marks.Insert(99, Rectangle(0, 0, 10, 10));
        Eq(marks.Count, 1, "an insert past the end lands at the end");
        marks.Insert(-4, Rectangle(20, 20, 30, 30));
        Eq(marks[0].Bounds, R(20, 20, 30, 30), "an insert before the start lands at the start");
        marks.RemoveAt(77);
        Eq(marks.Count, 2, "removing a place that is not there leaves the list alone");
        marks.Replace(77, Rectangle(0, 0, 1, 1));
        Eq(marks.Count, 2, "and so does replacing one");
    }

    [Fact]
    public void TheSelectionStaysOnItsMark()
    {
        var document = new Document();
        document.Open(new Capture(new Surface(), CropOf(0, 0, 100, 100)));
        foreach (Mark mark in new[] { Rectangle(0, 0, 10, 10), Rectangle(20, 20, 30, 30), Rectangle(40, 40, 50, 50) })
        {
            document.AddMark(mark);
        }
        document.Select(2);
        Eq(document.SelectedMark!.Bounds, R(40, 40, 50, 50), "the third mark is selected");
        document.RemoveMark(0);
        Eq(document.SelectedIndex, 1, "removing a mark before it moves the selection down with its mark");
        Eq(document.SelectedMark!.Bounds, R(40, 40, 50, 50), "so it is still the same mark");
        document.RemoveMark(1);
        Assert.False(document.HasSelectedMark, "removing the selected mark leaves nothing selected");
        document.Select(9);
        Eq(document.SelectedIndex, -1, "and a place that is not there cannot be selected");
    }

    [Fact]
    public void MarksAreFoundUnderThePointer()
    {
        int band = _metrics.Tolerance.HitBand;
        int radius = _metrics.Tolerance.GripRadius;
        var marks = new Marks([Rectangle(100, 100, 200, 200), new TextMark { A = P(300, 300), B = P(400, 340), Text = "hi" }]);
        Eq(marks.IndexAt(P(100, 100), band), 0, "the box is found on its outline");
        Eq(marks.IndexAt(P(600, 600), band), -1, "and nothing is found in empty space");
        Eq(marks.TextIndexAt(P(320, 320), band), 1, "the text is found");
        Eq(marks.TextIndexAt(P(100, 100), band), -1, "and the box is not text");

        var over = new Marks([Rectangle(100, 100, 200, 200), Rectangle(100, 100, 250, 250)]);
        Eq(over.IndexAt(P(100, 150), band), 1, "where two marks overlap, the one drawn last is found");

        Eq(marks[0].GripAt(P(100, 100), radius), Grip.NW, "the box offers its NW grip at its corner");
        Eq(marks[0].GripAt(P(150, 150), radius), Grip.None, "and none in its middle");
    }

    [Fact]
    public void StepsCountInDrawingOrder()
    {
        var marks = new Marks([Rectangle(0, 0, 10, 10), new StepMark { A = P(20, 20), B = P(40, 40) }, new StepMark { A = P(60, 60), B = P(80, 80) }]);
        Eq(marks.StepNumberAt(1), 1, "the first step is 1");
        Eq(marks.StepNumberAt(2), 2, "the second is 2");
        Eq(marks.NextStepNumber, 3, "and the next one drawn would be 3");
        marks.RemoveAt(1);
        Eq(marks.StepNumberAt(1), 1, "removing a step renumbers the ones after it");
        Eq(marks.NextStepNumber, 2, "and the next one drawn");
    }

    [Fact]
    public void APenStraightRunLaysPointsEvenly()
    {
        Eq(_metrics.Sizes.RaySpacing, 6, "RaySpacing at 100 %");

        DrawingMark longRun = DragPen(Pen(P(0, 0)), P(60, 0), shift: true);
        Eq(PenOf(longRun).Samples, 11, "a 60 px straight run lays a point every 6 px after the first");
        Eq(PenOf(longRun).Path![5], P(30, 0), "and spaces them evenly");

        DrawingMark shortRun = DragPen(Pen(P(0, 0)), P(3, 0), shift: true);
        Eq(PenOf(shortRun).Samples, 2, "a short straight run is still one point beyond the first");
        Eq(PenOf(shortRun).Path![1], P(3, 0), "and the last point lands on the end");
    }

    [Fact]
    public void APenStrokeAddsAPointForEachMove()
    {
        DrawingMark a = Pen(P(100, 100));
        Eq(PenOf(a).Samples, 1, "a new stroke has its first point");
        Eq(PenOf(a).Path![0], P(100, 100), "where the press was");

        DrawingMark b = DragPen(a, P(110, 100), shift: false);
        Eq(PenOf(b).Samples, 2, "freehand adds one point");
        Assert.True(b.PenDirtyRect.Width > 0, "and says where to repaint");
        Assert.True(b.DragTo(P(110, 100), false, _metrics.Sizes) is null, "a repeated point is refused, because it makes the spline loop");

        for (int i = 0; i < 400; i++)
        {
            b = b.DragTo(P(200 + i, 300), false, _metrics.Sizes) ?? b;
        }
        Assert.True(PenOf(b).Samples > 256, $"a long scribble keeps every point, {PenOf(b).Samples} of them");
        Eq(PenOf(b).Path![0], P(100, 100), "and its first point is still where the press was");
    }

    [Fact]
    public void AShiftedPenRunSnapsTo45Degrees()
    {
        DrawingMark s = DragPen(Pen(P(0, 0)), P(30, 2), shift: false);
        int freehand = PenOf(s).Samples;
        s = DragPen(s, P(120, 6), shift: true);
        Assert.True(PenOf(s).Samples > freehand, "a straight run lays down several points, not one");
        Eq(PenOf(s).Path![^1], P(120, 2), "and snaps to the 45 degree line from the point where Shift was pressed");

        int afterRun = PenOf(s).Samples;
        s = DragPen(s, P(60, 4), shift: true);
        Assert.True(PenOf(s).Samples < afterRun, "holding Shift rewrites the run instead of growing it");

        s = DragPen(s, P(60, 40), shift: false);
        s = DragPen(s, P(100, 44), shift: true);
        Eq(PenOf(s).Path![^1], P(100, 40), "Shift pressed again straightens from the point it was pressed on");

        Assert.False(ReferenceEquals(PenOf(Pen(P(5, 5))).Path, PenOf(Pen(P(9, 9))).Path), "every stroke gets a path of its own");
    }

    private static PenMark PenOf(DrawingMark drawing)
    {
        return (PenMark)drawing.Mark;
    }

    private DrawingMark Pen(Point at)
    {
        return DrawingMark.Start(MarkKind.Pen, default, at, false, _metrics.Sizes);
    }

    private DrawingMark DragPen(DrawingMark pen, Point to, bool shift)
    {
        return pen.DragTo(to, shift, _metrics.Sizes)!;
    }
}
