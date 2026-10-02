using static CapChief.Tests.Fixtures;

namespace CapChief.Tests;

/// <summary>
/// Tests that marks are drawn, selected, moved, recolored and deleted, that the area moves and
/// resizes, that each of these undoes as the one step the user took, and that the cursor promises
/// what a press would do.
/// </summary>
public sealed class DrawingTests : IDisposable
{
    private readonly Headless _run = new();

    private App App
    {
        get
        {
            return _run.App;
        }
    }

    private Marks Marks
    {
        get
        {
            return _run.Marks;
        }
    }

    public void Dispose()
    {
        _run.Dispose();
    }

    [Fact]
    public void DrawsAndTakesBack()
    {
        _run.Open(R(0, 0, 500, 500));
        _run.Draw(MarkKind.Rectangle, 100, 100, 200, 200);
        Eq(Marks.Count, 1, "a press, a drag and a release draw one mark");
        Eq(Marks[0].Bounds, R(100, 100, 200, 200), "shaped by the drag");

        _run.Draw(MarkKind.Rectangle, 300, 300, 360, 360);
        Eq(Marks.Count, 2, "a second drag draws a second");

        App.Workspace.Undo();
        Eq(Marks.Count, 1, "undo takes the newest back");
        App.Workspace.Undo();
        Eq(Marks.Count, 0, "and then the first");

        App.Workspace.Redo();
        Eq(Marks.Count, 1, "redo puts one back");
        App.Workspace.Redo();
        Eq(Marks.Count, 2, "and then the other");
        Eq(Marks[1].Bounds, R(300, 300, 360, 360), "in the place it came from");
    }

    [Fact]
    public void AClickDrawsNothing()
    {
        _run.Open(R(0, 0, 500, 500));
        App.Tools.PickTool(MarkKind.Rectangle);
        _run.Click(P(100, 100));
        Eq(Marks.Count, 0, "a click with a shape tool draws nothing");

        App.Tools.PickTool(MarkKind.Step);
        _run.Click(P(100, 100));
        Eq(Marks.Count, 1, "but a click with the step tool places a numbered step");
    }

    [Fact]
    public void AWobbleOffTheAreaDrawsNothing()
    {
        _run.Open(R(100, 100, 400, 300));
        App.Tools.PickTool(MarkKind.Rectangle);

        App.Workflow.OnMouseDown(P(60, 60), false);
        App.Workflow.OnMouseMove(P(62, 61), false, false);
        Eq(Marks.Count, 0, "a wobble on the button draws nothing");
        App.Workflow.OnMouseMove(P(160, 90), false, false);
        App.Workflow.OnMouseUp();
        Eq(Marks.Count, 1, "a press that travelled draws");
        Eq(Marks[0].A, P(60, 60), "from where the button went down");
    }

    [Fact]
    public void SelectsAndDeletes()
    {
        _run.Open(R(0, 0, 500, 500));
        _run.Draw(MarkKind.Rectangle, 100, 100, 200, 200);
        Assert.False(App.Document.HasSelectedMark, "a fresh mark is not left selected");

        _run.Click(P(150, 100));
        Assert.True(App.Document.HasSelectedMark, "a click on a mark selects it");

        _run.Keystroke(Key.Delete);
        Eq(Marks.Count, 0, "Delete deletes the selected mark");
        Assert.False(App.Document.HasSelectedMark, "and nothing is left selected");
        Eq(App.Mode, Mode.Annotating, "and the area stays");

        App.Workspace.Undo();
        Eq(Marks.Count, 1, "undo brings it back");
        Eq(Marks[0].Bounds, R(100, 100, 200, 200), "where it was");

        Assert.False(App.Workspace.DropSelectedMark(), "with nothing selected there is no selection to drop");
        _run.Click(P(150, 100));
        Assert.True(App.Workspace.DropSelectedMark(), "a selection drops");
    }

    [Fact]
    public void NudgesAsOneStep()
    {
        _run.Open(R(0, 0, 500, 500));
        _run.Draw(MarkKind.Rectangle, 100, 100, 200, 200);
        _run.Click(P(150, 100));

        _run.Keystroke(Key.Right);
        _run.Keystroke(Key.Right);
        _run.Keystroke(Key.Right, shift: true);
        Eq(Marks[0].Bounds, R(112, 100, 212, 200), "two nudges and a far one move the mark twelve pixels");

        App.Workspace.Undo();
        Eq(Marks[0].Bounds, R(100, 100, 200, 200), "a held arrow key undoes as one step");
        Eq(Marks.Count, 1, "and only that step");
    }

    [Fact]
    public void RecolorsAsOneStep()
    {
        _run.Open(R(0, 0, 500, 500));
        _run.Draw(MarkKind.Rectangle, 100, 100, 200, 200);
        Color first = Marks[0].Color;
        _run.Click(P(150, 100));

        _run.Keystroke(Key.D2);
        _run.Keystroke(Key.D3);
        _run.Keystroke(Key.D4);
        Eq(Marks[0].Color, Tools.ColorAt(3), "the digit keys recolor the selected mark");

        App.Workspace.Undo();
        Eq(Marks[0].Color, first, "and a run of recolors undoes to the first color");
        Eq(Marks.Count, 1, "in one step");
    }

