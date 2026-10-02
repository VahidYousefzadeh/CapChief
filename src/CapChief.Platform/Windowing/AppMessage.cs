namespace CapChief.Windowing;

/// <summary>
/// Provides helpers for creating the identifiers of application-defined window messages.
/// </summary>
public static class AppMessage
{
    public static uint Id(int n)
    {
        return Win32.WM_APP + (uint)n;
    }

    public static uint Named(string name)
    {
        return Win32.RegisterWindowMessageW(name);
    }
}
