namespace CapChief.Windowing;

/// <summary>
/// Represents the title, bounds and behavior options used to create a window.
/// </summary>
public sealed record WindowSpec
{
    public string Title { get; init; } = "";

    public Rect Bounds { get; init; }

    public bool AlwaysOnTop { get; init; }

    public bool TakesFocus { get; init; } = true;

    public bool ShowsInTaskbar { get; init; } = true;

    public bool PerPixelAlpha { get; init; }

    public bool ClickThrough { get; init; }
}
