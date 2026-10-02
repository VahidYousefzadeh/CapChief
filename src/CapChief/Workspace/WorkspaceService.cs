namespace CapChief.Workspace;

/// <summary>
/// Provides the editing actions on the open capture and the saved captures, such as delete, save to
/// history, undo, recolor, ending a capture, and reopening a saved capture.
/// </summary>
public sealed class WorkspaceService
{
    private readonly Document _document;
    private readonly Tools _tools;
    private readonly CaptureHistory _history;
    private readonly CaptureWorkflow _workflow;
    private readonly IShell _shell;

    public WorkspaceService(Document document, Tools tools, CaptureHistory history, CaptureWorkflow workflow, IShell shell)
    {
        _document = document;
        _tools = tools;
        _history = history;
        _workflow = workflow;
        _shell = shell;
    }

    private Mode CurrentMode
    {
        get
        {
            return _workflow.CurrentMode;
        }
    }

    private Capture? Capture
    {
        get
        {
            return _document.Capture;
        }
    }

    public bool HasCapture
    {
        get
        {
            return Capture is not null;
        }
    }

    public bool IsAnnotating
    {
        get
        {
            return CurrentMode == Mode.Annotating && Capture is not null;
        }
    }

    public bool HasSelectedMark
    {
        get
        {
            return CurrentMode == Mode.Annotating && _document.HasSelectedMark;
        }
    }

    public bool IsSavedToHistory
    {
        get
        {
            return Capture is { } capture && _history.IsUpToDate(capture);
        }
    }

    public bool HasHistory
    {
        get
        {
            return _history.Entries.Count > 0;
        }
    }

    public void EndCapture()
    {
        if (Capture is not { } capture)
        {
            return;
        }
        _workflow.Close();
        _history.FreeUnlessSaved(capture);
    }

    public bool OpenFromHistory(Capture entry)
    {
        if (CurrentMode != Mode.Idle || _shell.IsDialogOpen || _history.EntryOf(entry) is null)
        {
            return false;
        }
        return _workflow.Open(entry.Copy());
    }

    public void SaveToHistory()
    {
        if (Capture is not { } capture)
        {
            return;
        }
        _history.Save(capture);
        _shell.OnSavedToHistory(capture);
    }

    public void Delete()
    {
        if (Capture is not { } capture)
        {
            return;
        }
        if (_document.HasSelectedMark)
        {
            DeleteSelectedMark();
            return;
        }

        _workflow.Close();
        _history.Delete(capture);
    }

    public void DeleteSelectedMark()
    {
        if (!_document.HasSelectedMark)
        {
            return;
        }
        int at = _document.SelectedIndex;
        _document.Select(-1);
        _document.DeleteMark(at);
    }

    public bool DropSelectedMark()
    {
        if (!_document.HasSelectedMark)
        {
            return false;
        }
        _document.Select(-1);
        return true;
    }

    public void Nudge(Point offset)
    {
        if (Capture is not { } capture)
        {
            return;
        }
        if (_document.HasSelectedMark)
        {
            _document.NudgeMark(_document.SelectedIndex, offset);
            return;
        }

        Crop to = capture.Crop.Dragged(Grip.Move, offset, false, capture.CropBounds);
        if (to != capture.Crop)
        {
            _document.NudgeCrop(to);
        }
    }

    public void Undo()
    {
        PrepareForUndo();
        _document.Undo();
    }

    public void Redo()
    {
        PrepareForUndo();
        _document.Redo();
    }

    public void Free()
    {
        EndCapture();
        _history.Clear();
    }

    public void PickColor(int index)
    {
        if (_tools.SetColor(index))
        {
            Recolor();
        }
    }

    public void AdvanceColor(int delta)
    {
        if (_tools.AdvanceColor(delta))
        {
            Recolor();
        }
    }

    private void PrepareForUndo()
    {
        _document.StopMerging();
        _document.Select(-1);
    }

    private void Recolor()
    {
        if (IsAnnotating)
        {
            _document.RecolorMark(_document.SelectedIndex, _tools.Color);
        }
    }
}
