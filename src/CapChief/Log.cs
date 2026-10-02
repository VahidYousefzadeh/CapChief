namespace CapChief;

/// <summary>
/// Provides methods for writing errors to a log file in the user's local application data folder,
/// which stays writable when the app is installed as a package.
/// </summary>
public static class Log
{
    private const string FileName = Product.Name + ".log";

    private static string Folder
    {
        get
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Product.Name);
        }
    }

    public static bool Tolerated(string what, Exception e)
    {
        if (!IsFileFailure(e))
        {
            return false;
        }
        Write($"{what}: {e.Message}");
        return true;
    }

    public static void Fatal(object? e)
    {
        Write(e?.ToString() ?? "");
    }

    /// <summary>
    /// Tells whether a file operation failed because of the disk, the path or the rights, not a
    /// bug. A bad path throws a plain ArgumentException; its null and out-of-range kinds are bugs.
    /// </summary>
    private static bool IsFileFailure(Exception e)
    {
        return e is IOException or UnauthorizedAccessException or NotSupportedException
            or ArgumentException and not (ArgumentNullException or ArgumentOutOfRangeException);
    }

    private static void Write(string body)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.AppendAllText(Path.Combine(Folder, FileName), $"{DateTime.Now} {body}{Environment.NewLine}");
        }
        catch { }
    }
}
