namespace CapChief.Windowing;

/// <summary>
/// Represents the two parameters that arrive with an application-defined window message.
/// </summary>
public readonly struct AppSignal
{
    private readonly IntPtr _a;
    private readonly IntPtr _b;

    public AppSignal(IntPtr a, IntPtr b)
    {
        _a = a;
        _b = b;
    }

    public Point Where
    {
        get
        {
            return Win32.PointOf(_a);
        }
    }

    public int Code
    {
        get
        {
            return Win32.LoWord(_b);
        }
    }
}
