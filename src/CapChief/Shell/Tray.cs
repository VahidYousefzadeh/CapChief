namespace CapChief.Shell;

/// <summary>
/// Manages the tray icon, its context menu, and the notice it shows when a capture shortcut does not
/// work.
/// </summary>
public sealed class Tray
{
    private const int CaptureItem = 1, TimedItem = 2, RecordItem = 3, ToolbarItem = 4, AboutItem = 5, QuitItem = 6;

    private readonly AppIcon _appIcon;
    private readonly Commands _commands;
    private readonly Action<Point> _showAbout;
    private readonly TrayIcon _icon = new();

    private Window _owner = Window.None;

    public Tray(AppIcon appIcon, Commands commands, Action<Point> showAbout)
    {
        _appIcon = appIcon;
        _commands = commands;
        _showAbout = showAbout;
    }

    public void Add(Window owner)
    {
        _owner = owner;
        string key = _commands.Capture.ShownKey;
        string tip;
        if (key.Length > 0)
        {
            tip = $"{Product.Name} - {key} to capture";
        }
        else
        {
            tip = Product.Name;
        }
        _icon.Add(owner, _appIcon, tip, AppMessages.Tray);
        if (Notice() is { } notice)
        {
            _icon.ShowNotice(Product.Name, notice);
        }
    }

    public void Restore()
    {
        _icon.Restore();
    }

    public void Remove()
    {
        _icon.Remove();
    }

    public void OnMessage(AppSignal signal, Mode mode, bool toolbarHidden, bool historyKept)
    {
        switch (TrayIcon.Decode(signal, out Point at))
        {
            case TrayAction.Chosen:
                _commands.Run(_commands.ToggleToolbar);
                break;
            case TrayAction.Menu:
                ShowMenu(at, mode, toolbarHidden, historyKept);
                break;
        }
    }

    private string? Notice()
    {
        string[] taken = _commands.Hotkeys.Where(c => c.IsHotkeyTaken).Select(c => c.Keys[0].Shown).ToArray();
        if (taken.Length == 0)
        {
            return null;
        }
        string ending;
        if (taken.Length == 1)
        {
            ending = " is already used by another app, so it does nothing here.";
        }
        else
        {
            ending = " are already used by other apps, so they do nothing here.";
        }
        return string.Join(" and ", taken) + ending;
    }

    private void ShowMenu(Point at, Mode mode, bool toolbarHidden, bool historyKept)
    {
        var menu = new PopupMenu();
        if (!menu.Open())
        {
            return;
        }

        menu.Add(CaptureItem, $"&Capture\t{_commands.Capture.ShownKey}");
        string timerText;
        if (mode == Mode.CountingDown)
        {
            timerText = "&Cancel timed capture\t";
        }
        else
        {
            timerText = "&Timed capture\t";
        }
        menu.Add(TimedItem, timerText + _commands.TimedCapture.ShownKey);
        string recordText;
        if (mode == Mode.Recording)
        {
            recordText = "&Stop recording\t";
        }
        else
        {
            recordText = "&Record an area\t";
        }
        menu.Add(RecordItem, recordText + _commands.Record.ShownKey);
        menu.AddSeparator();
        string toolbarText;
        if (toolbarHidden)
        {
            toolbarText = "&Show toolbar";
        }
        else
        {
            toolbarText = "&Hide toolbar";
        }
        menu.Add(ToolbarItem, toolbarText);
        menu.AddSeparator();
        menu.Add(AboutItem, $"&About {Product.Name}");
        string quitSuffix;
        if (historyKept)
        {
            quitSuffix = " (clears the history)";
        }
        else
        {
            quitSuffix = "";
        }
        menu.Add(QuitItem, $"&Quit {Product.Name}{quitSuffix}");
        menu.Default(CaptureItem);

        Command? command;
        switch (menu.ShowAt(_owner, at))
        {
            case CaptureItem:
                command = _commands.Capture;
                break;
            case TimedItem:
                command = _commands.TimedCapture;
                break;
            case RecordItem:
                command = _commands.Record;
                break;
            case ToolbarItem:
                command = _commands.ToggleToolbar;
                break;
            case AboutItem:
                _showAbout(at);
                command = null;
                break;
            case QuitItem:
                command = _commands.Quit;
                break;
            default:
                command = null;
                break;
        }
        if (command is not null)
        {
            _commands.Run(command);
        }
    }
}
