namespace CapChief;

/// <summary>
/// Represents the running application, which creates its parts and connects them to each other.
/// </summary>
public sealed class App
{
    private readonly Metrics _metrics;
    private readonly GdiPlus _gdiplus = new();
    private readonly ElementScan _scan = new(Crop.MinSize);
    private readonly Selecting _selecting;
    private bool _displayChanged;
    private bool _pressClosedSomething;

    public App(Desktop desk, ScreenSource screen, bool excludedFromCapture)
    {
        Desktop = desk;
        _metrics = new Metrics(desk.ScaleAt(desk.ScreenPrimary.Center), desk);
        var icon = new AppIcon();

        MarkPainter = new MarkPainter(_metrics);
        Review = new Review(Document);
        Recording = new Recording(screen);
        _selecting = new Selecting(new WindowFinder(desk, _scan, Crop.MinSize), desk, _metrics);
        Overlay = new Overlay(desk, _metrics, icon, excludedFromCapture, _selecting, Document, Review, MarkPainter, Tools);
        Shell = new AppShell(icon, Overlay, new ToolbarForm(icon, new ToolbarPlacement(desk), desk), new HistoryForm(icon, desk, MarkPainter), new ToolPickerForm(icon, desk, Tools, Document), new AboutForm(icon, desk), Tools, Document, Review, History);
        Workflow = new CaptureWorkflow(Document, desk, screen, _metrics, Overlay, _selecting, Drawing, CountingDown, Recording, Review, Shell);
        Workspace = new WorkspaceService(Document, Tools, History, Workflow, Shell);
        CaptureService = new CaptureService(Document, Workflow, Workspace, Review, MarkPainter, Shell);
        Commands = new Commands(Workflow, Workspace, CaptureService, Review, _selecting, Drawing, Tools, Shell);

        _selecting.SelectionChanged += Overlay.OnSelectionChanged;
        Document.MarkAdded += Overlay.OnMarkAdded;
        Document.MarkRemoved += Overlay.OnMarkRemoved;
        Document.MarkReplaced += Overlay.OnMarkReplaced;
        Document.SelectedMarkChanged += Overlay.OnSelectedMarkChanged;
        Document.SelectedMarkChanged += (_, now) => Tools.MatchColorOf(now);
        Document.SelectedMarkChanged += (_, now) => Tools.MatchTextSizeOf(now);
        Document.CropMoved += Overlay.OnCropMoved;
        Document.CropMoved += Shell.OnCropMoved;
        Document.CropMoved += (_, now) => Review.CropTo(now);
        Document.StepsRenumbered += Overlay.OnStepsRenumbered;
        Document.ClipEdited += Review.OnClipEdited;
        Document.Changed += Shell.InvalidateToolbar;

        Shell.HistoryForm.Picked += entry => Workspace.OpenFromHistory(entry);
        Shell.ToolPicker.ToolPicked += Tools.PickTool;
        Shell.ToolPicker.ColorPicked += Workspace.PickColor;

        Workflow.ModeChanged += Shell.OnModeChanged;
        Workflow.ModeChanged += mode =>
        {
            if (mode == Mode.Idle && _displayChanged)
            {
                OnDisplayChanged();
            }
        };

        Drawing.ClickedOutsideCrop += () =>
        {
            if (!_pressClosedSomething)
            {
                Workspace.EndCapture();
            }
        };
        Overlay.MouseDown += (at, shift) =>
        {
            _pressClosedSomething = Shell.ToolPicker.IsShown || Drawing.IsEditingText;
            Shell.ToolPicker.Hide();
            Workflow.OnMouseDown(at, shift);
        };
        Overlay.DoubleClick += Workflow.OnDoubleClick;
        Overlay.RightClicked += Commands.RightClick;
        Overlay.WheelTurned += Commands.TurnWheel;
        Overlay.MouseMove += Workflow.OnMouseMove;
        Overlay.MouseUp += Workflow.OnMouseUp;
        Overlay.CtrlChanged += Workflow.OnCtrlChanged;
        Overlay.AltChanged += Workflow.OnAltChanged;
        Overlay.KeyDown += Shell.OnKeyDown;
        Overlay.TypingKeyDown += Commands.RunTypingKey;
        Overlay.CursorWanted += Workflow.CursorAt;
        Overlay.Tick += Workflow.OnTick;
        Overlay.TrayMessageReceived += Shell.OnTrayMessage;
        Overlay.ShowToolbarFromTrayRequested += Shell.ShowToolbarFromTray;
        Overlay.ElementsScanned += Workflow.OnElementsScanned;
        Overlay.ClipCut += Review.TakeCut;
        Overlay.TaskbarCreatedMessageReceived += Shell.OnTaskbarCreated;
        Overlay.ToolbarRaiseRequested += Shell.RaiseToolbar;
        Overlay.HotkeyPressed += Commands.RunHotkey;
        Overlay.DisplayChanged += OnDisplayChanged;

        Review.Changed += Shell.InvalidateToolbar;
        History.Changed += Shell.InvalidateToolbar;
        Review.TickMsChanged += ms => Overlay.TickMs = ms;
        Review.FrameChanged += Overlay.OnFrameChanged;
    }

    public Desktop Desktop { get; }

    public Document Document { get; } = new();

    public Tools Tools { get; } = new();

    public CaptureHistory History { get; } = new();

    public MarkPainter MarkPainter { get; }

    public Review Review { get; }

    public CountingDown CountingDown { get; } = new();

    public Recording Recording { get; }

    public Overlay Overlay { get; }

    public Drawing Drawing
    {
        get
        {
            return Overlay.Drawing;
        }
    }

    public AppShell Shell { get; }

    public CaptureWorkflow Workflow { get; }

    public WorkspaceService Workspace { get; }

    public CaptureService CaptureService { get; }

    public Commands Commands { get; }

    public Mode Mode
    {
        get
        {
            return Workflow.CurrentMode;
        }
    }

    public Capture? Capture
    {
        get
        {
            return Document.Capture;
        }
    }

    public void Start()
    {
        if (!_gdiplus.Start())
        {
            throw new InvalidOperationException("GDI+ could not be started");
        }
        Shell.Start(Commands);
        _scan.WhenScanned(Overlay.Window.Poster(AppMessages.ElementsScanned));
        Review.WhenCut(Overlay.Window.Poster(AppMessages.ClipCut));
    }

    public void Shutdown()
    {
        Shell.Shutdown();
        _scan.Close();
        Recording.Abort();
        Workspace.Free();
        Workflow.CancelSelecting();
        _metrics.Free();
        _gdiplus.Stop();
    }

    private void OnDisplayChanged()
    {
        _displayChanged = Mode != Mode.Idle;
        if (!_displayChanged && Desktop.Remeasure())
        {
            Shell.ShowToolbar();
        }
    }
}
