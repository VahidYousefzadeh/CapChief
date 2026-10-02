using System.Diagnostics;

namespace CapChief.Lab;

/// <summary>
/// Checks on the real screen, by filming a picture of the synthetic screen put up on it, that the
/// live overlay stays out of a recording and that a recording holds what was on the screen.
/// </summary>
public sealed class GifCheck
{
    private const int Sessions = 3;
    private const int Frames = 8;
    private const int FrameMs = 60;
    private const double MaxMeanError = 8.0;

    private readonly Driver _d;
    private readonly SyntheticScreen _screen;
    private readonly Report _report = new("gifcheck");

    public GifCheck(Driver driver, SyntheticScreen screen)
    {
        _d = driver;
        _screen = screen;
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
        var shot = new Rect { Left = 120, Top = 120, Right = 520, Bottom = 400 };
        using var picture = new Surface();
        var window = new PictureWindow(picture);
        bool shown = picture.OpenCompatible(shot.Width, shot.Height);
        if (shown)
        {
            _screen.BlitInto(picture, shot);
            shown = window.Show(_d.Desk.ToScreen(shot));
        }
        _screen.On = false;

        if (!shown)
        {
            _report.Check(false, "a picture of the synthetic screen is put up to film");
        }
        else
        {
            App.Shell.Toolbar.Hide();
            Rect area = shot.Inflated(-4);
            byte[] onScreen = _d.Screen(area);
            _report.Note($"the picture shows {area.Width}x{area.Height} at {area.Left},{area.Top}");
            LiveOverlayIsNotCaptured(area, onScreen, Path.GetDirectoryName(Path.GetFullPath(path))!);
            RecordsWhatIsOnScreen(area, onScreen);
        }

        window.Close();
        App.Commands.Run(App.Commands.HideToTray);
        return _report.Write(path);
    }

    private void LiveOverlayIsNotCaptured(Rect area, byte[] onScreen, string dir)
    {
        for (int session = 1; session <= Sessions; session++)
        {
            App.CaptureService.StartCapture(CaptureKind.Frozen);
            _d.Escape();
            App.CaptureService.StartCapture(CaptureKind.Recording);
            double live = MeanError(_d.Screen(area), onScreen);
            if (live >= 1)
            {
                Pictures.Screen(_d, area, Path.Combine(dir, $"gifcheck-live-{session}.png"));
            }
            _d.Escape();
            _report.Check(live < 1, $"session {session} of {Sessions}: after a frozen capture, the screen under the live overlay still shows the picture (mean error {live:0.00})");
        }
    }

