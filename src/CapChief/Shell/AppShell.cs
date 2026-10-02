namespace CapChief.Shell;

/// <summary>
/// Manages the tray icon, the toolbar, the history drop-down, the tool picker and the dialogs, and
/// passes key presses to commands.
/// </summary>
public sealed class AppShell : IShell
{
    private readonly AppIcon _icon;
    private readonly Overlay _overlay;
    private readonly Tools _tools;
    private readonly Document _document;
    private readonly Review _review;
    private readonly CaptureHistory _history;

    private Commands? _commands;
    private Tray? _tray;
    private AnnotateStrip? _annotate;
    private Mode _mode = Mode.Idle;

    public AppShell(AppIcon icon, Overlay overlay, ToolbarForm toolbar, HistoryForm historyForm, ToolPickerForm toolPicker, Tools tools, Document document, Review review, CaptureHistory history)
    {
        _icon = icon;
        _overlay = overlay;
        _tools = tools;
        _document = document;
        _review = review;
        _history = history;
        Toolbar = toolbar;
        HistoryForm = historyForm;
        ToolPicker = toolPicker;

        tools.Changed += Toolbar.Invalidate;
        ToolPicker.Changed += Toolbar.Invalidate;
        ToolPicker.Changed += () =>
        {
            Rect clearArea;
            if (ToolPicker.IsShown)
            {
                clearArea = ToolPicker.OnScreenBounds;
            }
            else
            {
                clearArea = default;
            }
            Toolbar.KeepTipsClearOf(clearArea);
        };
        Toolbar.ToastEnded += ShowToolbar;
        Toolbar.Pressed += OnToolbarPressed;
    }

    public ToolbarForm Toolbar { get; }

    public HistoryForm HistoryForm { get; }

    public ToolPickerForm ToolPicker { get; }

    public bool IsDialogOpen { get; private set; }

    public void Start(Commands commands)
    {
        _commands = commands;
        _tray = new Tray(_icon, commands);

        _annotate = new AnnotateStrip(commands, _tools, _document, ToolPicker);
        _overlay.Create(commands);
        Toolbar.Create(_overlay.Window, (Mode.Idle, new CaptureStrip(commands)), (Mode.Annotating, _annotate), (Mode.Reviewing, new ReviewStrip(commands, _review, new Timeline(_review))));
        HistoryForm.Create(_overlay.Window);
        ToolPicker.Create(_overlay.Window);
    }

    public void ShowTray()
    {
        _tray?.Add(_overlay.Window);
    }

    public void Shutdown()
    {
        _tray?.Remove();
        HistoryForm.Free();
        ToolPicker.Free();
        Toolbar.Free();
        _overlay.Free();
    }

    public void ShowToolbar()
    {
        ToolPicker.Hide();
        Toolbar.Show(_mode, _document.Capture?.Crop ?? default);
    }

    public void OnModeChanged(Mode mode)
    {
        HistoryForm.Hide();
        _mode = mode;
        ShowToolbar();
    }

    public void ToggleHistory()
    {
        if (HistoryForm.IsShown)
        {
            HistoryForm.Hide();
        }
        else
        {
            HistoryForm.Show(_history.Entries, Toolbar.PanelOnScreen);
        }
    }

    public void ToggleToolPicker(ToolGroup group, Command button)
    {
        if (ToolPicker.HangsFrom(button))
        {
            ToolPicker.Hide();
            return;
        }
        _tools.PickTool(_tools.ShownIn(group));
        ToolPicker.ShowTools(group, button, Toolbar.ColumnOnScreen(button));
    }

    public void ToggleColorPicker(Command button)
    {
        if (ToolPicker.HangsFrom(button))
        {
            ToolPicker.Hide();
        }
        else
        {
            ToolPicker.ShowColors(button, Toolbar.ColumnOnScreen(button));
        }
    }

    public void OnCropMoved(Crop was, Crop now)
    {
        ShowToolbar();
    }

    public void InvalidateToolbar()
    {
        Toolbar.Invalidate();
    }

    public void RaiseToolbar()
    {
        Toolbar.Raise();
    }

