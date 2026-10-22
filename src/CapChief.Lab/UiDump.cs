namespace CapChief.Lab;

/// <summary>
/// Walks the app through the states a user can reach, writes a picture of each to a folder, and
/// checks along the way that each state is the one expected and repaints the same as a full
/// repaint.
/// </summary>
public sealed class UiDump
{
    private readonly Driver _d;
    private readonly string _dir;
    private readonly Report _report = new("dumpui");

    public UiDump(Driver driver, string dir)
    {
        _d = driver;
        _dir = dir;
    }

    private App App
    {
        get
        {
            return _d.App;
        }
    }

    public int Run()
    {
        Directory.CreateDirectory(_dir);
        Size size = _d.Desk.Size;
        _report.Note($"desktop {size.Width}x{size.Height} at {_d.Desk.ScaleAt(_d.Desk.ScreenPrimary.Center) * 100:0}%");
        Toolbar();
        Selecting();
        Marks();
        Endings();
        Pickers();
        History();
        Film();
        RecordingEdits();
        HideToTray();
        return _report.Write(Path.Combine(_dir, "dumpui.txt"));
    }

    private string At(string name)
    {
        return Path.Combine(_dir, name);
    }

    private void SameAsFullRepaint(string what)
    {
        Rect screen = _d.Desk.Bounds;
        Repaint repaint = _d.RepaintDifference(screen);
        string ending;
        if (repaint.Differ == 0)
        {
            ending = ")";
        }
        else
        {
            ending = $" in {repaint.Where.Width}x{repaint.Where.Height} at {repaint.Where.Left},{repaint.Where.Top})";
        }
        _report.Check(repaint.Differ == 0, $"{what}: the overlay repaints the same in full ({repaint.Differ} px differ{ending}");
        if (repaint.Differ == 0)
        {
            return;
        }
        var name = new string(what.Where(char.IsLetterOrDigit).ToArray());
        Pictures.Pixels(repaint.Before, screen.Width, screen.Height, At($"differ-{name}-incremental.png"));
        Pictures.Pixels(repaint.After, screen.Width, screen.Height, At($"differ-{name}-full.png"));
    }

    private void Toolbar()
    {
        App.Shell.ShowToolbar();
        Pictures.Toolbar(_d, At("toolbar-idle.png"));
        Pictures.Image(App.Shell.Toolbar.ShowTipFor(App.Commands.TimedCapture), At("toolbar-tooltip.png"));
        App.Shell.Toolbar.HideTip();
        App.Shell.Toolbar.Toast("Copied!");
        Pictures.Toolbar(_d, At("toolbar-toast.png"));
        App.Shell.ShowToolbar();
    }

    private void Selecting()
    {
        var area = new Rect { Left = 160, Top = 160, Right = 900, Bottom = 600 };
        App.CaptureService.StartCapture(CaptureKind.Frozen);
        _report.Check(App.Mode == Mode.Selecting, "a capture starts by selecting over the frozen screen");
        _d.Press(200, 200);
        for (int i = 1; i <= 10; i++)
        {
            _d.Move(200 + i * 60, 200 + i * 30);
        }
        Pictures.Screen(_d, area, At("select-dragging.png"));
        SameAsFullRepaint("dragging out an area");

        _d.Key(Keys.Tab);
        foreach (char digit in "640")
        {
            _d.Key(digit);
        }
        Pictures.Screen(_d, area, At("select-typed-size.png"));
        _d.Release(800, 500);
        _d.Key(Keys.Enter);
        _report.Check(App.Mode == Mode.Annotating && App.Capture?.Crop.Width == 640, $"a typed width of 640 opens a capture that wide ({App.Capture?.Crop.Width})");
        _d.Key(Keys.Escape);
        _report.Check(App.Mode == Mode.Idle, "Escape closes it");
    }

