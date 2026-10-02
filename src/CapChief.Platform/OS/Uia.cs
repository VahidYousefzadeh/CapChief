namespace CapChief.OS;

/// <summary>
/// Wraps the UI Automation COM interfaces used to walk the elements inside a window.
/// </summary>
public sealed class Uia
{
    private const int ReleaseSlot = 2;

    private const int ElementFromHandleSlot = 6;
    private const int ControlViewWalkerSlot = 14;
    private const int ConnectionTimeoutSlot = 61;
    private const int TransactionTimeoutSlot = 63;

    private const int FirstChildSlot = 4;
    private const int NextSiblingSlot = 6;

    private const int BoundingRectangleSlot = 43;

    private const uint ConnectionTimeoutMs = 2000;

    private const uint TransactionTimeoutMs = 10000;

    private static readonly Guid ClsidCUIAutomation8 = new("e22ad333-b25f-460c-83d0-0581107395c9");
    private static readonly Guid IidIUIAutomation2 = new("34723aff-0c9d-49d0-9896-7ab52df8cd8a");
    private IntPtr _auto;
    private IntPtr _walker;

    private bool IsRunning
    {
        get
        {
            return _auto != IntPtr.Zero;
        }
    }

    public bool Start()
    {
        if (IsRunning)
        {
            return true;
        }
        if (Win32.CoInitializeEx(IntPtr.Zero, Win32.COINIT_MULTITHREADED) < 0)
        {
            return false;
        }
        if (Win32.CoCreateInstance(in ClsidCUIAutomation8, IntPtr.Zero, Win32.CLSCTX_INPROC_SERVER, in IidIUIAutomation2, out IntPtr auto) < 0)
        {
            Win32.CoUninitialize();
            return false;
        }

        _auto = auto;
        Timeout(ConnectionTimeoutSlot, ConnectionTimeoutMs);
        Timeout(TransactionTimeoutSlot, TransactionTimeoutMs);

        _walker = Fetch(_auto, ControlViewWalkerSlot);
        if (_walker != IntPtr.Zero)
        {
            return true;
        }

        Stop();
        return false;
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }
        Release(_walker);
        Release(_auto);
        _walker = IntPtr.Zero;
        _auto = IntPtr.Zero;
        Win32.CoUninitialize();
    }

    public IntPtr FromHandle(IntPtr hwnd)
    {
        if (!IsRunning)
        {
            return IntPtr.Zero;
        }
        return Call(_auto, ElementFromHandleSlot, hwnd);
    }

    public IntPtr FirstChild(IntPtr element)
    {
        return Walk(FirstChildSlot, element);
    }

    public IntPtr NextSibling(IntPtr element)
    {
        return Walk(NextSiblingSlot, element);
    }

    public unsafe bool BoundsOf(IntPtr element, out Rect r)
    {
        r = default;
        if (element == IntPtr.Zero)
        {
            return false;
        }
        fixed (Rect* p = &r)
        {
            int hr = ((delegate* unmanaged<IntPtr, Rect*, int>)Vt(element)[BoundingRectangleSlot])(element, p);
            return hr >= 0;
        }
    }

    public static unsafe void Release(IntPtr com)
    {
        if (com != IntPtr.Zero)
        {
            ((delegate* unmanaged<IntPtr, uint>)Vt(com)[ReleaseSlot])(com);
        }
    }

    private IntPtr Walk(int slot, IntPtr element)
    {
        if (_walker == IntPtr.Zero || element == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }
        return Call(_walker, slot, element);
    }

    private unsafe void Timeout(int slot, uint ms)
    {
        ((delegate* unmanaged<IntPtr, uint, int>)Vt(_auto)[slot])(_auto, ms);
    }

    private static unsafe IntPtr Call(IntPtr obj, int slot, IntPtr arg)
    {
        IntPtr got;
        int hr = ((delegate* unmanaged<IntPtr, IntPtr, IntPtr*, int>)Vt(obj)[slot])(obj, arg, &got);
        if (hr < 0)
        {
            return IntPtr.Zero;
        }
        else
        {
            return got;
        }
    }

    private static unsafe IntPtr Fetch(IntPtr obj, int slot)
    {
        IntPtr got;
        int hr = ((delegate* unmanaged<IntPtr, IntPtr*, int>)Vt(obj)[slot])(obj, &got);
        if (hr < 0)
        {
            return IntPtr.Zero;
        }
        else
        {
            return got;
        }
    }

    private static unsafe void** Vt(IntPtr obj)
    {
        return *(void***)obj;
    }
}
