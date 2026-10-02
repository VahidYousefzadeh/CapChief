namespace CapChief.Workspace;

/// <summary>
/// Represents the capture open for editing, the mark selected on it, and the history of changes that
/// can be undone and redone.
/// </summary>
public sealed class Document
{
    private readonly EditHistory _history = new();

    public event Action<Mark>? MarkAdded;
    public event Action<Mark>? MarkRemoved;
    public event Action<Mark, Mark>? MarkReplaced;
    public event Action<Mark?, Mark?>? SelectedMarkChanged;
    public event Action<Crop, Crop>? CropMoved;
    public event Action<Marks, int>? StepsRenumbered;
    public event Action? ClipEdited;
    public event Action? Changed;

    public Capture? Capture { get; private set; }

    public int SelectedIndex { get; private set; } = -1;

    public Mark? SelectedMark
    {
        get
        {
            return MarkAt(SelectedIndex);
        }
    }

    public bool HasSelectedMark
    {
        get
        {
            return SelectedMark is not null;
        }
    }

    public int NextStepNumber
    {
        get
        {
            return Capture?.Marks.NextStepNumber ?? 1;
        }
    }

    public void Open(Capture capture)
    {
        Capture = capture;
        SelectedIndex = -1;
        _history.Clear();
        Changed?.Invoke();
    }

    public void Close()
    {
        Capture = null;
        SelectedIndex = -1;
        _history.Clear();
        Changed?.Invoke();
    }

    public void AddMark(Mark mark)
    {
        if (Capture is not { } capture)
        {
            return;
        }
        int index = capture.Marks.Count;
        InsertMark(index, mark);
        Record(new AddMarkEdit(index, mark));
    }

    public void DeleteMark(int index)
    {
        if (MarkAt(index) is not { } mark)
        {
            return;
        }
        RemoveMark(index);
        Record(new RemoveMarkEdit(index, mark));
    }

    public void RecolorMark(int index, Color color)
    {
        if (MarkAt(index) is { TakesColor: true } before && before.Color != color)
        {
            ChangeMark(index, before with { Color = color }, Merge.Recolor);
        }
    }

    public void ChangeMark(int index, Mark mark, Merge merge = Merge.None)
    {
        if (MarkAt(index) is not { } before)
        {
            return;
        }
        ReplaceMark(index, mark);
        Record(new ChangeMarkEdit(index, before, mark, merge));
    }

    public void NudgeMark(int index, Point offset)
    {
        if (MarkAt(index) is { } before)
        {
            ChangeMark(index, before.Moved(offset), Merge.Nudge);
        }
    }

    public void ChangeCrop(Crop to, Merge merge = Merge.None)
    {
        if (Capture is not { } capture)
        {
            return;
        }
        Crop from = capture.Crop;
        MoveCrop(to);
        Record(new MoveCropEdit(from, to, merge));
    }

    public void NudgeCrop(Crop to)
    {
        ChangeCrop(to, Merge.Nudge);
    }

    public void TrimClip(int first, int last)
    {
        if (Capture?.Clip is not { } clip)
        {
            return;
        }
        int wasFirst = clip.In;
        int wasLast = clip.Out;
        SetTrim(first, last);
        RecordTrim(wasFirst, wasLast);
    }

    public void RecordTrim(int wasFirst, int wasLast)
    {
        if (Capture?.Clip is not { } clip || clip.In == wasFirst && clip.Out == wasLast)
        {
            return;
        }
        Record(new TrimClipEdit(wasFirst, wasLast, clip.In, clip.Out));
    }

    public void ChangeClipSpeed(ClipSpeed speed)
    {
        if (Capture?.Clip is not { } clip || clip.Speed == speed)
        {
            return;
        }
        ClipSpeed was = clip.Speed;
        SetSpeed(speed);
        Record(new ClipSpeedEdit(was, speed));
    }

    public void SetTrim(int first, int last)
    {
        if (Capture?.Clip is not { } clip)
        {
            return;
        }
        clip.Trim(first, last);
        ClipEdited?.Invoke();
        Changed?.Invoke();
    }

    public void SetSpeed(ClipSpeed speed)
    {
        if (Capture?.Clip is not { } clip)
        {
            return;
        }
        clip.SetSpeed(speed);
        ClipEdited?.Invoke();
        Changed?.Invoke();
    }

    public void InsertMark(int index, Mark mark)
    {
        if (Capture is not { } capture)
        {
            return;
        }
        capture.Marks.Insert(index, mark);
        MarkAdded?.Invoke(mark);
        if (mark is StepMark)
        {
            StepsRenumbered?.Invoke(capture.Marks, index + 1);
        }
        Changed?.Invoke();
    }

    public void RemoveMark(int index)
    {
        if (Capture is not { } capture || !capture.Marks.HasIndex(index))
        {
            return;
        }
        Mark mark = capture.Marks[index];
        capture.Marks.RemoveAt(index);
        if (SelectedIndex == index)
        {
            SelectedIndex = -1;
        }
        else if (SelectedIndex > index)
        {
            SelectedIndex--;
        }
        MarkRemoved?.Invoke(mark);
        if (mark is StepMark)
        {
            StepsRenumbered?.Invoke(capture.Marks, index);
        }
        Changed?.Invoke();
    }

    public void ReplaceMark(int index, Mark mark)
    {
        if (Capture is not { } capture || !capture.Marks.HasIndex(index))
        {
            return;
        }
        Mark was = capture.Marks[index];
        capture.Marks.Replace(index, mark);
        MarkReplaced?.Invoke(was, mark);
        Changed?.Invoke();
    }

    public void MoveCrop(Crop to)
    {
        if (Capture is not { } capture)
        {
            return;
        }
        Crop was = capture.Crop;
        capture.Crop = to;
        Point offset = was.OffsetTo(to);
        if (offset != default)
        {
            for (int i = 0; i < capture.Marks.Count; i++)
            {
                ReplaceMark(i, capture.Marks[i].Moved(offset));
            }
        }
        CropMoved?.Invoke(was, to);
        Changed?.Invoke();
    }

    public void Select(int index)
    {
        if (Capture is not { } capture)
        {
            return;
        }
        int was = SelectedIndex;
        if (capture.Marks.HasIndex(index))
        {
            SelectedIndex = index;
        }
        else
        {
            SelectedIndex = -1;
        }
        if (was == SelectedIndex)
        {
            return;
        }

        StopMerging();
        SelectedMarkChanged?.Invoke(MarkAt(was), SelectedMark);
        Changed?.Invoke();
    }

    public void Undo()
    {
        _history.Undo(this);
        Changed?.Invoke();
    }

    public void Redo()
    {
        _history.Redo(this);
        Changed?.Invoke();
    }

    public void StopMerging()
    {
        _history.StopMerging();
    }

    private void Record(Edit edit)
    {
        _history.Record(edit);
        Changed?.Invoke();
    }

    private Mark? MarkAt(int index)
    {
        if (Capture is { } capture && capture.Marks.HasIndex(index))
        {
            return capture.Marks[index];
        }
        else
        {
            return null;
        }
    }
}
