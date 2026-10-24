using static CapChief.Tests.Fixtures;

namespace CapChief.Tests;

/// <summary>
/// Tests that every key asks for the command it should in each mode, that every tooltip reads as it
/// always has with the key its command binds, and that a command runs only where it means
/// something.
/// </summary>
public sealed class CommandsTests : IDisposable
{
    private readonly Headless _run = new();

    private App App
    {
        get
        {
            return _run.App;
        }
    }

    private Commands Commands
    {
        get
        {
            return App.Commands;
        }
    }

    public void Dispose()
    {
        _run.Dispose();
    }

    [Fact]
    public void TheScreenshotKeysCopyAndSave()
    {
        Eq(In(Mode.Annotating, Key.C, ctrl: true), "copy", "Ctrl+C copies the screenshot");
        Eq(In(Mode.Annotating, Key.Enter), "copy", "and so does Enter");
        Eq(In(Mode.Annotating, Key.H, ctrl: true), "save-to-history", "Ctrl+H saves it to the history");
        Eq(In(Mode.Annotating, Key.S, ctrl: true), "save", "Ctrl+S saves it");
        Eq(In(Mode.Annotating, Key.Delete), "delete-mark", "Delete deletes the selected mark");
        Eq(In(Mode.Annotating, Key.Backspace), "delete-mark", "and so does Backspace");
    }

    [Fact]
    public void DeleteWithNothingSelectedKeepsTheCapture()
    {
        _run.Open(R(100, 100, 400, 300));
        _run.Draw(MarkKind.Rectangle, 150, 150, 250, 220);
        Assert.False(_run.Keystroke(Key.Delete), "Delete right after drawing has no selected mark to act on");
        Assert.False(_run.Keystroke(Key.Backspace), "and neither has Backspace");
        Eq(App.Mode, Mode.Annotating, "so the capture stays open");
        Eq(_run.Marks.Count, 1, "with its mark");

        App.Commands.Run(App.Commands.Delete);
        Eq(App.Mode, Mode.Idle, "the Delete button still throws the capture away");
    }

    [Fact]
    public void TheUndoKeysUndoAndRedo()
    {
        Eq(In(Mode.Annotating, Key.Z, ctrl: true), "undo", "Ctrl+Z undoes");
        Eq(In(Mode.Annotating, Key.Z, ctrl: true, shift: true), "redo", "Ctrl+Shift+Z redoes");
        Eq(In(Mode.Annotating, Key.Y, ctrl: true), "redo", "and so does Ctrl+Y");
        Eq(In(Mode.Annotating, Key.Z), "", "Z on its own asks for nothing");
    }

    [Fact]
    public void TheColorKeysStepAndPickColors()
    {
        Eq(In(Mode.Annotating, Key.Left, ctrl: true), "color-back", "Ctrl+Left steps the color back");
        Eq(In(Mode.Annotating, Key.Right, ctrl: true), "color-next", "Ctrl+Right steps it on");
        Eq(In(Mode.Annotating, Key.D1), "color-1", "the digit keys pick the color they name");
        Eq(In(Mode.Annotating, Key.D9), "color-9", "all nine of them");
        Eq(In(Mode.Annotating, Key.D0), "", "and zero picks none");
        Eq(In(Mode.Annotating, Key.D1, ctrl: true), "", "Ctrl and a digit asks for nothing");
    }

    [Fact]
    public void TheNudgeKeysCarryTheirStep()
    {
        Eq(In(Mode.Annotating, Key.Left), "nudge-left", "an arrow key moves one pixel");
        Eq(In(Mode.Annotating, Key.Left, shift: true), "nudge-left-far", "and ten with Shift held");
        Eq(In(Mode.Annotating, Key.Up), "nudge-up", "up is up");
        Eq(In(Mode.Annotating, Key.Down, shift: true), "nudge-down-far", "and down is down, coarse or fine");
    }

