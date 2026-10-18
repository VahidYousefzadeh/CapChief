namespace CapChief.Shell;

/// <summary>
/// Displays the About box: the logo, name and version at the top, links to the app's page, its source
/// code and its privacy policy, and the license and copyright below. It closes on Escape, on its close
/// button, on a link, or when another window is activated.
/// </summary>
public sealed class AboutForm : Form, IKeyDownHandler, IDeactivateHandler
{
    private const string ClassName = "CCAbout";
    private const string PageUrl = "https://www.vahidyousefzadeh.com/capchief";
    private const string SourceUrl = "https://github.com/VahidYousefzadeh/CapChief";
    private const string PrivacyUrl = "https://github.com/VahidYousefzadeh/CapChief/blob/main/PRIVACY.md";

    private const float WidthDip = 290f, PadDip = 10f, LogoDip = 40f, LogoBoxDip = 46f, NameGapDip = 12f, CloseDip = 28f;

    private const float RuleInsetDip = 6f, NoteInsetDip = 10f;

    private readonly Desktop _desk;

    public AboutForm(AppIcon appIcon, Desktop desk)
        : base(new WindowKind(ClassName, appIcon, doubleClicks: false, redrawOnResize: false, ownsBackground: true, tracksLeaving: true))
    {
        _desk = desk;

        var names = new StackPanel { Orientation = Orientation.Vertical, Spacing = Length.Of(1f) };
        names.Add(Gap(4f));
        names.Add(new Label { Text = Product.Name, Font = Theme.Title });
        names.Add(new Label { Text = $"Version {Product.Version}", Font = Theme.Caption, Foreground = Color.FromArgb(Palette.KeyTextArgb) });

        var identity = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = Length.Of(NameGapDip),
        };
        identity.Add(new Spacer { Width = Length.Of(2f) });
        identity.Add(new AppLogo(Length.Of(LogoDip), Length.Of(LogoBoxDip)));
        identity.Add(names);

        var header = new Panel { HorizontalAlignment = HorizontalAlignment.Stretch };
        header.Add(identity);
        header.Add(new GlyphButton(Glyph.Close, Length.Of(CloseDip), Hide) { HorizontalAlignment = HorizontalAlignment.Right });

        var rows = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = Length.Of(WidthDip),
            Padding = Thickness.All(Length.Of(PadDip)),
        };
        rows.Add(header);
        rows.Add(Gap(12f));
        rows.Add(Rule());
        rows.Add(Gap(6f));
        rows.Add(new AboutLink(Glyph.Globe, "Project page", () => OpenLink(PageUrl)));
        rows.Add(new AboutLink(Glyph.Code, "Source code", () => OpenLink(SourceUrl)));
        rows.Add(new AboutLink(Glyph.Shield, "Privacy policy", () => OpenLink(PrivacyUrl)));
        rows.Add(Gap(6f));
        rows.Add(Rule());
        rows.Add(Gap(10f));
        rows.Add(Note("Free and open source under the MIT license"));
        rows.Add(Gap(2f));
        rows.Add(Note("© 2026 Vahid Yousefzadeh"));
        rows.Add(Gap(4f));

        var panel = new ShadedPanel(Theme.BarRadius);
        panel.Add(rows);
        Content = panel;
    }

    public void Create(Window owner)
    {
        Open(FloatingSpec, owner);
    }

    /// <summary>
    /// Shows the box in the middle of the monitor that holds the point, and gives it the keyboard.
    /// </summary>
    public void ShowNear(Point screen)
    {
        Size size = Layout(Unbounded);
        Rect monitor = _desk.MonitorAt(screen);
        Rect body = ContentBounds;
        int left = monitor.Left + (monitor.Width - body.Width) / 2;
        int top = monitor.Top + (monitor.Height - body.Height) / 2;
        PlaceAndShow(left - body.Left, top - body.Top, size);
        Invalidate();
        Window.Focus();
    }

    public void Hide()
    {
        if (!IsShown)
        {
            return;
        }
        ResetMouse();
        Window.Hide();
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

    public void OnDeactivated()
    {
        Hide();
    }

    private static Spacer Gap(float dip)
    {
        return new() { Height = Length.Of(dip) };
    }

    private static Separator Rule()
    {
        return new()
        {
            Orientation = Orientation.Horizontal,
            Height = Length.Of(1f),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = Thickness.Sides(Length.Of(RuleInsetDip)),
            Foreground = Color.FromArgb(Palette.SepArgb),
        };
    }

    private static Label Note(string text)
    {
        return new()
        {
            Text = text,
            Font = Theme.Footnote,
            Foreground = Color.FromArgb(Palette.KeyTextArgb),
            Padding = Thickness.Sides(Length.Of(NoteInsetDip)),
        };
    }

    private void OpenLink(string url)
    {
        Hide();
        Browser.Open(url);
    }
}