    private void Marks()
    {
        App.CaptureService.StartCapture(CaptureKind.Frozen);
        _d.Drag(80, 80, 1480, 880, 1);
        _d.Draw(MarkKind.Rectangle, 140, 140, 400, 320);
        _d.Draw(MarkKind.Ellipse, 440, 140, 700, 320);
        _d.Draw(MarkKind.Arrow, 740, 140, 1000, 320);
        _d.Draw(MarkKind.Mosaic, 440, 380, 700, 520);
        _d.Draw(MarkKind.Blackout, 740, 380, 1000, 520);
        _d.Pick(MarkKind.Pen);
        _d.Press(140, 440);
        for (int i = 1; i <= 20; i++)
        {
            int dy;
            if (i % 2 == 0)
            {
                dy = 40;
            }
            else
            {
                dy = -30;
            }
            _d.Move(140 + i * 12, 440 + dy);
        }
        _d.Release(380, 480);
        _d.Pick(MarkKind.Step);
        _d.Click(1100, 180);
        _d.Click(1200, 180);
        _d.Pick(MarkKind.Text);
        _d.Drag(140, 600, 520, 680);
        _d.Type("Every tool");
        _d.Draw(MarkKind.Highlighter, 120, 618, 360, 618);
        _d.Pick(MarkKind.Arrow);
        _d.Click(1300, 700);
        _report.Check(App.Capture?.Marks.Count == 10, $"ten marks, one of each kind and two steps ({App.Capture?.Marks.Count})");
        Pictures.Screen(_d, new Rect { Left = 40, Top = 40, Right = 1520, Bottom = 920 }, At("marks-every-kind.png"));
        Pictures.Toolbar(_d, At("toolbar-annotate.png"));
        var open = new Rect { Left = 100, Top = 100, Right = 1100, Bottom = 700 };
        byte[] own = _d.OverlayPixels(open), shown = _d.Screen(open);
        int unlike = 0;
        for (int i = 0; i + 2 < own.Length && i + 2 < shown.Length; i += 4)
        {
            if (own[i] != shown[i] || own[i + 1] != shown[i + 1] || own[i + 2] != shown[i + 2])
            {
                unlike++;
            }
        }
        _report.Check(own.Length == shown.Length && own.Length > 0 && unlike == 0, $"the overlay's own pixels are what the screen shows where nothing covers it ({unlike} differ)");
        SameAsFullRepaint("every kind of mark");

        Point words = App.Capture!.Marks.All.Single(m => m is TextMark).Bounds.Center;
        _d.DoubleClick(words.X, words.Y);
        _report.Check(App.Drawing.IsEditingText, "a double click on the text opens it for typing");
        Pictures.Screen(_d, new Rect { Left = 100, Top = 560, Right = 600, Bottom = 720 }, At("marks-text-reopened.png"));
        _d.KeyInTextBox(Keys.End);
        _d.Type(" too");
        _d.KeyInTextBox(Keys.Escape);
        _report.Check(!App.Drawing.IsEditingText && App.Capture.Marks.All.Count(m => m is TextMark { Text: "Every tool too" }) == 1, "and Escape finishes the typing, keeping what was typed");
        App.Workspace.Undo();
        _report.Check(App.Capture.Marks.All.Count(m => m is TextMark { Text: "Every tool" }) == 1, "which undo takes back");
        SameAsFullRepaint("the text put back");

        const int ellipse = 1;
        Point on = _d.PointOn(ellipse);
        _d.Click(on.X, on.Y);
        _report.Check(App.Document.SelectedIndex == ellipse, "a click on the ellipse selects it");
        Pictures.Screen(_d, new Rect { Left = 400, Top = 100, Right = 740, Bottom = 360 }, At("marks-selected.png"));
        SameAsFullRepaint("a selected mark");

        Rect before = App.Capture!.Marks[ellipse].Bounds;
        on = _d.PointOn(ellipse);
        _d.Press(on.X, on.Y);
        _d.Move(on.X + 30, on.Y + 20);
        _report.Check(App.Drawing.IsDragImageShown, "moving the ellipse lifts it onto the drag image");
        Pictures.DragImage(_d, At("marks-drag-image.png"));
        _d.Release(on.X + 30, on.Y + 20);
        Rect moved = App.Capture!.Marks[ellipse].Bounds;
        _report.Check(moved == before.Moved(new Point(30, 20)), "the ellipse moved with the pointer");
        SameAsFullRepaint("the ellipse dropped");

        App.Workspace.Undo();
        _report.Check(App.Capture?.Marks[ellipse].Bounds == before, "undo puts the ellipse back");
        App.Workspace.Redo();
        _report.Check(App.Capture?.Marks[ellipse].Bounds == moved, "redo moves it again");
        SameAsFullRepaint("after undo and redo");

        on = _d.PointOn(ellipse);
        _d.Click(on.X, on.Y);
        Rect area = App.Capture!.Crop;
        var edge = new Point(area.Left + area.Width / 4, area.Top);
        _d.Press(edge.X, edge.Y);
        _d.Move(edge.X + 30, edge.Y + 20);
        _report.Check(App.Drawing.HasMarksOnImage &&App.Document.SelectedIndex == ellipse, "moving the area lifts its pen, arrow and selected ellipse onto the drag image");
        Pictures.DragImage(_d, At("marks-area-drag-image.png"));
        SameAsFullRepaint("the area being moved");
        _d.Release(edge.X + 30, edge.Y + 20);
        _report.Check(App.Capture.Crop.Bounds == area.Moved(new Point(30, 20)) && App.Capture.Marks[ellipse].Bounds == moved.Moved(new Point(30, 20)), "the area moved with the pointer, its marks with it");
        SameAsFullRepaint("the area dropped");
        App.Workspace.Undo();
        _report.Check(App.Capture.Crop.Bounds == area && App.Capture.Marks[ellipse].Bounds == moved, "undo puts the area and its marks back");

        var clipboard = new ClipboardCheck(App, _report);
        Capture copied = App.Capture!;
        byte[]? drawn = clipboard.Render(copied);
        clipboard.CopiesLeakNothing(copied);
        App.CaptureService.CopyToClipboard();
        _report.Check(App.Mode == Mode.Idle, "Copy finishes the capture");
        clipboard.CopiedAsDrawn(drawn, copied.Crop);
    }

