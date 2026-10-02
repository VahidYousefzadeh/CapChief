namespace CapChief.Geometry;

/// <summary>
/// Specifies which sides of a rectangle a resize operation moves.
/// </summary>
[Flags]
public enum Edges
{
    None = 0,
    West = 1,
    East = 2,
    North = 4,
    South = 8,
}

/// <summary>
/// Provides helpers for testing which sides an Edges value includes.
/// </summary>
public static class EdgeSides
{
    public static bool IsWest(this Edges e)
    {
        return (e & Edges.West) != 0;
    }

    public static bool IsEast(this Edges e)
    {
        return (e & Edges.East) != 0;
    }

    public static bool IsNorth(this Edges e)
    {
        return (e & Edges.North) != 0;
    }

    public static bool IsSouth(this Edges e)
    {
        return (e & Edges.South) != 0;
    }
}
