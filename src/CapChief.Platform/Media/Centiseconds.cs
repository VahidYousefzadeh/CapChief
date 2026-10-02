namespace CapChief.Media;

/// <summary>
/// Converts elapsed milliseconds into GIF frame delays measured in hundredths of a second.
/// </summary>
public struct Centiseconds
{
    private int _owed;

    public int Next(int elapsedMs)
    {
        int ms = Math.Max(0, elapsedMs) + _owed;
        _owed = ms % 10;
        return ms / 10;
    }
}
