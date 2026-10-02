namespace CapChief.OS;

/// <summary>
/// Provides helpers for reading whether the Shift, Ctrl and Alt keys are currently pressed.
/// </summary>
public static class Keyboard
{
    public static bool ShiftDown
    {
        get
        {
            return IsDown(Win32.VK_SHIFT);
        }
    }

    public static bool CtrlDown
    {
        get
        {
            return IsDown(Win32.VK_CONTROL);
        }
    }

    public static bool AltDown
    {
        get
        {
            return IsDown(Win32.VK_MENU);
        }
    }

    private static bool IsDown(int virtualKey)
    {
        return (Win32.GetKeyState(virtualKey) & 0x8000) != 0;
    }
}
