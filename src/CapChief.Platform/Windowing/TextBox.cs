namespace CapChief.Windowing;

/// <summary>
/// Represents a native multiline edit control hosted inside a parent window.
/// </summary>
public sealed class TextBox
{
    private readonly ITextBoxEvents _events;
    private readonly Win32.SubclassProc _sub;

    private IntPtr _h;
    private Window _parent = Window.None;

    public TextBox(ITextBoxEvents events)
    {
        _events = events;
        _sub = EditProc;
    }

    public bool IsOpen
    {
        get
        {
            return _h != IntPtr.Zero;
        }
    }

    public string Text
    {
        get
        {
            int len = Win32.GetWindowTextLengthW(_h);
            if (len <= 0)
            {
                return "";
            }
            var chars = new char[len + 1];
            Win32.GetWindowTextW(_h, chars, chars.Length);
            return new string(chars, 0, len);
        }
    }

    public bool Owns(EditRef edit)
    {
        return edit.Is(_h);
    }

    public void Open(Window parent, Rect r, string? text, IntPtr font)
    {
        _parent = parent;
        _h = Win32.CreateWindowExW(0, "EDIT", text ?? "", Win32.WS_CHILD | Win32.WS_VISIBLE | Win32.ES_MULTILINE | Win32.ES_AUTOVSCROLL, r.Left, r.Top, r.Width, r.Height, parent.Handle, IntPtr.Zero, Win32.GetModuleHandleW(null), IntPtr.Zero);

        SetFont(font);
        Win32.SetWindowSubclass(_h, _sub, (UIntPtr)1, IntPtr.Zero);
        Win32.SetFocus(_h);
        Win32.SendMessageW(_h, Win32.EM_SETSEL, (IntPtr)(-1), (IntPtr)(-1));
    }

    public void SetFont(IntPtr font)
    {
        Win32.SendMessageW(_h, Win32.WM_SETFONT, font, (IntPtr)1);
        // A new font brings the edit control's own margins back, so they are taken off again.
        Win32.SendMessageW(_h, Win32.EM_SETMARGINS, (IntPtr)Win32.EC_MARGINS, IntPtr.Zero);
    }

    public void Resize(int w, int h)
    {
        Win32.SetWindowPos(_h, IntPtr.Zero, 0, 0, w, h, Win32.SWP_NOMOVE | Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE);
    }

    public void Refresh()
    {
        Win32.InvalidateRect(_h, IntPtr.Zero, true);
    }

    public void Close()
    {
        IntPtr edit = _h;
        _h = IntPtr.Zero;
        Win32.DestroyWindow(edit);
        Win32.SetFocus(_parent.Handle);
    }

    public Rect Measure(string text, int width, IntPtr font, uint format)
    {
        var r = new Rect { Right = width };
        // With the box closed this is the screen's device context, which measures text the same.
        IntPtr dc = Win32.GetDC(_h);
        using (new FontScope(dc, font))
        {
            Win32.DrawTextW(dc, text, text.Length, ref r, Win32.DT_CALCRECT | format);
        }
        Win32.ReleaseDC(_h, dc);
        return r;
    }

    public Rect ClientBox()
    {
        Win32.GetClientRect(_h, out Rect cr);
        return cr;
    }

    public Point OriginInParent()
    {
        Point org = default;
        Win32.ClientToScreen(_h, ref org);
        Win32.ScreenToClient(_parent.Handle, ref org);
        return org;
    }

    private IntPtr EditProc(IntPtr h, uint m, IntPtr w, IntPtr l, UIntPtr id, IntPtr data)
    {
        if (m == Win32.WM_PAINT)
        {
            Win32.InvalidateRect(h, IntPtr.Zero, false);
            return Win32.DefSubclassProc(h, m, w, l, id, data);
        }
        if (m == Win32.WM_KEYDOWN && _events.OnTextKey(w.ToInt32()))
        {
            // The key did something else, so the character it makes is not typed: Ctrl+Minus makes
            // an unseen control character.
            Win32.PeekMessageW(out _, h, Win32.WM_CHAR, Win32.WM_CHAR, Win32.PM_REMOVE);
            return IntPtr.Zero;
        }
        if (m == Win32.WM_MOUSEWHEEL && (w.ToInt64() & Win32.MK_CONTROL) != 0)
        {
            return Win32.SendMessageW(_parent.Handle, m, w, l);
        }
        if (m == Win32.WM_KILLFOCUS)
        {
            _events.OnTextFocusLost();
            return IntPtr.Zero;
        }

        IntPtr r = Win32.DefSubclassProc(h, m, w, l, id, data);
        if (m is Win32.WM_CHAR or Win32.WM_KEYDOWN or Win32.WM_CUT or Win32.WM_PASTE or Win32.WM_CLEAR or Win32.WM_UNDO)
        {
            _events.OnTextEdited();
        }
        return r;
    }
}
