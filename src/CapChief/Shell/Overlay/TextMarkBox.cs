namespace CapChief.Shell;

/// <summary>
/// Represents the editable text box used to type a text mark.
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

    public event Action? EndRequested;

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

    public void OpenAt(Rect r, Rect limit, string? text)
    {
        _rc = _opened = r;
        _limit = limit;
        _box.Open(_parent, r, text, _metrics.Text);
        Fit();
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

        string s = _box.Text;
        Rect limit;
        if (_limit.Contains(_rc.TopLeft))
        {
            limit = _limit;
        }
        else
        {
            limit = _desk.Bounds;
        }
        int pad = Length.AtLeast(PadFloorPx, PadDip).Px(_metrics.Scale);

        int w = Math.Clamp(Measure(s, 0).Width + pad, _opened.Width, Math.Max(_opened.Width, limit.Right - _rc.Left));
        int h = Math.Clamp(Measure(s, w).Height + pad, _opened.Height, Math.Max(_opened.Height, limit.Bottom - _rc.Top));
        if (w == _rc.Width && h == _rc.Height)
        {
            return;
        }

        _rc.Right = _rc.Left + w;
        _rc.Bottom = _rc.Top + h;
        _box.Resize(w, h);
        Refresh();
    }

    private Rect Measure(string text, int width)
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
        return _box.Measure(text, boxWidth, _metrics.Text, format);
    }

    public bool OnTextKey(int virtualKey)
    {
        Chord chord = Chord.Of(virtualKey);
        if (!Array.Exists(Finish, key => key.Matches(chord)))
        {
            return false;
        }
        EndRequested?.Invoke();
        return true;
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
