using static CapChief.Tests.Fixtures;

namespace CapChief.Tests;

/// <summary>
/// Tests that changes to the history fold into one step, and that a new change drops what could have
/// been redone.
/// </summary>
public sealed class EditHistoryTests
{
    private readonly EditHistory _history = new();
    private readonly Document _document = new();

    private int Edits
    {
        get
        {
            return _history.Edits.Count();
        }
    }

    [Fact]
    public void ARunOfRecolorsIsOneStep()
    {
        Record(Change(0, 10, 20, Merge.Recolor), Change(0, 20, 30, Merge.Recolor));
        Eq(Edits, 1, "two recolors of one mark fold into one step");
        var edit = (ChangeMarkEdit)_history.Edits.Single();
        Eq(edit.Before.Bounds, R(0, 0, 10, 10), "which goes back to where the run started");
        Eq(edit.After.Bounds, R(0, 0, 30, 30), "and forward to where it ended");
    }

    [Fact]
    public void OnlyTheSameChangeToTheSameMarkFolds()
    {
        Record(Change(0, 10, 20, Merge.Recolor), Change(0, 20, 30, Merge.Nudge));
        Eq(Edits, 2, "a nudge does not fold into a recolor");

        Record(Change(1, 20, 30, Merge.Nudge));
        Eq(Edits, 3, "a nudge of another mark does not fold in either");

        Record(Change(1, 30, 40), Change(1, 40, 50, Merge.Nudge));
        Eq(Edits, 5, "a plain change between two nudges closes the first run");

        _history.StopMerging();
        Record(Change(1, 50, 60, Merge.Nudge));
        Eq(Edits, 6, "and so does StopMerging");
    }

    [Fact]
    public void NudgesOfTheAreaFoldButNotIntoNudgesOfAMark()
    {
        Record(MoveCrop(0, 5, Merge.Nudge), MoveCrop(5, 10, Merge.Nudge));
        Eq(Edits, 1, "a held arrow key moves the area as one step");
        Eq(((MoveCropEdit)_history.Edits.Single()).To, R(10, 0, 20, 10), "to where the run ended");

        Record(MoveCrop(10, 20), MoveCrop(20, 30));
        Eq(Edits, 3, "two drags stay two steps");

        Record(Change(0, 10, 20, Merge.Nudge), MoveCrop(30, 40, Merge.Nudge), Change(0, 20, 30, Merge.Nudge));
        Eq(Edits, 6, "a nudge of the area and a nudge of a mark never fold into each other");
    }

    [Fact]
    public void ANewChangeDropsTheRedo()
    {
        Record(Change(0, 10, 20), Change(0, 20, 30));
        Assert.True(_history.Undo(_document), "a step undoes");
        Assert.True(_history.CanRedo, "and can be redone");

        Record(Change(0, 20, 40));
        Assert.False(_history.CanRedo, "until a new change is made");
        Eq(Edits, 2, "which takes the undone step's place");
    }

    [Fact]
    public void ARedoneStepIsClosedToFolding()
    {
        Record(Change(0, 10, 20, Merge.Recolor));
        _history.Undo(_document);
        _history.Redo(_document);
        Record(Change(0, 20, 30, Merge.Recolor));
        Eq(Edits, 2, "a recolor after a redo is a step of its own");
    }

    private static ChangeMarkEdit Change(int index, int from, int to, Merge merge = Merge.None)
    {
        return new(index, Rectangle(0, 0, from, from), Rectangle(0, 0, to, to), merge);
    }

    private static MoveCropEdit MoveCrop(int from, int to, Merge merge = Merge.None)
    {
        return new(CropOf(from, 0, from + 10, 10), CropOf(to, 0, to + 10, 10), merge);
    }

    private void Record(params Edit[] edits)
    {
        foreach (Edit edit in edits)
        {
            _history.Record(edit);
        }
    }
}
