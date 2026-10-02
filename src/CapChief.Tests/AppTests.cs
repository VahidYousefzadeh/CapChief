using static CapChief.Tests.Fixtures;

namespace CapChief.Tests;

/// <summary>
/// Tests that a press, a drag, a release and a key do what the mode the app is in says they do,
/// that the cursor promises what a press would do, and that undo stays inside the open capture.
/// </summary>
public sealed class AppTests : IDisposable
{
    private readonly Headless _run = new();

    private App App
    {
        get
        {
            return _run.App;
        }
    }

    public void Dispose()
    {
        _run.Dispose();
    }

    [Fact]
    public void ADragWhileSelectingOpensTheCapture()
    {
        Eq(App.Mode, Mode.Idle, "it starts idle");

        App.CaptureService.StartCapture(CaptureKind.Frozen);
        Eq(App.Mode, Mode.Selecting, "a capture starts by selecting");
        Assert.True(App.Workflow.FrozenScreen is { IsOpen: true }, "over a frozen picture of the screen");

        _run.Drag(P(100, 100), P(400, 300));
        Eq(App.Mode, Mode.Annotating, "releasing a big enough area opens it for annotating");
        Eq(_run.Capture.Crop, R(100, 100, 400, 300), "with the area the drag drew");
        Assert.True(App.Workflow.FrozenScreen is null, "and the frozen picture now belongs to the capture");
    }

    [Fact]
    public void ASmallDragKeepsSelecting()
    {
        App.CaptureService.StartCapture(CaptureKind.Frozen);
        _run.Drag(P(100, 100), P(105, 105));
        Eq(App.Mode, Mode.Selecting, "an area under the smallest size is not taken");
    }

    [Fact]
    public void ATypedSizeIsTheSizeTaken()
    {
        App.CaptureService.StartCapture(CaptureKind.Frozen);
        App.Workflow.OnMouseDown(P(200, 200), false);
        App.Workflow.OnMouseMove(P(800, 500), false, false);
        _run.Keystroke(Key.Tab);
        _run.Keystroke(Key.D6);
        _run.Keystroke(Key.D4);
        _run.Keystroke(Key.D0);
        App.Workflow.OnMouseUp();
        Eq(App.Mode, Mode.Selecting, "a release while a size is typed does not take the area");

        _run.Keystroke(Key.Enter);
        Eq(App.Mode, Mode.Annotating, "Enter takes it");
        Eq(_run.Capture.Crop, R(200, 200, 840, 500), "as wide as typed, as tall as dragged");
    }

    [Fact]
    public void APressOffTheCropIsAClickUntilItMoves()
    {
        _run.Open(R(100, 100, 400, 300));
        _run.Draw(MarkKind.Rectangle, 150, 150, 250, 220);

        App.Workflow.OnMouseDown(P(60, 60), false);
        App.Workflow.OnMouseMove(P(61, 61), false, false);
        App.Workflow.OnMouseUp();
        Eq(App.Mode, Mode.Idle, "a click off the crop finishes the capture");
    }

    [Fact]
    public void ATravelledPressOffTheCropDrawsInstead()
    {
        _run.Open(R(100, 100, 400, 300));

        _run.Drag(P(60, 60), P(140, 140));
        Eq(App.Mode, Mode.Annotating, "travelling makes it a drag, so the capture stays");
        Eq(_run.Marks.Count, 1, "and the drag drew one mark");
        Eq(_run.Marks[0].A, P(60, 60), "starting where the button went down");
    }

    [Fact]
    public void AMoveWithNothingHeldIsNotADrag()
    {
        _run.Open(R(100, 100, 400, 300));

        App.Workflow.OnMouseMove(P(200, 200), false, false);
        App.Workflow.OnMouseMove(P(260, 260), false, false);
        App.Workflow.OnMouseUp();
        Eq(_run.Marks.Count, 0, "moving with no button down draws nothing");
        Eq(App.Mode, Mode.Annotating, "and a release nobody pressed changes nothing");
    }

    [Fact]
    public void EscapeBacksOutOneStepAtATime()
    {
        App.CaptureService.StartCapture(CaptureKind.Frozen);
        _run.Keystroke(Key.Escape);
        Eq(App.Mode, Mode.Idle, "Escape while selecting gives the capture up");

        _run.Open(R(100, 100, 400, 300));
        _run.Draw(MarkKind.Rectangle, 150, 150, 250, 220);
        _run.Click(P(200, 150));
        Assert.True(App.Document.HasSelectedMark, "a click on a mark selects it");
        _run.Keystroke(Key.Escape);
        Assert.False(App.Document.HasSelectedMark, "the first Escape drops the selection");
        Eq(App.Mode, Mode.Annotating, "and keeps the capture");
        _run.Keystroke(Key.Escape);
        Eq(App.Mode, Mode.Idle, "the second closes it");
    }