    private void Endings()
    {
        App.CaptureService.StartCapture(CaptureKind.Frozen);
        _d.Drag(200, 200, 500, 400, 1);
        Capture deleted = App.Capture!;
        App.Workspace.Delete();
        _report.Check(App.Mode == Mode.Idle, "Delete throws the capture away");
        App.Workspace.Undo();
        _report.Check(App.Mode == Mode.Idle && !deleted.Picture.IsOpen, "for good: undo does not bring it back, and its picture is freed");

        App.CaptureService.StartCapture(CaptureKind.Frozen);
        _d.Drag(100, 100, 300, 250, 1);
        App.Workspace.Undo();
        _report.Check(App.Mode == Mode.Annotating && App.Capture?.Crop.Bounds.Left == 100, "undo in a new, empty capture leaves it open, and never reaches the capture before it");
        _d.Escape();

        App.CaptureService.StartCapture(CaptureKind.Frozen);
        _d.Drag(200, 200, 500, 400, 1);
        _d.Draw(MarkKind.Rectangle, 250, 250, 400, 350);
        _d.Key(Keys.Backspace);
        _report.Check(App.Mode == Mode.Annotating && App.Capture?.Marks.Count == 1, "Backspace with nothing selected leaves the capture as it was");
        _d.ClickToolbarButton(App.Commands.Close);
        _report.Check(App.Mode == Mode.Idle, "the toolbar's Close button closes it");

        App.CaptureService.StartCapture(CaptureKind.Frozen);
        _d.Drag(200, 200, 500, 400, 1);
        _d.Click(120, 120);
        _report.Check(App.Mode == Mode.Idle, "a click outside the area closes the capture");

        App.CaptureService.StartCapture(CaptureKind.Frozen);
        _d.RightClick(400, 400);
        _report.Check(App.Mode == Mode.Idle, "a right-click while selecting gives the capture up");
    }

