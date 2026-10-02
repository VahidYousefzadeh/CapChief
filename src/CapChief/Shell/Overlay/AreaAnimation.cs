namespace CapChief.Shell;

/// <summary>
/// Represents a window that animates a copy of the selected area: flashing it with a message, or
/// shrinking it into a point.
/// </summary>
public sealed class AreaAnimation : ITimerHandler, ICloseHandler
{
    private const string ClassName = "CCAnimation";

    private const uint FirstTickMs = 10;

    private const uint FlashMs = 200, ShowMs = 1200, FadeMs = 120;

    private const byte FlashAlpha = 0x10;

    private const int WhiteSide = 16;

    private const uint ShrinkMs = 225;

    private const float NearPower = 1.6f, FarPower = 3.2f, StartOpacity = 0.9f;

    private readonly WindowKind _kind = new(ClassName, null, doubleClicks: false, redrawOnResize: false, ownsBackground: true, tracksLeaving: false);
    private readonly Desktop _desk;
    private readonly MarkPainter _markPainter;
    private readonly LabelPainter _labelPainter;
    private readonly Surface _picture = new();
    private readonly Surface _white = new();
    private readonly AlphaSurface _frame = new();

    private Window _window = Window.None;
    private Ticker? _ticker;
    private Action<float>? _draw;

    private string _message = "";
    private Point _at;
    private Rect _pictureAt, _toastAt;
    private FlashLook? _shown;

    private Rect _from;
    private Point _to;
    private int _farthest;

    public AreaAnimation(Desktop desk, MarkPainter markPainter, LabelPainter labelPainter)
    {
        _desk = desk;
        _markPainter = markPainter;
        _labelPainter = labelPainter;
    }

    public void Flash(Capture capture, string message)
    {
        Close();
        Rect crop = capture.Crop;
        Rect toast = _labelPainter.ToastRect(crop, message);
        Rect box = Rect.Union(crop, toast);
        Rect onScreen = _desk.ToScreen(box);
        if (!capture.Render(_picture, _markPainter) || !_white.OpenDib(WhiteSide, WhiteSide) || !_frame.Open(box.Width, box.Height))
        {
            Free();
            return;
        }

        _message = message;
        _at = onScreen.TopLeft;
        _pictureAt = crop.Moved(-box.TopLeft);
        _toastAt = toast.Moved(-box.TopLeft);
        _shown = null;
        Play(onScreen, ShowMs + FadeMs, DrawFlash);
    }

    public void ShrinkInto(DeviceContext shown, Rect area, Point to)
    {
        Close();
        Rect from = _desk.ToScreen(area);
        if (!_picture.OpenCompatible(area.Width, area.Height) || !_frame.Open(from.Width, from.Height))
        {
            Free();
            return;
        }
        _picture.DeviceContext.Blit(_picture.Bounds, shown, area.Left, area.Top);

        _from = from;
        _to = to;
        _farthest = Math.Max(1, FarthestEdge(from, to));
        Play(from, ShrinkMs, DrawShrink);
    }

    public bool Close()
    {
        if (!_window.Exists)
        {
            return false;
        }
        _window.Destroy();
        return true;
    }

    public void OnTimer(int id)
    {
        if (id != Timers.Animation || _ticker is not { } ticker)
        {
            return;
        }
        if (!ticker.IsRunning)
        {
            ticker.Begin(_window);
        }
        else if (ticker.Advance(_window))
        {
            _draw?.Invoke(ticker.Progress);
        }
        else
        {
            _window.Destroy();
        }
    }

    public void OnClosed()
    {
        _ticker?.End(_window);
        _window = Window.None;
        Free();
    }

