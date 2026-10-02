namespace CapChief.Graphics;

/// <summary>
/// Provides the text formatting flags used when drawing wrapped and single-line text.
/// </summary>
public static class TextLayout
{
    public const uint Wrap = Win32.DT_WORDBREAK | Win32.DT_NOPREFIX;

    public const uint Line = Win32.DT_SINGLELINE | Win32.DT_NOPREFIX;
}
