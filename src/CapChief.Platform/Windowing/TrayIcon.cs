namespace CapChief.Windowing;

/// <summary>
/// Displays an icon in the Windows notification area and reports clicks on it.
/// </summary>
public sealed class TrayIcon
{
    private const uint IconId = 1;

    private Window _owner = Window.None;
    private bool _added;
    private string _tip = "";
    private IntPtr _icon;
    private uint _message;

    public static uint TaskbarCreated { get; } = AppMessage.Named("TaskbarCreated");

    public void Add(Window owner, AppIcon icon, string tip, uint message)
    {
        _owner = owner;
        _icon = icon.Small;
        _tip = tip;
        _message = message;
        Put(Win32.NIM_ADD);
    }

    public void Restore()
    {
        _added = false;
        Put(Win32.NIM_ADD);
    }

    public unsafe void ShowNotice(string title, string text)
    {
        if (!_added)
        {
            return;
        }
        Win32.NOTIFYICONDATAW nid = Ours();
        nid.uFlags = Win32.NIF_INFO;
        nid.dwInfoFlags = Win32.NIIF_WARNING;
        Fill(nid.szInfoTitle, Win32.NOTIFYICONDATAW.InfoTitleChars, title);
        Fill(nid.szInfo, Win32.NOTIFYICONDATAW.InfoChars, text);
        Win32.Shell_NotifyIconW(Win32.NIM_MODIFY, ref nid);
    }

    public void Remove()
    {
        if (!_added)
        {
            return;
        }
        Win32.NOTIFYICONDATAW nid = Ours();
        Win32.Shell_NotifyIconW(Win32.NIM_DELETE, ref nid);
        _added = false;
    }

    public static TrayAction Decode(AppSignal signal, out Point at)
    {
        at = signal.Where;
        int ev = signal.Code;
        if (ev == Win32.NIN_SELECT || ev == Win32.NIN_KEYSELECT)
        {
            return TrayAction.Chosen;
        }
        if (ev == Win32.WM_CONTEXTMENU)
        {
            return TrayAction.Menu;
        }
        else
        {
            return TrayAction.None;
        }
    }

    private unsafe Win32.NOTIFYICONDATAW Ours()
    {
        return new()
        {
            cbSize = (uint)sizeof(Win32.NOTIFYICONDATAW),
            hWnd = _owner.Handle,
            uID = IconId,
        };
    }

    private unsafe void Put(uint op)
    {
        if (!_owner.Exists)
        {
            return;
        }
        Win32.NOTIFYICONDATAW nid = Ours();
        nid.uFlags = Win32.NIF_MESSAGE | Win32.NIF_ICON | Win32.NIF_TIP | Win32.NIF_SHOWTIP;
        nid.uCallbackMessage = _message;
        nid.hIcon = _icon;
        Fill(nid.szTip, Win32.NOTIFYICONDATAW.TipChars, _tip);
        if (!Win32.Shell_NotifyIconW(op, ref nid))
        {
            return;
        }
        _added = true;

        nid.uVersion = Win32.NOTIFYICON_VERSION_4;
        Win32.Shell_NotifyIconW(Win32.NIM_SETVERSION, ref nid);
    }

    private static unsafe void Fill(char* dst, int cap, string s)
    {
        int n = Math.Min(s.Length, cap - 1);
        for (int i = 0; i < n; i++)
        {
            dst[i] = s[i];
        }
        dst[n] = '\0';
    }
}

/// <summary>
/// Specifies the action the user performed on the tray icon.
/// </summary>
public enum TrayAction
{
    None,
    Chosen,
    Menu,
}
