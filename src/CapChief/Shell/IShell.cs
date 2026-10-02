namespace CapChief.Shell;

/// <summary>
/// Defines what the services ask of the windows around them: telling the user what happened, the
/// save dialog, the clipboard, and keeping the app's own windows out of a screenshot.
/// </summary>
public interface IShell
{
    bool IsDialogOpen { get; }

    void OnSaved(Capture capture, string path);

    void OnClipSaved(string path);

    void OnCopied(Capture capture);

    void OnSavedToHistory(Capture capture);

    void OnFailed(Failure failure);

    string? AskSavePath(SaveKind kind);

    bool CopyToClipboard(Surface image);

    void HideForScreenshot();
}

/// <summary>
/// Specifies an action that could not be completed, such as saving an image or starting a
/// recording.
/// </summary>
public enum Failure
{
    SaveImage,
    CopyToClipboard,
    SaveRecording,
    StartRecording,
    WriteRecording,
    ReadRecording,
    TakeScreenshot,
    ShowRecording,
}
