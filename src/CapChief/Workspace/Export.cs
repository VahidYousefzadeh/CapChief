namespace CapChief.Workspace;

/// <summary>
/// Provides methods for writing a rendered screenshot to a PNG file or to the clipboard.
/// </summary>
public static class Export
{
    public static bool ToFile(Surface image, string path)
    {
        int w = image.Width;
        int h = image.Height;
        if ((long)w * h * 4 > int.MaxValue || image.ReadPixels(image.Bounds) is not { } bgra)
        {
            return false;
        }
        try
        {
            using var file = new FileStream(path, FileMode.Create, FileAccess.Write);
            return Png.Write(file, bgra, w, h, w * 4);
        }
        catch (Exception e) when (Log.Tolerated("save the PNG", e))
        {
            return false;
        }
    }

    public static bool ToClipboard(Surface image, Window owner)
    {
        return image.Width > 0 && image.Height > 0 && Clipboard.Put(image, owner);
    }
}
