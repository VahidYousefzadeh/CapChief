namespace CapChief.Capturing;

/// <summary>
/// Moves the app between its modes and passes mouse input to the current one.
/// </summary>
public sealed class CaptureWorkflow
{
    private readonly Document _document;
    private readonly Desktop _desk;
    private readonly ScreenSource _screen;
    private readonly Metrics _metrics;
    private readonly Overlay _overlay;
    private readonly Selecting _selecting;
    private readonly Drawing _drawing;
    private readonly CountingDown _countingDown;
    private readonly Recording _recording;
    private readonly Review _review;
    private readonly IShell _shell;
    private bool _held;

    public event Action<Mode>? ModeChanged;

    public CaptureWorkflow(Document document, Desktop desk, ScreenSource screen, Metrics metrics, Overlay overlay, Selecting selecting, Drawing drawing, CountingDown countingDown, Recording recording, Review review, IShell shell)
    {
        _document = document;
        _desk = desk;
        _screen = screen;
        _metrics = metrics;
        _overlay = overlay;
        _selecting = selecting;
        _drawing = drawing;
        _countingDown = countingDown;
        _recording = recording;
        _review = review;
        _shell = shell;
    }

    public Mode CurrentMode { get; private set; } = Mode.Idle;

    public Surface? FrozenScreen { get; private set; }

    public void StartSelecting(CaptureKind kind)
    {
        EndSelecting();
        if (kind != CaptureKind.Frozen)
        {
            _shell.HideForScreenshot();
            CurrentMode = Mode.Selecting;
            _selecting.Begin(kind, _overlay.CursorPosition);
            _overlay.ShowLive(_selecting.Selection);
            ModeChanged?.Invoke(CurrentMode);
            return;
        }

        FrozenScreen = TakeScreenshot();
        if (FrozenScreen is not { } screen)
        {
            ModeChanged?.Invoke(CurrentMode);
            _shell.OnFailed(Failure.TakeScreenshot);
            return;
        }

        CurrentMode = Mode.Selecting;
        _selecting.Begin(kind, _overlay.CursorPosition);
        if (!_overlay.ShowSelecting(screen))
        {
            EndSelecting();
        }
        ModeChanged?.Invoke(CurrentMode);
    }

    public void AcceptSelection()
    {
        EndPress();
        if (_selecting.Accept() is { } crop)
        {
            FinishSelecting(crop);
        }
    }

    private void FinishSelecting(Crop crop)
    {
        _selecting.End();
        switch (_selecting.Kind)
        {
            case CaptureKind.Timed:
                StartCountdown(crop);
                return;
            case CaptureKind.Recording:
                StartRecording(crop);
                return;
        }
        if (FrozenScreen is not { } screen)
        {
            return;
        }
        var capture = new Capture(screen, crop);
        FrozenScreen = null;
        _document.Open(capture);
        CurrentMode = Mode.Annotating;
        _overlay.ShowAnnotating();
        ModeChanged?.Invoke(CurrentMode);
    }

    public bool Open(Capture capture)
    {
        Close();
        bool opened = TryOpen(capture);
        ModeChanged?.Invoke(CurrentMode);
        return opened;
    }

    private bool TryOpen(Capture capture)
    {
        if (capture.Clip is { } clip && !_review.Open(clip, capture.Crop))
        {
            _shell.OnFailed(Failure.ReadRecording);
            return false;
        }

        _metrics.ScaleToMonitorAt(capture.Crop.Center);
        if (OpenAndShow(capture))
        {
            return true;
        }
        CurrentMode = Mode.Idle;
        return false;
    }

    private bool OpenAndShow(Capture capture)
    {
        _document.Open(capture);
        bool shown;
        if (capture.Clip is null)
        {
            CurrentMode = Mode.Annotating;
            shown = _overlay.ShowAnnotating(capture.Picture);
        }
        else
        {
            CurrentMode = Mode.Reviewing;
            shown = _overlay.ShowReview(capture.Picture);
        }
        if (shown)
        {
            return true;
        }

        _review.Close();
        _document.Close();
        return false;
    }