/// <summary>
/// Represents one link row of the About box: its icon and text on the left, and an arrow on the right
/// that shows it opens the browser.
/// </summary>
public sealed class AboutLink : Button
{
    private const float HeightDip = 32f, IconCenterDip = 18f, TextLeftDip = 36f, ArrowCenterDip = 17f, RightRoomDip = 46f;

    private readonly Glyph _glyph;
    private readonly string _text;
    private readonly Action _clicked;

    public AboutLink(Glyph glyph, string text, Action clicked)
    {
        _glyph = glyph;
        _text = text;
        _clicked = clicked;

        Height = Length.Of(HeightDip);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        CornerRadius = CornerRadius.OfHeight(Theme.ButtonRadiusFraction);
        Cursor = CursorShape.Hand;
        HoverBackground = Color.FromArgb(Palette.ActiveArgb);
        PressedBackground = Color.FromArgb(Palette.PressArgb);
    }

    public override void OnClick(EventArgs e)
    {
        _clicked();
    }

    protected override Size MeasureOverride(Size available)
    {
        return new(Px(TextLeftDip) + Draw.Measure(_text, Font).Width + Px(RightRoomDip), Px(HeightDip));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Rect r = Bounds;
        int side = r.Height;
        int iconCenter = r.Left + Px(IconCenterDip);
        GlyphPainter.Draw(e.Draw, _glyph, Rect.At(iconCenter - side / 2, r.Top, side, side), default);

        int text = e.Draw.Measure(_text, Font).Width;
        e.Draw.Text(_text, Rect.At(r.Left + Px(TextLeftDip), r.Top, text, r.Height), Foreground, Font);

        int arrowCenter = r.Right - Px(ArrowCenterDip);
        GlyphPainter.Draw(e.Draw, Glyph.LinkOut, Rect.At(arrowCenter - side / 2, r.Top, side, side), default);
    }

    private int Px(float dip)
    {
        return Length.Of(dip).Px(Scale);
    }
}

/// <summary>
/// Represents a square button that shows one icon and runs an action when clicked, such as the About
/// box's close button.
/// </summary>
public sealed class GlyphButton : Button
{
    private readonly Glyph _glyph;
    private readonly Action _clicked;

    public GlyphButton(Glyph glyph, Length side, Action clicked)
    {
        _glyph = glyph;
        _clicked = clicked;

        Width = side;
        Height = side;
        CornerRadius = CornerRadius.OfHeight(Theme.ButtonRadiusFraction);
        Cursor = CursorShape.Hand;
        HoverBackground = Color.FromArgb(Palette.HoverArgb);
        PressedBackground = Color.FromArgb(Palette.PressArgb);
    }

    public override void OnClick(EventArgs e)
    {
        _clicked();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        GlyphPainter.Draw(e.Draw, _glyph, Bounds, default);
    }
}

/// <summary>
/// Represents the app's logo, drawn at its size in the middle of the control.
/// </summary>
public sealed class AppLogo : Control
{
    private readonly Length _side;

    public AppLogo(Length side, Length box)
    {
        _side = side;
        Width = box;
        Height = box;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Rect b = Bounds;
        float side = _side.Exact(Scale);
        LogoGlyph.Draw(e.Draw, b.Left + (b.Width - side) / 2f, b.Top + (b.Height - side) / 2f, side);
    }
}
