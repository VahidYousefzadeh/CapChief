namespace CapChief.Windowing;

/// <summary>
/// Represents a reference to a native edit control identified by its window handle.
/// </summary>
public readonly struct EditRef
{
    private readonly IntPtr _h;

    public EditRef(IntPtr h)
    {
        _h = h;
    }

    public bool Is(IntPtr other)
    {
        return _h == other && _h != IntPtr.Zero;
    }
}