    [Fact]
    public void MovingAMarkIsOneStep()
    {
        _run.Open(R(0, 0, 800, 600));
        _run.Draw(MarkKind.Rectangle, 100, 100, 300, 300);

        App.Workflow.OnMouseDown(P(200, 100), false);
        Eq(App.Document.SelectedIndex, 0, "a press on a mark selects it at once");
        App.Workflow.OnMouseMove(P(210, 80), false, false);
        App.Workflow.OnMouseMove(P(220, 60), false, false);
        App.Workflow.OnMouseUp();
        Eq(Marks[0].Bounds, R(120, 60, 320, 260), "the mark follows the pointer by the whole drag");

        App.Workspace.Undo();
        Eq(Marks[0].Bounds, R(100, 100, 300, 300), "and one undo takes the whole drag back");

        _run.Click(P(200, 100));
        App.Workspace.Redo();
        Eq(Marks[0].Bounds, R(120, 60, 320, 260), "a press that never travelled records nothing, so the drag is still there to redo");
    }

    [Fact]
    public void ResizingAMarkIsOneStep()
    {
        _run.Open(R(0, 0, 800, 600));
        _run.Draw(MarkKind.Rectangle, 100, 100, 300, 300);
        _run.Click(P(200, 100));

        _run.Drag(P(300, 300), P(350, 330));
        Eq(Marks[0].Bounds, R(100, 100, 350, 330), "the corner grip of a selected mark resizes it");
        App.Workspace.Undo();
        Eq(Marks[0].Bounds, R(100, 100, 300, 300), "and undo sizes it back");
    }

    [Fact]
    public void TheAreaMovesAndResizesBack()
    {
        _run.Open(R(100, 100, 200, 200));

        _run.Drag(P(120, 100), P(160, 130));
        Eq(_run.Capture.Crop, R(140, 130, 240, 230), "a drag on the border moves the area");
        App.Workspace.Undo();
        Eq(_run.Capture.Crop, R(100, 100, 200, 200), "undo puts it back");
        App.Workspace.Redo();
        Eq(_run.Capture.Crop, R(140, 130, 240, 230), "and redo moves it again");

        _run.Drag(P(240, 230), P(340, 290));
        Eq(_run.Capture.Crop, R(140, 130, 340, 290), "a drag on the corner resizes it");
        App.Workspace.Undo();
        Eq(_run.Capture.Crop, R(140, 130, 240, 230), "and undo sizes it back");
    }

    [Fact]
    public void ResizingTheAreaLeavesTheMarksWhereTheyAre()
    {
        _run.Open(R(100, 100, 300, 300));
        _run.Draw(MarkKind.Rectangle, 150, 150, 200, 200);

        _run.Drag(P(100, 200), P(60, 200));
        Eq(_run.Capture.Crop, R(60, 100, 300, 300), "the west edge moves");
        Eq(Marks[0].Bounds, R(150, 150, 200, 200), "and the marks stay where they were drawn");
    }

    [Fact]
    public void MovingTheAreaTakesTheMarksAlong()
    {
        _run.Open(R(100, 100, 300, 300));
        _run.Draw(MarkKind.Rectangle, 150, 150, 200, 200);

        _run.Drag(P(100, 150), P(140, 170));
        Eq(_run.Capture.Crop, R(140, 120, 340, 320), "a drag on the border moves the area");
        Eq(Marks[0].Bounds, R(190, 170, 240, 220), "and the marks move with it");

        App.Workspace.Undo();
        Eq(_run.Capture.Crop, R(100, 100, 300, 300), "undo puts the area back");
        Eq(Marks[0].Bounds, R(150, 150, 200, 200), "and the marks with it");
    }

    [Fact]
    public void AnEdgeResizedNearALineLandsOnIt()
    {
        _run.Open(R(280, 120, 560, 280));
        PaintBox(R(300, 150, 500, 250));

        _run.Drag(P(280, 200), P(296, 200));
        Eq(_run.Capture.Crop, R(300, 120, 560, 280), "the west edge lands on the border of the box 4 px away, keeping it inside");

        _run.Drag(P(560, 280), P(503, 254));
        Eq(_run.Capture.Crop, R(300, 120, 500, 250), "a corner lands both its edges on the borders near them");

        App.Workspace.Undo();
        App.Workspace.Undo();
        _run.Drag(P(280, 200), P(290, 200));
        Eq(_run.Capture.Crop, R(290, 120, 560, 280), "10 px from the border the edge stays with the pointer");
    }

