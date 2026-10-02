namespace CapChief.Shell;

/// <summary>
/// Registers the capture commands' shortcuts as global hotkeys, which answer whatever window has
/// focus, marks each one another app already holds, and finds the command Windows reports.
/// </summary>
public static class Hotkeys
{
    public static void Register(Window window, IReadOnlyList<Command> commands)
    {
        for (int i = 0; i < commands.Count; i++)
        {
            if (commands[i].Keys is not [var key, ..])
            {
                continue;
            }
            var hotkey = Hotkey.Of(Chord.VirtualKeyOf(key.Key), key.Ctrl == true, key.Alt, key.Shift == true);
            commands[i].IsHotkeyTaken = !window.AddHotkey(IdOf(i), hotkey);
        }
    }

    public static void Unregister(Window window, IReadOnlyList<Command> commands)
    {
        for (int i = 0; i < commands.Count; i++)
        {
            window.DropHotkey(IdOf(i));
        }
    }

    public static Command? Find(int id, IReadOnlyList<Command> commands)
    {
        int i = id - 1;
        if (i >= 0 && i < commands.Count)
        {
            return commands[i];
        }
        else
        {
            return null;
        }
    }

    private static int IdOf(int index)
    {
        return index + 1;
    }
}
