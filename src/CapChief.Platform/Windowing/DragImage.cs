namespace CapChief.Windowing;

/// <summary>
/// Displays a translucent bitmap on top of the screen that mouse clicks pass through.
/// </summary>
public sealed class DragImage
{
    private const string ClassName = "CCDragImage";

    private readonly WindowKind _kind = new(ClassName, null, doubleClicks: false, redrawOnResize: false, ownsBackground: false, tracksLeaving: false);

    private Window _window = Window.None;
    private Window _owner = Window.None;

    public bool IsAttached { get; private set; }

    public void Host(Window owner)
    {
        _owner = owner;
    }

    public bool Show(AlphaSurface alpha, int x, int y)
    {
        Ensure();
        if (!_window.Exists)
        {
            return false;
        }
        if (!_window.PushPixels(alpha.Surface, x, y))
        {
            return false;
        }

        _window.Reveal();
        IsAttached = true;
        return true;
    }

    public void Move(int x, int y)
    {
        if (_window.Exists)
        {
            _window.MoveTo(x, y);
        }
    }

    public void Hide()
    {
        IsAttached = false;
        if (_window.Exists && _window.IsVisible)
        {
            _window.Hide();
        }
    }

    private void Ensure()
    {
        if (_window.Exists)
        {
            return;
        }

        _window = _kind.Open(new WindowSpec
        {
            Bounds = new Rect { Right = 1, Bottom = 1 },
            AlwaysOnTop = true,
            TakesFocus = false,
            ShowsInTaskbar = false,
            PerPixelAlpha = true,
            ClickThrough = true,
        }, _owner, null);

        if (_window.Exists)
        {
            _window.ExcludeFromCapture();
        }
    }
}
