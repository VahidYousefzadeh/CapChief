namespace CapChief.Windowing;

/// <summary>
/// Provides the current position of the mouse pointer in screen coordinates.
/// </summary>
public static class MousePointer
{
    public static Point At
    {
        get
        {
            if (Win32.GetCursorPos(out Point p))
            {
                return p;
            }
            else
            {
                return default;
            }
        }
    }
}
