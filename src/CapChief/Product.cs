namespace CapChief;

/// <summary>
/// Provides the product name and the identifiers derived from it.
/// </summary>
public static class Product
{
    public const string Name = "CapChief";

    public const string FilePrefix = Name + "_";

    public const string MutexName = @"Local\" + Name + ".SingleInstance";
}
