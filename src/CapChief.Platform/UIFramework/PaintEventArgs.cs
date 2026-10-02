namespace CapChief.UIFramework;

/// <summary>
/// Provides the drawing context to a control while it is being painted.
/// </summary>
public sealed class PaintEventArgs : EventArgs
{
    public PaintEventArgs(DrawContext draw)
    {
        Draw = draw;
    }

    public DrawContext Draw { get; }
}
