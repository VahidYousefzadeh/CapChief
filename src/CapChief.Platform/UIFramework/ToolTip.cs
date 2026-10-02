namespace CapChief.UIFramework;

/// <summary>
/// Displays a brief description of the control under the mouse pointer in a popup window.
/// </summary>
public sealed class ToolTip
{
    private const int ScreenInset = 2;

    private readonly Form _form;
    private readonly Func<Point, Rect> _screen;

    private bool _up;

    public ToolTip(Form form, Func<Point, Rect> screen, int timerId)
    {
        _form = form;
        _screen = screen;
        TimerId = timerId;
    }

    public int TimerId { get; }

    public uint Delay { get; set; }

    public Length Gap { get; set; }

    public Rect Avoid { get; set; }

    public Label? Content { get; set; }

    public void Show(string text, Rect target, Rect owner)
    {
        if (text.Length == 0 || Content is not { } content || !_form.Window.Exists)
        {
            return;
        }

        content.Text = text;
        _form.Layout(Form.Unbounded);
        Rect body = _form.ContentBounds;
        Rect screen = _screen(target.Center);
        int gap = Gap.Px(_form.Scale, 0);

        int x = (target.Left + target.Right) / 2 - body.Width / 2;
        int below = owner.Bottom + gap;
        int above = owner.Top - body.Height - gap;
        bool Covers(int top)
        {
            return Avoid.Overlaps(Rect.At(x, top, body.Width, body.Height));
        }

        bool fitsBelow = below + body.Height <= screen.Bottom;
        int y;
        if (Covers(below))
        {
            y = above;
        }
        else if (Covers(above) || fitsBelow)
        {
            y = below;
        }
        else
        {
            y = above;
        }

        Rect at = Rect.Within(Rect.At(x, y, body.Width, body.Height), screen.Inflated(-ScreenInset));

        _form.PlaceAt(at.Left - body.Left, at.Top - body.Top);
        _form.Invalidate();
        _form.Window.Reveal();
        _up = true;
    }

    public void Hide()
    {
        if (!_up)
        {
            return;
        }
        _up = false;
        _form.Window.Hide();
    }
}