    [Fact]
    public void TheToolKeysPickTheirTool()
    {
        Eq(In(Mode.Annotating, Key.A), "pick-arrow", "A picks the arrow");
        Eq(In(Mode.Annotating, Key.P), "pick-pen", "P the pen");
        Eq(In(Mode.Annotating, Key.R), "pick-rectangle", "R the rectangle");
        Eq(In(Mode.Annotating, Key.E), "pick-ellipse", "E the ellipse");
        Eq(In(Mode.Annotating, Key.T), "pick-text", "T the text");
        Eq(In(Mode.Annotating, Key.N), "pick-step", "N the numbered step");
        Eq(In(Mode.Annotating, Key.M), "pick-mosaic", "M the mosaic");
        Eq(In(Mode.Annotating, Key.B), "pick-blackout", "B the solid bar");
        Eq(In(Mode.Annotating, Key.A, shift: true), "", "and Shift takes them all away");
    }

    [Fact]
    public void TheReviewKeysPlayAndTrim()
    {
        Eq(In(Mode.Reviewing, Key.Space), "play", "Space plays and pauses");
        Eq(In(Mode.Reviewing, Key.Home), "to-start", "Home goes to the start");
        Eq(In(Mode.Reviewing, Key.End), "to-end", "End to the end");
        Eq(In(Mode.Reviewing, Key.I), "trim-head", "I trims the head");
        Eq(In(Mode.Reviewing, Key.O), "trim-tail", "O the tail");
        Eq(In(Mode.Reviewing, Key.S), "speed", "S alone cycles the speed");
        Eq(In(Mode.Reviewing, Key.S, ctrl: true), "save-clip", "and Ctrl+S saves it, as it saves a screenshot");
        Eq(In(Mode.Reviewing, Key.Left), "step-back", "Left on its own is a step back");
        Eq(In(Mode.Reviewing, Key.Left, ctrl: true), "", "and Ctrl+Left is a color, which a clip has none of");
        Eq(In(Mode.Reviewing, Key.Enter), "", "Enter copies a screenshot, and a recording cannot be copied");
        Eq(In(Mode.Reviewing, Key.Delete), "", "Delete deletes a selected mark, and a recording has none");
        Eq(In(Mode.Reviewing, Key.X, ctrl: true, shift: true), "untrim", "Ctrl+Shift+X clears the trim");
        Eq(In(Mode.Reviewing, Key.H, ctrl: true), "save-to-history", "Ctrl+H saves it to the history");
        Eq(In(Mode.Reviewing, Key.Z, ctrl: true), "undo", "Ctrl+Z undoes a change to the area, the trim or the speed");
        Eq(In(Mode.Reviewing, Key.Y, ctrl: true), "redo", "and Ctrl+Y redoes it");
    }

    [Fact]
    public void TheSizeKeysTypeAndRubOut()
    {
        Eq(In(Mode.Selecting, Key.Tab), "size-tab", "Tab moves between width and height");
        Eq(In(Mode.Selecting, Key.Tab, ctrl: true), "", "but not with Ctrl held");
        Eq(In(Mode.Selecting, Key.Backspace), "size-back", "Backspace rubs out a digit");
        Eq(In(Mode.Selecting, Key.D0), "size-0", "and zero is a digit here");
        Eq(In(Mode.Selecting, Key.Enter), "accept", "Enter takes the area");
        Eq(In(Mode.Selecting, Key.Enter, ctrl: true), "", "but not Ctrl+Enter");
    }

    [Fact]
    public void EveryBusyModeAnswersEscapeAndIdleAnswersNothing()
    {
        foreach (Mode mode in new[] { Mode.Selecting, Mode.CountingDown, Mode.Recording, Mode.Reviewing, Mode.Annotating })
        {
            Eq(In(mode, Key.Escape), "escape", $"Escape backs out while {mode}");
        }
        Eq(Commands.For(Mode.Idle).Count, 0, "and idle, there is nothing to back out of");
        Eq(In(Mode.Annotating, Key.None), "", "a key this app does not know asks for nothing");
    }

