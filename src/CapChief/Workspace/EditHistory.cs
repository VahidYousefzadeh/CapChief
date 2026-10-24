using System.Diagnostics.CodeAnalysis;

namespace CapChief.Workspace;

/// <summary>
/// Stores the changes the user made to the open capture, in the order they were made, so each one
/// can be undone and redone.
/// </summary>
public sealed class EditHistory
{
    private readonly List<Edit> _edits = new();
    private int _done;
    private bool _merging;

    private bool CanUndo
    {
        get
        {
            return _done > 0;
        }
    }

    public bool CanRedo
    {
        get
        {
            return _done < _edits.Count;
        }
    }

    public IEnumerable<Edit> Edits
    {
        get
        {
            return _edits;
        }
    }

    public void Record(Edit edit)
    {
        DropRedo();
        if (_merging && _done > 0 && _edits[_done - 1] is IMergingEdit last && last.TryMergeWith(edit, out Edit? merged))
        {
            _edits[_done - 1] = merged;
            return;
        }
        _edits.Add(edit);
        _done++;
        _merging = true;
    }

    public void StopMerging()
    {
        _merging = false;
    }

    public bool Undo(Document document)
    {
        if (!CanUndo)
        {
            return false;
        }
        _merging = false;
        _edits[--_done].Undo(document);
        return true;
    }

    public bool Redo(Document document)
    {
        if (!CanRedo)
        {
            return false;
        }
        _merging = false;
        _edits[_done++].Do(document);
        return true;
    }

    public void Clear()
    {
        _edits.Clear();
        _done = 0;
        _merging = false;
    }

    private void DropRedo()
    {
        if (CanRedo)
        {
            _edits.RemoveRange(_done, _edits.Count - _done);
        }
    }
}

/// <summary>
/// Represents one change to the open capture that can be made again and taken back.
/// </summary>
public abstract record Edit
{
    public abstract void Do(Document document);

    public abstract void Undo(Document document);
}

/// <summary>
/// Defines a change that merges the next one into itself, so a run undoes as one step.
/// </summary>
public interface IMergingEdit
{
    bool TryMergeWith(Edit next, [NotNullWhen(true)] out Edit? merged);
}

/// <summary>
/// Represents a mark added to the open capture at a position in the drawing order.
/// </summary>
public sealed record AddMarkEdit(int Index, Mark Mark) : Edit
{
    public override void Do(Document document)
    {
        document.InsertMark(Index, Mark);
    }

    public override void Undo(Document document)
    {
        document.RemoveMark(Index);
    }
}

/// <summary>
/// Represents a mark removed from the open capture, kept so it can be put back where it was.
/// </summary>
public sealed record RemoveMarkEdit(int Index, Mark Mark) : Edit
{
    public override void Do(Document document)
    {
        document.RemoveMark(Index);
    }

    public override void Undo(Document document)
    {
        document.InsertMark(Index, Mark);
    }
}

/// <summary>
/// Represents a mark replaced by a changed copy of itself, such as moved, resized or recolored.
/// </summary>
public sealed record ChangeMarkEdit(int Index, Mark Before, Mark After, Merge Merge = Merge.None) : Edit, IMergingEdit
{
    public override void Do(Document document)
    {
        document.ReplaceMark(Index, After);
    }

    public override void Undo(Document document)
    {
        document.ReplaceMark(Index, Before);
    }

    public bool TryMergeWith(Edit next, [NotNullWhen(true)] out Edit? merged)
    {
        merged = null;
        if (Merge == Merge.None || next is not ChangeMarkEdit change || change.Merge != Merge || change.Index != Index)
        {
            return false;
        }
        merged = this with { After = change.After };
        return true;
    }
}

/// <summary>
/// Represents the selected area moved or resized.
/// </summary>
public sealed record MoveCropEdit(Crop From, Crop To, Merge Merge = Merge.None) : Edit, IMergingEdit
{
    public override void Do(Document document)
    {
        document.MoveCrop(To);
    }

    public override void Undo(Document document)
    {
        document.MoveCrop(From);
    }

    public bool TryMergeWith(Edit next, [NotNullWhen(true)] out Edit? merged)
    {
        merged = null;
        if (Merge == Merge.None || next is not MoveCropEdit move || move.Merge != Merge)
        {
            return false;
        }
        merged = this with { To = move.To };
        return true;
    }
}

/// <summary>
/// Represents the ends of a recording trimmed, with the first and last frames it kept before and
/// after.
/// </summary>
public sealed record TrimClipEdit(int FromFirst, int FromLast, int ToFirst, int ToLast) : Edit
{
    public override void Do(Document document)
    {
        document.SetTrim(ToFirst, ToLast);
    }

    public override void Undo(Document document)
    {
        document.SetTrim(FromFirst, FromLast);
    }
}

/// <summary>
/// Represents the playback speed of a recording changed.
/// </summary>
public sealed record ClipSpeedEdit(ClipSpeed From, ClipSpeed To) : Edit
{
    public override void Do(Document document)
    {
        document.SetSpeed(To);
    }

    public override void Undo(Document document)
    {
        document.SetSpeed(From);
    }
}

/// <summary>
/// Specifies which kind of repeated change merges into a single undo step.
/// </summary>
public enum Merge
{
    None,
    Recolor,
    Nudge,
    Resize,
}
