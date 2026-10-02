namespace CapChief.Shell;

/// <summary>
/// Represents a square toolbar button that displays an icon.
/// </summary>
public sealed class ToolButton : Button
{
    private const float DisabledKeep = 0.35f;

    private readonly Commands _commands;

    public ToolButton(Glyph glyph, Command command, Commands commands, ButtonStyle style = ButtonStyle.Plain)
    {
        Glyph = glyph;
        _commands = commands;
        Command = command;
        ToolTip = command.Tip;

        Width = Height = Theme.ButtonSide;
        CornerRadius = CornerRadius.OfHeight(Theme.ButtonRadiusFraction);
        Cursor = CursorShape.Hand;
        if (style == ButtonStyle.Accent)
        {
            Background = Color.FromArgb(Palette.AccentArgb);
            HoverBackground = Color.FromArgb(Palette.AccentHoverArgb);
            PressedBackground = Color.FromArgb(Palette.AccentPressArgb);
        }
        else
        {
            Background = Color.FromArgb(0);
            HoverBackground = Color.FromArgb(Palette.HoverArgb);
            PressedBackground = Color.FromArgb(Palette.PressArgb);
        }
        CheckedBackground = Color.FromArgb(Palette.ActiveArgb);
    }

    public Command Command { get; }

    public ToolGroup Group { get; init; }

    public bool OpensDropDown { get; init; }

    public Glyph Glyph { get; set; }

    public string Label { get; set; } = "";

    public int Step { get; set; }

    public Color Swatch { get; set; }

    public override void OnClick(EventArgs e)
    {
        _commands.Run(Command);
    }

    public override CursorShape? CursorAt(Point p)
    {
        if (Command.Enabled())
        {
            return Cursor;
        }
        else
        {
            return CursorShape.Arrow;
        }
    }

    public override void Refresh()
    {
        ToolTip = Command.Tip;
        base.Refresh();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        bool enabled = Command.Enabled();
        if (enabled)
        {
            base.OnPaint(e);
        }
        GlyphPainter.Draw(e.Draw, Glyph, Bounds, new GlyphState(Step, Swatch, Label));
        if (!enabled)
        {
            e.Draw.FadeToBackground(Bounds, DisabledKeep);
        }
    }
}

/// <summary>
/// Specifies whether a toolbar button uses the plain or the accent color.
/// </summary>
public enum ButtonStyle
{
    Plain,
    Accent,
}