    private void Pickers()
    {
        ToolPickerForm picker = App.Shell.ToolPicker;
        App.CaptureService.StartCapture(CaptureKind.Frozen);
        _d.Drag(300, 200, 1100, 700, 1);

        _d.Pick(MarkKind.Rectangle);
        _d.Pick(MarkKind.Arrow);
        App.Commands.Run(App.Commands.RectangleTool);
        _report.Check(App.Tools.CurrentTool == MarkKind.Rectangle && picker.HangsFrom(App.Commands.RectangleTool), "one click on a tool that is off picks the one it shows and drops its pair down");
        Pictures.Image(picker.Pixels, At("picker-tools.png"));
        _d.ClickChoice(MarkKind.Ellipse);
        _report.Check(!picker.IsShown && App.Tools.CurrentTool == MarkKind.Ellipse && App.Tools.ShownIn(ToolGroup.Rect) == MarkKind.Ellipse, "picking the ellipse closes the list, and its button shows the ellipse from then on");

        App.Commands.Run(App.Commands.RectangleTool);
        _report.Check(picker.IsShown && App.Tools.CurrentTool == MarkKind.Ellipse, "a click on the tool that is on drops down its pair, and changes nothing yet");
        _d.Key(Keys.Up);
        _d.Key(Keys.Enter);
        _report.Check(!picker.IsShown && App.Tools.CurrentTool == MarkKind.Rectangle, "Up and Enter pick the rectangle from the list");
        App.Commands.Run(App.Commands.RectangleTool);
        _report.Check(!_d.ClickActivates(App.Shell.Toolbar) && !_d.ClickActivates(picker), "a click on the toolbar or on a drop-down leaves the keys with the capture");
        _d.ClickToolbarButton(App.Commands.TextTool);
        _report.Check(picker.HangsFrom(App.Commands.TextTool) && App.Tools.CurrentTool == MarkKind.Text, "with the list open, one click on the text button picks text and moves the drop-down to its pair");
        _d.ClickToolbarButton(App.Commands.TextTool);
        _report.Check(!picker.IsShown, "and a click on the same button closes it");
        _d.ClickToolbarButton(App.Commands.ArrowTool);
        _report.Check(picker.HangsFrom(App.Commands.ArrowTool), "the arrow button drops down the arrow, the pen and the highlighter");
        Pictures.Image(picker.Pixels, At("picker-arrow-tools.png"));
        _d.ClickToolbarButton(App.Commands.ArrowTool);
        _d.ClickToolbarButton(App.Commands.RectangleTool);
        _d.ClickToolbarButton(App.Commands.TextTool);
        _d.Key(Keys.Enter);
        _report.Check(!picker.IsShown && App.Tools.CurrentTool == MarkKind.Text, "moved to text's pair, the drop-down starts on the tool its button shows, so Enter picks text");

        App.Commands.Run(App.Commands.ArrowTool);
        _d.Key(Keys.Right);
        _report.Check(picker.HangsFrom(App.Commands.RectangleTool) && App.Tools.CurrentTool == MarkKind.Rectangle, "with the list open, Right moves the drop-down to the next button, as a click on it does");
        _d.Key(Keys.Left);
        _d.Key(Keys.Left);
        _report.Check(picker.HangsFrom(App.Commands.Color) && App.Tools.CurrentTool == MarkKind.Arrow, "and Left moves it back, from the first button round to the color button");
        Pictures.Toolbar(_d, At("toolbar-colors-open.png"));
        _d.Key(Keys.Escape);

        App.Commands.Run(App.Commands.Color);
        _report.Check(picker.IsShown, "a click on the color button drops down the colors");
        Pictures.Image(picker.Pixels, At("picker-colors.png"));
        _d.ClickSwatch(3);
        _report.Check(!picker.IsShown && App.Tools.Color == Tools.ColorAt(3), "picking a color closes the row and takes the color");

        int marks = App.Capture!.Marks.Count;
        App.Commands.Run(App.Commands.Color);
        _d.Click(150, 120);
        _report.Check(!picker.IsShown && App.Mode == Mode.Annotating && App.Capture?.Marks.Count == marks, "a click outside the capture only closes the drop-down: nothing drawn, the capture still open");
        App.Commands.Run(App.Commands.Color);
        _d.Key(Keys.Escape);
        _report.Check(!picker.IsShown && App.Mode == Mode.Annotating, "and so does Escape");

        App.Workspace.PickColor(0);
        _d.Escape();
    }

