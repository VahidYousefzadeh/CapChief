namespace CapChief.Workspace;

/// <summary>
/// Represents one screenshot or recording: the frozen screen it was cut from, the area selected on it,
/// and the marks drawn over it or the clip recorded in it. A copy shares the frozen screen and the
/// clip's file with the capture it was copied from, so freeing either one frees them for both.
/// </summary>
public sealed class Capture
{
    public Capture(Surface picture, Crop crop)
    {
        Picture = picture;
        Crop = crop;
        Marks = new Marks();
    }

    private Capture(Capture from)
    {
        Picture = from.Picture;
        Crop = from.Crop;
        Marks = new Marks(from.Marks.All);
        Clip = from.Clip?.Copy();
    }

    public Surface Picture { get; }

    public Crop Crop { get; set; }

    public Rect CropBounds
    {
        get
        {
            return Clip?.Area ?? Picture.Bounds;
        }
    }

    public Marks Marks { get; }

    public Clip? Clip { get; init; }

    public Capture Copy()
    {
        return new(this);
    }

    public bool Render(Surface into, MarkPainter markPainter)
    {
        Rect crop = Crop;
        if (!into.OpenCompatible(crop.Width, crop.Height))
        {
            return false;
        }
        into.DeviceContext.Blit(into.Bounds, Picture.DeviceContext, crop.Left, crop.Top);
        markPainter.DrawCrop(into.DeviceContext, this);
        return true;
    }

    public void Free()
    {
        Picture.Free();
        Clip?.DeleteFile();
    }
}
