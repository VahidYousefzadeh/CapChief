namespace CapChief.UIFramework;

/// <summary>
/// Provides data for the mouse events raised by a control.
/// </summary>
public sealed class MouseEventArgs : EventArgs
{
    public MouseEventArgs(Point at)
    {
        Location = at;
    }

    public Point Location { get; }

    public bool Handled { get; set; }
}