    [Fact]
    public void TheCaptureToolbarReadsAsItDid()
    {
        Eq(Commands.Capture.Tip, "Capture (Ctrl+Alt+C)", "the capture tooltip");
        Eq(Commands.TimedCapture.Tip, "Timed capture (Ctrl+Alt+T)", "the timed tooltip");
        Eq(Commands.Record.Tip, "Record GIF (Ctrl+Alt+G)", "the record tooltip");
        Eq(Commands.History.Tip, "History", "the history tooltip");
        Eq(Commands.HideToTray.Tip, "Hide to tray", "the hide tooltip");
    }

    [Fact]
    public void TheAnnotateToolbarReadsAsItDid()
    {
        Eq(Commands.Copy.Tip, "Copy (Ctrl+C)", "the copy tooltip");
        Eq(Commands.Save.Tip, "Save as PNG (Ctrl+S)", "the save tooltip");
        Eq(Commands.SaveToHistory.Tip, "Save to history (Ctrl+H)", "the save to history tooltip");
        Eq(Commands.Delete.Tip, "Delete", "the delete tooltip, which names no key: the keys delete only a selected mark");
        Eq(Commands.ArrowTool.Tip, "Arrow (A)", "a tool tooltip, as its drop-down names it");
        Eq(Commands.RedactTool.Tip, "Mosaic redact (M)", "with its key");
        Eq(Commands.Color.Tip, "Color", "the color tooltip");
        Eq(Commands.Close.Tip, "Close (Esc)", "the close tooltip");
    }

    [Fact]
    public void AToolKeyPressedAgainKeepsItsTool()
    {
        _run.Open(R(100, 100, 400, 300));
        _run.Keystroke(Key.P);
        Eq(App.Tools.CurrentTool, MarkKind.Pen, "P picks the pen");
        _run.Keystroke(Key.P);
        Eq(App.Tools.CurrentTool, MarkKind.Pen, "pressed again, it stays on the pen");
        _run.Keystroke(Key.H);
        Eq(App.Tools.CurrentTool, MarkKind.Highlighter, "H picks the highlighter");
    }

    [Fact]
    public void TheReviewToolbarReadsAsItDid()
    {
        Eq(Commands.Speed.Tip, "Playback speed (S)", "the speed tooltip");
        Eq(Commands.SaveClip.Tip, "Save as GIF (Ctrl+S)", "the save tooltip");
        Eq(Commands.Play.Tip, "Play (Space)", "the play tooltip, with nothing playing");
    }

    [Fact]
    public void CaptureStartsFromTheModesThatCanGiveOneUp()
    {
        Assert.True(Commands.Capture.Enabled(), "a capture may start while idle");
        App.CaptureService.StartCapture(CaptureKind.Frozen);
        Assert.True(Commands.Capture.Enabled(), "or while selecting");
        _run.Drag(P(100, 100), P(400, 300));
        Assert.True(Commands.Capture.Enabled(), "or while annotating");
        _run.Keystroke(Key.Escape);

        _run.Record(R(100, 100, 400, 300));
        Eq(App.Mode, Mode.Reviewing, "a recording opens its review");
        Assert.True(Commands.Capture.Enabled(), "and a capture may start from the review too, as from a screenshot");
        Commands.Run(Commands.Capture);
        Eq(App.Mode, Mode.Selecting, "giving the review up for the new capture");
        _run.Keystroke(Key.Escape);

        _run.Start(CaptureKind.Recording, R(100, 100, 400, 300));
        Eq(App.Mode, Mode.Recording, "a live drag starts a recording");
        Assert.False(Commands.Capture.Enabled(), "and a capture may not start while it runs");
        Assert.True(Commands.Record.Enabled(), "though the record command may, because it stops it");
        _run.Keystroke(Key.Escape);

        _run.Start(CaptureKind.Timed, R(100, 100, 400, 300));
        Eq(App.Mode, Mode.CountingDown, "a timed drag counts down");
        Assert.False(Commands.Capture.Enabled(), "and no capture starts while it does");
        Assert.True(Commands.TimedCapture.Enabled(), "though the timed capture command may, because it cancels the countdown");
        Eq(Commands.TimedCapture.Tip, "Cancel (Ctrl+Alt+T)", "which is what the countdown's label says");
        Commands.Run(Commands.TimedCapture);
        Eq(App.Mode, Mode.Idle, "and does");
    }