    public void HideForScreenshot()
    {
        bool hid = false;
        if (_overlay.IsVisible)
        {
            _overlay.HideWindow();
            hid = true;
        }
        if (_overlay.CloseAnimation())
        {
            hid = true;
        }
        if (Toolbar.IsShown)
        {
            Toolbar.Hide();
            hid = true;
        }
        if (HistoryForm.IsShown)
        {
            HistoryForm.Hide();
            hid = true;
        }
        if (ToolPicker.IsShown)
        {
            ToolPicker.Hide();
            hid = true;
        }
        if (hid)
        {
            WindowLoop.AwaitFrame();
        }
    }

    public void HideToTray()
    {
        HistoryForm.Hide();
        Toolbar.IsHiddenToTray = true;
    }

    public void ToggleToolbar()
    {
        HistoryForm.Hide();
        Toolbar.IsHiddenToTray = !Toolbar.IsHiddenToTray;
    }

    public void ShowToolbarFromTray()
    {
        if (_mode == Mode.Idle)
        {
            Toolbar.ShowFromTray();
        }
    }

    public void Quit()
    {
        Toolbar.Close();
        _overlay.Close();
    }

    public void OnSaved(Capture capture, string path)
    {
        _overlay.Flash(capture, $"Saved {Path.GetFileName(path)}");
    }

    public void OnClipSaved(string path)
    {
        Toolbar.Toast($"Saved {Path.GetFileName(path)}");
    }

    public void OnCopied(Capture capture)
    {
        _overlay.Flash(capture, "Copied");
    }

    public void OnSavedToHistory(Capture capture)
    {
        _overlay.Toast("Saved to history");
        if (Toolbar.IsShown && _commands is { } commands)
        {
            _overlay.ShrinkInto(capture, Toolbar.ColumnOnScreen(commands.SaveToHistory).Center);
        }
    }

    public void OnFailed(Failure failure)
    {
        MessageBox.Warn(_overlay.Window, WarningOf(failure), Product.Name);
    }

    private static string WarningOf(Failure failure)
    {
        switch (failure)
        {
            case Failure.SaveImage:
                return "Could not save the image.";
            case Failure.CopyToClipboard:
                return "Could not access the clipboard.";
            case Failure.SaveRecording:
                return "Could not save the recording.";
            case Failure.StartRecording:
                return "Could not start recording.";
            case Failure.WriteRecording:
                return "Could not write the recording.";
            case Failure.ReadRecording:
                return "Could not read the recording back.";
            case Failure.TakeScreenshot:
                return "Could not take a screenshot.";
            case Failure.ShowRecording:
                return "Could not show the recording.";
            default:
                throw new ArgumentOutOfRangeException(nameof(failure), failure, null);
        }
    }

    public string? AskSavePath(SaveKind kind)
    {
        Toolbar.HideForDialog();
        IsDialogOpen = true;
        string? path = SaveDialog.Ask(_overlay.Window, kind);
        IsDialogOpen = false;
        Toolbar.ShowAfterDialog();
        return path;
    }

    public bool CopyToClipboard(Surface image)
    {
        return Export.ToClipboard(image, _overlay.Window);
    }

    public bool OnKeyDown(Chord chord)
    {
        if (ToolPicker.OnKeyDown(chord))
        {
            return true;
        }
        if (ToolPicker.IsShown && chord is { Ctrl: false, Key: Key.Left or Key.Right })
        {
            int step;
            if (chord.Key == Key.Right)
            {
                step = 1;
            }
            else
            {
                step = -1;
            }
            _annotate?.MoveDropDown(step);
            return true;
        }
        ToolPicker.Hide();
        if (_commands?.RunKey(chord) != true)
        {
            return false;
        }
        Toolbar.Invalidate();
        return true;
    }

    private void OnToolbarPressed(Control? pressed)
    {
        if (pressed is not ToolButton { OpensDropDown: true })
        {
            ToolPicker.Hide();
        }
    }

    public void OnTrayMessage(AppSignal signal)
    {
        _tray?.OnMessage(signal, _mode, Toolbar.IsHiddenToTray, _history.Entries.Count > 0);
    }

    public void OnTaskbarCreated()
    {
        _tray?.Restore();
    }
}
