namespace CapChief.Workspace;

/// <summary>
/// Tracks the drawing tool, mark color and text size the next press draws with, and which tool of
/// each pair its toolbar button shows.
/// </summary>
public sealed class Tools
{
    private static readonly Color[] Colors =
    [
        Color.FromArgb(0xFFE61E1Eu), Color.FromArgb(0xFFFFAA00u), Color.FromArgb(0xFF1EAA3Cu),
        Color.FromArgb(0xFF1E6EF0u), Color.FromArgb(0xFF000000u), Color.FromArgb(0xFFFFFFFFu),
    ];

    private static readonly MarkKind[] Kinds = Enum.GetValues<MarkKind>();

    private readonly MarkKind[] _shownInGroup = FirstOfEachGroup();

    private MarkKind _tool = MarkKind.Arrow;
    private int _colorIndex;
    private TextSize _textSize = TextSize.Normal;

    public event Action? Changed;

    public static int ColorCount
    {
        get
        {
            return Colors.Length;
        }
    }

    public MarkKind CurrentTool
    {
        get
        {
            return _tool;
        }
    }

    public Color Color
    {
        get
        {
            return Colors[_colorIndex];
        }
    }

    public TextSize TextSize
    {
        get
        {
            return _textSize;
        }
    }

    public static Color ColorAt(int i)
    {
        return Colors[i];
    }

    public bool SetColor(int i)
    {
        if (i < 0 || i >= Colors.Length || i == _colorIndex)
        {
            return false;
        }
        _colorIndex = i;
        Changed?.Invoke();
        return true;
    }

    public void MatchColorOf(Mark? mark)
    {
        if (mark is { TakesColor: true })
        {
            SetColor(Array.IndexOf(Colors, mark.Color));
        }
    }

    public bool AdvanceColor(int delta)
    {
        return SetColor(((_colorIndex + delta) % Colors.Length + Colors.Length) % Colors.Length);
    }

    public bool SetTextSize(TextSize size)
    {
        if (size == _textSize)
        {
            return false;
        }
        _textSize = size;
        Changed?.Invoke();
        return true;
    }

    public void MatchTextSizeOf(Mark? mark)
    {
        if (mark is TextMark text)
        {
            SetTextSize(text.Size);
        }
    }

    public MarkKind ShownIn(ToolGroup g)
    {
        return _shownInGroup[(int)g];
    }

    public bool IsIn(ToolGroup g)
    {
        return GroupOf(_tool) == g;
    }

    public void PickTool(MarkKind k)
    {
        _tool = k;
        ToolGroup g = GroupOf(k);
        if (g != ToolGroup.None)
        {
            _shownInGroup[(int)g] = k;
        }
        Changed?.Invoke();
    }

    public static ToolGroup GroupOf(MarkKind kind)
    {
        switch (kind)
        {
            case MarkKind.Arrow or MarkKind.Pen or MarkKind.Highlighter:
                return ToolGroup.Arrow;
            case MarkKind.Rectangle or MarkKind.Ellipse:
                return ToolGroup.Rect;
            case MarkKind.Mosaic or MarkKind.Blackout:
                return ToolGroup.Redact;
            case MarkKind.Text or MarkKind.Step:
                return ToolGroup.Text;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    public static MarkKind FirstIn(ToolGroup g)
    {
        foreach (MarkKind k in Kinds)
        {
            if (GroupOf(k) == g)
            {
                return k;
            }
        }
        return default;
    }

    private static MarkKind[] FirstOfEachGroup()
    {
        var shown = new MarkKind[Enum.GetValues<ToolGroup>().Length];
        for (int i = 0; i < shown.Length; i++)
        {
            shown[i] = FirstIn((ToolGroup)i);
        }
        return shown;
    }
}

/// <summary>
/// Specifies a pair of related marks that share one toolbar button.
/// </summary>
public enum ToolGroup
{
    None,
    Arrow,
    Rect,
    Text,
    Redact,
}
