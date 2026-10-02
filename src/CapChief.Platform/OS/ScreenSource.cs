namespace CapChief.OS;

/// <summary>
/// Represents a source of screen images: the whole desktop, or one area with the pointer.
/// </summary>
public abstract class ScreenSource
{
    /// <summary>
    /// Copies the whole desktop into dst, or returns false when the screen cannot be read, as while
    /// Windows shows the secure desktop.
    /// </summary>
    public abstract bool Capture(Surface dst);

    public abstract void CaptureWithPointer(Surface dst, Rect area);

    public abstract int ElapsedMs(long from, long now);
}
