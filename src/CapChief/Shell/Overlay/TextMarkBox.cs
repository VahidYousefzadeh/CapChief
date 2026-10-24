namespace CapChief.Shell;

/// <summary>
/// Represents the editable text box used to type a text mark, and works out the box a text mark
/// needs at each size.
/// </summary>
public sealed class TextMarkBox : ITextBoxEvents
{
    private const int PadFloorPx = 4;
    private const float PadDip = 6f;

    private const int RuleFloorPx = 1;
    private const float RuleDip = 2f;

    private static readonly Shortcut[] Finish = [new(Key.Escape), new(Key.Enter, Ctrl: true)];

    private readonly Desktop _desk;
    private readonly Metrics _metrics;
    private readonly TextBox _box;

    private Window _parent = Window.None;
    private Rect _rc;
    private Rect _opened;
    private Rect _limit;
    private TextSize _size;

    public event Action? EndRequested;
    public event Func<Chord, bool>? KeyDown;

    public TextMarkBox(Desktop desk, Metrics metrics)
    {
        _desk = desk;
        _metrics = metrics;
        _box = new TextBox(this);
    }

    public bool IsOpen
    {
        get
        {
            return _box.IsOpen;
        }
    }

    public Rect Bounds
    {
        get
        {
            return _rc;
        }
    }

    public bool Owns(EditRef edit)
    {
        return _box.Owns(edit);
    }

    public void Host(Window parent)
    {
        _parent = parent;
    }

    public void OpenAt(Rect r, Rect limit, string? text, TextSize size)
    {
        _rc = _opened = r;
        _limit = limit;
        _size = size;
        _box.Open(_parent, r, text, _metrics.Text(size));
        Fit();
    }

    /// <summary>
    /// Changes the size of the text being typed: the box the typing began in grows or shrinks by as
    /// much as the text, and the box fits the text again.
    /// </summary>
    public void Resize(TextSize size)
    {
        if (!_box.IsOpen || size == _size)
        {
            return;
        }
        _opened = Scaled(_opened, Metrics.TextFactor(size) / Metrics.TextFactor(_size), _limit);
        _size = size;
        _box.SetFont(_metrics.Text(size));
        Fit();
        Refresh();
    }

    /// <summary>
    /// Gives the box a finished text mark needs at another size: its box grown or shrunk by as much
    /// as the text from its top-left corner, then fitted to the text as a box being typed in is.
    /// </summary>
    public Rect Resized(TextMark mark, TextSize size, Rect limit)
    {
        Rect least = Scaled(mark.Bounds, Metrics.TextFactor(size) / Metrics.TextFactor(mark.Size), limit);
        return Fitted(mark.Text ?? "", size, least, limit);
    }

    public string? Text()
    {
        string s = _box.Text.Trim();
        if (s.Length == 0)
        {
            return null;
        }
        else
        {
            return s;
        }
    }

    public void Close()
    {
        _box.Close();
    }

    public void Refresh()
    {
        _box.Refresh();
    }

    public void PaintBackdrop(DeviceContext dc, DeviceContext backdrop, Color tint)
    {
        Rect cr = _box.ClientBox();
        Point org = _box.OriginInParent();
        dc.Blit(cr, backdrop, org.X, org.Y);
        int rule = Length.AtLeast(RuleFloorPx, RuleDip).Px(_metrics.Scale);
        dc.FillRect(Rect.At(0, cr.Height - rule, cr.Width, rule), Palette.AccentRef);
        dc.SetTextColor(tint.ColorRef);
    }

    private void Fit()
    {
        if (!_box.IsOpen)
        {
            return;
        }

        Rect fitted = Fitted(_box.Text, _size, _opened, _limit);
        if (fitted.Width == _rc.Width && fitted.Height == _rc.Height)
        {
            return;
        }

        _rc = fitted;
        _box.Resize(_rc.Width, _rc.Height);
        Refresh();
    }

    /// <summary>
    /// Gives the box a text needs at a size: wide enough for its longest line up to the edge of the
    /// area, where it wraps, and tall enough for its lines, but never smaller than the least box.
    /// </summary>
    private Rect Fitted(string text, TextSize size, Rect least, Rect limit)
    {
        Rect area = AreaAt(least.TopLeft, limit);
        int pad = Length.AtLeast(PadFloorPx, PadDip).Px(_metrics.Scale);
        TextFont font = _metrics.Text(size);

        int w = Math.Clamp(Measure(text, 0, font).Width + pad, least.Width, Math.Max(least.Width, area.Right - least.Left));
        int h = Math.Clamp(Measure(text, w, font).Height + pad, least.Height, Math.Max(least.Height, area.Bottom - least.Top));
        return Rect.At(least.Left, least.Top, w, h);
    }

    /// <summary>
    /// Grows or shrinks a box by a factor from its top-left corner, reaching no further past the
    /// edge of the area than it already does.
    /// </summary>
    private Rect Scaled(Rect r, float factor, Rect limit)
    {
        Rect area = AreaAt(r.TopLeft, limit);
        int w = Math.Min((int)MathF.Round(r.Width * factor), Math.Max(r.Width, area.Right - r.Left));
        int h = Math.Min((int)MathF.Round(r.Height * factor), Math.Max(r.Height, area.Bottom - r.Top));
        return Rect.At(r.Left, r.Top, w, h);
    }

    private Rect AreaAt(Point p, Rect limit)
    {
        if (limit.Contains(p))
        {
            return limit;
        }
        else
        {
            return _desk.Bounds;
        }
    }

    private Rect Measure(string text, int width, TextFont font)
    {
        if (text.Length == 0)
        {
            return default;
        }
        uint format;
        int boxWidth;
        if (width > 0)
        {
            format = MarkPainter.TextFormat;
            boxWidth = width;
        }
        else
        {
            format = TextLayout.Line;
            boxWidth = _desk.Size.Width;
        }
        return _box.Measure(text, boxWidth, font, format);
    }

    public bool OnTextKey(int virtualKey)
    {
        Chord chord = Chord.Of(virtualKey);
        if (Array.Exists(Finish, key => key.Matches(chord)))
        {
            EndRequested?.Invoke();
            return true;
        }
        return KeyDown?.Invoke(chord) ?? false;
    }

    public void OnTextFocusLost()
    {
        EndRequested?.Invoke();
    }

    public void OnTextEdited()
    {
        Fit();
        Refresh();
    }
}
