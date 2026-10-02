namespace CapChief.Graphics;

/// <summary>
/// Represents the pixel data of a bitmap together with its width, height, and pixels per row.
/// </summary>
public readonly ref struct Pixmap
{
    public Pixmap(Span<byte> pixels, int width, int height, int pixelsPerRow)
    {
        Pixels = pixels;
        Width = width;
        Height = height;
        PixelsPerRow = pixelsPerRow;
    }

    public Span<byte> Pixels { get; }

    public int Width { get; }

    public int Height { get; }

    public int PixelsPerRow { get; }

    public void CopyArea(Rect area, Span<byte> into)
    {
        for (int y = 0; y < area.Height; y++)
        {
            Pixels.Slice(((area.Top + y) * PixelsPerRow + area.Left) * 4, area.Width * 4).CopyTo(into.Slice(y * area.Width * 4));
        }
    }
}
