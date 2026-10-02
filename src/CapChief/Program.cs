namespace CapChief;

/// <summary>
/// Contains the entry point of the app.
/// </summary>
internal static class Program
{
    private static void Main()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log.Fatal(e.ExceptionObject);

        if (!SingleInstance.Claim(Product.MutexName))
        {
            SingleInstance.HandOver(Overlay.ClassName, AppMessages.ShowToolbar);
            return;
        }

        Desktop.MakeProcessDpiAware();
        var desk = Desktop.Measure();
        var app = new App(desk, new DesktopScreen(desk), excludedFromCapture: true);

        app.Start();
        app.Shell.ShowTray();
        WindowLoop.Pump();
        app.Shutdown();
    }
}
