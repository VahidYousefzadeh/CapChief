namespace CapChief.Windowing;

/// <summary>
/// Represents a system-wide keyboard shortcut that can be registered with a window.
/// </summary>
public readonly struct Hotkey
{
    private readonly uint _modifiers;
    private readonly int _virtualKey;

    private Hotkey(uint modifiers, int virtualKey)
    {
        _modifiers = modifiers;
        _virtualKey = virtualKey;
    }

    public static Hotkey Of(int virtualKey, bool ctrl, bool alt, bool shift)
    {
        uint mods = Win32.MOD_NOREPEAT;
        if (ctrl)
        {
            mods |= Win32.MOD_CONTROL;
        }
        if (alt)
        {
            mods |= Win32.MOD_ALT;
        }
        if (shift)
        {
            mods |= Win32.MOD_SHIFT;
        }
        return new(mods, virtualKey);
    }

    public bool RegisterOn(IntPtr window, int id)
    {
        return Win32.RegisterHotKey(window, id, _modifiers, (uint)_virtualKey);
    }
}
