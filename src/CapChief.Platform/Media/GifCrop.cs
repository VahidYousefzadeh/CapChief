namespace CapChief.Media;

/// <summary>
/// Provides a method for copying an animated GIF cut down to a rectangle inside it, with every frame
/// encoded again from its full-color image.
/// </summary>
public static class GifCrop
{
    public static List<GifFrame>? Write(GifReader source, Rect area, Stream to, CancellationToken cancel = default)
    {
        var whole = new Rect { Right = source.Width, Bottom = source.Height };
        if (!source.IsOpen || area.IsEmpty || Rect.Intersect(area, whole) != area)
        {
            return null;
        }

        IReadOnlyList<GifFrame> index = source.Index;
        int w = area.Width;
        int h = area.Height;
        var gif = new GifWriter(to);
        var frames = new List<GifFrame>(index.Count);
        var cut = new byte[w * h * 4];
        for (int i = 0; i < index.Count; i++)
        {
            if (cancel.IsCancellationRequested || source.Composite(i) is not { } canvas)
            {
                return null;
            }
            new Pixmap(canvas, source.Width, source.Height, source.Width).CopyArea(area, cut);
            if (i == 0 && !gif.Begin(w, h, cut))
            {
                return null;
            }

            if (!gif.Frame(cut, index[i].DelayCs, out GifFrame frame))
            {
                return null;
            }
            frames.Add(frame);
        }
        if (gif.End())
        {
            return frames;
        }
        else
        {
            return null;
        }
    }
}