    private void History()
    {
        Capture older = SaveToHistory(300, 250, 700, 500);
        _report.Check(App.Mode == Mode.Annotating && App.History.Entries.Count == 1 && !App.Commands.SaveToHistory.Enabled(), "saving to history leaves the capture open, and its button off until something changes");
        Pictures.Screen(_d, older.Crop.Bounds.Inflated(40), At("history-saved.png"));
        SameAsFullRepaint("the toast in the middle of a saved capture");
        Pictures.Toolbar(_d, At("toolbar-annotate-saved.png"));
        Pictures.Image(App.Shell.Toolbar.ShowTipFor(App.Commands.SaveToHistory), At("toolbar-tooltip-saved.png"));
        App.Shell.Toolbar.HideTip();
        _d.Escape();
        Capture newer = SaveToHistory(820, 300, 1300, 620);
        _d.Escape();
        _report.Check(App.Mode == Mode.Idle && App.History.Entries.Count == 2 && App.History.Entries[0] == newer, "the history keeps the captures newest first");

        HistoryForm dropDown = App.Shell.HistoryForm;
        App.Commands.Run(App.Commands.History);
        _report.Check(dropDown.IsShown, "the History button drops the saved captures down");
        Pictures.Image(dropDown.Pixels, At("history-dropdown.png"));

        _d.ClickThumbnail(1);
        _report.Check(App.Mode == Mode.Annotating && !dropDown.IsShown && App.Capture?.Picture == older.Picture && App.Capture.Crop == older.Crop && App.Capture.Marks.Count == 1, "a click on a picture opens that capture again where it was, with its mark");
        Pictures.Screen(_d, older.Crop.Bounds.Inflated(120), At("history-reopened.png"));
        SameAsFullRepaint("a capture from the history");

        Rect saved = older.Crop.Bounds;
        _d.Drag(saved.Left, saved.Center.Y, saved.Left - 100, saved.Center.Y);
        _report.Check(App.Capture?.Crop.Bounds == saved with { Left = saved.Left - 100 }, "its area drags out over its old frozen screen");
        SameAsFullRepaint("a capture from the history, grown");
        _d.Draw(MarkKind.Ellipse, 520, 300, 660, 420);
        _d.Escape();
        _report.Check(App.Mode == Mode.Idle && older.Crop.Bounds == saved && older.Marks.Count == 1 && older.Picture.IsOpen, "Escape leaves the saved capture as it was");

        App.Workspace.OpenFromHistory(older);
        App.Workspace.Delete();
        _report.Check(App.Mode == Mode.Idle && App.History.Entries.Count == 1 && !older.Picture.IsOpen, "Delete takes it out of the history and frees its frozen screen");
        App.Workspace.Undo();
        _report.Check(App.Mode == Mode.Idle && App.History.Entries.Count == 1, "undo with no capture open changes nothing");
    }

    private Capture SaveToHistory(int left, int top, int right, int bottom)
    {
        App.CaptureService.StartCapture(CaptureKind.Frozen);
        _d.Drag(left, top, right, bottom, 1);
        _d.Draw(MarkKind.Rectangle, left + 30, top + 30, left + 160, top + 120);
        App.Commands.Run(App.Commands.SaveToHistory);
        return App.History.Entries[0];
    }

    private void Film()
    {
        var area = new Rect { Left = 180, Top = 180, Right = 830, Bottom = 520 };
        App.CaptureService.StartCapture(CaptureKind.Timed);
        _d.Drag(200, 200, 800, 500, 4);
        _report.Check(App.Mode == Mode.CountingDown, "a timed capture counts down once its area is chosen");
        Pictures.LiveLayer(_d, area.Inflated(40), At("timed-countdown.png"));
        for (int i = 0; i < 10 && App.Mode == Mode.CountingDown; i++)
        {
            App.Workflow.OnTick();
        }
        _report.Check(App.Mode == Mode.Annotating && App.Capture?.Crop.Width == 600, "the countdown ends in a frozen capture");
        SameAsFullRepaint("a timed capture");
        _d.Escape();

        App.CaptureService.StartCapture(CaptureKind.Timed);
        _d.Drag(200, 200, 800, 500, 4);
        App.Commands.Run(App.Commands.TimedCapture);
        _report.Check(App.Mode == Mode.Idle, "the timed capture's own shortcut cancels its countdown");

        App.CaptureService.StartCapture(CaptureKind.Recording);
        _d.Press(200, 200);
        _d.Move(500, 350);
        Pictures.LiveLayer(_d, area, At("record-selecting.png"));
        _d.Move(800, 500);
        _d.Release(800, 500);
        _report.Check(App.Mode == Mode.Recording, "a recording starts once its area is chosen");
        for (int i = 0; i < 12; i++)
        {
            App.Workflow.OnTick();
        }
        Pictures.LiveLayer(_d, area.Inflated(40), At("record-running.png"));
        Rect stop = App.Overlay.StopButton;
        _report.Check(stop.Width > 3 * stop.Height, $"the whole recording label is the stop button ({stop.Width}x{stop.Height})");
        App.CaptureService.StartOrStopRecording();
        _report.Check(App.Mode == Mode.Reviewing && App.Review.Clip?.Count >= 12, $"the record key stops it and opens the review ({App.Review.Clip?.Count} frames)");
        string? clipFile = App.Review.Clip?.FilePath;

        App.Review.Pause();
        App.Review.Advance(3);
        App.Review.Trim(head: true);
        Pictures.Screen(_d, area, At("review.png"));
        Pictures.Toolbar(_d, At("toolbar-review.png"));
        SameAsFullRepaint("the review");
        App.Workspace.Undo();
        _report.Check(App.Review.Clip?.In == 0, "undo takes the trim back");
        App.Workspace.Redo();
        _d.Escape();
        _report.Check(App.Mode == Mode.Idle && clipFile is not null && !File.Exists(clipFile), "Escape ends the review and deletes the recording");
    }

