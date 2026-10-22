namespace CapChief.Shell;

/// <summary>
/// Represents the picture that follows the pointer while a mark is moved, or while the area moves
/// with its marks.
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
        return Show(capture, [mark], mark);
    }

    public bool Show(Capture capture, IReadOnlyList<Mark> marks, Mark? selected)
    {
        int grip = _metrics.Tolerance.GripRadius;
        Rect r = default;
        foreach (Mark mark in marks)
        {
            r = Rect.UnionIgnoringEmpty(r, mark.Extent(_metrics.Sizes));
        }
        if (selected is not null)
        {
            r = Rect.UnionIgnoringEmpty(r, selected.SelectedExtent(_metrics.Sizes, grip));
        }
        if (!_owner.Exists || !_pixels.Open(r.Width, r.Height))
        {
            return false;
        }

        Point shift = -r.TopLeft;
        foreach (Mark mark in marks)
        {
            _markPainter.DrawMark(_pixels.DeviceContext, capture, mark, 0, shift);
        }
        _pixels.Seal();
        if (selected is not null)
        {
            foreach (Grip g in selected.Grips)
            {
                Rect at = selected.GripRect(g, grip).Moved(shift);
                MarkPainter.DrawGrip(_pixels.DeviceContext, at);
                _pixels.Opaque(at);
            }
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
