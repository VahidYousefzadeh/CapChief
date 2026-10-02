namespace CapChief.Shell;

/// <summary>
/// Represents the drop-down under the toolbar that shows the saved captures as pictures to pick
/// from.
/// </summary>
public sealed class HistoryForm : Form, IDeactivateHandler, IKeyDownHandler
{
    private const string ClassName = "CCHistory";

    private const int PerRow = 5;

    private readonly Desktop _desk;
    private readonly ThumbnailButton[] _thumbnails = new ThumbnailButton[CaptureHistory.MaxEntries];

    private Rect _under;

    public event Action<Capture>? Picked;

    public HistoryForm(AppIcon appIcon, Desktop desk, MarkPainter markPainter)
        : base(new WindowKind(ClassName, appIcon, doubleClicks: false, redrawOnResize: false, ownsBackground: true, tracksLeaving: true))
    {
        _desk = desk;

        var rows = new WrapPanel
        {
            MaxPerRow = PerRow,
            Spacing = Theme.Gap,
            Padding = Thickness.All(Theme.Pad),
        };
        for (int i = 0; i < _thumbnails.Length; i++)
        {
            _thumbnails[i] = new ThumbnailButton(markPainter, Pick);
            rows.Add(_thumbnails[i]);
        }

        var panel = new ShadedPanel(Theme.BarRadius);
        panel.Add(rows);
        Content = panel;
    }

    public void Create(Window owner)
    {
        Open(FloatingSpec, owner);
    }

    public void Show(IReadOnlyList<Capture> entries, Rect under)
    {
        for (int i = 0; i < _thumbnails.Length; i++)
        {
            Capture? entry;
            if (i < entries.Count)
            {
                entry = entries[i];
            }
            else
            {
                entry = null;
            }
            _thumbnails[i].Show(entry);
        }
        _under = under;
        Reposition();
        Window.Activate();
    }

    public void Hide()
    {
        if (!IsShown)
        {
            return;
        }
        ResetMouse();
        Window.Hide();
        foreach (ThumbnailButton thumbnail in _thumbnails)
        {
            thumbnail.Show(null);
        }
    }

    public void OnDeactivated()
    {
        Hide();
    }

    public bool OnKeyDown(int virtualKey)
    {
        if (virtualKey != Keys.Escape)
        {
            return false;
        }
        Hide();
        return true;
    }

    public override void Free()
    {
        base.Free();
        foreach (ThumbnailButton thumbnail in _thumbnails)
        {
            thumbnail.Show(null);
        }
    }

    protected override void Rescale()
    {
        if (IsShown)
        {
            Reposition();
        }
    }

    private void Reposition()
    {
        Rect screen = _desk.MonitorAt(_under.Center);
        Size size = Layout(screen.Size);
        int left = (_under.Left + _under.Right) / 2 - ContentBounds.Width / 2;
        ShowUnder(_under, left, Theme.Gap.Px(Scale), screen, size);
    }

    private void Pick(Capture entry)
    {
        Hide();
        Picked?.Invoke(entry);
    }
}

/// <summary>
/// Represents a button that shows a small picture of a saved capture, marks included and a
/// recording marked GIF, and opens it when clicked.
/// </summary>
public sealed class ThumbnailButton : Button
{
    private const float RadiusFraction = 0.1f;

    private const float GifMarkRadiusFraction = 0.3f;

    private readonly MarkPainter _markPainter;
    private readonly Action<Capture> _picked;
    private readonly Surface _image = new();

    private Capture? _capture;
    private Size _imageRoom;

    public ThumbnailButton(MarkPainter markPainter, Action<Capture> picked)
    {
        _markPainter = markPainter;
        _picked = picked;

        Width = Theme.ThumbnailWidth;
        Height = Theme.ThumbnailHeight;
        Padding = Thickness.All(Theme.ThumbnailInset);
        CornerRadius = CornerRadius.OfHeight(RadiusFraction);
        Cursor = CursorShape.Hand;
        HoverBackground = Color.FromArgb(Palette.HoverArgb);
        PressedBackground = Color.FromArgb(Palette.PressArgb);
        Visibility = Visibility.Collapsed;
    }

    public void Show(Capture? capture)
    {
        _capture = capture;
        _image.Free();
        Visibility = VisibleIf(capture is not null);
    }

    public override void OnClick(EventArgs e)
    {
        if (_capture is { } capture)
        {
            _picked(capture);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Rect room = Padding.Shrink(Bounds, Scale);
        if (_capture is not { } capture || !Render(capture, room.Size))
        {
            return;
        }

        int x = room.Left + (room.Width - _image.Width) / 2;
        int y = room.Top + (room.Height - _image.Height) / 2;
        Rect image = Rect.At(x, y, _image.Width, _image.Height);
        e.Draw.DrawImage(_image, image);
        if (capture.Clip is not null)
        {
            DrawGifMark(e.Draw, image);
        }
    }

    private void DrawGifMark(DrawContext d, Rect image)
    {
        const string gif = "GIF";
        Size text = d.Measure(gif, Theme.GifMark);
        int pad = Theme.GifMarkPad.Px(Scale);
        int inset = Theme.ThumbnailInset.Px(Scale);
        Rect mark = Rect.At(image.Left + inset, image.Bottom - inset - text.Height - pad, text.Width + 2 * pad, text.Height + pad);
        d.Fill(mark, GlyphStyle.PanelMiddle, CornerRadius.OfHeight(GifMarkRadiusFraction));
        d.CenteredText(gif, mark, GlyphStyle.Foreground, Theme.GifMark);
    }

    private bool Render(Capture capture, Size room)
    {
        if (_image.IsOpen && _imageRoom == room)
        {
            return true;
        }
        _image.Free();
        _imageRoom = room;

        using var full = new Surface();
        if (room.Width <= 0 || room.Height <= 0 || !capture.Render(full, _markPainter))
        {
            return false;
        }
        float fit = MathF.Min(1f, MathF.Min((float)room.Width / full.Width, (float)room.Height / full.Height));
        int w = Math.Max(1, (int)(full.Width * fit));
        int h = Math.Max(1, (int)(full.Height * fit));
        if (!_image.OpenCompatible(w, h))
        {
            return false;
        }
        _image.DeviceContext.StretchBlit(_image.Bounds, full.DeviceContext, full.Bounds);
        return true;
    }
}
