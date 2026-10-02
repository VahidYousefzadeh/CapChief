namespace CapChief.Capturing;

/// <summary>
/// Provides the actions that start a capture or a recording, and that save or copy one.
/// </summary>
public sealed class CaptureService
{
    private readonly Document _document;
    private readonly CaptureWorkflow _workflow;
    private readonly WorkspaceService _workspace;
    private readonly Review _review;
    private readonly MarkPainter _markPainter;
    private readonly IShell _shell;

    public CaptureService(Document document, CaptureWorkflow workflow, WorkspaceService workspace, Review review, MarkPainter markPainter, IShell shell)
    {
        _document = document;
        _workflow = workflow;
        _workspace = workspace;
        _review = review;
        _markPainter = markPainter;
        _shell = shell;
    }

    public bool CanStart
    {
        get
        {
            return _workflow.CurrentMode is Mode.Idle or Mode.Selecting or Mode.Annotating or Mode.Reviewing && !_shell.IsDialogOpen;
        }
    }

    public void StartCapture(CaptureKind kind)
    {
        if (_workflow.CurrentMode is Mode.Annotating or Mode.Reviewing)
        {
            _workspace.EndCapture();
        }
        _workflow.StartSelecting(kind);
    }

    public void StartOrCancelTimed()
    {
        if (_workflow.CurrentMode == Mode.CountingDown)
        {
            _workflow.CancelLive();
        }
        else
        {
            StartCapture(CaptureKind.Timed);
        }
    }

    public void StartOrStopRecording()
    {
        if (_workflow.CurrentMode == Mode.Recording)
        {
            _workflow.StopRecording();
        }
        else
        {
            StartCapture(CaptureKind.Recording);
        }
    }

    public void Save()
    {
        if (_document.Capture is not { } capture || _shell.AskSavePath(SaveKind.Png) is not { } path)
        {
            return;
        }
        using var image = new Surface();
        if (!capture.Render(image, _markPainter) || !Export.ToFile(image, path))
        {
            _shell.OnFailed(Failure.SaveImage);
            return;
        }
        _shell.OnSaved(capture, path);
        _workspace.EndCapture();
    }

    public void CopyToClipboard()
    {
        if (_document.Capture is not { } capture)
        {
            return;
        }
        using var image = new Surface();
        if (!capture.Render(image, _markPainter) || !_shell.CopyToClipboard(image))
        {
            _shell.OnFailed(Failure.CopyToClipboard);
            return;
        }
        _shell.OnCopied(capture);
        _workspace.EndCapture();
    }

    public void SaveClip()
    {
        if (_workflow.CurrentMode != Mode.Reviewing)
        {
            return;
        }
        _review.Pause();
        if (_shell.AskSavePath(SaveKind.Gif) is not { } path)
        {
            return;
        }
        if (!_review.SaveAs(path))
        {
            _shell.OnFailed(Failure.SaveRecording);
            return;
        }
        _workspace.EndCapture();
        _shell.OnClipSaved(path);
    }
}
