namespace CapChief.Shell;

/// <summary>
/// Represents the full-screen window a capture happens in, which shows the frozen screen, passes
/// the mouse and the keyboard to the app, and repaints what changed.
/// </summary>
public sealed class Overlay : IPaintHandler, IMouseHandler, IDoubleClickHandler, IRightClickHandler, IKeyDownHandler, IKeyUpHandler,
                              ITimerHandler, IHotkeyHandler, IActivateHandler, IDeactivateHandler, IEditBackdropHandler,
                              IAppMessageHandler, IDisplayChangeHandler, ICloseHandler
{
    public const string ClassName = "CCMain";

    private const int InitialWidthPx = 200, InitialHeightPx = 80;

    public event Action<Point, bool>? MouseDown;
    public event Action<Point, bool>? DoubleClick;
    public event Action? RightClicked;
    public event Action<Point, bool, bool>? MouseMove;
    public event Action? MouseUp;
    public event Action<bool>? CtrlChanged;
    public event Action<bool>? AltChanged;
    public event Func<Chord, bool>? KeyDown;
    public event Func<Point, Cursor>? CursorWanted;
    public event Action? Tick;
    public event Action<AppSignal>? TrayMessageReceived;
    public event Action? ShowToolbarFromTrayRequested;
    public event Action? ElementsScanned;
    public event Action? ClipCut;
    public event Action? TaskbarCreatedMessageReceived;
    public event Action? ToolbarRaiseRequested;
    public event Action<int>? HotkeyPressed;
    public event Action? DisplayChanged;

    private readonly Desktop _desk;
    private readonly Metrics _metrics;
    private readonly AppIcon _icon;
    private readonly bool _isExcludedFromCapture;
    private readonly Selecting _selecting;
    private readonly Document _document;
    private readonly WindowKind _windowKind;
    private readonly AreaAnimation _animation;
    private readonly OverlayPainter _painter;
    private readonly LabelPainter _labelPainter;
    private readonly LivePainter _live;
    private readonly Crosshair _crosshair = new(Palette.AccentArgb);
    private readonly CaretBlink _caret = new(Timers.Caret);
    private readonly TextMarkBox _textBox;
    private readonly MarkDragImage _dragImage;
    private readonly Region _update = new();
    private readonly List<Rect> _markAreas = new();

    private IReadOnlyList<Command> _hotkeys = [];
    private Command? _cancelCountdown;
    private Command? _stopRecording;
    private uint _tickMs;
    private bool _mouseCaptured;
    private TypedSize? _caretFor;
    private bool _layered;
    private LiveContent _liveShown = LiveContent.None;

    public Overlay(Desktop desk, Metrics metrics, AppIcon icon, bool excludedFromCapture, Selecting selecting, Document document, Review review, MarkPainter markPainter, Tools tools)
    {
        _desk = desk;
        _metrics = metrics;
        _icon = icon;
        _selecting = selecting;
        _document = document;
        _textBox = new TextMarkBox(desk, metrics);
        _dragImage = new MarkDragImage(desk, metrics, markPainter);
        _dragImage.Shown += () => ToolbarRaiseRequested?.Invoke();
        _isExcludedFromCapture = excludedFromCapture;
        _labelPainter = new LabelPainter(desk, metrics, _caret);
        _live = new LivePainter(_labelPainter, metrics);
        Drawing = new Drawing(document, tools, this, metrics, _textBox, _dragImage);
        _textBox.EndRequested += () => Drawing.EndTextEdit(cancel: false);
        _painter = new OverlayPainter(desk, metrics, _labelPainter, selecting, Drawing, document, review, markPainter);
        _windowKind = new WindowKind(ClassName, icon, doubleClicks: true, redrawOnResize: true, ownsBackground: true, tracksLeaving: false);
        _animation = new AreaAnimation(desk, markPainter, _labelPainter);
    }

    private Layer Layer
    {
        get
        {
            return _live.Layer;
        }
    }

    public Window Window { get; private set; } = Window.None;

    public Drawing Drawing { get; }

    public bool IsVisible
    {
        get
        {
            return Window.Exists && Window.IsVisible;
        }
    }

    public Point CursorPosition
    {
        get
        {
            return _desk.FromScreen(MousePointer.At);
        }
    }

    public Rect StopButton
    {
        get
        {
            return _live.StopButton;
        }
    }

    public uint TickMs
    {
        set
        {
            if (value == _tickMs)
            {
                return;
            }
            _tickMs = value;
            if (value == 0)
            {
                Window.StopTimer(Timers.Tick);
            }
            else
            {
                Window.StartTimer(Timers.Tick, value);
            }
        }
    }

    public void Create(Commands commands)
    {
        _cancelCountdown = commands.TimedCapture;
        _stopRecording = commands.Record;
        Window = _windowKind.Open(new WindowSpec
        {
            Title = Product.Name,
            Bounds = new Rect { Right = InitialWidthPx, Bottom = InitialHeightPx },
            AlwaysOnTop = true,
        }, null, this);
        _textBox.Host(Window);
        _dragImage.Host(Window);
        Window.SetIcon(_icon);
        if (_isExcludedFromCapture)
        {
            Window.ExcludeFromCapture();
        }
        _hotkeys = commands.Hotkeys;
        Hotkeys.Register(Window, _hotkeys);
    }

    public void Close()
    {
        if (Window.Exists)
        {
            Window.Destroy();
        }
    }

    public void Free()
    {
        _animation.Close();
        _painter.Close();
        _live.Free();
        _dragImage.Free();
        _update.Free();
        _crosshair.Free();
        _labelPainter.Free();
    }

    public bool ShowSelecting(Surface screen)
    {
        return ShowFrozen(screen, OverlayContent.Selecting);
    }

    public bool ShowAnnotating(Surface screen)
    {
        return ShowFrozen(screen, OverlayContent.Annotating);
    }

    public bool ShowReview(Surface screen)
    {
        return ShowFrozen(screen, OverlayContent.Review);
    }

    public void ShowAnnotating()
    {
        _painter.Show(OverlayContent.Annotating);
        InvalidateAll();
    }

    private bool ShowFrozen(Surface screen, OverlayContent content)
    {
        CloseLive();
        if (!_painter.Open(screen, content))
        {
            return false;
        }
        Window.PlaceAt(_desk.ScreenBounds);
        Window.ShowKeepingOrder();
        Window.InvalidateAll();
        Window.PaintNow();
        Window.Focus();
        return true;
    }

    public void ShowLive(Rect crop)
    {
        _painter.Close();
        Layer.Open(_desk.ScreenBounds);
        SetLayered(true);
        _live.ClearLayer(Palette.DimA);
        _liveShown = LiveContent.Selection;
        DrawLiveSelection(crop);
        Window.PlaceAt(_desk.ScreenBounds);
        Window.PushLayer(Layer);
        Window.ShowKeepingOrder();
        Window.Focus();
    }

    public void ShowCountdown(Rect crop, int seconds, bool showsBorder)
    {
        if (_liveShown != LiveContent.Countdown)
        {
            _live.ClearLayer(Layer.ClickableAlpha);
            Window.ClickThrough = true;
            _liveShown = LiveContent.Countdown;
        }
        _live.DrawCountdown(crop, seconds, showsBorder, _cancelCountdown?.Tip ?? "");
        Window.PushLayer(Layer);
    }

    public void ShowRecording()
    {
        if (_liveShown == LiveContent.Recording)
        {
            return;
        }
        // Alpha 0 lets clicks reach the windows below, except on the frame and the stop label.
        _live.ClearLayer(0);
        Window.ClickThrough = false;
        Window.PushLayer(Layer);
        _liveShown = LiveContent.Recording;
    }

    public void ShowRecording(Rect crop, int seconds)
    {
        ShowRecording();
        _live.DrawRecording(crop, ClipText.RecordingTag(seconds, _stopRecording?.Tip ?? ""));
        Window.PushLayer(Layer, _live.Dirty);
    }

    public void HideWindow()
    {
        if (Window.Exists)
        {
            Window.Hide();
        }
    }

    public void Hide()
    {
        TickMs = 0;
        ReleaseMouse();
        UpdateCaret(null);
        _dragImage.Hide();
        EndToast();
        HideWindow();
        _painter.Close();
        CloseLive();
    }

    public void Toast(string message)
    {
        if (_document.Capture is not { } capture || !_painter.IsOpen)
        {
            return;
        }
        Invalidate(_painter.EndToast());
        Invalidate(_painter.ShowToast(capture.Crop, message));
        Window.StartTimer(Timers.Toast, Timers.ToastMs);
    }

    private void EndToast()
    {
        Window.StopTimer(Timers.Toast);
        Invalidate(_painter.EndToast());
    }

    private void CloseLive()
    {
        if (_liveShown == LiveContent.None && !_layered)
        {
            return;
        }
        Window.ClickThrough = false;
        SetLayered(false);
        Layer.Close();
        _live.Clear();
        _liveShown = LiveContent.None;
    }

    private void SetLayered(bool on)
    {
        if (on == _layered)
        {
            return;
        }
        _layered = on;
        if (on && _isExcludedFromCapture)
        {
            Window.ExcludeFromCapture();
        }
        Window.PerPixelAlpha = on;
    }

    private void DrawLiveSelection(Rect crop)
    {
        _live.DrawSelecting(crop, !crop.IsEmpty, _selecting.ShowsTarget, _selecting.Kind == CaptureKind.Timed, _selecting.Typed);
    }

    public void CaptureMouse()
    {
        if (_mouseCaptured)
        {
            return;
        }
        _mouseCaptured = true;
        Window.CaptureMouse();
    }

    public void ReleaseMouse()
    {
        if (!_mouseCaptured)
        {
            return;
        }
        _mouseCaptured = false;
        Window.ReleaseMouse();
    }

    public void Invalidate(Rect r)
    {
        if (_painter.IsOpen)
        {
            Window.Invalidate(r);
        }
    }

    private void InvalidateBackdrop(Rect r)
    {
        if (!_painter.IsOpen)
        {
            return;
        }
        _painter.MarkStale(r);
        Window.Invalidate(r);
    }

    public void InvalidateAll()
    {
        if (!_painter.IsOpen)
        {
            return;
        }
        _painter.MarkStale(_desk.Bounds);
        Window.InvalidateAll();
    }

    public void InvalidateMark(Mark mark, bool selected, bool backdrop)
    {
        _markAreas.Clear();
        if (selected)
        {
            mark.AddSelectedAreas(_markAreas, _metrics.Sizes, _metrics.Tolerance.GripRadius);
        }
        else
        {
            mark.AddPaintedAreas(_markAreas, _metrics.Sizes);
        }
        foreach (Rect r in _markAreas)
        {
            if (backdrop)
            {
                InvalidateBackdrop(r);
            }
            else
            {
                Invalidate(r);
            }
        }
    }

    public void OnMarkAdded(Mark mark)
    {
        InvalidateMark(mark, selected: false, backdrop: true);
    }

    public void OnMarkRemoved(Mark mark)
    {
        InvalidateMark(mark, selected: true, backdrop: true);
    }

    public void OnMarkReplaced(Mark was, Mark now)
    {
        InvalidateMark(was, selected: true, backdrop: true);
        InvalidateMark(now, selected: true, backdrop: true);
    }

    public void OnSelectedMarkChanged(Mark? was, Mark? now)
    {
        if (was is not null)
        {
            InvalidateMark(was, selected: true, backdrop: false);
        }
        if (now is not null)
        {
            InvalidateMark(now, selected: true, backdrop: false);
        }
    }

    public void OnStepsRenumbered(Marks marks, int from)
    {
        for (int i = from; i < marks.Count; i++)
        {
            if (marks[i] is StepMark step)
            {
                InvalidateMark(step, selected: false, backdrop: true);
            }
        }
    }

    public void OnCropMoved(Crop was, Crop now)
    {
        InvalidateCrop(was, now);
    }

    public void OnFrameChanged()
    {
        if (_document.Capture?.Clip is { } clip)
        {
            InvalidateBackdrop(clip.Area);
        }
    }

    public void OnSelectionChanged(Rect was, Rect now)
    {
        UpdateCaret(_selecting.Typed);
        if (!_layered)
        {
            InvalidateCrop(was, now);
            return;
        }
        DrawLiveSelection(now);
        Window.PushLayer(Layer, _live.Dirty);
    }

    public void InvalidateCrop(Rect was, Rect now)
    {
        Span<Rect> bands = stackalloc Rect[RectBands.ChangeBandCount];
        int n = RectBands.ChangeBands(was, now, bands);
        for (int i = 0; i < n; i++)
        {
            InvalidateBackdrop(bands[i]);
        }
        InvalidateFrame(was);
        InvalidateFrame(now);
        InvalidateSizeLabel(was);
        InvalidateSizeLabel(now);
    }

    public void InvalidateSizeLabel(Rect crop)
    {
        Invalidate(_painter.SizeLabelArea(crop));
    }

    private void InvalidateFrame(Rect crop)
    {
        if (crop.IsEmpty)
        {
            return;
        }
        int w = _painter.CropFrameWidth;
        Span<Rect> strips = stackalloc Rect[RectBands.OutsideBandCount];
        int n = RectBands.Outside(crop.Inflated(w), crop.Inflated(-w), strips);
        for (int i = 0; i < n; i++)
        {
            Invalidate(strips[i]);
        }
    }

    public void Flash(Capture capture, string message)
    {
        _animation.Flash(capture, message);
    }

    public void ShrinkInto(Capture capture, Point at)
    {
        if (!_painter.IsOpen)
        {
            return;
        }
        _painter.UpdateBackdrop();
        _animation.ShrinkInto(_painter.Backdrop, capture.Crop, at);
    }

    public bool CloseAnimation()
    {
        return _animation.Close();
    }

    private void UpdateCaret(TypedSize? typed)
    {
        if (typed == _caretFor)
        {
            return;
        }
        _caretFor = typed;
        if (typed is { IsFocusedFieldEmpty: true })
        {
            _caret.Start(Window);
        }
        else
        {
            _caret.Stop(Window);
        }
    }

    public void OnPaint()
    {
        bool collected = _update.Open() && Window.CollectUpdate(_update);
        using PaintScope paint = Window.Paint();
        if (paint.HasBegun)
        {
            Region? update;
            if (collected)
            {
                update = _update;
            }
            else
            {
                update = null;
            }
            _painter.Paint(paint.DeviceContext, paint.Area, update);
        }
    }

    public void OnMouseDown(Point at, bool shift)
    {
        ToolbarRaiseRequested?.Invoke();
        MouseDown?.Invoke(at, shift);
    }

    public void OnDoubleClick(Point at, bool shift)
    {
        ToolbarRaiseRequested?.Invoke();
        DoubleClick?.Invoke(at, shift);
    }

    public void OnRightClick()
    {
        RightClicked?.Invoke();
    }

    public void OnMouseMove(Point at, bool shift, bool ctrl)
    {
        MouseMove?.Invoke(at, shift, ctrl);
    }

    public void OnMouseUp(Point at)
    {
        MouseUp?.Invoke();
    }

    public bool OnKeyDown(int virtualKey)
    {
        Chord chord = Chord.Of(virtualKey);
        switch (chord.Key)
        {
            case Key.Ctrl:
                CtrlChanged?.Invoke(true);
                return true;
            case Key.Alt:
                AltChanged?.Invoke(true);
                return true;
            default:
                return KeyDown?.Invoke(chord) ?? false;
        }
    }

    public bool OnKeyUp(int virtualKey)
    {
        switch (Chord.KeyOf(virtualKey))
        {
            case Key.Ctrl:
                CtrlChanged?.Invoke(false);
                return true;
            case Key.Alt:
                AltChanged?.Invoke(false);
                return true;
            default:
                return false;
        }
    }

    public CursorShape OnCursorWanted()
    {
        switch (CursorWanted?.Invoke(Window.ToClient(MousePointer.At)) ?? Cursor.Arrow)
        {
            case Cursor.Draw:
                return _crosshair.ShapeAt(_desk.ScaleAt(MousePointer.At));
            case Cursor.Hand:
                return CursorShape.Hand;
            case Cursor.Cross:
                return CursorShape.Cross;
            case Cursor.SizeNWSE:
                return CursorShape.SizeNWSE;
            case Cursor.SizeNESW:
                return CursorShape.SizeNESW;
            case Cursor.SizeNS:
                return CursorShape.SizeNS;
            case Cursor.SizeWE:
                return CursorShape.SizeWE;
            case Cursor.SizeAll:
                return CursorShape.SizeAll;
            default:
                return CursorShape.Arrow;
        }
    }

    public void OnTimer(int id)
    {
        if (id == Timers.Tick)
        {
            Tick?.Invoke();
        }
        else if (id == Timers.Toast)
        {
            EndToast();
        }
        else if (id == Timers.Caret)
        {
            _caret.Toggle();
            if (!_layered)
            {
                InvalidateSizeLabel(_selecting.Selection);
            }
            else if (_liveShown == LiveContent.Selection)
            {
                DrawLiveSelection(_selecting.Selection);
                Window.PushLayer(Layer, _live.Dirty);
            }
        }
    }

    public void OnHotkey(int id)
    {
        HotkeyPressed?.Invoke(id);
    }

    public bool OnAppMessage(uint id, AppSignal signal)
    {
        if (id == AppMessages.Tray)
        {
            TrayMessageReceived?.Invoke(signal);
        }
        else if (id == AppMessages.ShowToolbar)
        {
            ShowToolbarFromTrayRequested?.Invoke();
        }
        else if (id == AppMessages.ElementsScanned)
        {
            ElementsScanned?.Invoke();
        }
        else if (id == AppMessages.ClipCut)
        {
            ClipCut?.Invoke();
        }
        else if (id == AppMessages.TaskbarCreated)
        {
            TaskbarCreatedMessageReceived?.Invoke();
        }
        else
        {
            return false;
        }
        return true;
    }

    public bool OnEditBackdrop(DeviceContext dc, EditRef edit)
    {
        if (!_textBox.Owns(edit) || !_painter.IsOpen)
        {
            return false;
        }
        _painter.UpdateBackdrop();
        _textBox.PaintBackdrop(dc, _painter.Backdrop, Drawing.TextColor);
        return true;
    }

    public void OnDisplayChanged()
    {
        DisplayChanged?.Invoke();
    }

    public void OnActivated()
    {
        ToolbarRaiseRequested?.Invoke();
    }

    public void OnDeactivated()
    {
        ToolbarRaiseRequested?.Invoke();
    }

    public void OnClosed()
    {
        Hotkeys.Unregister(Window, _hotkeys);
        WindowLoop.Quit();
    }
}
