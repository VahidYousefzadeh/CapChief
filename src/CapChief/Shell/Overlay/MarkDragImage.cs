namespace CapChief.Shell;

/// <summary>
/// Represents the picture of a mark that follows the pointer while the mark is moved.
/// </summary>
public sealed class MarkDragImage
{
    private readonly Desktop _desk;
    private readonly Metrics _metrics;
    private readonly MarkPainter _markPainter;
    private readonly DragImage _dragImage = new();
    private readonly AlphaSurface _pixels = new();

    private Window _owner = Window.None;
    private Point _shownAt;

    public event Action? Shown;

    public MarkDragImage(Desktop desk, Metrics metrics, MarkPainter markPainter)
    {
        _desk = desk;
        _metrics = metrics;
        _markPainter = markPainter;
    }

    public void Host(Window owner)
    {
        _owner = owner;
        _dragImage.Host(owner);
    }

    public bool Show(Capture capture, Mark mark)
    {
        int grip = _metrics.Tolerance.GripRadius;
        Rect r = mark.SelectedExtent(_metrics.Sizes, grip);
        if (!_owner.Exists || !_pixels.Open(r.Width, r.Height))
        {
            return false;
        }

        Point shift = -r.TopLeft;
        _markPainter.DrawMark(_pixels.DeviceContext, capture, mark, 0, shift);
        _pixels.Seal();
        foreach (Grip g in mark.Grips)
        {
            Rect at = mark.GripRect(g, grip).Moved(shift);
            MarkPainter.DrawGrip(_pixels.DeviceContext, at);
            _pixels.Opaque(at);
        }

        Rect on = _desk.ToScreen(r);
        if (!_dragImage.Show(_pixels, on.Left, on.Top))
        {
            _pixels.Free();
            return false;
        }
        _shownAt = on.TopLeft;
        Shown?.Invoke();
        return true;
    }

    public void Move(Point offset)
    {
        Point at = _shownAt + offset;
        _dragImage.Move(at.X, at.Y);
    }

    public void Hide()
    {
        if (!_dragImage.IsAttached)
        {
            return;
        }
        _owner.PaintNow();
        _dragImage.Hide();
        _pixels.Free();
    }

    public void Free()
    {
        _pixels.Free();
    }
}
