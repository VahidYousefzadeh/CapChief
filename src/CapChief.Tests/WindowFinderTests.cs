using static CapChief.Tests.Fixtures;

namespace CapChief.Tests;

/// <summary>
/// Tests that the window and element under the pointer are found correctly.
/// </summary>
public sealed class WindowFinderTests
{
    [Fact]
    public void PicksSmallest()
    {
        Rect[] rects = [R(0, 0, 400, 400), R(100, 100, 300, 300), R(150, 150, 200, 200)];

        Eq(Rect.SmallestContaining(rects, P(160, 160)), R(150, 150, 200, 200), "the innermost rect containing the point wins");
        Eq(Rect.SmallestContaining(rects, P(120, 120)), R(100, 100, 300, 300), "a point only the middle rect holds gets the middle rect");
        Eq(Rect.SmallestContaining(rects, P(10, 10)), R(0, 0, 400, 400), "a point only the outer rect holds gets the outer rect");
        Assert.True(Rect.SmallestContaining(rects, P(900, 900)).IsEmpty, "a point in none of them is empty, not the nearest");
        Assert.True(Rect.SmallestContaining([], P(10, 10)).IsEmpty, "nothing to choose from is empty");

        Rect[] tied = [R(0, 0, 100, 100), R(0, 0, 100, 100)];
        Eq(Rect.SmallestContaining(tied, P(50, 50)), R(0, 0, 100, 100), "two rects of equal area give the first, which is the shallower");

        Rect[] wide = [R(0, 0, 100, 40), R(0, 0, 40, 100)];
        Eq(Rect.SmallestContaining(wide, P(20, 20)), R(0, 0, 100, 40), "it compares area, so neither dimension alone decides it");

        Assert.True(Rect.SmallestContaining([R(50, 50, 50, 90)], P(50, 60)).IsEmpty, "a zero-width rect is not an answer");
    }

    [Fact]
    public void FallsBackToTheWindow()
    {
        Eq(Window(R(0, 0, 500, 500)).ElementAt(P(100, 100)), R(0, 0, 500, 500), "a window with no elements found answers with the window");
        Eq(Window(R(0, 0, 500, 500), R(10, 10, 100, 100)).ElementAt(P(300, 300)), R(0, 0, 500, 500), "a point past every element found still answers with the window");
        Eq(Window(R(0, 0, 500, 500), R(10, 10, 100, 100)).ElementAt(P(30, 30)), R(10, 10, 100, 100), "and a point on an element answers with the element");
        Assert.True(new WindowOnScreen(IntPtr.Zero, R(0, 0, 10, 10)).IsMonitor, "a monitor has no window handle");
    }

    [Fact]
    public void RejectsParentDuplicate()
    {
        Rect parent = R(100, 100, 300, 300);
        Assert.False(ElementScan.IsSelectable(parent, parent, Crop.MinSize), "a child with its parent's exact bounds is not offered");
        Assert.True(ElementScan.IsSelectable(R(100, 100, 300, 299), parent, Crop.MinSize), "one pixel shorter is a different rect, and is offered");
        Assert.False(ElementScan.IsSelectable(R(0, 0, 9, 100), parent, Crop.MinSize), "a rect under MinSize wide is not offered");
        Assert.False(ElementScan.IsSelectable(R(0, 0, 100, 9), parent, Crop.MinSize), "nor one under MinSize tall");
        Assert.False(ElementScan.IsSelectable(default, parent, Crop.MinSize), "nor the empty rect a clipped-away element leaves");
        Assert.True(ElementScan.IsSelectable(R(0, 0, Crop.MinSize, Crop.MinSize), parent, Crop.MinSize), "exactly MinSize is offered");
    }

    private static WindowOnScreen Window(Rect bounds, params Rect[] elements)
    {
        return new((IntPtr)1, bounds) { Elements = elements };
    }
}
