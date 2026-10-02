using System.Runtime.InteropServices;

namespace CapChief.Windowing;

/// <summary>
/// Provides methods for ensuring only one instance of the app runs at a time.
/// </summary>
public static class SingleInstance
{
    public static bool Claim(string name)
    {
        IntPtr held = Win32.CreateMutexW(IntPtr.Zero, true, name);
        return held != IntPtr.Zero && Marshal.GetLastWin32Error() != Win32.ERROR_ALREADY_EXISTS;
    }

    public static void HandOver(string className, uint message)
    {
        IntPtr running = Win32.FindWindowW(className, null);
        if (running != IntPtr.Zero)
        {
            Win32.PostMessageW(running, message, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
