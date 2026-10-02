namespace CapChief.Shell;

/// <summary>
/// Represents a row of toolbar buttons shown for one mode of the app, or a toast.
/// </summary>
public abstract class ToolbarStrip : StackPanel
{
    protected ToolbarStrip()
    {
        Orientation = Orientation.Horizontal;
        Spacing = Theme.Gap;
        Padding = Thickness.All(Theme.Pad);
        MinHeight = Theme.ToolbarHeight;
        Visibility = Visibility.Collapsed;
    }

    protected override bool Spaced(Control before, Control after)
    {
        return before is not Separator && after is not Separator;
    }

    public ToolButton? ButtonFor(Command command)
    {
        foreach (Control c in Children)
        {
            if (c is ToolButton button && button.Command == command)
            {
                return button;
            }
        }
        return null;
    }

    protected static Separator Rule()
    {
        return new()
        {
            Width = Theme.SepWidth,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = Thickness.Ends(-Theme.Pad),
            Padding = Thickness.Ends(Theme.SepInset),
            LineWidth = Theme.SepLine,
            Foreground = Color.FromArgb(Palette.SepArgb),
        };
    }
}

/// <summary>
/// Represents the toolbar shown when the app is idle, offering the capture commands.
/// </summary>
public sealed class CaptureStrip : ToolbarStrip
{
    public CaptureStrip(Commands commands)
    {
        Add(new ToolButton(Glyph.Camera, commands.Capture, commands));
        Add(new ToolButton(Glyph.Clock, commands.TimedCapture, commands));
        Add(new ToolButton(Glyph.Record, commands.Record, commands));
        Add(new ToolButton(Glyph.History, commands.History, commands));
        Add(Rule());
        Add(new ToolButton(Glyph.Close, commands.HideToTray, commands, ButtonStyle.Accent));
    }
}

/// <summary>
/// Represents the toolbar shown while the user draws on a screenshot.
/// </summary>
public sealed class AnnotateStrip : ToolbarStrip
{
    private readonly Document _doc;
    private readonly Tools _tools;
    private readonly ToolPickerForm _picker;
    private readonly ToolButton _text;
    private readonly ToolButton _color;
    private readonly ToolButton[] _pairs;
    private readonly ToolButton[] _dropDowns;

    public AnnotateStrip(Commands commands, Tools tools, Document doc, ToolPickerForm picker)
    {
        _doc = doc;
        _tools = tools;
        _picker = picker;

        _text = Pair(commands, commands.TextTool, ToolGroup.Text);
        _pairs =
        [
            Pair(commands, commands.ArrowTool, ToolGroup.Arrow),
            Pair(commands, commands.RectangleTool, ToolGroup.Rect),
            _text,
            Pair(commands, commands.RedactTool, ToolGroup.Redact),
        ];
        _color = new ToolButton(Glyph.Swatch, commands.Color, commands)
        {
            OpensDropDown = true,
        };
        _dropDowns = [.. _pairs, _color];

        foreach (ToolButton b in _dropDowns)
        {
            Add(b);
        }
        Add(Rule());
        Add(new ToolButton(Glyph.Bookmark, commands.SaveToHistory, commands));
        Add(new ToolButton(Glyph.Trash, commands.Delete, commands));
        Add(new ToolButton(Glyph.Copy, commands.Copy, commands));
        Add(new ToolButton(Glyph.Save, commands.Save, commands));
        Add(Rule());
        Add(new ToolButton(Glyph.Close, commands.Close, commands, ButtonStyle.Accent));
    }

    public void MoveDropDown(int by)
    {
        int at = Array.FindIndex(_dropDowns, b => _picker.HangsFrom(b.Command));
        if (at >= 0)
        {
            _dropDowns[(at + by + _dropDowns.Length) % _dropDowns.Length].OnClick(EventArgs.Empty);
        }
    }

    public override void Refresh()
    {
        foreach (ToolButton b in _pairs)
        {
            b.IsChecked = _tools.IsIn(b.Group);
            b.Glyph = GlyphPainter.GlyphOf(_tools.ShownIn(b.Group));
        }

        _color.Swatch = _tools.Color;
        _color.IsChecked = _picker.HangsFrom(_color.Command);
        _text.Step = _doc.NextStepNumber;

        base.Refresh();

        foreach (ToolButton b in _dropDowns)
        {
            if (_picker.HangsFrom(b.Command))
            {
                b.ToolTip = "";
            }
        }
    }

    private static ToolButton Pair(Commands commands, Command command, ToolGroup group)
    {
        return new(Glyph.None, command, commands) { Group = group, OpensDropDown = true };
    }
}

/// <summary>
/// Represents the toolbar content that displays a brief confirmation message.
/// </summary>
public sealed class ToastStrip : ToolbarStrip
{
    private readonly Label _message = new();

    public ToastStrip()
    {
        _message.Padding = Thickness.Sides(Theme.ToastPad);
        _message.Margin = Thickness.All(-Theme.Pad);
        _message.VerticalAlignment = VerticalAlignment.Stretch;
        Add(_message);
    }

    public void Say(string message)
    {
        _message.Text = message;
    }
}

/// <summary>
/// Represents the toolbar shown while the user reviews and trims a recording.
/// </summary>
public sealed class ReviewStrip : ToolbarStrip
{
    private readonly Review _review;
    private readonly ToolButton _play;
    private readonly ToolButton _speed;
    private readonly Label _readout = new();

    public ReviewStrip(Commands commands, Review review, Timeline timeline)
    {
        _review = review;
        _play = new ToolButton(Glyph.Play, commands.Play, commands);
        _speed = new ToolButton(Glyph.Speed, commands.Speed, commands);

        _readout.Margin = Thickness.AtLeft(Theme.TimelineGap);
        _readout.Width = Theme.ReadoutWidth;
        _readout.Height = Theme.ButtonSide;

        Add(_play);
        Add(_speed);
        Add(Rule());
        Add(timeline);
        Add(_readout);
        Add(Rule());
        Add(new ToolButton(Glyph.Bookmark, commands.SaveToHistory, commands));
        Add(new ToolButton(Glyph.Trash, commands.Delete, commands));
        Add(new ToolButton(Glyph.Save, commands.SaveClip, commands));
        Add(Rule());
        Add(new ToolButton(Glyph.Close, commands.Close, commands, ButtonStyle.Accent));
    }

    public override void Refresh()
    {
        Clip? clip = _review.Clip;
        if (_review.IsPlaying)
        {
            _play.Glyph = Glyph.Pause;
        }
        else
        {
            _play.Glyph = Glyph.Play;
        }
        if (clip is null)
        {
            _speed.Label = "";
            _readout.Text = "";
        }
        else
        {
            _speed.Label = ClipText.SpeedOf(clip);
            _readout.Text = ClipText.Of(clip, _review.SaveSize);
        }
        base.Refresh();
    }
}
