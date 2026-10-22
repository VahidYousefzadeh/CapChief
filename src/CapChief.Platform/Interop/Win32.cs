using System.Runtime.InteropServices;

namespace CapChief.Interop;

/// <summary>
/// Provides the P/Invoke declarations for the Windows API functions the app uses.
/// </summary>
public static class Win32
{
    /// <summary>
    /// Represents the method that processes messages sent to a window.
    /// </summary>
    public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Represents the method that is called once for each display monitor during enumeration.
    /// </summary>
    public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

    /// <summary>
    /// Represents the method that is called once for each top-level window during enumeration.
    /// </summary>
    public delegate bool WindowEnumProc(IntPtr hWnd, IntPtr lParam);

    /// <summary>
    /// Represents the method that is called once for each named resource during enumeration.
    /// </summary>
    public delegate bool EnumResNameProc(IntPtr mod, IntPtr type, IntPtr name, IntPtr param);

    /// <summary>
    /// Represents the method that processes messages for a subclassed window.
    /// </summary>
    public delegate IntPtr SubclassProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData);

    /// <summary>
    /// Represents the method that is called when a window timer elapses.
    /// </summary>
    public delegate void TimerProc(IntPtr hWnd, uint msg, UIntPtr id, uint time);

    public static IntPtr LParamOf(int x, int y)
    {
        return (IntPtr)((y << 16) | (x & 0xFFFF));
    }

    public static Point PointOf(IntPtr lParam)
    {
        return new((short)(lParam.ToInt64() & 0xFFFF), (short)((lParam.ToInt64() >> 16) & 0xFFFF));
    }

    public static int LoWord(IntPtr v)
    {
        return (int)(v.ToInt64() & 0xFFFF);
    }

    public static float ScaleFromDpi(uint dpi)
    {
        return dpi / (float)USER_DEFAULT_SCREEN_DPI;
    }

    public static unsafe Rect RectOf(IntPtr p)
    {
        if (p == IntPtr.Zero)
        {
            return default;
        }
        else
        {
            return *(Rect*)p;
        }
    }

    public const uint WM_DESTROY = 0x0002;
    public const uint WM_PAINT = 0x000F;
    public const uint WM_ERASEBKGND = 0x0014;
    public const uint WM_SETCURSOR = 0x0020;
    public const uint WM_TIMER = 0x0113;
    public const uint WM_MOUSEMOVE = 0x0200;
    public const uint WM_LBUTTONDOWN = 0x0201;
    public const uint WM_LBUTTONUP = 0x0202;
    public const uint WM_LBUTTONDBLCLK = 0x0203;
    public const uint WM_RBUTTONUP = 0x0205;
    public const uint WM_DISPLAYCHANGE = 0x007E;
    public const uint EM_SETSEL = 0x00B1;
    public const uint EM_SETMARGINS = 0x00D3;
    public const uint WM_SETFONT = 0x0030;
    public const uint WM_KEYDOWN = 0x0100;
    public const uint WM_KEYUP = 0x0101;
    public const uint WM_CHAR = 0x0102;
    public const uint WM_SYSKEYDOWN = 0x0104;
    public const uint WM_SYSKEYUP = 0x0105;

    public const uint WM_CUT = 0x0300;
    public const uint WM_PASTE = 0x0302;
    public const uint WM_CLEAR = 0x0303;
    public const uint WM_UNDO = 0x0304;
    public const uint WM_KILLFOCUS = 0x0008;
    public const uint WM_MOUSELEAVE = 0x02A3;
    public const uint WM_MOUSEACTIVATE = 0x0021;
    public const int MA_NOACTIVATE = 3;
    public const uint WM_CTLCOLOREDIT = 0x0133;

    public const uint WM_NCHITTEST = 0x0084;
    public const uint WM_HOTKEY = 0x0312;
    public const uint WM_DPICHANGED = 0x02E0;
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_NOREPEAT = 0x4000;
    public const int HTCLIENT = 1;
    public const int HTTRANSPARENT = -1;
    public const int RGN_OR = 2;
    public const int SW_SHOWNORMAL = 1;
    public const int SW_SHOWNOACTIVATE = 4;
    public const int COLORONCOLOR = 3;
    public const int HALFTONE = 4;
    public const uint OFN_OVERWRITEPROMPT = 0x00000002;
    public const uint OFN_NOCHANGEDIR = 0x00000008;
    public const uint OFN_PATHMUSTEXIST = 0x00000800;
    public const uint OFN_EXPLORER = 0x00080000;
    public const int NULLREGION = 1;
    public const int VK_BACK = 0x08;
    public const int VK_TAB = 0x09;
    public const int VK_NUMPAD0 = 0x60;
    public const int VK_NUMPAD9 = 0x69;
    public const int VK_RETURN = 0x0D;
    public const int VK_ESCAPE = 0x1B;
    public const int VK_SPACE = 0x20;
    public const int VK_END = 0x23;
    public const int VK_HOME = 0x24;
    public const int VK_DELETE = 0x2E;
    public const int VK_LEFT = 0x25;
    public const int VK_UP = 0x26;
    public const int VK_RIGHT = 0x27;
    public const int VK_DOWN = 0x28;
    public const int VK_CONTROL = 0x11;
    public const int VK_SHIFT = 0x10;
    public const int VK_MENU = 0x12;

    public const int MK_SHIFT = 0x0004;
    public const int MK_CONTROL = 0x0008;

    public const int GWL_EXSTYLE = -20;
    public const long WS_EX_TOPMOST = 0x00000008;
    public const long WS_EX_TRANSPARENT = 0x00000020;
    public const long WS_EX_TOOLWINDOW = 0x00000080;
    public const long WS_EX_LAYERED = 0x00080000;
    public const long WS_EX_NOACTIVATE = 0x08000000;

    public const uint WS_POPUP = 0x80000000;
    public const uint WS_CHILD = 0x40000000;
    public const uint WS_VISIBLE = 0x10000000;

    public const uint ES_MULTILINE = 0x0004;
    public const uint ES_AUTOVSCROLL = 0x0040;

    public const uint CS_VREDRAW = 0x0001;
    public const uint CS_HREDRAW = 0x0002;
    public const uint CS_DBLCLKS = 0x0008;

    public const int SW_HIDE = 0;

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint WM_ACTIVATE = 0x0006;
    public const int WA_INACTIVE = 0;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;
    public static readonly IntPtr HWND_TOPMOST = new(-1);

    public const uint SRCCOPY = 0x00CC0020;
    private const int AC_SRC_OVER = 0;
    private const int AC_SRC_ALPHA = 1;
    public const uint ULW_ALPHA = 2;

    public const int TRANSPARENT = 1;
    public const int CLEARTYPE_QUALITY = 5;
    public const int ANTIALIASED_QUALITY = 4;
    public const int DEFAULT_CHARSET = 1;

    public const int NULL_BRUSH = 5;

    public const uint GMEM_MOVEABLE = 0x0002;
    public const uint CF_BITMAP = 2;
    public const uint CF_DIB = 8;
    public const int BI_RGB = 0;
    public const int DIB_RGB_COLORS = 0;

    public const int TME_LEAVE = 0x0002;
    public const int IDI_APPLICATION = 32512;

    public const uint WM_SETICON = 0x0080;
    public const int ICON_SMALL = 0;
    public const int ICON_BIG = 1;

    public const int RT_GROUP_ICON = 14;
    public const uint IMAGE_ICON = 1;
    public const uint LR_SHARED = 0x8000;
    public const uint IMAGE_CURSOR = 2;
    public const uint LR_LOADFROMFILE = 0x0010;
    public const int SM_CXICON = 11;
    public const int SM_CYICON = 12;
    public const int SM_CXSMICON = 49;
    public const int SM_CYSMICON = 50;
    public const int SM_XVIRTUALSCREEN = 76;
    public const int SM_YVIRTUALSCREEN = 77;
    public const int SM_CXVIRTUALSCREEN = 78;
    public const int SM_CYVIRTUALSCREEN = 79;

    public const uint WM_APP = 0x8000;
    public const uint WM_NULL = 0x0000;
    public const uint WM_CONTEXTMENU = 0x007B;

    public const uint NIM_ADD = 0;
    public const uint NIM_MODIFY = 1;
    public const uint NIM_DELETE = 2;
    public const uint NIM_SETVERSION = 4;

    public const uint NIF_MESSAGE = 0x01;
    public const uint NIF_ICON = 0x02;
    public const uint NIF_TIP = 0x04;
    public const uint NIF_INFO = 0x10;
    public const uint NIF_SHOWTIP = 0x80;
    public const uint NIIF_WARNING = 0x02;

    public const uint NOTIFYICON_VERSION_4 = 4;
    public const uint NIN_SELECT = 0x0400;
    public const uint NIN_KEYSELECT = 0x0401;

    public const int ERROR_ALREADY_EXISTS = 183;

    public const uint MF_STRING = 0x0000;
    public const uint MF_SEPARATOR = 0x0800;
    public const uint MF_BYCOMMAND = 0x0000;

    public const uint TPM_RIGHTBUTTON = 0x0002;
    public const uint TPM_RETURNCMD = 0x0100;

    public const int IDC_ARROW = 32512;
    public const int IDC_CROSS = 32515;
    public const int IDC_HAND = 32649;
    public const int IDC_SIZENWSE = 32642;
    public const int IDC_SIZENESW = 32643;
    public const int IDC_SIZEWE = 32644;
    public const int IDC_SIZENS = 32645;
    public const int IDC_SIZEALL = 32646;
    public const int IDC_IBEAM = 32513;
    public const int IDC_WAIT = 32514;
    public const int IDC_UPARROW = 32516;
    public const int IDC_NO = 32648;
    public const int IDC_APPSTARTING = 32650;
    public const int IDC_HELP = 32651;
    public const int IDC_PIN = 32671;
    public const int IDC_PERSON = 32672;
    public const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
    public const uint WDA_NONE = 0;

    public const uint DT_CENTER = 0x1;
    public const uint DT_VCENTER = 0x4;
    public const uint TA_LEFT = 0;
    public const uint TA_BASELINE = 24;
    public const uint DT_SINGLELINE = 0x20;
    public const uint DT_NOPREFIX = 0x800;
    public const uint DT_WORDBREAK = 0x10;
    public const uint DT_CALCRECT = 0x400;

    public const uint EC_MARGINS = 0x0003;

    public const uint RDW_INVALIDATE = 0x1;
    public const uint RDW_UPDATENOW = 0x100;

    public const int MDT_EFFECTIVE_DPI = 0;
    public const uint MONITOR_DEFAULTTOPRIMARY = 1;
    public const uint MONITOR_DEFAULTTONEAREST = 2;
    public const uint USER_DEFAULT_SCREEN_DPI = 96;

    public const uint MB_ICONWARNING = 0x30;

    public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new(-4);

    /// <summary>
    /// Represents the placement and pixel extents of a single character in a font.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GLYPHMETRICS
    {
        public uint BlackBoxX, BlackBoxY;
        public Point Origin;
        public short CellIncX, CellIncY;
    }

    /// <summary>
    /// Represents a fixed-point number with a 16-bit integer part and a 16-bit fraction.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FIXED { public short Fract; public short Value; }

    /// <summary>
    /// Represents a 2-by-2 transformation matrix of fixed-point values for character outlines.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MAT2
    {
        public FIXED eM11, eM12, eM21, eM22;
        public static MAT2 Identity
        {
            get
            {
                return new() { eM11 = new FIXED { Value = 1 }, eM22 = new FIXED { Value = 1 } };
            }
        }
    }

    public const uint GGO_METRICS = 0;
    public const uint GDI_ERROR = 0xFFFFFFFF;

    /// <summary>
    /// Represents the basic metrics of a physical font, such as height and ascent.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct TEXTMETRICW
    {
        public int tmHeight, tmAscent, tmDescent, tmInternalLeading, tmExternalLeading;
        public int tmAveCharWidth, tmMaxCharWidth, tmWeight, tmOverhang;
        public int tmDigitizedAspectX, tmDigitizedAspectY;
        public char tmFirstChar, tmLastChar, tmDefaultChar, tmBreakChar;
        public byte tmItalic, tmUnderlined, tmStruckOut, tmPitchAndFamily, tmCharSet;
    }

    /// <summary>
    /// Represents a message retrieved from a thread's message queue.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public IntPtr hwnd; public uint message; public IntPtr wParam; public IntPtr lParam;
        public uint time; public Point pt;
    }

    /// <summary>
    /// Represents the attributes of a window class to register.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WNDCLASSEX
    {
        public uint cbSize; public uint style; public IntPtr lpfnWndProc;
        public int cbClsExtra; public int cbWndExtra; public IntPtr hInstance;
        public IntPtr hIcon; public IntPtr hCursor; public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
        public IntPtr hIconSm;
    }

    /// <summary>
    /// Represents the source and destination blending settings for alpha blending.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BLENDFUNCTION
    {
        public byte BlendOp; public byte BlendFlags; public byte SourceConstantAlpha; public byte AlphaFormat;
        public static BLENDFUNCTION Over
        {
            get
            {
                return new()
                {
                    BlendOp = AC_SRC_OVER,
                    SourceConstantAlpha = 255,
                    AlphaFormat = AC_SRC_ALPHA,
                };
            }
        }
    }

    /// <summary>
    /// Represents a request to be notified when the mouse leaves or hovers over a window.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TRACKMOUSEEVENT
    {
        public uint cbSize; public uint dwFlags; public IntPtr hwndTrack; public uint dwHoverTime;
    }

    /// <summary>
    /// Represents the information an application uses to paint the client area of a window.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct PAINTSTRUCT
    {
        public IntPtr hdc; public int fErase; public Rect rcPaint; public int fRestore; public int fIncUpdate;
        public fixed byte rgbReserved[32];
    }

    /// <summary>
    /// Represents the dimensions and color format of a device-independent bitmap.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public uint biSize; public int biWidth; public int biHeight; public ushort biPlanes; public ushort biBitCount;
        public uint biCompression; public uint biSizeImage; public int biXPelsPerMeter; public int biYPelsPerMeter;
        public uint biClrUsed; public uint biClrImportant;
    }

    /// <summary>
    /// Represents the display area and work area of a monitor.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public uint cbSize; public Rect rcMonitor; public Rect rcWork; public uint dwFlags;
    }

    /// <summary>
    /// Represents the bitmaps and hot spot of an icon or cursor.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ICONINFO
    {
        public bool fIcon; public uint xHotspot; public uint yHotspot; public IntPtr hbmMask; public IntPtr hbmColor;
    }

    /// <summary>
    /// Represents the global cursor and its position on the screen.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CURSORINFO
    {
        public uint cbSize; public uint flags; public IntPtr hCursor; public Point ptScreenPos;
    }

    /// <summary>
    /// Represents the settings for an icon shown in the taskbar notification area.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public unsafe struct NOTIFYICONDATAW
    {
        public const int TipChars = 128;
        public const int InfoChars = 256;
        public const int InfoTitleChars = 64;

        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        public fixed char szTip[TipChars];
        public uint dwState;
        public uint dwStateMask;
        public fixed char szInfo[InfoChars];
        public uint uVersion;
        public fixed char szInfoTitle[InfoTitleChars];
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern ushort RegisterClassExW(ref WNDCLASSEX wc);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern IntPtr CreateWindowExW(uint exStyle, string cls, string name, uint style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);

    [DllImport("user32.dll")] public static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")] public static extern int GetMessageW(out MSG msg, IntPtr hWnd, uint min, uint max);

    [DllImport("user32.dll")] public static extern bool TranslateMessage(ref MSG msg);
    [DllImport("user32.dll")] public static extern IntPtr DispatchMessageW(ref MSG msg);
    [DllImport("user32.dll")] public static extern void PostQuitMessage(int code);

    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern bool DestroyWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out Rect rc);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out Rect rc);

    [DllImport("user32.dll")] public static extern int GetSystemMetrics(int i);
    [DllImport("user32.dll")] public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc proc, IntPtr data);
    [DllImport("user32.dll")] public static extern bool EnumWindows(WindowEnumProc proc, IntPtr data);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassNameW(IntPtr h, [Out] char[] cls, int max);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentProcessId();

    public const uint DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    public const uint DWMWA_CLOAKED = 14;
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, uint attr, out Rect val, int size);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, uint attr, out int val, int size);
    [DllImport("dwmapi.dll")] public static extern int DwmFlush();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern bool GetMonitorInfoW(IntPtr hMon, ref MONITORINFO info);
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromPoint(Point pt, uint flags);

    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr ctx);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr h);
    [DllImport("shcore.dll")] public static extern int GetDpiForMonitor(IntPtr hMon, int type, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] public static extern IntPtr GetWindowLongPtr(IntPtr h, int i);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] public static extern IntPtr SetWindowLongPtr(IntPtr h, int i, IntPtr v);

    [DllImport("user32.dll")] public static extern bool SetWindowDisplayAffinity(IntPtr h, uint affinity);
    [DllImport("user32.dll")] public static extern IntPtr SetCapture(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ReleaseCapture();
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point p);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref Point p);
    [DllImport("user32.dll")] public static extern bool ScreenToClient(IntPtr h, ref Point p);
    [DllImport("user32.dll")] public static extern short GetKeyState(int vk);

    [DllImport("user32.dll")] public static extern uint GetCaretBlinkTime();
    [DllImport("user32.dll")] public static extern UIntPtr SetTimer(IntPtr h, UIntPtr id, uint ms, IntPtr proc);
    [DllImport("user32.dll")] public static extern UIntPtr SetTimer(IntPtr h, UIntPtr id, uint ms, TimerProc proc);
    [DllImport("user32.dll")] public static extern bool KillTimer(IntPtr h, UIntPtr id);
    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint vk);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
    [DllImport("user32.dll")] public static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT tme);
    [DllImport("user32.dll")] public static extern bool InvalidateRect(IntPtr h, IntPtr rect, bool erase);
    [DllImport("user32.dll")] public static extern bool InvalidateRect(IntPtr h, ref Rect rect, bool erase);
    [DllImport("user32.dll")] public static extern bool UpdateWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool RedrawWindow(IntPtr h, IntPtr rc, IntPtr rgn, uint flags);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr SetFocus(IntPtr h);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr LoadCursorW(IntPtr inst, IntPtr id);
    [DllImport("user32.dll")] public static extern IntPtr SetCursor(IntPtr cur);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr LoadIconW(IntPtr inst, IntPtr name);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr LoadImageW(IntPtr inst, IntPtr name, uint type, int cx, int cy, uint load);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern bool EnumResourceNamesW(IntPtr mod, IntPtr type, EnumResNameProc proc, IntPtr param);
    [DllImport("user32.dll")] public static extern IntPtr CreateIconIndirect(ref ICONINFO piconinfo);
    [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr hIcon);
    [DllImport("user32.dll")] public static extern bool DestroyCursor(IntPtr hCursor);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr LoadImageW(IntPtr inst, string name, uint type, int cx, int cy, uint load);
    [DllImport("user32.dll")] public static extern IntPtr CopyImage(IntPtr image, uint type, int cx, int cy, uint flags);

    [DllImport("user32.dll")] public static extern bool GetCursorInfo(ref CURSORINFO info);
    [DllImport("user32.dll")] public static extern bool GetIconInfo(IntPtr icon, ref ICONINFO info);
    [DllImport("user32.dll")] public static extern bool DrawIconEx(IntPtr hdc, int x, int y, IntPtr icon, int cx, int cy, uint step, IntPtr brush, uint flags);

    public const uint CURSOR_SHOWING = 0x00000001;

    public const uint DI_NORMAL = 0x0003;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] public static extern bool Shell_NotifyIconW(uint msg, ref NOTIFYICONDATAW data);
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr ShellExecuteW(IntPtr h, string? verb, string file, string? parameters, string? directory, int show);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern uint RegisterWindowMessageW(string name);
    [DllImport("user32.dll")] public static extern bool PostMessageW(IntPtr h, uint m, IntPtr w, IntPtr l);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindowW(string? cls, string? title);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindowExW(IntPtr parent, IntPtr after, string? cls, string? title);

    [DllImport("user32.dll")] public static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll")] public static extern bool DestroyMenu(IntPtr menu);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern bool AppendMenuW(IntPtr menu, uint flags, UIntPtr id, string? item);
    [DllImport("user32.dll")] public static extern bool SetMenuDefaultItem(IntPtr menu, uint item, uint byPos);
    [DllImport("user32.dll")] public static extern int TrackPopupMenu(IntPtr menu, uint flags, int x, int y, int reserved, IntPtr h, IntPtr rc);

    [DllImport("user32.dll")] public static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll")] public static extern bool EmptyClipboard();
    [DllImport("user32.dll")] public static extern IntPtr SetClipboardData(uint fmt, IntPtr data);
    [DllImport("user32.dll")] public static extern bool CloseClipboard();

    [DllImport("user32.dll")] public static extern IntPtr GetClipboardData(uint fmt);
    [DllImport("user32.dll")] public static extern bool IsClipboardFormatAvailable(uint fmt);

    [DllImport("user32.dll")] public static extern uint GetGuiResources(IntPtr proc, uint flags);
    [DllImport("kernel32.dll")] public static extern IntPtr GetCurrentProcess();

    public const uint GR_GDIOBJECTS = 0;

    [DllImport("gdi32.dll")] public static extern int GetObjectW(IntPtr h, int size, ref BITMAP bm);

    /// <summary>
    /// Represents the dimensions, color depth and pixel data of a bitmap.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAP
    {
        public int bmType; public int bmWidth; public int bmHeight; public int bmWidthBytes;
        public ushort bmPlanes; public ushort bmBitsPixel; public IntPtr bmBits;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowTextLengthW(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowTextW(IntPtr hWnd, [Out] char[] lpString, int nMaxCount);
    [DllImport("user32.dll")] public static extern IntPtr SendMessageW(IntPtr h, uint m, IntPtr w, IntPtr l);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr GetModuleHandleW(string? name);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)] public static extern int RegGetValueW(IntPtr key, string subKey, string value, uint flags, IntPtr type, char[] data, ref int bytes);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)] public static extern int RegGetValueW(IntPtr key, string subKey, string value, uint flags, IntPtr type, out int data, ref int bytes);

    public static readonly IntPtr HKEY_CURRENT_USER = new(unchecked((int)0x80000001));
    public const uint RRF_RT_REG_SZ = 0x00000002;
    public const uint RRF_RT_REG_DWORD = 0x00000010;
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] public static extern IntPtr CreateMutexW(IntPtr attr, bool initialOwner, string name);

    [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr h);
    [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr h, IntPtr hdc);

    /// <summary>
    /// Represents the settings for the Open and Save As common dialog boxes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct OPENFILENAME
    {
        public uint lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public IntPtr lpstrFilter;
        public IntPtr lpstrCustomFilter;
        public uint nMaxCustFilter;
        public uint nFilterIndex;
        public IntPtr lpstrFile;
        public uint nMaxFile;
        public IntPtr lpstrFileTitle;
        public uint nMaxFileTitle;
        public IntPtr lpstrInitialDir;
        public IntPtr lpstrTitle;
        public uint Flags;
        public ushort nFileOffset;
        public ushort nFileExtension;
        public IntPtr lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public IntPtr lpTemplateName;
        public IntPtr pvReserved;
        public uint dwReserved;
        public uint FlagsEx;
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode)] public static extern bool GetSaveFileNameW(ref OPENFILENAME ofn);

    /// <summary>
    /// Represents the source, position and blending settings for updating a layered window.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UPDATELAYEREDWINDOWINFO
    {
        public uint cbSize;
        public IntPtr hdcDst;
        public IntPtr pptDst;
        public IntPtr psize;
        public IntPtr hdcSrc;
        public IntPtr pptSrc;
        public uint crKey;
        public IntPtr pblend;
        public uint dwFlags;
        public IntPtr prcDirty;
    }

    [DllImport("user32.dll")] public static extern bool UpdateLayeredWindowIndirect(IntPtr hwnd, ref UPDATELAYEREDWINDOWINFO info);

    [DllImport("user32.dll")] public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point pptDst, ref Size psize, IntPtr hdcSrc, ref Point pprSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

    [DllImport("user32.dll")] public static extern IntPtr BeginPaint(IntPtr h, out PAINTSTRUCT ps);
    [DllImport("user32.dll")] public static extern bool EndPaint(IntPtr h, ref PAINTSTRUCT ps);
    [DllImport("user32.dll")] public static extern int FillRect(IntPtr hdc, ref Rect rc, IntPtr brush);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int DrawTextW(IntPtr hdc, string s, int len, ref Rect rc, uint fmt);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int MessageBoxW(IntPtr h, string text, string cap, uint type);

    [DllImport("comctl32.dll")] public static extern bool SetWindowSubclass(IntPtr h, SubclassProc proc, UIntPtr id, IntPtr data);
    [DllImport("comctl32.dll")] public static extern IntPtr DefSubclassProc(IntPtr h, uint m, IntPtr w, IntPtr l, UIntPtr id, IntPtr data);

    [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] public static extern bool GdiFlush();
    [DllImport("gdi32.dll")] public static extern int GetClipBox(IntPtr hdc, out Rect rc);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateBitmap(int width, int height, uint planes, uint bitcount, byte[] bits);
    [DllImport("gdi32.dll")] public static extern bool StretchBlt(IntPtr dst, int dx, int dy, int dw, int dh, IntPtr src, int sx, int sy, int sw, int sh, uint rop);
    [DllImport("gdi32.dll")] public static extern int SetStretchBltMode(IntPtr hdc, int mode);
    [DllImport("gdi32.dll")] public static extern bool SetBrushOrgEx(IntPtr hdc, int x, int y, IntPtr pt);
    [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr o);
    [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr hdc, IntPtr o);
    [DllImport("gdi32.dll")] public static extern IntPtr GetStockObject(int i);
    [DllImport("gdi32.dll")] public static extern bool BitBlt(IntPtr dst, int dx, int dy, int dw, int dh, IntPtr src, int sx, int sy, uint rop);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFOHEADER bmi, uint usage, out IntPtr bits, IntPtr section, uint offset);
    [DllImport("gdi32.dll")] public static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint start, uint lines, IntPtr bits, ref BITMAPINFOHEADER bmi, uint usage);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr CreateFontW(int h, int w, int esc, int orient, int weight, uint italic, uint underline, uint strikeout, uint charset, uint outprec, uint clipprec, uint quality, uint pitchfamily, string face);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateSolidBrush(uint colorRef);
    [DllImport("gdi32.dll")] public static extern uint SetTextColor(IntPtr hdc, uint colorRef);
    [DllImport("gdi32.dll")] public static extern int SetBkMode(IntPtr hdc, int m);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] public static extern bool TextOutW(IntPtr hdc, int x, int y, string s, int len);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] public static extern bool GetTextExtentPoint32W(IntPtr hdc, string s, int len, out Size sz);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] public static extern bool GetTextMetricsW(IntPtr hdc, out TEXTMETRICW tm);
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] public static extern uint GetGlyphOutlineW(IntPtr hdc, uint ch, uint format, out GLYPHMETRICS gm, uint cb, IntPtr buf, ref MAT2 mat);
    [DllImport("gdi32.dll")] public static extern uint SetTextAlign(IntPtr hdc, uint mode);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateRectRgn(int l, int t, int r, int b);
    [DllImport("gdi32.dll")] public static extern int CombineRgn(IntPtr dst, IntPtr a, IntPtr b, int mode);
    [DllImport("gdi32.dll")] public static extern int SelectClipRgn(IntPtr hdc, IntPtr rgn);
    [DllImport("gdi32.dll")] public static extern int GetRgnBox(IntPtr rgn, out Rect rc);
    [DllImport("user32.dll")] public static extern int GetUpdateRgn(IntPtr h, IntPtr rgn, bool erase);
    [DllImport("gdi32.dll")] public static extern int SaveDC(IntPtr hdc);
    [DllImport("gdi32.dll")] public static extern int RestoreDC(IntPtr hdc, int nSavedDC);
    [DllImport("gdi32.dll")] public static extern int IntersectClipRect(IntPtr hdc, int l, int t, int r, int b);

    /// <summary>
    /// Represents the startup settings passed to GDI+ when it is initialized.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GdiplusStartupInput
    {
        public uint GdiplusVersion;
        public IntPtr DebugEventCallback;
        public bool SuppressBackgroundThread;
        public bool SuppressExternalCodecs;
        public static GdiplusStartupInput Default
        {
            get
            {
                return new() { GdiplusVersion = 1 };
            }
        }
    }

    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdiplusStartup(out IntPtr token, ref GdiplusStartupInput input, IntPtr output);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern void GdiplusShutdown(IntPtr token);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipCreateFromHDC(IntPtr hdc, out IntPtr graphics);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipDeleteGraphics(IntPtr graphics);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipSetSmoothingMode(IntPtr graphics, int smoothingMode);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipCreatePen1(uint argb, float width, int unit, out IntPtr pen);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipDeletePen(IntPtr pen);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipSetPenStartCap(IntPtr pen, int cap);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipSetPenEndCap(IntPtr pen, int cap);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipSetPenLineJoin(IntPtr pen, int join);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipDrawLineI(IntPtr graphics, IntPtr pen, int x1, int y1, int x2, int y2);

    public const int PixelFormat32bppPARGB = 0x000E200B;

    public const int SmoothingModeAntiAlias = 4;
    public const int PixelOffsetModeDefault = 0;
    public const int PixelOffsetModeHighQuality = 2;
    public const int UnitPixel = 2;
    public const int LineCapRound = 2;
    public const int LineCapTriangle = 3;
    public const int LineJoinRound = 2;
    public const int FillModeAlternate = 0;
    public const int FillModeWinding = 1;
    public const int FlushIntentionSync = 1;

    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipCreateBitmapFromScan0(int width, int height, int stride, int format, IntPtr scan0, out IntPtr bitmap);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipDisposeImage(IntPtr image);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipGetImageGraphicsContext(IntPtr image, out IntPtr graphics);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipSetPixelOffsetMode(IntPtr graphics, int mode);

    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipFlush(IntPtr graphics, int intention);

    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipCreateSolidFill(uint argb, out IntPtr brush);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipDeleteBrush(IntPtr brush);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipSetSolidFillColor(IntPtr brush, uint argb);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipSetPenColor(IntPtr pen, uint argb);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipSetPenWidth(IntPtr pen, float width);

    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipCreatePath(int fillMode, out IntPtr path);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipDeletePath(IntPtr path);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipClosePathFigure(IntPtr path);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipAddPathLine(IntPtr path, float x1, float y1, float x2, float y2);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipAddPathArc(IntPtr path, float x, float y, float w, float h, float start, float sweep);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipAddPathEllipse(IntPtr path, float x, float y, float w, float h);

    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipAddPathCurve2(IntPtr path, PointF[] points, int count, float tension);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipAddPathLine2(IntPtr path, PointF[] points, int count);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipFillPath(IntPtr graphics, IntPtr brush, IntPtr path);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipDrawPath(IntPtr graphics, IntPtr pen, IntPtr path);

    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipDrawLine(IntPtr graphics, IntPtr pen, float x1, float y1, float x2, float y2);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipDrawEllipse(IntPtr graphics, IntPtr pen, float x, float y, float w, float h);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipFillEllipse(IntPtr graphics, IntPtr brush, float x, float y, float w, float h);
    [DllImport("gdiplus.dll", ExactSpelling = true)] public static extern int GdipFillRectangle(IntPtr graphics, IntPtr brush, float x, float y, float w, float h);

    [DllImport("msimg32.dll")] public static extern bool AlphaBlend(IntPtr dst, int dx, int dy, int dw, int dh, IntPtr src, int sx, int sy, int sw, int sh, BLENDFUNCTION bf);

    [DllImport("kernel32.dll")] public static extern IntPtr GlobalAlloc(uint flags, nuint bytes);
    [DllImport("kernel32.dll")] public static extern IntPtr GlobalLock(IntPtr h);
    [DllImport("kernel32.dll")] public static extern bool GlobalUnlock(IntPtr h);
    [DllImport("kernel32.dll")] public static extern IntPtr GlobalFree(IntPtr h);

    public const uint COINIT_MULTITHREADED = 0x0;
    public const uint CLSCTX_INPROC_SERVER = 0x1;

    [DllImport("ole32.dll")] public static extern int CoInitializeEx(IntPtr reserved, uint flags);
    [DllImport("ole32.dll")] public static extern void CoUninitialize();
    [DllImport("ole32.dll")] public static extern int CoCreateInstance(in Guid clsid, IntPtr outer, uint ctx, in Guid iid, out IntPtr ppv);
}
