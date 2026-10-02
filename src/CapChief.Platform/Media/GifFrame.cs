namespace CapChief.Media;

/// <summary>
/// Represents one frame of a GIF file: its byte offset, length and display delay.
/// </summary>
public readonly record struct GifFrame(long Offset, int Length, int DelayCs)
{
    public static List<int> DelaysOf(IReadOnlyList<GifFrame> index)
    {
        var cs = new List<int>(index.Count);
        for (int i = 0; i < index.Count; i++)
        {
            cs.Add(index[i].DelayCs);
        }
        return cs;
    }
}