    [Fact]
    public void UndoStaysInsideTheOpenCapture()
    {
        _run.Open(R(100, 100, 400, 300));
        _run.Draw(MarkKind.Rectangle, 150, 150, 250, 220);
        App.Workspace.Undo();
        Eq(_run.Marks.Count, 0, "undo takes the mark off");
        App.Workspace.Undo();
        Eq(App.Mode, Mode.Annotating, "and an undo with nothing left to undo keeps the capture open");

        App.Workspace.Redo();
        Eq(_run.Marks.Count, 1, "redo puts the mark back");
        _run.Keystroke(Key.Escape);
        App.Workspace.Undo();
        Eq(App.Mode, Mode.Idle, "a closed capture does not come back with undo");
    }

    [Fact]
    public void UndoNeverReachesAnEarlierCapture()
    {
        _run.Open(R(100, 100, 400, 300));
        _run.Draw(MarkKind.Rectangle, 150, 150, 250, 220);
        _run.Keystroke(Key.Escape);

        _run.Open(R(500, 500, 700, 700));
        App.Workspace.Undo();
        Eq(_run.Capture.Crop, R(500, 500, 700, 700), "undo in a new, empty capture leaves it open");
        Eq(_run.Marks.Count, 0, "and empty");
    }

    [Fact]
    public void AClosedCaptureLetsItsPictureGo()
    {
        _run.Open(R(100, 100, 400, 300));
        Capture closed = _run.Capture;
        _run.Keystroke(Key.Escape);
        Assert.False(closed.Picture.IsOpen, "Escape frees the frozen screen of a capture nothing keeps");
    }

    [Fact]
    public void ATimedCaptureCountsDownAndFreezes()
    {
        App.CaptureService.StartCapture(CaptureKind.Timed);
        Eq(App.Mode, Mode.Selecting, "a timed capture starts by selecting the live screen");
        Assert.True(App.Workflow.FrozenScreen is null, "which is not frozen");

        _run.Drag(P(100, 100), P(400, 300));
        Eq(App.Mode, Mode.CountingDown, "the release starts the countdown");
        Eq(App.CountingDown.SecondsLeft, CountingDown.Seconds, "from the full count");

        int ticks = 0;
        while (App.Mode == Mode.CountingDown && ticks < 100)
        {
            App.Workflow.OnTick();
            ticks++;
        }
        Eq(ticks, (int)(CountingDown.Seconds * 1000 / CountingDown.TickMs), "it ticks for the whole count");
        Eq(App.Mode, Mode.Annotating, "and then freezes the screen into a capture");
        Eq(_run.Capture.Crop, R(100, 100, 400, 300), "of the area chosen");
    }

    [Fact]
    public void EscapeStopsACountdown()
    {
        _run.Start(CaptureKind.Timed, R(100, 100, 400, 300));
        _run.Keystroke(Key.Escape);
        Eq(App.Mode, Mode.Idle, "Escape gives the countdown up");
        App.Workflow.OnTick();
        Eq(App.Mode, Mode.Idle, "and a late tick changes nothing");
    }

    [Fact]
    public void ARecordingStopsIntoItsReview()
    {
        _run.Start(CaptureKind.Recording, R(100, 100, 400, 300));
        Eq(App.Mode, Mode.Recording, "a drag over the live screen starts recording");

        for (int i = 0; i < 5; i++)
        {
            App.Workflow.OnTick();
        }

        App.Commands.Run(App.Commands.Record);
        Eq(App.Mode, Mode.Reviewing, "stopping opens the review");
        Eq(App.Review.Clip?.Count, 6, "of the five frames ticked and the one the start took");
        string? file = App.Review.Clip?.FilePath;
        Assert.True(file is not null && File.Exists(file), "written to a file");

        _run.Keystroke(Key.Escape);
        Eq(App.Mode, Mode.Idle, "Escape ends the review");
        Assert.False(File.Exists(file), "and deletes the file, which nothing can reach any more");
    }

