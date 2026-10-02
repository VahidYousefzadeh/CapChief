namespace CapChief.Input;

/// <summary>
/// Specifies the grip being dragged: one of the eight resize grips, or the whole shape.
/// </summary>
public enum Grip
{
    None,
    NW,
    N,
    NE,
    E,
    SE,
    S,
    SW,
    W,
    Move,
}

/// <summary>
/// Provides helpers for locating the grips of a rectangle and choosing the cursor that matches one.
/// </summary>
public static class RectGrips
{
    public const int HitMargin = 2;

    public static readonly Grip[] All =
        [Grip.NW, Grip.N, Grip.NE, Grip.E, Grip.SE, Grip.S, Grip.SW, Grip.W];

    public static Edges EdgesOf(this Grip g)
    {
        switch (g)
        {
            case Grip.NW:
                return Edges.West | Edges.North;
            case Grip.N:
                return Edges.North;
            case Grip.NE:
                return Edges.East | Edges.North;
            case Grip.E:
                return Edges.East;
            case Grip.SE:
                return Edges.East | Edges.South;
            case Grip.S:
                return Edges.South;
            case Grip.SW:
                return Edges.West | Edges.South;
            case Grip.W:
                return Edges.West;
            default:
                return Edges.None;
        }
    }

    public static Point GripCenter(this in Rect r, Grip g)
    {
        return r.AnchorOf(g.EdgesOf());
    }

    public static Rect GripRect(this in Rect r, Grip g, int gripRadius)
    {
        return Rect.Around(r.GripCenter(g), gripRadius);
    }

    public static Cursor CursorFor(this Grip g)
    {
        switch (g)
        {
            case Grip.NW or Grip.SE:
                return Cursor.SizeNWSE;
            case Grip.NE or Grip.SW:
                return Cursor.SizeNESW;
            case Grip.N or Grip.S:
                return Cursor.SizeNS;
            case Grip.W or Grip.E:
                return Cursor.SizeWE;
            case Grip.Move:
                return Cursor.SizeAll;
            default:
                return Cursor.Arrow;
        }
    }
}

/// <summary>
/// Specifies the mouse cursor shapes the app can display.
/// </summary>
public enum Cursor
{
    Arrow,
    Draw,
    Hand,
    Cross,
    SizeNWSE,
    SizeNESW,
    SizeNS,
    SizeWE,
    SizeAll,
}