    public void Close()
    {
        if (CurrentMode is not (Mode.Annotating or Mode.Reviewing))
        {
            return;
        }
        EndPress();
        if (_drawing.IsEditingText)
        {
            _drawing.EndTextEdit(cancel: true);
        }
        _drawing.Clear();
        _review.Close();
        _overlay.Hide();
        _document.Close();
        CurrentMode = Mode.Idle;
        ModeChanged?.Invoke(CurrentMode);
    }

    private void StartCountdown(Crop crop)
    {
        CurrentMode = Mode.CountingDown;
        _countingDown.Begin(crop);
        _overlay.ShowCountdown(crop, _countingDown.SecondsLeft, _countingDown.ShowsBorder);
        _overlay.TickMs = CountingDown.TickMs;
        ModeChanged?.Invoke(CurrentMode);
    }

    private void TakeTimedCapture()
    {
        _overlay.TickMs = 0;
        if (TakeScreenshot() is not { } screen)
        {
            CancelLive(Failure.TakeScreenshot);
            return;
        }

        var capture = new Capture(screen, _countingDown.Crop);
        if (!OpenAndShow(capture))
        {
            capture.Free();
            CancelLive();
            return;
        }
        ModeChanged?.Invoke(CurrentMode);
    }

    private void StartRecording(Crop crop)
    {
        CurrentMode = Mode.Recording;
        _overlay.ShowRecording();
        if (!_recording.Start(crop))
        {
            CancelLive(Failure.StartRecording);
            return;
        }
        _overlay.ShowRecording(crop, _recording.Seconds);
        _overlay.TickMs = Recording.FrameMs;
        ModeChanged?.Invoke(CurrentMode);
    }

    public void StopRecording()
    {
        if (CurrentMode != Mode.Recording)
        {
            return;
        }
        _overlay.TickMs = 0;
        Crop crop = _recording.Crop;
        if (_recording.Stop() is not { } clip)
        {
            CancelLive(Failure.WriteRecording);
            return;
        }
        Surface? shot = TakeScreenshot();
        Surface? screen = shot ?? BlankScreen();
        if (screen is null)
        {
            clip.DeleteFile();
            CancelLive(Failure.ShowRecording);
            return;
        }

        var capture = new Capture(screen, crop) { Clip = clip };
        if (!_review.Open(clip, crop))
        {
            capture.Free();
            CancelLive(Failure.ReadRecording);
            return;
        }
        if (!OpenAndShow(capture))
        {
            capture.Free();
            CancelLive(Failure.ShowRecording);
            return;
        }
        ModeChanged?.Invoke(CurrentMode);
        if (shot is null)
        {
            _shell.OnFailed(Failure.TakeScreenshot);
        }
    }

    public void CancelLive()
    {
        _overlay.TickMs = 0;
        _recording.Abort();
        _selecting.End();
        _overlay.Hide();
        CurrentMode = Mode.Idle;
        ModeChanged?.Invoke(CurrentMode);
    }

    private void CancelLive(Failure failure)
    {
        CancelLive();
        _shell.OnFailed(failure);
    }

    private Surface? TakeScreenshot()
    {
        _shell.HideForScreenshot();
        Size size = _desk.Size;
        var shot = new Surface();
        if (shot.OpenCompatible(size.Width, size.Height) && _screen.Capture(shot))
        {
            return shot;
        }
        shot.Free();
        return null;
    }

    /// <summary>
    /// Makes a black backdrop, so a recording whose screenshot failed can still be reviewed.
    /// </summary>
    private Surface? BlankScreen()
    {
        Size size = _desk.Size;
        var blank = new Surface();
        if (blank.OpenDib(size.Width, size.Height))
        {
            return blank;
        }
        else
        {
            return null;
        }
    }

    public void CancelSelecting()
    {
        bool selecting = CurrentMode == Mode.Selecting;
        EndSelecting();
        if (selecting)
        {
            ModeChanged?.Invoke(CurrentMode);
        }
    }

    private void EndSelecting()
    {
        if (CurrentMode == Mode.Selecting)
        {
            EndPress();
            _overlay.Hide();
            CurrentMode = Mode.Idle;
        }
        _selecting.End();
        FrozenScreen?.Free();
        FrozenScreen = null;
    }

