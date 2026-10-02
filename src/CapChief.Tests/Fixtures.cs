namespace CapChief.Tests;

/// <summary>
/// Provides methods for building the values the tests share.
/// </summary>
public static class Fixtures
{
    public static Rect R(int l, int t, int r, int b)
    {
        return new() { Left = l, Top = t, Right = r, Bottom = b };
    }

    public static Point P(int x, int y)
    {
        return new(x, y);
    }

    public static Crop CropOf(int l, int t, int r, int b)
    {
        if (Crop.TryCreate(R(l, t, r, b), out Crop crop))
        {
            return crop;
        }
        else
        {
            throw new ArgumentException("smaller than a crop");
        }
    }

    public static Mark Rectangle(int l, int t, int r, int b)
    {
        return new RectangleMark { A = P(l, t), B = P(r, b) };
    }

    public static Mark Drawn(MarkKind k, int l, int t, int r, int b)
    {
        Mark m = Mark.Create(k) with { A = P(l, t), B = P(r, b) };
        if (m is PenMark pen)
        {
            return pen with { Path = new Polyline(P(l, t), P(r, b)) };
        }
        else
        {
            return m;
        }
    }

    public static Desktop Desk()
    {
        return new(R(0, 0, 1920, 1080), R(0, 0, 1920, 1080), [R(0, 0, 1920, 1080)]);
    }

    public static Metrics MetricsFor(Desktop desk)
    {
        return new(1f, desk);
    }
}

/// <summary>
/// Provides a screen for the tests: a flat gray with a block that moves along and changes color each
/// time a recording takes a frame of it.
/// </summary>
public sealed class TestScreen : ScreenSource
{
    public const uint Ground = 0x303030u;

    private static readonly uint[] BlockColors = [0xF0E0D0u, 0x55AAFFu, 0x55FFFFu, 0x0000AAu, 0xAAAA00u, 0xFF55FFu];

    private int _framesTaken;

    public static uint BlockColorAt(int frame)
    {
        return BlockColors[frame % BlockColors.Length];
    }

    public override bool Capture(Surface dst)
    {
        Paint(dst, dst.Width, dst.Height, 0);
        return true;
    }

    public override void CaptureWithPointer(Surface dst, Rect area)
    {
        Paint(dst, area.Width, area.Height, _framesTaken++);
        DeviceContext.Flush();
    }

    public override int ElapsedMs(long from, long now)
    {
        return 100;
    }

    public static Rect BlockAt(int frame, int width)
    {
        int x = frame * 12 % Math.Max(1, width - 8);
        return new() { Left = x, Top = 2, Right = x + 8, Bottom = 10 };
    }

    private static void Paint(Surface dst, int w, int h, int frame)
    {
        DeviceContext dc = dst.DeviceContext;
        dc.FillRect(new Rect { Right = w, Bottom = h }, Ground);
        dc.FillRect(BlockAt(frame, w), BlockColorAt(frame));
    }
}

/// <summary>
/// Represents the app running without windows, over a test screen, the way the tests drive it:
/// by pointer and keys, as the overlay would hand them over.
/// </summary>
public sealed class Headless : IDisposable
{
    public Headless()
    {
        App = new App(Fixtures.Desk(), new TestScreen(), excludedFromCapture: false);
    }

    public App App { get; }

    public Capture Capture
    {
        get
        {
            return App.Capture!;
        }
    }

    public Marks Marks
    {
        get
        {
            return Capture.Marks;
        }
    }

    public void Start(CaptureKind kind, Rect area)
    {
        App.CaptureService.StartCapture(kind);
        Drag(area.TopLeft, Fixtures.P(area.Right, area.Bottom));
    }

    public void Open(Rect area)
    {
        Start(CaptureKind.Frozen, area);
    }

    public void Record(Rect area, int frames = 5)
    {
        Start(CaptureKind.Recording, area);
        for (int i = 0; i < frames; i++)
        {
            App.Workflow.OnTick();
        }
        App.CaptureService.StartOrStopRecording();
    }

    public void Drag(Point from, Point to, bool shift = false)
    {
        App.Workflow.OnMouseDown(from, shift);
        App.Workflow.OnMouseMove(to, shift, false);
        App.Workflow.OnMouseUp();
    }

    public void Click(Point at)
    {
        App.Workflow.OnMouseDown(at, false);
        App.Workflow.OnMouseUp();
    }

    public bool Keystroke(Key key, bool ctrl = false, bool shift = false)
    {
        return App.Commands.RunKey(new Chord(key, ctrl, shift));
    }

    public void Draw(MarkKind kind, int x1, int y1, int x2, int y2)
    {
        App.Tools.PickTool(kind);
        Drag(Fixtures.P(x1, y1), Fixtures.P(x2, y2));
    }

    public void Dispose()
    {
        App.Shutdown();
    }
}
