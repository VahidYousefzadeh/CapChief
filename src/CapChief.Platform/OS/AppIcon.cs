namespace CapChief.OS;

/// <summary>
/// Loads the app icon from the executable's resources in large and small sizes.
/// </summary>
public sealed class AppIcon
{
    private readonly Win32.EnumResNameProc _enumIcon;

    private IntPtr _group;
    private IntPtr _big, _small;
    private bool _loaded;

    public AppIcon()
    {
        _enumIcon = OnGroupIcon;
    }

    public IntPtr Big
    {
        get
        {
            Load();
            return _big;
        }
    }

    public IntPtr Small
    {
        get
        {
            Load();
            return _small;
        }
    }

    public void Apply(IntPtr hwnd)
    {
        Win32.SendMessageW(hwnd, Win32.WM_SETICON, (IntPtr)Win32.ICON_BIG, Big);
        Win32.SendMessageW(hwnd, Win32.WM_SETICON, (IntPtr)Win32.ICON_SMALL, Small);
    }

    private void Load()
    {
        if (_loaded)
        {
            return;
        }
        _loaded = true;

        IntPtr hInst = Win32.GetModuleHandleW(null);
        Win32.EnumResourceNamesW(hInst, (IntPtr)Win32.RT_GROUP_ICON, _enumIcon, IntPtr.Zero);

        if (_group == IntPtr.Zero)
        {
            _big = _small = Win32.LoadIconW(IntPtr.Zero, (IntPtr)Win32.IDI_APPLICATION);
            return;
        }

        _big = Pick(hInst, Win32.SM_CXICON, Win32.SM_CYICON);
        _small = Pick(hInst, Win32.SM_CXSMICON, Win32.SM_CYSMICON);
    }

    private IntPtr Pick(IntPtr hInst, int cxMetric, int cyMetric)
    {
        int cx = Win32.GetSystemMetrics(cxMetric);
        int cy = Win32.GetSystemMetrics(cyMetric);
        IntPtr h = Win32.LoadImageW(hInst, _group, Win32.IMAGE_ICON, cx, cy, Win32.LR_SHARED);
        if (h != IntPtr.Zero)
        {
            return h;
        }
        else
        {
            return Win32.LoadIconW(hInst, _group);
        }
    }

    private bool OnGroupIcon(IntPtr mod, IntPtr type, IntPtr name, IntPtr param)
    {
        if ((ulong)name >= 0x10000)
        {
            return true;
        }
        if (_group == IntPtr.Zero || (ulong)name < (ulong)_group)
        {
            _group = name;
        }
        return true;
    }
}