    [Fact]
    public void AReviewResizesTheAreaInsideWhatWasRecorded()
    {
        _run.Record(R(100, 100, 400, 300));
        Eq(App.Workflow.CursorAt(P(400, 300)), Cursor.SizeNWSE, "the review offers the area's grips");
        Eq(App.Workflow.CursorAt(P(250, 200)), Cursor.Arrow, "and nothing to draw with inside it");

        _run.Drag(P(400, 300), P(300, 250));
        Eq(_run.Capture.Crop, R(100, 100, 300, 250), "a grip pulls the area in");
        Eq(App.Mode, Mode.Reviewing, "and the review goes on");
        Eq(_run.Marks.Count, 0, "with nothing drawn");

        _run.Drag(P(300, 250), P(600, 500));
        Eq(_run.Capture.Crop, R(100, 100, 400, 300), "the area stops at the edge of what was recorded");

        _run.Drag(P(400, 300), P(300, 250));
        _run.Drag(P(150, 100), P(250, 130));
        Eq(_run.Capture.Crop, R(200, 130, 400, 280), "its border moves it, only as far as the recording goes");

        App.Workspace.Undo();
        Eq(_run.Capture.Crop, R(100, 100, 300, 250), "undo puts the area back");

        _run.Click(P(50, 50));
        Eq(App.Mode, Mode.Reviewing, "and a click outside it keeps the review open");
    }

    [Fact]
    public void PressingTheStopButtonEndsTheRecording()
    {
        _run.Start(CaptureKind.Recording, R(100, 100, 400, 300));
        App.Workflow.OnTick();
        Rect stop = App.Overlay.StopButton;
        Assert.False(stop.IsEmpty, "a recording offers a stop button");

        App.Workflow.OnMouseDown(stop.Center, false);
        Eq(App.Mode, Mode.Reviewing, "pressing it stops the recording");
    }

    [Fact]
    public void SelectingDrawsEverywhere()
    {
        App.CaptureService.StartCapture(CaptureKind.Frozen);
        foreach (Point p in Grid())
        {
            Eq(App.Workflow.CursorAt(p), Cursor.Draw, $"the selecting cursor at {p.X},{p.Y}");
        }
    }

    [Fact]
    public void TheQuietModesShowAnArrow()
    {
        foreach (Point p in Grid())
        {
            Eq(App.Workflow.CursorAt(p), Cursor.Arrow, $"idle, the cursor at {p.X},{p.Y}");
        }

        _run.Start(CaptureKind.Timed, R(100, 100, 400, 300));
        foreach (Point p in Grid())
        {
            Eq(App.Workflow.CursorAt(p), Cursor.Arrow, $"counting down, the cursor at {p.X},{p.Y}");
        }
    }

    [Fact]
    public void ARecordingOffersItsLabelAndNothingElse()
    {
        _run.Start(CaptureKind.Recording, R(100, 100, 400, 300));
        App.Workflow.OnTick();
        Rect stop = App.Overlay.StopButton;
        Assert.True(stop.Width > stop.Height * 3, "the whole label stops the recording, not only the square in it");
        foreach (Point p in Grid().Append(stop.Center))
        {
            Cursor expected;
            if (stop.Contains(p))
            {
                expected = Cursor.Hand;
            }
            else
            {
                expected = Cursor.Arrow;
            }
            Eq(App.Workflow.CursorAt(p), expected, $"the recording cursor at {p.X},{p.Y}");
        }
        App.Workflow.StopRecording();
    }

    [Fact]
    public void TheCloseButtonClosesTheCaptureAtOnce()
    {
        _run.Open(R(100, 100, 400, 300));
        _run.Draw(MarkKind.Rectangle, 150, 150, 250, 220);
        _run.Click(P(200, 150));
        Assert.True(App.Document.HasSelectedMark, "with a mark selected");
        App.Commands.Run(App.Commands.Close);
        Eq(App.Mode, Mode.Idle, "Close closes the capture in one go, where Escape first drops the selection");

        _run.Record(R(100, 100, 400, 300));
        App.Commands.Run(App.Commands.Close);
        Eq(App.Mode, Mode.Idle, "and it closes a review too");
        Assert.False(App.Commands.Close.Enabled(), "with nothing open, there is nothing to close");
    }

    private static IEnumerable<Point> Grid()
    {
        for (int x = 60; x <= 460; x += 20)
        {
            for (int y = 60; y <= 360; y += 20)
            {
                yield return P(x, y);
            }
        }
    }
}
