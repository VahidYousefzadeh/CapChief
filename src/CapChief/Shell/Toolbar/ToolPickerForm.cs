namespace CapChief.Shell;

/// <summary>
/// Represents the drop-down under an annotate toolbar button, its first choice lined up with the
/// button, that offers the tools of the button's pair, or the mark colors, to pick from.
/// </summary>
public sealed class ToolPickerForm : Form
{
    private const string ClassName = "CCPicker";

    private readonly Desktop _desk;
    private readonly Tools _tools;
    private readonly Document _doc;
    private readonly StackPanel _toolList;
    private readonly StackPanel _colorRow;
    private readonly Dictionary<MarkKind, ChoiceButton> _toolChoices = new();
    private readonly ChoiceButton[] _colorChoices = new ChoiceButton[Tools.ColorCount];

    private Rect _under;
    private Command? _button;
    private ChoiceButton[] _shown = [];
    private ChoiceButton? _current;

    public event Action<MarkKind>? ToolPicked;

    public event Action<int>? ColorPicked;

    public event Action? Changed;

    public ToolPickerForm(AppIcon appIcon, Desktop desk, Tools tools, Document doc)
        : base(new WindowKind(ClassName, appIcon, doubleClicks: false, redrawOnResize: false, ownsBackground: true, tracksLeaving: true))
    {
        _desk = desk;
        _tools = tools;
        _doc = doc;

        _toolList = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = Theme.Gap,
            Padding = Thickness.All(Theme.Pad),
        };
        foreach (MarkKind kind in Enum.GetValues<MarkKind>())
        {
            _toolChoices[kind] = new ChoiceButton(() => PickTool(kind))
            {
                Glyph = GlyphPainter.GlyphOf(kind),
                Text = Commands.NameOf(kind),
                ShownKey = Commands.ShortcutOf(kind).Shown,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            _toolList.Add(_toolChoices[kind]);
        }

        _colorRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = Theme.Gap,
            Padding = Thickness.All(Theme.Pad),
        };
        for (int i = 0; i < _colorChoices.Length; i++)
        {
            int index = i;
            _colorChoices[i] = new ChoiceButton(() => PickColor(index))
            {
                Glyph = Glyph.Swatch,
                Swatch = Tools.ColorAt(i),
            };
            _colorRow.Add(_colorChoices[i]);
        }