    private void Play(Rect onScreen, uint lengthMs, Action<float> draw)
    {
        _window = _kind.Open(new WindowSpec
        {
            Bounds = onScreen,
            AlwaysOnTop = true,
            TakesFocus = false,
            ShowsInTaskbar = false,
            PerPixelAlpha = true,
            ClickThrough = true,
        }, null, this);
        if (!_window.Exists)
        {
            Free();
            return;
        }

        _ticker = new Ticker(Timers.Animation, lengthMs, 1);
        _draw = draw;
        draw(0f);
        _window.Reveal();
        _window.StartTimer(Timers.Animation, FirstTickMs);
    }

    private void DrawFlash(float progress)
    {
        float ms = progress * (ShowMs + FadeMs);
        var look = new FlashLook(WhiteAt(ms), OpacityAt(ms));
        if (look == _shown)
        {
            return;
        }
        if (look.White != _shown?.White)
        {
            Compose(look.White);
        }
        _shown = look;
        _window.PushPixels(_frame.Surface, _at.X, _at.Y, look.Opacity);
    }

    /// <summary>
    /// Gives the alpha of the white veil at this moment: it starts at FlashAlpha and fades out over
    /// the first FlashMs.
    /// </summary>
    private static byte WhiteAt(float ms)
    {
        if (ms < FlashMs)
        {
            return (byte)(FlashAlpha * Squared(1f - ms / FlashMs));
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Gives the opacity at this moment: fully shown until ShowMs, then faded out over FadeMs.
    /// </summary>
    private static byte OpacityAt(float ms)
    {
        if (ms < ShowMs)
        {
            return 255;
        }
        else
        {
            return (byte)Math.Clamp((int)(255 * (1f - (ms - ShowMs) / FadeMs)), 0, 255);
        }
    }

    private void Compose(byte white)
    {
        _frame.Clear();
        DeviceContext frame = _frame.DeviceContext;
        frame.Blit(_pictureAt, _picture.DeviceContext, 0, 0);
        _frame.Opaque(_pictureAt);
        if (white > 0)
        {
            // Every byte set to the alpha makes premultiplied white at that alpha.
            _white.Pixels.Fill(white);
            frame.AlphaBlend(_pictureAt, _white.DeviceContext, _white.Bounds);
        }
        _labelPainter.DrawToast(_frame, _toastAt, _message);
    }

    private void DrawShrink(float progress)
    {
        var at = new Rect
        {
            Left = Toward(_from.Left, _to.X, progress),
            Top = Toward(_from.Top, _to.Y, progress),
            Right = Toward(_from.Right, _to.X, progress),
            Bottom = Toward(_from.Bottom, _to.Y, progress),
        };
        var part = new Rect { Right = Math.Max(1, at.Width), Bottom = Math.Max(1, at.Height) };
        _frame.DeviceContext.StretchBlit(part, _picture.DeviceContext, _picture.Bounds);
        _frame.Opaque(part);
        byte opacity = (byte)(255 * StartOpacity * (1f - progress * progress * progress));
        _window.PushPixels(_frame.Surface, part, at.Left, at.Top, opacity);
    }

    private int Toward(int edge, int to, float progress)
    {
        float power = NearPower + (FarPower - NearPower) * Math.Abs(edge - to) / _farthest;
        return edge + (int)MathF.Round((to - edge) * MathF.Pow(progress, power));
    }

    private void Free()
    {
        _picture.Free();
        _white.Free();
        _frame.Free();
    }

    /// <summary>
    /// Gives how far the farthest edge of the rectangle is from the point.
    /// </summary>
    private static int FarthestEdge(Rect from, Point to)
    {
        return Math.Max(Math.Max(Math.Abs(from.Left - to.X), Math.Abs(from.Right - to.X)), Math.Max(Math.Abs(from.Top - to.Y), Math.Abs(from.Bottom - to.Y)));
    }

    private static float Squared(float v)
    {
        return v * v;
    }

    /// <summary>
    /// Represents how white and how opaque the flash is at one moment.
    /// </summary>
    private readonly record struct FlashLook(byte White, byte Opacity);
}
