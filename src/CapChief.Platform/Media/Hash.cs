namespace CapChief.Media;

/// <summary>
/// Provides helpers for hashing an integer key into a table position.
/// </summary>
public static class Hash
{
    private const uint Knuth = 2654435761u;

    public static int Slot(uint key, int bits)
    {
        return (int)((key * Knuth) >> (32 - bits));
    }
}