    [Fact]
    public void ARightClickCancelsSelectingAndNothingElse()
    {
        App.CaptureService.StartCapture(CaptureKind.Frozen);
        Commands.RightClick();
        Eq(App.Mode, Mode.Idle, "a right-click while selecting gives the capture up, as Escape does");

        _run.Open(R(100, 100, 400, 300));
        Commands.RightClick();
        Eq(App.Mode, Mode.Annotating, "and a right-click on an open capture changes nothing");
    }

    [Fact]
    public void TheTextSizeKeysStepTheSize()
    {
        Eq(In(Mode.Annotating, Key.Plus, ctrl: true), "text-bigger", "Ctrl+Plus makes text bigger");
        Eq(In(Mode.Annotating, Key.Minus, ctrl: true), "text-smaller", "Ctrl+Minus makes it smaller");
        Eq(In(Mode.Annotating, Key.Plus, ctrl: true, shift: true), "text-bigger", "Shift may be down, as some keyboards need it for Plus");
        Eq(In(Mode.Annotating, Key.Plus), "", "Plus on its own asks for nothing");
        Eq(Chord.KeyOf(Keys.NumpadPlus), Key.Plus, "the number pad's plus is Plus");
        Eq(Chord.KeyOf(Keys.NumpadMinus), Key.Minus, "and its minus is Minus");
        Eq(Chord.VirtualKeyOf(Key.Plus), Keys.Plus, "while Plus stands for the main keyboard's plus key");
    }

    [Fact]
    public void OnlyTheTextSizeKeysAnswerWhileTyping()
    {
        _run.Open(R(100, 100, 400, 300));
        Assert.True(Commands.RunTypingKey(new Chord(Key.Plus, Ctrl: true, Shift: false)), "Ctrl+Plus answers while typing");
        Eq(App.Tools.TextSize, TextSize.Large, "and makes the text a size bigger");
        Assert.False(Commands.RunTypingKey(new Chord(Key.A, Ctrl: false, Shift: false)), "a letter is left to be typed");
        Assert.False(Commands.RunTypingKey(new Chord(Key.Z, Ctrl: true, Shift: false)), "and so is Ctrl+Z, which the box undoes itself");
    }

    [Fact]
    public void TheScreenshotCommandsWantAnAreaToActOn()
    {
        Command[] acting = [Commands.Copy, Commands.Save, Commands.SaveToHistory, Commands.Delete];
        foreach (Command c in acting)
        {
            Assert.False(c.Enabled(), $"{c.Name} has nothing to act on while idle");
        }

        _run.Open(R(100, 100, 400, 300));
        foreach (Command c in acting)
        {
            Assert.True(c.Enabled(), $"{c.Name} acts on the area being annotated");
        }
        Assert.False(Commands.SaveClip.Enabled(), "and saving a clip means nothing over a screenshot");
    }

    [Fact]
    public void AGlobalShortcutNeverMatchesAKeyTheWindowsReceive()
    {
        Assert.False(Commands.Capture.Keys[0].Matches(new Chord(Key.C, Ctrl: true, Shift: false)), "Ctrl+C in the app's windows does not match the Ctrl+Alt+C capture shortcut");
    }

    [Fact]
    public void AShortcutAnotherAppHoldsIsLeftOutOfItsLabels()
    {
        Command capture = Commands.Capture;
        capture.IsHotkeyTaken = true;
        Eq(capture.ShownKey, "", "a taken hotkey has no key to show");
        Eq(capture.Tip, "Capture", "so its tooltip names none");
    }

    private string In(Mode mode, Key k, bool ctrl = false, bool shift = false)
    {
        return Command.Find(Commands.For(mode), new Chord(k, ctrl, shift))?.Name ?? "";
    }
}
