using static CapChief.Tests.Fixtures;

namespace CapChief.Tests;

/// <summary>
/// Tests that a press becomes a drag when it travels, and that drags and typed sizes shape the
/// selected area correctly.
/// </summary>
public sealed class GestureTests
{
    private readonly Desktop _desk = Desk();
    private readonly Metrics _metrics = MetricsFor(Desk());

    [Fact]
    public void PressTravels()
    {
        Point from = P(400, 300);
        Assert.False(Gesture.MovedPastDragThreshold(from, P(400, 300)), "a press that has not moved is still a click");
        Assert.False(Gesture.MovedPastDragThreshold(from, P(403, 300)), "three pixels is the hand on the button, not a drag");
        Assert.True(Gesture.MovedPastDragThreshold(from, P(404, 300)), "four is a drag");
        Assert.False(Gesture.MovedPastDragThreshold(from, P(397, 297)), "three on both axes is still a click");
        Assert.True(Gesture.MovedPastDragThreshold(from, P(400, 296)), "and four on either axis is a drag");

        var d = DrawingMark.Start(MarkKind.Arrow, Color.FromArgb(0xFF0000FFu), from, false, _metrics.Sizes);
        Assert.False(d.DragTo(P(403, 300), false, _metrics.Sizes)!.ShouldKeep, "a shape dragged three pixels is a slipped click");
        Assert.True(d.DragTo(P(404, 300), false, _metrics.Sizes)!.ShouldKeep, "one dragged four is a mark");
        Assert.True(d.DragTo(P(400, 300), false, _metrics.Sizes) is null, "and dragging to where it already is changes nothing");
    }

    [Fact]
    public void CropDragMoves()
    {
        Eq(Crop.MinSize, 10, "the smallest crop is 10");
        Eq(_metrics.Tolerance.EdgeBand, 6, "EdgeBand at 100 %");

        Assert.False(Crop.TryCreate(default, out _), "no area is not big enough to keep");
        Assert.True(Crop.TryCreate(R(100, 100, 300, 300), out _), "200x200 is big enough to keep");
        Assert.False(Crop.TryCreate(R(100, 100, 105, 105), out _), "5x5 is under MinSize, so not big enough to keep");

        Crop start = CropOf(100, 100, 300, 300);
        Eq(start.Dragged(Grip.Move, P(50, 50), false, _desk.Bounds).Bounds, R(150, 150, 350, 350), "Move translates the area");

        Rect near = start.Dragged(Grip.Move, P(-5000, -5000), false, _desk.Bounds);
        Eq(near.TopLeft, P(0, 0), "Move stops at the screen's corner");
        Rect far = start.Dragged(Grip.Move, P(5000, 5000), false, _desk.Bounds);
        Eq(far.Right, _desk.Size.Width, "Move stops at the right edge");
        Eq(far.Bottom, _desk.Size.Height, "and at the bottom edge");
        Eq(CropOf(0, 0, 200, 200).Dragged(Grip.Move, P(-10, 0), false, _desk.Bounds).Bounds, R(0, 0, 200, 200), "a nudge against the screen edge leaves the area where it was");
    }

    [Fact]
    public void CropDragResizes()
    {
        Crop se = CropOf(100, 100, 300, 300);
        Eq(se.Dragged(Grip.SE, P(100, 50), false, _desk.Bounds).Bounds, R(100, 100, 400, 350), "SE drags the far corner");
        Eq(se.Dragged(Grip.SE, P(-1000, -1000), false, _desk.Bounds).Width, Crop.MinSize, "a resize cannot go below MinSize");
        Eq(se.Dragged(Grip.SE, P(5000, 0), false, _desk.Bounds).Bounds.Right, _desk.Size.Width, "a resize stops at the screen's edge");

        Rect sq = CropOf(0, 0, 100, 100).Dragged(Grip.SE, P(200, 0), true, _desk.Bounds);
        Eq(sq.Width, sq.Height, "Shift keeps the area's proportions");
    }

    [Fact]
    public void ACropHasGripsAtItsCornersAndMovesFromTheInside()
    {
        Crop c = CropOf(100, 100, 300, 300);
        int band = _metrics.Tolerance.EdgeBand;
        int radius = _metrics.Tolerance.GripRadius;
        Eq(c.GripAt(P(100, 100), band, radius), Grip.NW, "a corner is a grip");
        Eq(c.GripAt(P(200, 100), band, radius), Grip.N, "and so is the middle of an edge");
        Eq(c.GripAt(P(200, 200), band, radius), Grip.None, "the inside takes hold of nothing");
        Eq(c.GripAt(P(150, 102), band, radius), Grip.Move, "and the border moves the area");
        Eq(c.GripAt(P(700, 600), band, radius), Grip.None, "nor does a point far outside");
    }

