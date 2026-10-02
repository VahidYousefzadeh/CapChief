namespace CapChief.Capturing;

/// <summary>
/// Handles choosing the part of the screen to capture: hovering picks the window or control under
/// the pointer, a drag draws a rectangle, and while the button is down Tab hands the width and
/// height to the keyboard.
/// </summary>
public sealed class Selecting
{
    private readonly WindowFinder _finder;
    private readonly Desktop _desk;
    private readonly Metrics _metrics;

    public event Action<Rect, Rect>? SelectionChanged;

    private PointerState _pointer;
    private bool _insideWindow;
    private Point _from, _to;

    public Selecting(WindowFinder finder, Desktop desk, Metrics metrics)
    {
        _finder = finder;
        _desk = desk;
        _metrics = metrics;
    }

    public CaptureKind Kind { get; private set; }

    public Rect Selection { get; private set; }

    public TypedSize? Typed { get; private set; }

    public bool ShowsTarget
    {
        get
        {
            return _pointer != PointerState.Dragging;
        }
    }

    private bool IsTypingSize
    {
        get
        {
            return Typed is not null;
        }
    }

    public void Begin(CaptureKind kind, Point cursor)
    {
        Kind = kind;
        _pointer = PointerState.Hovering;
        _insideWindow = false;
        Typed = null;
        _finder.FindWindows();
        Selection = _finder.WindowAt(cursor);
        Point center;
        if (Selection.IsEmpty)
        {
            center = cursor;
        }
        else
        {
            center = Selection.Center;
        }
        _metrics.ScaleToMonitorAt(center);
    }

    public void End()
    {
        Selection = default;
        Typed = null;
        _pointer = PointerState.Hovering;
        _finder.CancelScan();
    }

    public void Hover(Point p, bool ctrl)
    {
        if (IsTypingSize)
        {
            return;
        }
        _insideWindow = ctrl && Kind == CaptureKind.Frozen;
        Rect target;
        if (_insideWindow)
        {
            _finder.ScanElementsAt(p);
            target = _finder.ElementAt(p);
        }
        else
        {
            target = _finder.WindowAt(p);
        }
        if (_pointer == PointerState.Hovering && target == Selection)
        {
            return;
        }
        _pointer = PointerState.Hovering;
        Show(target);
    }

    public void ElementsScanned(Point cursor)
    {
        if (_finder.TakeScannedElements() && _pointer == PointerState.Hovering)
        {
            Hover(cursor, _insideWindow);
        }
    }

    public void Press(Point p)
    {
        if (_pointer == PointerState.Dragging)
        {
            Show(default);
        }
        _pointer = PointerState.Pressed;
        _from = _to = p;
        Typed = null;
    }

    public void Drag(Point p)
    {
        if (_pointer == PointerState.Pressed)
        {
            if (!Gesture.MovedPastDragThreshold(_from, p))
            {
                return;
            }
            _pointer = PointerState.Dragging;
        }
        if (_pointer != PointerState.Dragging || IsTypingSize)
        {
            return;
        }

        _to = p;
        ShowDraggedSelection();
    }

    public Crop? Release()
    {
        if (IsTypingSize)
        {
            return null;
        }
        else
        {
            return Accept();
        }
    }

    public Crop? Accept()
    {
        _pointer = PointerState.Hovering;
        Typed = null;
        if (Crop.TryCreate(Selection, out Crop crop))
        {
            return crop;
        }
        Show(default);
        return null;
    }

    public bool FocusNextSizeField()
    {
        if (_pointer == PointerState.Hovering)
        {
            return false;
        }
        if (Typed is { } typed)
        {
            Typed = typed.FocusOtherField();
        }
        else
        {
            Typed = new TypedSize();
        }
        ApplyTypedSize();
        return true;
    }

    public bool EraseSizeDigit()
    {
        if (Typed is not { } typed)
        {
            return false;
        }
        Typed = typed.RemoveLastDigit();
        ApplyTypedSize();
        return true;
    }

    public bool TypeSizeDigit(int d)
    {
        if (Typed is not { } typed)
        {
            return false;
        }
        Typed = typed.AppendDigit(d);
        ApplyTypedSize();
        return true;
    }

    public bool CancelTypedSize()
    {
        if (!IsTypingSize)
        {
            return false;
        }
        Typed = null;
        ApplyTypedSize();
        return true;
    }

    private void ApplyTypedSize()
    {
        if (Typed is { HasDigits: true })
        {
            _pointer = PointerState.Dragging;
        }
        if (_pointer == PointerState.Dragging)
        {
            ShowDraggedSelection();
        }
        else
        {
            Show(Selection);
        }
    }

    private void ShowDraggedSelection()
    {
        TypedSize size = Typed ?? default;
        Show(FromDrag(_from, _to, size.Width, size.Height, _desk.Bounds));
    }

    private void Show(Rect selection)
    {
        Rect was = Selection;
        Selection = selection;
        SelectionChanged?.Invoke(was, selection);
    }

    public static Rect FromDrag(Point anchor, Point pointer, int width, int height, Rect bounds)
    {
        int min;
        if (width > 0 || height > 0)
        {
            min = Crop.MinSize;
        }
        else
        {
            min = 0;
        }
        (int left, int right) = Span(anchor.X, pointer.X, width, min, bounds.Left, bounds.Right);
        (int top, int bottom) = Span(anchor.Y, pointer.Y, height, min, bounds.Top, bounds.Bottom);
        return new Rect { Left = left, Top = top, Right = right, Bottom = bottom };
    }

    private static (int From, int To) Span(int anchor, int pointer, int length, int min, int lo, int hi)
    {
        bool back = pointer < anchor;
        int room;
        if (back)
        {
            room = anchor - lo;
        }
        else
        {
            room = hi - anchor;
        }
        int len;
        if (length > 0)
        {
            len = length;
        }
        else
        {
            len = Math.Max(Math.Abs(pointer - anchor), min);
        }
        len = Math.Clamp(len, 0, Math.Max(0, room));
        if (back)
        {
            return (anchor - len, anchor);
        }
        else
        {
            return (anchor, anchor + len);
        }
    }

    /// <summary>
    /// Specifies what the pointer is doing while the user selects: hovering over a window, holding
    /// the button down, or dragging out a rectangle of its own.
    /// </summary>
    private enum PointerState
    {
        Hovering,
        Pressed,
        Dragging,
    }
}