        var panel = new ShadedPanel(Theme.BarRadius);
        panel.Add(_toolList);
        panel.Add(_colorRow);
        Content = panel;
    }

    public Rect OnScreenBounds
    {
        get
        {
            return OnScreen(ContentBounds);
        }
    }

    private ChoiceButton? Highlighted
    {
        get
        {
            return Hovered as ChoiceButton ?? _current;
        }
    }

    public void Create(Window owner)
    {
        Open(FloatingSpec with { TakesFocus = false }, owner);
    }

    public bool HangsFrom(Command button)
    {
        return IsShown && _button == button;
    }

    public void ShowTools(ToolGroup group, Command button, Rect under)
    {
        var pair = new List<ChoiceButton>();
        foreach (MarkKind kind in Enum.GetValues<MarkKind>())
        {
            bool inGroup = Tools.GroupOf(kind) == group;
            _toolChoices[kind].Visibility = Control.VisibleIf(inGroup);
            if (inGroup)
            {
                pair.Add(_toolChoices[kind]);
            }
        }
        _toolChoices[MarkKind.Step].Step = _doc.NextStepNumber;
        ShowOnly(_toolList, pair.ToArray(), _toolChoices[_tools.ShownIn(group)], button, under);
    }

    public void ShowColors(Command button, Rect under)
    {
        ShowOnly(_colorRow, _colorChoices, Array.Find(_colorChoices, c => c.Swatch == _tools.Color), button, under);
    }

    public void Hide()
    {
        if (!IsShown)
        {
            return;
        }
        ResetMouse();
        Window.Hide();
        Changed?.Invoke();
    }

    public bool OnKeyDown(Chord chord)
    {
        if (!IsShown || chord.Ctrl)
        {
            return false;
        }
        switch (chord.Key)
        {
            case Key.Escape:
                Hide();
                return true;
            case Key.Up:
                MoveHighlight(-1);
                return true;
            case Key.Down:
                MoveHighlight(1);
                return true;
            case Key.Enter or Key.Space:
                Highlighted?.OnClick(EventArgs.Empty);
                return true;
            default:
                return false;
        }
    }

    public override void OnMouseLeft()
    {
        base.OnMouseLeft();
        if (IsShown)
        {
            HoverOver(_current);
        }
    }

    protected override void Rescale()
    {
        if (IsShown)
        {
            Reposition();
        }
    }

    private void MoveHighlight(int by)
    {
        int at = Array.IndexOf(_shown, Highlighted);
        int next;
        if (at < 0)
        {
            if (by > 0)
            {
                next = 0;
            }
            else
            {
                next = _shown.Length - 1;
            }
        }
        else
        {
            next = (at + by + _shown.Length) % _shown.Length;
        }
        HoverOver(_shown[next]);
    }

    private void ShowOnly(StackPanel choices, ChoiceButton[] shown, ChoiceButton? current, Command button, Rect under)
    {
        ResetMouse();
        _toolList.Visibility = Control.VisibleIf(choices == _toolList);
        _colorRow.Visibility = Control.VisibleIf(choices == _colorRow);
        _shown = shown;
        _current = current;
        _button = button;
        _under = under;
        Reposition();
        HoverOver(current);
        Changed?.Invoke();
    }

    private void Reposition()
    {
        Size size = Layout(Unbounded);
        int inset;
        if (_shown.Length > 0)
        {
            inset = _shown[0].Bounds.Left - ContentBounds.Left;
        }
        else
        {
            inset = 0;
        }
        ShowUnder(_under, _under.Left - inset, Theme.Gap.Px(Scale), _desk.MonitorAt(_under.Center), size);
    }

    private void PickTool(MarkKind kind)
    {
        Hide();
        ToolPicked?.Invoke(kind);
    }

    private void PickColor(int index)
    {
        Hide();
        ColorPicked?.Invoke(index);
    }
}

/// <summary>
/// Represents one choice in the tool picker: a tool, shown by its icon, name and key, or a color, shown
/// as a drop of it.
/// </summary>
public sealed class ChoiceButton : Button
{
    private readonly Action _picked;

    public ChoiceButton(Action picked)
    {
        _picked = picked;

        Height = Theme.ButtonSide;
        MinWidth = Theme.ButtonSide;
        CornerRadius = CornerRadius.OfHeight(Theme.ButtonRadiusFraction);
        Cursor = CursorShape.Hand;
        HoverBackground = Color.FromArgb(Palette.ActiveArgb);
        PressedBackground = Color.FromArgb(Palette.PressArgb);
    }

    public Glyph Glyph { get; init; }

    public Color Swatch { get; init; }

    public string Text { get; init; } = "";

    public string ShownKey { get; init; } = "";

    public int Step { get; set; }

    public override void OnClick(EventArgs e)
    {
        _picked();
    }

    protected override Size MeasureOverride(Size available)
    {
        if (Text.Length == 0)
        {
            return default;
        }
        int side = Theme.ButtonSide.Px(Scale);
        return new(side + Draw.Measure(Text, Font).Width + Theme.ChoiceKeyGap.Px(Scale) + Draw.Measure(ShownKey, Font).Width + Theme.ChoiceEndPad.Px(Scale), side);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Rect r = Bounds;
        Rect icon = Rect.At(r.Left, r.Top, r.Height, r.Height);
        GlyphPainter.Draw(e.Draw, Glyph, icon, new GlyphState(Step, Swatch, ""));
        if (Text.Length == 0)
        {
            return;
        }

        int name = e.Draw.Measure(Text, Font).Width;
        int key = e.Draw.Measure(ShownKey, Font).Width;
        int end = r.Right - Theme.ChoiceEndPad.Px(Scale);
        e.Draw.Text(Text, Rect.At(icon.Right, r.Top, name, r.Height), Foreground, Font);
        e.Draw.Text(ShownKey, Rect.At(end - key, r.Top, key, r.Height), Color.FromArgb(Palette.KeyTextArgb), Font);
    }
}