    [Fact]
    public void ATypedSizeStartsWithTheWidthAndTakesDigits()
    {
        var e = new TypedSize();
        Assert.False(e.HasDigits, "a new entry has no digit in it");
        Assert.False(e.HeightFocused, "and holds the width, which is what anyone types first");
        Assert.True(e.IsFocusedFieldEmpty, "so the width waits for its digits");

        e = e.AppendDigit(6);
        Eq(e.Width, 6, "one digit is the width");
        e = e.AppendDigit(4).AppendDigit(0);
        Eq(e.Width, 640, "the digits accumulate");
        Assert.True(e.HasDigits, "a digit has landed");
        Assert.False(e.IsFocusedFieldEmpty, "and the width no longer waits");
        Eq(e.Height, 0, "and the height is untouched");

        e = e.AppendDigit(7);
        Eq(e.Width, 6407, "a fourth digit lands: 6407 is a width a screen can have");
        Eq(e.AppendDigit(7), e, "a fifth digit is refused: no screen is five digits wide");
        e = e.RemoveLastDigit();
        Eq(e.Width, 640, "and the fourth comes off again");

        e = e.FocusOtherField();
        Assert.True(e.HeightFocused, "switching puts the digits in the height");
        Assert.True(e.IsFocusedFieldEmpty, "which waits until it is typed");
        e = e.AppendDigit(2).AppendDigit(0).AppendDigit(0);
        Eq(e.Height, 200, "the height takes its own digits");
        Eq(e.Width, 640, "and the width keeps what it was given");
        e = e.FocusOtherField();
        Assert.False(e.HeightFocused, "and switching again comes back to the width");

        e = e.RemoveLastDigit();
        Eq(e.Width, 64, "a backspace takes the last digit off");
        e = e.RemoveLastDigit().RemoveLastDigit();
        Eq(e.Width, 0, "backspacing it empty hands the width back to the pointer");
        Eq(e.RemoveLastDigit(), e, "and there is nothing left to take");
        Assert.True(e.HasDigits, "the height is still typed, so the entry still is");
    }

    [Fact]
    public void SizeTypedRefusesALeadingZero()
    {
        var fresh = new TypedSize();
        Eq(fresh.AppendDigit(0), fresh, "a leading zero is not the start of a size");
        fresh = fresh.FocusOtherField();
        Assert.True(fresh.HeightFocused && fresh.IsFocusedFieldEmpty, "the height waits in its turn, having no digits either");
        fresh = fresh.FocusOtherField();
        Assert.True(!fresh.HeightFocused && fresh.IsFocusedFieldEmpty, "and round to the width again, still waiting");
    }

    [Fact]
    public void CropSpansFromTheAnchor()
    {
        Rect screen = R(0, 0, 1920, 1080);

        Eq(Selecting.FromDrag(P(200, 200), P(800, 500), 0, 0, screen), R(200, 200, 800, 500), "with nothing typed the area is what the pointer drew");
        Eq(Selecting.FromDrag(P(800, 500), P(200, 200), 0, 0, screen), R(200, 200, 800, 500), "and the same dragged the other way");

        Eq(Selecting.FromDrag(P(200, 200), P(800, 500), 300, 0, screen), R(200, 200, 500, 500), "a typed width grows right from the anchor while the pointer is right of it");
        Eq(Selecting.FromDrag(P(800, 200), P(200, 500), 300, 0, screen), R(500, 200, 800, 500), "and left from the anchor while the pointer is left of it");

        Eq(Selecting.FromDrag(P(200, 200), P(800, 260), 300, 0, screen), R(200, 200, 500, 260), "the untyped height is still the pointer's");
        Eq(Selecting.FromDrag(P(200, 200), P(800, 500), 300, 40, screen), R(200, 200, 500, 240), "and a typed height grows down from the anchor");

        Eq(Selecting.FromDrag(P(1500, 200), P(1600, 300), 9999, 0, screen), R(1500, 200, 1920, 300), "a width past the screen's edge stops at the room the anchor left");
        Eq(Selecting.FromDrag(P(1500, 200), P(1400, 300), 9999, 0, screen), R(0, 200, 1500, 300), "and at the other edge going the other way");

        Eq(Selecting.FromDrag(P(200, 200), P(200, 200), 800, 0, screen), R(200, 200, 1000, 200 + Crop.MinSize), "a typed width with the pointer still on the anchor keeps a keepable height");
        Eq(Selecting.FromDrag(P(200, 200), P(200, 200), 0, 0, screen), R(200, 200, 200, 200), "while nothing typed on the anchor is still the empty area it was");
    }
}
