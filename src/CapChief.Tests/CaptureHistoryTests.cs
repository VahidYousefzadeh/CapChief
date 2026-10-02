using static CapChief.Tests.Fixtures;

namespace CapChief.Tests;

/// <summary>
/// Tests that a capture saved to the history opens again with its marks and its frozen screen,
/// changes only when it is saved again, and how long the history keeps it.
/// </summary>
public sealed class CaptureHistoryTests : IDisposable
{
    private readonly Headless _run = new();

    private App App
    {
        get
        {
            return _run.App;
        }
    }

    private IReadOnlyList<Capture> Entries
    {
        get
        {
            return App.History.Entries;
        }
    }

    public void Dispose()
    {
        _run.Dispose();
    }

    [Fact]
    public void SavingKeepsACopyAndLeavesTheCaptureOpen()
    {
        _run.Open(R(100, 100, 400, 300));
        _run.Draw(MarkKind.Rectangle, 120, 120, 220, 200);
        _run.Keystroke(Key.H, ctrl: true);
        Eq(App.Mode, Mode.Annotating, "saving to the history leaves the capture open");
        Eq(Entries.Count, 1, "and keeps one capture");
        Assert.False(ReferenceEquals(Entries[0], _run.Capture), "as a copy");
        Assert.True(ReferenceEquals(Entries[0].Picture, _run.Capture.Picture), "of the same frozen screen");
        Assert.False(App.Commands.SaveToHistory.Enabled(), "and the button waits for a change");

        _run.Draw(MarkKind.Rectangle, 250, 120, 350, 200);
        Assert.True(App.Commands.SaveToHistory.Enabled(), "a new mark is a change to save");
        Eq(Entries[0].Marks.Count, 1, "which stays out of the saved copy until it is saved");
        App.Workspace.Undo();
        Assert.False(App.Commands.SaveToHistory.Enabled(), "and undoing it leaves nothing new to save");

        _run.Keystroke(Key.Escape);
        Assert.True(Entries[0].Picture.IsOpen, "closing keeps the saved copy's frozen screen");
    }

    [Fact]
    public void ASavedCaptureOpensAgainAsACopy()
    {
        Capture saved = SaveNew(100);
        Assert.True(App.Workspace.OpenFromHistory(saved), "a saved capture opens again");
        Eq(App.Mode, Mode.Annotating, "in the editor");
        Assert.False(ReferenceEquals(App.Capture, saved), "as a copy");
        Assert.True(ReferenceEquals(_run.Capture.Picture, saved.Picture), "of the same frozen screen");
        Eq(_run.Capture.Crop, saved.Crop, "where it was");
        Eq(_run.Marks.Count, 1, "with its mark");
        Assert.False(App.Commands.SaveToHistory.Enabled(), "with nothing new to save");

        App.Workspace.Undo();
        Eq(_run.Marks.Count, 1, "and nothing to undo from before it was saved");
    }

    [Fact]
    public void ChangesReachTheHistoryOnlyWhenSavedAgain()
    {
        Capture saved = SaveNew(100);
        App.Workspace.OpenFromHistory(saved);
        _run.Draw(MarkKind.Rectangle, 150, 150, 250, 250);
        _run.Keystroke(Key.Escape);
        Eq(Entries[0].Marks.Count, 1, "Escape leaves the saved capture as it was");
        Assert.True(saved.Picture.IsOpen, "and its frozen screen kept");

        App.Workspace.OpenFromHistory(saved);
        _run.Draw(MarkKind.Rectangle, 150, 150, 250, 250);
        _run.Keystroke(Key.H, ctrl: true);
        Eq(Entries.Count, 1, "saving it again replaces it rather than adding one");
        Eq(Entries[0].Marks.Count, 2, "with the new mark");
        Assert.True(saved.Picture.IsOpen, "over the same frozen screen");
    }

    [Fact]
    public void SavingAgainPutsACaptureFirst()
    {
        Capture older = SaveNew(100);
        SaveNew(300);
        App.Workspace.OpenFromHistory(older);
        _run.Draw(MarkKind.Rectangle, 150, 150, 250, 250);
        _run.Keystroke(Key.H, ctrl: true);
        Assert.True(ReferenceEquals(Entries[0].Picture, older.Picture), "the capture saved last comes first");
        Eq(Entries.Count, 2, "and none is lost");
    }

