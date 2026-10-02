namespace CapChief.Tests;

/// <summary>
/// Tests that a wrap panel fills its rows from left to right, no more to a row than it may hold and no
/// wider than the room it is given.
/// </summary>
public sealed class LayoutTests
{
    [Fact]
    public void AWrapPanelStartsARowWhenTheNextWouldNotFit()
    {
        var rows = new WrapPanel { MaxPerRow = 5, Spacing = Length.Of(5f) };
        var cells = new Panel[7];
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = new Panel { Width = Length.Of(100f), Height = Length.Of(50f) };
            rows.Add(cells[i]);
        }
        var draw = new DrawContext(new BackBuffer());

        rows.Measure(new Size(10000, 10000), draw);
        Eq(rows.DesiredSize, new Size(520, 105), "five to a row with room to spare, the other two below");

        rows.Measure(new Size(330, 10000), draw);
        rows.Arrange(new Rect { Right = 330, Bottom = 10000 });
        Eq(rows.DesiredSize, new Size(310, 160), "three to a row when only three fit");
        Eq(cells[3].Bounds, new Rect { Top = 55, Right = 100, Bottom = 105 }, "the fourth starts the second row");
        Eq(cells[6].Bounds, new Rect { Top = 110, Right = 100, Bottom = 160 }, "and the seventh the third");

        cells[6].Visibility = Visibility.Collapsed;
        rows.Measure(new Size(330, 10000), draw);
        Eq(rows.DesiredSize, new Size(310, 105), "a collapsed one takes no room");
        draw.Free();
    }
}
