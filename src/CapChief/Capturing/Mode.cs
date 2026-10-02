namespace CapChief.Capturing;

/// <summary>
/// Specifies what the user is doing, which decides what a key, a press and a hover mean.
/// </summary>
public enum Mode
{
    Idle,
    Selecting,
    CountingDown,
    Recording,
    Reviewing,
    Annotating,
}

/// <summary>
/// Specifies the kind of capture the user started: a screenshot, a delayed screenshot or a
/// recording.
/// </summary>
public enum CaptureKind
{
    Frozen,
    Timed,
    Recording,
}
