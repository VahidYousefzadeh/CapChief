namespace CapChief.Shell;

/// <summary>
/// Represents one thing the user can ask for, by key, button or menu item: its name, its keys, the
/// tooltip its button shows, when it may run and what it does.
/// </summary>
public sealed class Command
{
    private readonly Action _run;

    public Command(string name, Action run)
    {
        Name = name;
        _run = run;
    }

    public string Name { get; }

    public Shortcut[] Keys { get; init; } = [];

    public Func<bool> Enabled { get; init; } = () => true;

    public Func<string> Describe { get; init; } = () => "";

    public bool LeavesTextOpen { get; init; }

    public bool IsHotkeyTaken { get; set; }

    public string ShownKey
    {
        get
        {
            if (Keys.Length > 0 && !IsHotkeyTaken)
            {
                return Keys[0].Shown;
            }
            else
            {
                return "";
            }
        }
    }

    public string Tip
    {
        get
        {
            if (ShownKey is { Length: > 0 } key)
            {
                return Describe().Replace("{key}", key);
            }
            else
            {
                return Describe().Replace(" ({key})", "");
            }
        }
    }

    public bool Run()
    {
        if (!Enabled())
        {
            return false;
        }
        _run();
        return true;
    }

    public static Command? Find(IReadOnlyList<Command> table, Chord chord)
    {
        foreach (Command c in table)
        {
            foreach (Shortcut s in c.Keys)
            {
                if (s.Matches(chord))
                {
                    return c;
                }
            }
        }
        return null;
    }
}

/// <summary>
/// Provides every command the user can give: the capture commands that answer anywhere, the app's
/// own, and one table of keys for each mode.
/// </summary>
public sealed class Commands
{
    private const int Fine = 1, Coarse = 10;

    private readonly CaptureWorkflow _workflow;
    private readonly WorkspaceService _workspace;
    private readonly Tools _tools;
    private readonly Selecting _selecting;
    private readonly Drawing _drawing;
    private readonly Command _cancelSelecting;
    private readonly Command _deleteMark;
    private readonly Command _undo;
    private readonly Command _redo;
    private readonly Command[] _selectingRows;
    private readonly Command[] _annotatingRows;
    private readonly Command[] _liveRows;
    private readonly Command[] _reviewingRows;