    [Fact]
    public void ShiftAltAndMovingLeaveTheEdgeWithThePointer()
    {
        _run.Open(R(280, 120, 560, 280));
        PaintBox(R(300, 150, 500, 250));

        _run.Drag(P(280, 200), P(296, 200), shift: true);
        Eq(_run.Capture.Crop.Bounds.Left, 296, "with Shift held the edge stays with the pointer");
        App.Workspace.Undo();

        App.Workflow.OnMouseDown(P(280, 200), false);
        App.Drawing.Drag(P(296, 200), shift: false, alt: true);
        App.Workflow.OnMouseUp();
        Eq(_run.Capture.Crop, R(296, 120, 560, 280), "and with Alt held");
        App.Workspace.Undo();

        _run.Drag(P(400, 120), P(416, 120));
        Eq(_run.Capture.Crop, R(296, 120, 576, 280), "and a moved area goes where it is moved");
    }

    [Fact]
    public void TheAreaNudgesAsOneStep()
    {
        _run.Open(R(100, 100, 200, 200));
        _run.Keystroke(Key.Right);
        _run.Keystroke(Key.Right);
        _run.Keystroke(Key.Down, shift: true);
        Eq(_run.Capture.Crop, R(102, 110, 202, 210), "with nothing selected the arrow keys move the area");

        App.Workspace.Undo();
        Eq(_run.Capture.Crop, R(100, 100, 200, 200), "and a held arrow key undoes as one step");
        Eq(App.Mode, Mode.Annotating, "leaving the capture open");
    }

    [Fact]
    public void ACopySharesItsPictureButNotItsMarks()
    {
        using var picture = new Surface();
        Assert.True(picture.OpenDib(16, 16), "a picture to share");
        var capture = new Capture(picture, CropOf(100, 100, 300, 200));
        var document = new Document();
        document.Open(capture);
        document.AddMark(Rectangle(120, 120, 160, 160));

        Capture copy = capture.Copy();
        document.Open(copy);
        document.AddMark(Rectangle(200, 120, 240, 160));
        document.ChangeCrop(CropOf(80, 80, 320, 220));
        Eq(copy.Marks.Count, 2, "a mark drawn on the copy is the copy's");
        Eq(capture.Marks.Count, 1, "and the capture it was copied from keeps its own");
        Eq(capture.Crop, R(100, 100, 300, 200), "and its area");
        Assert.True(ReferenceEquals(copy.Picture, capture.Picture), "while the picture is shared, not copied");
    }

    [Fact]
    public void TheCursorSaysWhatTheAreaEdgeWouldDo()
    {
        _run.Open(R(100, 100, 400, 300));
        Eq(App.Workflow.CursorAt(P(100, 100)), Cursor.SizeNWSE, "the top left corner resizes both ways");
        Eq(App.Workflow.CursorAt(P(400, 100)), Cursor.SizeNESW, "the top right corner the other way");
        Eq(App.Workflow.CursorAt(P(100, 300)), Cursor.SizeNESW, "and so does the bottom left");
        Eq(App.Workflow.CursorAt(P(400, 300)), Cursor.SizeNWSE, "and the bottom right");
        Eq(App.Workflow.CursorAt(P(250, 100)), Cursor.SizeNS, "the top edge resizes up and down");
        Eq(App.Workflow.CursorAt(P(100, 200)), Cursor.SizeWE, "the left edge side to side");
        Eq(App.Workflow.CursorAt(P(250, 200)), Cursor.Draw, "the middle of it draws");
        Eq(App.Workflow.CursorAt(P(60, 60)), Cursor.Arrow, "and off it, with nothing held, the pointer is an arrow");
    }

    [Fact]
    public void TheCursorSaysWhatAMarkWouldDo()
    {
        _run.Open(R(100, 100, 400, 300));
        _run.Draw(MarkKind.Rectangle, 150, 150, 250, 220);
        Eq(App.Workflow.CursorAt(P(170, 150)), Cursor.SizeAll, "a mark under the pointer moves");

        _run.Click(P(170, 150));
        Eq(App.Workflow.CursorAt(P(250, 220)), Cursor.SizeNWSE, "a corner of the selected mark resizes it");

        _run.Draw(MarkKind.Arrow, 150, 250, 250, 280);
        _run.Click(P(200, 265));
        Eq(App.Workflow.CursorAt(P(250, 280)), Cursor.Cross, "the free end of an arrow is a crosshair");
    }

    [Fact]
    public void HoldingTheButtonDrawsEvenOffTheArea()
    {
        _run.Open(R(100, 100, 400, 300));
        App.Workflow.OnMouseDown(P(250, 200), false);
        Eq(App.Workflow.CursorAt(P(60, 60)), Cursor.Draw, "off the area mid-press, it is still drawing");
    }

    [Fact]
    public void ADragKeepsTheGripItBeganWith()
    {
        _run.Open(R(100, 100, 400, 300));
        _run.Draw(MarkKind.Rectangle, 150, 150, 250, 220);
        _run.Click(P(170, 150));
        App.Workflow.OnMouseDown(P(250, 220), false);
        Eq(App.Workflow.CursorAt(P(60, 60)), Cursor.SizeNWSE, "a corner drag keeps its cursor wherever the pointer wanders");
    }

    private void PaintBox(Rect box)
    {
        DeviceContext dc = _run.Capture.Picture.DeviceContext;
        dc.FillRect(box, 0x808080u);
        dc.FillRect(box.Inflated(-1), TestScreen.Ground);
        DeviceContext.Flush();
    }
}