    private void RecordingEdits()
    {
        App.CaptureService.StartCapture(CaptureKind.Recording);
        _d.Drag(200, 200, 800, 500, 4);
        for (int i = 0; i < 6; i++)
        {
            App.Workflow.OnTick();
        }
        App.CaptureService.StartOrStopRecording();
        string? recording = App.Review.Clip?.FilePath;
        _report.Check(App.Mode == Mode.Reviewing && recording is not null, "a second recording opens its review");

        _d.Drag(800, 500, 650, 400);
        Rect cut = App.Capture?.Crop.Bounds ?? default;
        int reach = (int)MathF.Round(6 * _d.Desk.ScaleAt(_d.Desk.ScreenPrimary.Center));
        _report.Check(App.Mode == Mode.Reviewing && cut.TopLeft == new Point(200, 200) && Math.Abs(cut.Right - 650) <= reach && Math.Abs(cut.Bottom - 400) <= reach, $"a grip pulls the recording's area in, onto a line if one is in reach ({cut.Width}x{cut.Height})");
        Pictures.Screen(_d, cut.Inflated(180), At("review-cut.png"));
        SameAsFullRepaint("a recording cut down");

        string saved = At("review-cut.gif");
        bool wrote = App.Review.SaveAs(saved);
        byte[] gif;
        if (wrote)
        {
            gif = File.ReadAllBytes(saved);
        }
        else
        {
            gif = [];
        }
        _report.Check(gif.Length > 10 && gif[6] + (gif[7] << 8) == cut.Width && gif[8] + (gif[9] << 8) == cut.Height, "the cut recording saves at the size of its area");
        Pictures.Toolbar(_d, At("toolbar-review-cut.png"));

        int kept = App.History.Entries.Count;
        App.Commands.Run(App.Commands.SaveToHistory);
        _report.Check(App.Mode == Mode.Reviewing && App.History.Entries.Count == kept + 1 && App.History.Entries[0].Clip is not null, "saving a recording to the history keeps it, and its review open");
        _d.Escape();
        _report.Check(App.Mode == Mode.Idle && File.Exists(recording), "Escape keeps the saved recording's file");

        App.Commands.Run(App.Commands.History);
        Pictures.Image(App.Shell.HistoryForm.Pixels, At("history-dropdown-recording.png"));
        _d.ClickThumbnail(0);
        _report.Check(App.Mode == Mode.Reviewing && App.Capture?.Crop.Bounds == cut, "a click on it opens the recording's review again, cut as saved");
        Pictures.Screen(_d, cut.Inflated(180), At("history-recording-reopened.png"));
        SameAsFullRepaint("a recording from the history");
        Pictures.Toolbar(_d, At("toolbar-review-reopened.png"));

        _d.ClickToolbarButton(App.Commands.Delete);
        _report.Check(App.Mode == Mode.Idle && App.History.Entries.Count == kept && !File.Exists(recording), "the toolbar's delete button takes it out of the history and deletes its file");
    }

    private void HideToTray()
    {
        App.Commands.Run(App.Commands.HideToTray);
        _report.Check(App.History.Entries.Count == 1, "hiding to the tray keeps the history");
    }
}