    private void RecordsWhatIsOnScreen(Rect area, byte[] onScreen)
    {
        App.CaptureService.StartCapture(CaptureKind.Recording);
        _d.Drag(area.Left - 8, area.Top - 8, area.Right + 8, area.Bottom + 8, 4);
        if (App.Mode != Mode.Recording)
        {
            _report.Check(false, "a recording starts over the picture");
            return;
        }
        Rect crop = App.Recording.Crop;

        var clock = Stopwatch.StartNew();
        for (int i = 0; i < Frames; i++)
        {
            Thread.Sleep(FrameMs);
            App.Workflow.OnTick();
        }
        App.CaptureService.StartOrStopRecording();
        long took = clock.ElapsedMilliseconds;

        if (App.Mode != Mode.Reviewing || App.Review.Clip is not { } clip)
        {
            _report.Check(false, "stopping the recording opens its review");
            return;
        }
        _report.Check(clip.Count == Frames + 1, $"{clip.Count} frames for {Frames} ticks and the one the start took");
        long recordedMs = clip.Frames.Sum(f => f.DelayCs) * 10L;
        _report.Check(recordedMs > 0 && recordedMs <= took + 10, $"the frames last {recordedMs} ms of a recording that took {took} ms");

        var inside = new Rect { Left = area.Left - crop.Left, Top = area.Top - crop.Top };
        inside.Right = inside.Left + area.Width;
        inside.Bottom = inside.Top + area.Height;
        int black = 0;
        int flat = 0;
        double worst = 0;
        App.Review.Jump(toEnd: false);
        for (int i = 0; i < clip.Count; i++)
        {
            byte[] frame = Crop(App.Review.Frame, inside);
            if (Lit(frame) == 0)
            {
                black++;
            }
            if (IsOneColor(frame))
            {
                flat++;
            }
            worst = Math.Max(worst, MeanError(frame, onScreen));
            App.Review.Advance(1);
        }
        _report.Check(black == 0 && flat == 0, $"no frame is black ({black}) or one flat color ({flat})");
        _report.Check(worst < MaxMeanError, $"every frame shows the picture, the worst with a mean error of {worst:0.00} of 255");

        string saved = Path.Combine(Path.GetTempPath(), $"{Product.FilePrefix}gifcheck.gif");
        App.Review.Jump(toEnd: false);
        App.Review.Advance(2);
        App.Review.Trim(head: true);
        _report.Check(App.Review.SaveAs(saved) && File.ReadAllBytes(saved).AsSpan(0, 6).SequenceEqual("GIF89a"u8), "the trimmed recording saves as a GIF");
        try
        {
            File.Delete(saved);
        }
        catch (Exception e) when (Log.Tolerated("delete the checked GIF", e)) { }
        _d.Escape();
    }

    private static byte[] Crop(Surface s, Rect r)
    {
        var px = new byte[r.Width * r.Height * 4];
        new Pixmap(s.Pixels, s.Width, s.Height, s.Width).CopyArea(r, px);
        return px;
    }

    private static double MeanError(byte[] a, byte[] b)
    {
        if (a.Length != b.Length || a.Length == 0)
        {
            return 255;
        }
        long sum = 0;
        for (int i = 0; i < a.Length; i += 4)
        {
            for (int c = 0; c < 3; c++)
            {
                sum += Math.Abs(a[i + c] - b[i + c]);
            }
        }
        return sum / (a.Length / 4 * 3.0);
    }

    private static int Lit(byte[] px)
    {
        int lit = 0;
        for (int i = 0; i < px.Length; i += 4)
        {
            if (px[i] != 0 || px[i + 1] != 0 || px[i + 2] != 0)
            {
                lit++;
            }
        }
        return lit;
    }

    private static bool IsOneColor(byte[] px)
    {
        for (int i = 4; i < px.Length; i += 4)
        {
            if (px[i] != px[0] || px[i + 1] != px[1] || px[i + 2] != px[2])
            {
                return false;
            }
        }
        return true;
    }
}

/// <summary>
/// Represents a plain window of the Lab's own that shows a picture on the real screen, for a check
/// to film.
/// </summary>
public sealed class PictureWindow : IPaintHandler
{
    private static readonly WindowKind Kind = new("CCLabPicture", null, doubleClicks: false, redrawOnResize: false, ownsBackground: true, tracksLeaving: false);

    private readonly Surface _picture;

    private Window _window = Window.None;

    public PictureWindow(Surface picture)
    {
        _picture = picture;
    }

    public bool Show(Rect onScreen)
    {
        _window = Kind.Open(new WindowSpec
        {
            Bounds = onScreen,
            AlwaysOnTop = true,
            TakesFocus = false,
            ShowsInTaskbar = false,
        }, null, this);
        if (!_window.Exists)
        {
            return false;
        }
        _window.Show();
        _window.RepaintNow();
        return true;
    }

    public void Close()
    {
        if (_window.Exists)
        {
            _window.Destroy();
        }
    }

    public void OnPaint()
    {
        using var paint = _window.Paint();
        if (paint.HasBegun)
        {
            paint.DeviceContext.Blit(new Rect { Right = _picture.Width, Bottom = _picture.Height }, _picture.DeviceContext, 0, 0);
        }
    }
}