    public void OnMouseDown(Point p, bool shift)
    {
        if (CurrentMode == Mode.Recording && _overlay.StopButton.Contains(p))
        {
            StopRecording();
            return;
        }
        if (_drawing.IsEditingText)
        {
            _drawing.EndTextEdit(cancel: false);
        }
        if (CurrentMode is not (Mode.Selecting or Mode.Annotating or Mode.Reviewing))
        {
            return;
        }

        _held = true;
        _overlay.CaptureMouse();
        switch (CurrentMode)
        {
            case Mode.Selecting:
                _selecting.Press(p);
                break;
            case Mode.Annotating:
                _drawing.Press(p, shift);
                break;
            default:
                _drawing.PressCrop(p);
                break;
        }
    }

    public void OnMouseMove(Point p, bool shift, bool ctrl)
    {
        switch (CurrentMode)
        {
            case Mode.Selecting when _held:
                _selecting.Drag(p);
                break;
            case Mode.Selecting:
                _selecting.Hover(p, ctrl);
                break;
            case Mode.Annotating or Mode.Reviewing when _held:
                _drawing.Drag(p, shift, Keyboard.AltDown);
                break;
        }
    }

    public void OnMouseUp()
    {
        if (!_held)
        {
            return;
        }
        EndPress();
        if (CurrentMode == Mode.Selecting)
        {
            if (_selecting.Release() is { } crop)
            {
                FinishSelecting(crop);
            }
            return;
        }
        if (CurrentMode is Mode.Annotating or Mode.Reviewing)
        {
            _drawing.Release();
        }
    }

    public void OnDoubleClick(Point p, bool shift)
    {
        if (CurrentMode == Mode.Annotating && _drawing.ReopenTextAt(p))
        {
            return;
        }
        OnMouseDown(p, shift);
    }

    private void EndPress()
    {
        _held = false;
        _overlay.ReleaseMouse();
    }

    public void FinishPress()
    {
        if (!_held || CurrentMode is not (Mode.Annotating or Mode.Reviewing))
        {
            return;
        }
        EndPress();
        _drawing.FinishPress();
    }

    public void OnCtrlChanged(bool down)
    {
        if (CurrentMode == Mode.Selecting && !_held)
        {
            _selecting.Hover(_overlay.CursorPosition, down);
        }
    }

    public void OnAltChanged(bool down)
    {
        if (CurrentMode is Mode.Annotating or Mode.Reviewing && _held && _drawing.IsDraggingCrop)
        {
            _drawing.Drag(_overlay.CursorPosition, Keyboard.ShiftDown, down);
        }
    }

    public void OnTick()
    {
        switch (CurrentMode)
        {
            case Mode.CountingDown when _countingDown.Tick():
                _overlay.ShowCountdown(_countingDown.Crop, _countingDown.SecondsLeft, _countingDown.ShowsBorder);
                break;
            case Mode.CountingDown:
                TakeTimedCapture();
                break;
            case Mode.Recording when _recording.Frame():
                _overlay.ShowRecording(_recording.Crop, _recording.Seconds);
                break;
            case Mode.Recording:
                StopRecording();
                break;
            case Mode.Reviewing:
                _review.Tick();
                break;
        }
    }

    public void OnElementsScanned()
    {
        if (CurrentMode == Mode.Selecting)
        {
            _selecting.ElementsScanned(_overlay.CursorPosition);
        }
    }

    public Cursor CursorAt(Point p)
    {
        switch (CurrentMode)
        {
            case Mode.Selecting:
                return Cursor.Draw;
            case Mode.Annotating when _document.Capture is not null:
                return _drawing.CursorAt(p);
            case Mode.Reviewing when _document.Capture is not null:
                return _drawing.CropCursorAt(p);
            case Mode.Recording:
                if (_overlay.StopButton.Contains(p))
                {
                    return Cursor.Hand;
                }
                else
                {
                    return Cursor.Arrow;
                }
            default:
                return Cursor.Arrow;
        }
    }
}
