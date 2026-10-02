namespace CapChief.Graphics;

/// <summary>
/// Represents a GDI font created at a given pixel size and weight.
/// </summary>
public readonly struct TextFont
{
    private const string Family = "Segoe UI";

    private const string SemiboldFamily = "Segoe UI Semibold";

    private readonly IntPtr _h;

    private TextFont(IntPtr h)
    {
        _h = h;
    }

    public static implicit operator IntPtr(TextFont f)
    {
        return f._h;
    }

    public bool Exists
    {
        get
        {
            return _h != IntPtr.Zero;
        }
    }

    public static TextFont Sharp(int px, int weight)
    {
        return Create(px, weight, Family, Win32.CLEARTYPE_QUALITY);
    }

    public static TextFont SharpSemibold(int px, int weight)
    {
        return Create(px, weight, SemiboldFamily, Win32.CLEARTYPE_QUALITY);
    }

    public static TextFont Smooth(int px, int weight)
    {
        return Create(px, weight, Family, Win32.ANTIALIASED_QUALITY);
    }

    public static TextFont SmoothSemibold(int px, int weight)
    {
        return Create(px, weight, SemiboldFamily, Win32.ANTIALIASED_QUALITY);
    }

    public void Free()
    {
        if (_h != IntPtr.Zero)
        {
            Win32.DeleteObject(_h);
        }
    }

    private static TextFont Create(int px, int weight, string family, int quality)
    {
        return new(Win32.CreateFontW(-px, 0, 0, 0, weight, 0, 0, 0, (uint)Win32.DEFAULT_CHARSET, 0, 0, (uint)quality, 0, family));
    }
}

/// <summary>
/// Defines the numeric weights of semibold and bold text.
/// </summary>
public static class FontWeights
{
    public const int Semibold = 600;

    public const int Bold = 700;
}
