namespace CapChief.Shell;

/// <summary>
/// Defines the identifiers of the custom window messages the app receives.
/// </summary>
public static class AppMessages
{
    public static uint Tray
    {
        get
        {
            return AppMessage.Id(1);
        }
    }

    public static uint ShowToolbar
    {
        get
        {
            return AppMessage.Id(2);
        }
    }

    public static uint ElementsScanned
    {
        get
        {
            return AppMessage.Id(3);
        }
    }

    public static uint ClipCut
    {
        get
        {
            return AppMessage.Id(4);
        }
    }

    public static uint TaskbarCreated
    {
        get
        {
            return TrayIcon.TaskbarCreated;
        }
    }
}
