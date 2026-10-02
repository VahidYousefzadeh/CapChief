namespace CapChief.Windowing;

/// <summary>
/// Provides methods for running and stopping the app's message loop.
/// </summary>
public static class WindowLoop
{
    public static void Pump()
    {
        while (Win32.GetMessageW(out Win32.MSG msg, IntPtr.Zero, 0, 0) > 0)
        {
            Win32.TranslateMessage(ref msg);
            Win32.DispatchMessageW(ref msg);
        }
    }

    public static void Quit()
    {
        Win32.PostQuitMessage(0);
    }

    public static void AwaitFrame()
    {
        Win32.DwmFlush();
    }
}
