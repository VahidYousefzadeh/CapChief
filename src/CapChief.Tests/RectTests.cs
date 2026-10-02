using static CapChief.Tests.Fixtures;

namespace CapChief.Tests;

/// <summary>
/// Tests that rectangle geometry, resize handles and aspect locking behave correctly.
/// </summary>
public sealed class RectTests
{
    private readonly Metrics _metrics = MetricsFor(Desk());

    [Fact]
    public void ARectMeasuresItsSidesAndKnowsWhatItContains()
    {
        Rect r = R(10, 20, 110, 120);
        Eq(r.Width, 100, "the width is 100");
        Eq(r.Height, 100, "the height is 100");
        Assert.False(r.IsEmpty, "a 100x100 rect is not empty");
        Assert.True(R(10, 10, 10, 20).IsEmpty, "a zero-width rect is empty");
        Assert.True(R(10, 10, 5, 20).IsEmpty, "an inverted rect is empty");

        Assert.True(r.Contains(P(10, 20)), "the top-left corner is inside");
        Assert.False(r.Contains(P(110, 120)), "the bottom-right corner is outside");
        Assert.False(r.Contains(P(110, 60)), "the right edge is outside");
        Assert.True(r.Contains(P(109, 119)), "one pixel in from bottom-right is inside");

        Assert.False(R(0, 0, 10, 10).Overlaps(R(10, 0, 20, 10)), "abutting rects do not overlap");
        Assert.True(R(0, 0, 10, 10).Overlaps(R(9, 0, 20, 10)), "one shared column does overlap");
        Assert.False(R(0, 0, 10, 10).Overlaps(R(0, 10, 10, 20)), "stacked rects do not overlap");

        Eq(r, R(10, 20, 110, 120), "rects with equal edges are equal");
        Assert.True(r != R(10, 20, 110, 121), "a rect with one edge apart is not equal");

        var u = Rect.Union(R(0, 0, 10, 10), R(20, 20, 30, 30));
        Eq(u, R(0, 0, 30, 30), "Union spans both");

        Eq(Rect.UnionIgnoringEmpty(default, R(20, 20, 30, 30)), R(20, 20, 30, 30), "an empty left operand is ignored");
        Eq(Rect.UnionIgnoringEmpty(R(20, 20, 30, 30), default), R(20, 20, 30, 30), "an empty right operand is ignored");
        Eq(Rect.Union(default, R(20, 20, 30, 30)), R(0, 0, 30, 30), "Union, unlike UnionIgnoringEmpty, does drag in the origin");

        Eq(Rect.Intersect(R(0, 0, 20, 20), R(10, 10, 30, 30)), R(10, 10, 20, 20), "the intersection is the shared square");
        Assert.True(Rect.Intersect(R(0, 0, 5, 5), R(10, 10, 30, 30)).IsEmpty, "Intersect of disjoint rects is empty");
        Eq(R(10, 10, 20, 20).Inflated(5), R(5, 5, 25, 25), "inflating by 5 grows each side by 5");
        Eq(R(10, 10, 20, 20).Inflated(-2), R(12, 12, 18, 18), "inflating by -2 shrinks each side by 2");
        Eq(R(10, 10, 20, 20).Moved(P(3, -4)), R(13, 6, 23, 16), "moving by (3, -4) shifts both corners by that much");
        Eq(Rect.FromCorners(100, 100, 10, 10), R(10, 10, 100, 100), "FromCorners sorts the corners");
        Eq(Rect.Around(P(50, 50), 5), R(45, 45, 55, 55), "a rect around a point is centred on it");

        Eq(P(10, -3).AxisLocked(), P(10, 0), "AxisLocked keeps the larger component");
        Eq(P(2, -7).AxisLocked(), P(0, -7), "AxisLocked keeps the larger component");
    }

