namespace CapChief;

/// <summary>
/// Provides the product name, the identifiers derived from it, and the version the app was built as.
/// </summary>
public static class Product
{
    public const string Name = "CapChief";

    public const string FilePrefix = Name + "_";

    public const string MutexName = @"Local\" + Name + ".SingleInstance";

    /// <summary>
    /// Gets the version set in the project file, as major.minor.patch.
    /// </summary>
    public static string Version
    {
        get
        {
            if (typeof(Product).Assembly.GetName().Version is not { } v)
            {
                return "";
            }
            return $"{v.Major}.{v.Minor}.{v.Build}";
        }
    }
}
