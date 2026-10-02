namespace CapChief.Shell;

/// <summary>
/// Provides the Save As dialog for writing a screenshot or recording to a file.
/// </summary>
public static class SaveDialog
{
    public static string? Ask(Window owner, SaveKind kind)
    {
        FileType fileType = FileTypeOf(kind);
        string name = $"{Product.FilePrefix}{DateTime.Now:yyyy-MM-dd_HHmmss}.{fileType.Ext}";
        string pics = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        return SaveFileDialog.Ask(owner, fileType.Filter, fileType.Title, fileType.Ext, name, pics);
    }

    private static FileType FileTypeOf(SaveKind kind)
    {
        if (kind == SaveKind.Gif)
        {
            return new FileType("GIF animation\0*.gif\0All files\0*.*\0", "Save recording", "gif");
        }
        else
        {
            return new FileType("PNG image\0*.png\0All files\0*.*\0", "Save screenshot", "png");
        }
    }

    /// <summary>
    /// Represents the filter, title and file extension shown by the save dialog.
    /// </summary>
    private readonly record struct FileType(string Filter, string Title, string Ext);
}

/// <summary>
/// Specifies the file format a capture is saved in.
/// </summary>
public enum SaveKind
{
    Png,
    Gif,
}