    [Fact]
    public void ARectHasHandlesAtItsCornersAndEdges()
    {
        Eq(RectGrips.All.Length, 8, "there are eight handles");
        Eq(_metrics.Tolerance.GripRadius, 5, "the grip radius is 5 at 100 %");

        Rect r = R(100, 200, 300, 400);
        Eq(r.GripCenter(Grip.NW), P(100, 200), "the NW grip sits at the top-left corner");
        Eq(r.GripCenter(Grip.SE), P(300, 400), "the SE grip sits at the bottom-right corner");
        Eq(r.GripCenter(Grip.N), P(200, 200), "GripCenter N is the top midpoint");
        Eq(r.GripCenter(Grip.E), P(300, 300), "GripCenter E is the right midpoint");
        Eq(r.GripCenter(Grip.W), P(100, 300), "GripCenter W is the left midpoint");
        Eq(r.GripRect(Grip.NW, _metrics.Tolerance.GripRadius), R(95, 195, 105, 205), "GripRect is the square around its center");
    }

    [Fact]
    public void ALockedResizeKeepsTheRatioAndTheOppositeCorner()
    {
        Rect orig = R(0, 0, 100, 100);
        Rect got = R(0, 0, 200, 100).LockedTo(orig, Grip.SE.EdgesOf(), 10, null);
        Eq(got.Width, got.Height, "a corner drag stays square");
        Eq(got.TopLeft, P(0, 0), "the corner opposite the grip stays put");

        Rect nw = R(-100, -100, 100, 100).LockedTo(orig, Grip.NW.EdgesOf(), 10, null);
        Eq(P(nw.Right, nw.Bottom), P(100, 100), "an NW drag anchors SE");
        Eq(nw.Width, nw.Height, "an NW drag stays square");

        Rect wide = R(0, 0, 200, 100);
        Rect w2 = R(0, 0, 400, 400).LockedTo(wide, Grip.SE.EdgesOf(), 10, null);
        Eq(w2.Width, 2 * w2.Height, "LockedTo keeps the original ratio");

        Rect e = R(0, 0, 200, 100).LockedTo(orig, Grip.E.EdgesOf(), 10, null);
        Eq(e.Width, e.Height, "an edge grip still locks the ratio");
        Eq(e.Top + e.Bottom, 100, "an edge grip keeps the free axis centered");

        Rect fit = R(0, 0, 120, 120);
        Rect clamped = R(0, 0, 400, 400).LockedTo(orig, Grip.SE.EdgesOf(), 10, fit);
        Assert.True(clamped.Right <= 120 && clamped.Bottom <= 120, $"LockedTo honours fit, got {clamped.Right},{clamped.Bottom}");

        Rect flat = R(5, 5, 50, 50).LockedTo(R(0, 0, 0, 0), Grip.SE.EdgesOf(), 10, null);
        Eq(flat, R(5, 5, 50, 50), "a zero-sized original changes nothing");
    }

    [Fact]
    public void APointSnapsToTheNearestAxisOrDiagonal()
    {
        Point a = P(100, 100);
        Point e = P(150, 103).Snap45(a);
        Assert.True(e.Y == 100 && e.X > 100, $"Snap45: a near-horizontal drag goes horizontal, got {e.X},{e.Y}");
        Point d = P(200, 184).Snap45(a);
        Eq(d.X - 100, d.Y - 100, "Snap45: a near-diagonal drag lands on the diagonal");
        Point n = P(103, 40).Snap45(a);
        Assert.True(n.X == 100 && n.Y < 100, $"Snap45: a near-vertical drag goes vertical, got {n.X},{n.Y}");
        Point same = P(100, 100).Snap45(a);
        Eq(same, P(100, 100), "Snap45: no travel stays put");

        Point len = P(0, 100).Snap45(P(0, 0));
        Eq(len.Y, 100, "Snap45 keeps the drag length");

        Point deg20 = P(100, 36).Snap45(P(0, 0));
        Eq(deg20.Y, 0, "Snap45: a 20-degree drag falls to the horizontal");
        Point deg30 = P(100, 58).Snap45(P(0, 0));
        Eq(deg30.X, deg30.Y, "Snap45: a 30-degree drag rises to the diagonal");
    }
}
