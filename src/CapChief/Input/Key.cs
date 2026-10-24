namespace CapChief.Input;

/// <summary>
/// Specifies the keyboard keys the app responds to.
/// </summary>
public enum Key
{
    None = 0,
    Ctrl,
    Alt,
    Escape,
    Delete,
    Backspace,
    Enter,
    Space,
    Tab,
    Left,
    Right,
    Up,
    Down,
    Home,
    End,
    A,
    B,
    C,
    D,
    E,
    F,
    G,
    H,
    I,
    J,
    K,
    L,
    M,
    N,
    O,
    P,
    Q,
    R,
    S,
    T,
    U,
    V,
    W,
    X,
    Y,
    Z,
    D0,
    D1,
    D2,
    D3,
    D4,
    D5,
    D6,
    D7,
    D8,
    D9,
    Plus,
    Minus,
}

/// <summary>
/// Represents one keystroke: a key and the modifiers held down with it.
/// </summary>
public readonly record struct Chord(Key Key, bool Ctrl, bool Shift)
{
    private static readonly (Key Key, int VirtualKey)[] Named =
    [
        (Key.Ctrl, Keys.Ctrl),
        (Key.Alt, Keys.Alt),
        (Key.Escape, Keys.Escape),
        (Key.Tab, Keys.Tab),
        (Key.Delete, Keys.Delete),
        (Key.Backspace, Keys.Backspace),
        (Key.Enter, Keys.Enter),
        (Key.Space, Keys.Space),
        (Key.Left, Keys.Left),
        (Key.Right, Keys.Right),
        (Key.Up, Keys.Up),
        (Key.Down, Keys.Down),
        (Key.Home, Keys.Home),
        (Key.End, Keys.End),
        (Key.Plus, Keys.Plus),
        (Key.Plus, Keys.NumpadPlus),
        (Key.Minus, Keys.Minus),
        (Key.Minus, Keys.NumpadMinus),
    ];

    public static Chord Of(int virtualKey)
    {
        return new(KeyOf(virtualKey), Keyboard.CtrlDown, Keyboard.ShiftDown);
    }

    public static Key KeyOf(int vk)
    {
        if (vk >= Keys.Numpad0 && vk <= Keys.Numpad9)
        {
            vk = '0' + (vk - Keys.Numpad0);
        }
        if (vk >= '0' && vk <= '9')
        {
            return Key.D0 + (vk - '0');
        }
        if (vk >= 'A' && vk <= 'Z')
        {
            return Key.A + (vk - 'A');
        }
        foreach ((Key key, int named) in Named)
        {
            if (named == vk)
            {
                return key;
            }
        }
        return Key.None;
    }

    public static int VirtualKeyOf(Key key)
    {
        if (key >= Key.D0 && key <= Key.D9)
        {
            return '0' + (key - Key.D0);
        }
        if (key >= Key.A && key <= Key.Z)
        {
            return 'A' + (key - Key.A);
        }
        foreach ((Key named, int vk) in Named)
        {
            if (named == key)
            {
                return vk;
            }
        }
        return 0;
    }
}

/// <summary>
/// Represents a key that asks for a command, and the modifiers that must be held with it; a
/// modifier left unsaid may be up or down. Alt belongs to global hotkeys only, so a key the app's
/// windows receive never matches a shortcut that holds it.
/// </summary>
public readonly record struct Shortcut(Key Key, bool? Ctrl = null, bool? Shift = null, bool Alt = false)
{
    public string Shown
    {
        get
        {
            string shown = "";
            if (Ctrl == true)
            {
                shown += "Ctrl+";
            }
            if (Alt)
            {
                shown += "Alt+";
            }
            if (Shift == true)
            {
                shown += "Shift+";
            }
            return shown + Spelt(Key);
        }
    }

    public bool Matches(Chord chord)
    {
        return !Alt && chord.Key == Key && (Ctrl is not { } ctrl || ctrl == chord.Ctrl) && (Shift is not { } shift || shift == chord.Shift);
    }

    private static string Spelt(Key k)
    {
        switch (k)
        {
            case Key.Delete:
                return "Del";
            case Key.Escape:
                return "Esc";
            case Key.Left:
                return "<-";
            case Key.Right:
                return "->";
            default:
                if (k >= Key.D0 && k <= Key.D9)
                {
                    return (k - Key.D0).ToString();
                }
                else
                {
                    return k.ToString();
                }
        }
    }
}
