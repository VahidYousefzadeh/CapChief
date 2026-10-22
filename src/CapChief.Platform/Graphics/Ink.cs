namespace CapChief.Graphics;

/// <summary>
/// Represents the ink of a highlighter stroke, drawn once and white where the stroke is not, and the
/// box it covers around the stroke's points.
/// </summary>
public sealed class Ink
{
    public Surface Pixels { get; } = new();

    public Rect Box { get; set; }

    public void Free()
    {
        Pixels.Free();
        Box = default;
    }
}