    [Fact]
    public void TheHistoryKeepsTheTenNewest()
    {
        var saved = new List<Capture>();
        for (int i = 0; i < CaptureHistory.MaxEntries + 2; i++)
        {
            saved.Add(SaveNew(100 + i));
        }
        Eq(Entries.Count, CaptureHistory.MaxEntries, "the history keeps ten");
        Assert.True(ReferenceEquals(Entries[0], saved[^1]), "newest first");
        Assert.False(saved[0].Picture.IsOpen || saved[1].Picture.IsOpen, "and frees the two oldest");
    }

    [Fact]
    public void ARecordingSavedToTheHistoryOpensIntoItsReview()
    {
        _run.Record(R(100, 100, 400, 300));
        _run.Drag(P(400, 300), P(300, 250));
        App.Review.Advance(2);
        App.Review.Trim(head: true);
        _run.Keystroke(Key.H, ctrl: true);
        int savedOut = App.Review.Clip!.Out;
        Eq(App.Mode, Mode.Reviewing, "saving a recording to the history leaves its review open");
        Eq(Entries.Count, 1, "and keeps it");
        Assert.False(App.Commands.SaveToHistory.Enabled(), "and the button waits for a change");
        App.Review.Trim(head: false);
        Assert.True(App.Commands.SaveToHistory.Enabled(), "a new trim is a change to save");

        Capture saved = Entries[0];
        string? file = saved.Clip?.FilePath;
        _run.Keystroke(Key.Escape);
        Assert.True(file is not null && File.Exists(file), "closing the review keeps the saved recording's file");

        Assert.True(App.Workspace.OpenFromHistory(saved), "a saved recording opens again");
        Eq(App.Mode, Mode.Reviewing, "into its review");
        Eq(_run.Capture.Crop, R(100, 100, 300, 250), "with the area it was saved with");
        Eq(App.Review.Clip?.In, 2, "and its trim, as saved rather than as changed after");
        Eq(App.Review.Clip?.Out, savedOut, "at both ends");
        Assert.False(ReferenceEquals(App.Review.Clip, saved.Clip), "on a copy of it");
        Assert.False(App.Commands.SaveToHistory.Enabled(), "with nothing new to save");

        App.Commands.Run(App.Commands.Delete);
        Eq(App.Mode, Mode.Idle, "Delete ends the review");
        Eq(Entries.Count, 0, "takes the recording out of the history");
        Assert.False(File.Exists(file), "and deletes its file");
    }

    [Fact]
    public void DeletingAFreshRecordingDeletesItsFile()
    {
        _run.Record(R(100, 100, 400, 300));
        string? file = App.Review.Clip?.FilePath;
        App.Commands.Run(App.Commands.Delete);
        Eq(App.Mode, Mode.Idle, "Delete ends the review");
        Assert.False(file is null || File.Exists(file), "and deletes the recording");
        Eq(Entries.Count, 0, "which never reached the history");
    }

    [Fact]
    public void QuittingDeletesTheRecordingsInTheHistory()
    {
        _run.Record(R(100, 100, 400, 300));
        _run.Keystroke(Key.H, ctrl: true);
        _run.Keystroke(Key.Escape);
        string? file = Entries[0].Clip?.FilePath;
        App.Workspace.Free();
        Assert.False(file is null || File.Exists(file), "quitting deletes the file of a recording in the history");
    }

    [Fact]
    public void HidingToTheTrayKeepsTheHistoryAndQuittingFreesIt()
    {
        Capture saved = SaveNew(100);
        App.Commands.Run(App.Commands.HideToTray);
        Eq(Entries.Count, 1, "hiding to the tray keeps the history");
        App.Workspace.Free();
        Eq(Entries.Count, 0, "quitting empties it");
        Assert.False(saved.Picture.IsOpen, "and frees what it held");
    }

    private Capture SaveNew(int left)
    {
        _run.Open(R(left, 100, left + 300, 300));
        _run.Draw(MarkKind.Rectangle, left + 20, 120, left + 120, 200);
        _run.Keystroke(Key.H, ctrl: true);
        _run.Keystroke(Key.Escape);
        return Entries[0];
    }
}