    public Commands(CaptureWorkflow workflow, WorkspaceService workspace, CaptureService captureService, Review review, Selecting selecting, Drawing drawing, Tools tools, AppShell shell)
    {
        _workflow = workflow;
        _workspace = workspace;
        _tools = tools;
        _selecting = selecting;
        _drawing = drawing;

        Capture = new Command("capture", () => captureService.StartCapture(CaptureKind.Frozen))
        {
            Keys = [new(Key.C, Ctrl: true, Alt: true)],
            Enabled = () => captureService.CanStart,
            Describe = () => "Capture ({key})",
        };
        TimedCapture = new Command("timer", captureService.StartOrCancelTimed)
        {
            Keys = [new(Key.T, Ctrl: true, Alt: true)],
            Enabled = () => captureService.CanStart || workflow.CurrentMode == Mode.CountingDown,
            Describe = () =>
            {
                if (workflow.CurrentMode == Mode.CountingDown)
                {
                    return "Cancel ({key})";
                }
                else
                {
                    return "Timed capture ({key})";
                }
            },
        };
        Record = new Command("record", captureService.StartOrStopRecording)
        {
            Keys = [new(Key.G, Ctrl: true, Alt: true)],
            Enabled = () => captureService.CanStart || workflow.CurrentMode == Mode.Recording,
            Describe = () =>
            {
                if (workflow.CurrentMode == Mode.Recording)
                {
                    return "Stop ({key})";
                }
                else
                {
                    return "Record GIF ({key})";
                }
            },
        };
        Hotkeys = [Capture, TimedCapture, Record];

        HideToTray = new Command("hide-to-tray", shell.HideToTray) { Describe = () => "Hide to tray" };
        Close = new Command("close", workspace.EndCapture)
        {
            Keys = [new(Key.Escape)],
            Enabled = () => workspace.HasCapture,
            Describe = () => "Close ({key})",
        };
        ToggleToolbar = new Command("toggle-toolbar", shell.ToggleToolbar)
        {
            Enabled = () => workflow.CurrentMode == Mode.Idle,
        };
        Quit = new Command("quit", shell.Quit);

        ArrowTool = ToolPair("arrow", ToolGroup.Arrow, shell);
        RectangleTool = ToolPair("rect", ToolGroup.Rect, shell);
        TextTool = ToolPair("text", ToolGroup.Text, shell);
        RedactTool = ToolPair("redact", ToolGroup.Redact, shell);
        Color = DropDown("color", shell.ToggleColorPicker, () => "Color");

        SaveToHistory = new Command("save-to-history", workspace.SaveToHistory)
        {
            Keys = [new(Key.H, Ctrl: true)],
            Enabled = () => workspace.HasCapture && !workspace.IsSavedToHistory,
            Describe = () => "Save to history ({key})",
        };
        History = new Command("history", shell.ToggleHistory)
        {
            Enabled = () => workflow.CurrentMode == Mode.Idle && workspace.HasHistory,
            Describe = () => "History",
        };
        Delete = new Command("delete", workspace.Delete)
        {
            Enabled = () => workflow.CurrentMode is Mode.Annotating or Mode.Reviewing,
            Describe = () => "Delete",
        };
        _deleteMark = new Command("delete-mark", workspace.DeleteSelectedMark)
        {
            Keys = [new(Key.Delete), new(Key.Backspace)],
            Enabled = () => workspace.HasSelectedMark,
        };
        Copy = new Command("copy", captureService.CopyToClipboard)
        {
            Keys = [new(Key.C, Ctrl: true), new(Key.Enter)],
            Enabled = () => workspace.IsAnnotating,
            Describe = () => "Copy ({key})",
        };
        Save = new Command("save", captureService.Save)
        {
            Keys = [new(Key.S, Ctrl: true)],
            Enabled = () => workspace.IsAnnotating,
            Describe = () => "Save as PNG ({key})",
        };
        _undo = new Command("undo", workspace.Undo) { Keys = [new(Key.Z, Ctrl: true, Shift: false)] };
        _redo = new Command("redo", workspace.Redo)
        {
            Keys = [new(Key.Z, Ctrl: true, Shift: true), new(Key.Y, Ctrl: true)],
        };

        Func<bool> hasClip = () => review.Clip is not null;
        Play = new Command("play", review.TogglePlay)
        {
            Keys = [new(Key.Space)],
            Enabled = hasClip,
            Describe = () =>
            {
                if (review.IsPlaying)
                {
                    return "Pause ({key})";
                }
                else
                {
                    return "Play ({key})";
                }
            },
        };
        Speed = new Command("speed", review.CycleSpeed)
        {
            Keys = [new(Key.S, Ctrl: false)],
            Enabled = hasClip,
            Describe = () => "Playback speed ({key})",
        };
        SaveClip = new Command("save-clip", captureService.SaveClip)
        {
            Keys = [new(Key.S, Ctrl: true)],
            Enabled = () => workflow.CurrentMode == Mode.Reviewing,
            Describe = () => "Save as GIF ({key})",
        };

        _cancelSelecting = Escape(() =>
        {
            if (!_selecting.CancelTypedSize())
            {
                _workflow.CancelSelecting();
            }
        });
        _selectingRows = SelectingTable();
        _annotatingRows = AnnotatingTable();
        _liveRows = [Escape(workflow.CancelLive)];
        _reviewingRows = ReviewingTable(review, hasClip);
    }

    public Command Capture { get; }

    public Command TimedCapture { get; }

    public Command Record { get; }

    public IReadOnlyList<Command> Hotkeys { get; }

    public Command HideToTray { get; }

