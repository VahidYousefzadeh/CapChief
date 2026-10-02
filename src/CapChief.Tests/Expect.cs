namespace CapChief.Tests;

/// <summary>
/// Provides methods for asserting equality with a message that explains the expectation.
/// </summary>
public static class Expect
{
    public static void Eq(int got, int want, string what)
    {
        Assert.True(got == want, $"{what}: expected {want}, got {got}");
    }

    public static void Eq<T>(T got, T want, string what)
    {
        Assert.True(Equals(got, want), $"{what}: expected {want}, got {got}");
    }
}
