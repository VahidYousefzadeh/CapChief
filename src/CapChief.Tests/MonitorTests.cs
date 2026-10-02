using static CapChief.Tests.Fixtures;

namespace CapChief.Tests;

/// <summary>
/// Tests that the toolbar and the labels put beside a capture land on a monitor, on desktops whose
/// monitors differ in size and so leave parts of the desktop that no monitor shows.
/// </summary>
public sealed class MonitorTests
{
    private const int ToolbarWidth = 500, ToolbarHeight = 52, ToolbarGap = 10;

    private static readonly Rect Big = R(0, 0, 3840, 2160);

    public static TheoryData<string> Layouts
    {
        get
        {
            return ["small right, tops lined up", "small right, bottoms lined up", "small left", "small above"];
        }
    }

    [Fact]
    public void APointIsOnTheMonitorThatHoldsItOrTheNearest()
    {
        Desktop desk = Layout("small right, tops lined up");
        Rect small = desk.ScreenMonitors[1];
        Eq(desk.MonitorAt(P(100, 2000)), Big, "a point on the big monitor");
        Eq(desk.MonitorAt(P(4000, 500)), small, "a point on the small one");
        Eq(desk.MonitorAt(P(3840, 0)), small, "the first column of the small one is its own");
        Eq(desk.MonitorAt(P(5000, 1100)), small, "a point just under the small one, where no monitor is, is nearest to it");
        Eq(desk.MonitorAt(P(3900, 2100)), Big, "and one far under it is nearest to the big one");
    }

    [Theory]
    [MemberData(nameof(Layouts))]
    public void TheToolbarBesideACaptureIsOnAMonitorAndClearOfIt(string layout)
    {
        Desktop desk = Layout(layout);
        var bar = new ToolbarPlacement(desk);
        bar.Resize(ToolbarWidth, ToolbarHeight, ToolbarGap);
        foreach (Rect monitor in desk.ScreenMonitors)
        {
            foreach (Rect crop in CropsAlongTheEdgesOf(monitor))
            {
                bar.Place(desk.FromScreen(crop));
                Rect at = Rect.At(bar.X, bar.Y, ToolbarWidth, ToolbarHeight);
                Assert.True(OnAMonitor(desk, at), $"{layout}: the toolbar beside {crop} is on a monitor, not at {at}");
                Assert.False(at.Overlaps(crop), $"{layout}: the toolbar at {at} keeps clear of {crop}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Layouts))]
    public void TheToolbarOverACaptureOfAWholeMonitorStaysOnAMonitor(string layout)
    {
        Desktop desk = Layout(layout);
        var bar = new ToolbarPlacement(desk);
        bar.Resize(ToolbarWidth, ToolbarHeight, ToolbarGap);
        foreach (Rect monitor in desk.ScreenMonitors)
        {
            bar.Place(desk.FromScreen(monitor));
            Rect at = Rect.At(bar.X, bar.Y, ToolbarWidth, ToolbarHeight);
            Assert.True(OnAMonitor(desk, at), $"{layout}: the toolbar for all of {monitor} is on a monitor, not at {at}");
        }
    }

    [Theory]
    [MemberData(nameof(Layouts))]
    public void TheLabelsOfACaptureAreOnAMonitor(string layout)
    {
        Desktop desk = Layout(layout);
        var labels = new LabelPainter(desk, MetricsFor(desk), new CaretBlink(Timers.Caret));
        foreach (Rect monitor in desk.ScreenMonitors)
        {
            foreach (Rect crop in CropsAlongTheEdgesOf(monitor))
            {
                Rect area = desk.FromScreen(crop);
                Rect size = desk.ToScreen(labels.SizeLabelRect(area));
                Rect rec = desk.ToScreen(labels.RecTagRect(area, "REC 0:00 / 1:00"));
                Rect countdown = desk.ToScreen(labels.CountdownTagRect(area, "Cancel"));
                Rect toast = desk.ToScreen(labels.ToastRect(area, "Copied!"));
                Assert.True(OnAMonitor(desk, size), $"{layout}: the size of {crop} shows on a monitor, not at {size}");
                Assert.True(OnAMonitor(desk, rec), $"{layout}: the recording label of {crop} shows on a monitor, not at {rec}");
                Assert.True(OnAMonitor(desk, countdown), $"{layout}: the countdown label of {crop} shows on a monitor, not at {countdown}");
                Assert.True(OnAMonitor(desk, toast), $"{layout}: a toast in {crop} shows on a monitor, not at {toast}");
            }
        }
        labels.Free();
    }

    private static Desktop Layout(string name)
    {
        Rect small;
        switch (name)
        {
            case "small right, tops lined up":
                small = R(3840, 0, 5760, 1080);
                break;
            case "small right, bottoms lined up":
                small = R(3840, 1080, 5760, 2160);
                break;
            case "small left":
                small = R(-1920, 300, 0, 1380);
                break;
            case "small above":
                small = R(960, -1080, 2880, 0);
                break;
            default:
                throw new ArgumentException($"no layout named {name}");
        }
        return new Desktop(Rect.UnionIgnoringEmpty(Big, small), Big, [Big, small]);
    }

    private static IEnumerable<Rect> CropsAlongTheEdgesOf(Rect m)
    {
        const int w = 300, h = 200, inset = 20;
        int[] xs = [m.Left + inset, m.Left + (m.Width - w) / 2, m.Right - inset - w];
        int[] ys = [m.Top + inset, m.Top + (m.Height - h) / 2, m.Bottom - inset - h];
        foreach (int x in xs)
        {
            foreach (int y in ys)
            {
                yield return Rect.At(x, y, w, h);
            }
        }
    }

    private static bool OnAMonitor(Desktop desk, Rect r)
    {
        return desk.ScreenMonitors.Any(m => Rect.Intersect(m, r) == r);
    }
}