    public Command Close { get; }

    public Command ToggleToolbar { get; }

    public Command History { get; }

    public Command Quit { get; }

    public Command ArrowTool { get; }

    public Command RectangleTool { get; }

    public Command TextTool { get; }

    public Command RedactTool { get; }

    public Command Color { get; }

    public Command SaveToHistory { get; }

    public Command Delete { get; }

    public Command Copy { get; }

    public Command Save { get; }

    public Command Play { get; }

    public Command Speed { get; }

    public Command SaveClip { get; }

    public bool RunKey(Chord chord)
    {
        return Command.Find(For(_workflow.CurrentMode), chord) is { } command && Run(command);
    }

    public void RightClick()
    {
        if (_workflow.CurrentMode == Mode.Selecting)
        {
            Run(_cancelSelecting);
        }
    }

    public void RunHotkey(int id)
    {
        if (CapChief.Shell.Hotkeys.Find(id, Hotkeys) is { } command)
        {
            Run(command);
        }
    }

    public bool Run(Command command)
    {
        if (!command.Enabled())
        {
            return false;
        }
        _workflow.FinishPress();
        if (!command.LeavesTextOpen)
        {
            _drawing.EndTextEdit(cancel: false);
        }
        return command.Run();
    }

    public static Shortcut ShortcutOf(MarkKind kind)
    {
        Key key;
        switch (kind)
        {
            case MarkKind.Arrow:
                key = Key.A;
                break;
            case MarkKind.Pen:
                key = Key.P;
                break;
            case MarkKind.Rectangle:
                key = Key.R;
                break;
            case MarkKind.Ellipse:
                key = Key.E;
                break;
            case MarkKind.Text:
                key = Key.T;
                break;
            case MarkKind.Step:
                key = Key.N;
                break;
            case MarkKind.Mosaic:
                key = Key.M;
                break;
            case MarkKind.Blackout:
                key = Key.B;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
        return new(key, Ctrl: false, Shift: false);
    }

    public static string NameOf(MarkKind kind)
    {
        switch (kind)
        {
            case MarkKind.Arrow:
                return "Arrow";
            case MarkKind.Pen:
                return "Pen";
            case MarkKind.Rectangle:
                return "Rectangle";
            case MarkKind.Ellipse:
                return "Ellipse";
            case MarkKind.Mosaic:
                return "Mosaic redact";
            case MarkKind.Blackout:
                return "Blackout redact";
            case MarkKind.Text:
                return "Text";
            case MarkKind.Step:
                return "Numbered step";
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    public IReadOnlyList<Command> For(Mode mode)
    {
        switch (mode)
        {
            case Mode.Selecting:
                return _selectingRows;
            case Mode.Annotating:
                return _annotatingRows;
            case Mode.CountingDown or Mode.Recording:
                return _liveRows;
            case Mode.Reviewing:
                return _reviewingRows;
            default:
                return [];
        }
    }

    private Command[] SelectingTable()
    {
        var rows = new List<Command>
        {
            _cancelSelecting,
            new("accept", _workflow.AcceptSelection) { Keys = [new(Key.Enter, Ctrl: false)] },
            new("size-tab", () => _selecting.FocusNextSizeField())
            {
                Keys = [new(Key.Tab, Ctrl: false)],
            },
            new("size-back", () => _selecting.EraseSizeDigit())
            {
                Keys = [new(Key.Backspace, Ctrl: false)],
            },
        };
        for (int d = 0; d <= 9; d++)
        {
            int digit = d;
            rows.Add(new Command($"size-{d}", () => _selecting.TypeSizeDigit(digit)) { Keys = [new(Key.D0 + d, Ctrl: false)] });
        }
        return rows.ToArray();
    }

    private Command[] AnnotatingTable()
    {
        var rows = new List<Command>
        {
            Escape(() =>
            {
                if (!_workspace.DropSelectedMark())
                {
                    _workspace.EndCapture();
                }
            }),
            new("color-next", () => _workspace.AdvanceColor(1))
            {
                Keys = [new(Key.Right, Ctrl: true)],
                LeavesTextOpen = true,
            },
            new("color-back", () => _workspace.AdvanceColor(-1))
            {
                Keys = [new(Key.Left, Ctrl: true)],
                LeavesTextOpen = true,
            },
            SaveToHistory,
            _deleteMark,
            Copy,
            Save,
            _undo,
            _redo,
        };
        foreach (MarkKind kind in Enum.GetValues<MarkKind>())
        {
            rows.Add(new Command($"pick-{kind.ToString().ToLowerInvariant()}", () => _tools.PickTool(kind))
            {
                Keys = [ShortcutOf(kind)],
                LeavesTextOpen = true,
            });
        }
        for (int d = 1; d <= 9; d++)
        {
            int digit = d;
            rows.Add(new Command($"color-{d}", () => _workspace.PickColor(digit - 1))
            {
                Keys = [new(Key.D0 + d, Ctrl: false)],
                LeavesTextOpen = true,
            });
        }
        (string Name, Key Key, int Dx, int Dy)[] arrows =
        [
            ("left", Key.Left, -1, 0),
            ("right", Key.Right, 1, 0),
            ("up", Key.Up, 0, -1),
            ("down", Key.Down, 0, 1),
        ];
        foreach ((string name, Key key, int dx, int dy) in arrows)
        {
            rows.Add(new Command($"nudge-{name}", () => _workspace.Nudge(new Point(dx * Fine, dy * Fine)))
            {
                Keys = [new(key, Ctrl: false, Shift: false)],
            });
            rows.Add(new Command($"nudge-{name}-far", () => _workspace.Nudge(new Point(dx * Coarse, dy * Coarse)))
            {
                Keys = [new(key, Ctrl: false, Shift: true)],
            });
        }
        return rows.ToArray();
    }

    private Command[] ReviewingTable(Review review, Func<bool> hasClip)
    {
        Command OnClip(string name, Action run, Shortcut key)
        {
            return new(name, run) { Keys = [key], Enabled = hasClip };
        }

        return
        [
            Escape(_workspace.EndCapture),
            SaveClip,
            SaveToHistory,
            Play,
            Speed,
            _undo,
            _redo,
            OnClip("step-back", () => review.Advance(-Fine), new Shortcut(Key.Left, Ctrl: false, Shift: false)),
            OnClip("jump-back", () => review.Advance(-Coarse), new Shortcut(Key.Left, Ctrl: false, Shift: true)),
            OnClip("step-on", () => review.Advance(Fine), new Shortcut(Key.Right, Ctrl: false, Shift: false)),
            OnClip("jump-on", () => review.Advance(Coarse), new Shortcut(Key.Right, Ctrl: false, Shift: true)),
            OnClip("to-start", () => review.Jump(toEnd: false), new Shortcut(Key.Home)),
            OnClip("to-end", () => review.Jump(toEnd: true), new Shortcut(Key.End)),
            OnClip("trim-head", () => review.Trim(head: true), new Shortcut(Key.I)),
            OnClip("trim-tail", () => review.Trim(head: false), new Shortcut(Key.O)),
            OnClip("untrim", review.Untrim, new Shortcut(Key.X, Ctrl: true, Shift: true)),
        ];
    }

    private static Command Escape(Action run)
    {
        return new("escape", run) { Keys = [new(Key.Escape)] };
    }

    private Command ToolPair(string name, ToolGroup group, AppShell shell)
    {
        return DropDown(name, button => shell.ToggleToolPicker(group, button), () => $"{NameOf(_tools.ShownIn(group))} ({ShortcutOf(_tools.ShownIn(group)).Shown})");
    }

    private static Command DropDown(string name, Action<Command> toggle, Func<string> describe)
    {
        Command? button = null;
        button = new Command(name, () => toggle(button!)) { Describe = describe, LeavesTextOpen = true };
        return button;
    }
}
