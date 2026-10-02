namespace CapChief.UIFramework;

/// <summary>
/// Represents a button control that reacts to mouse clicks.
/// </summary>
public class Button : Control
{
    public Color HoverBackground { get; set; }

    public Color PressedBackground { get; set; }

    public Color CheckedBackground { get; set; }

    public bool IsChecked { get; set; }

    private bool _isPressed;

    public Color Tint
    {
        get
        {
            if (_isPressed && IsMouseOver)
            {
                return PressedBackground;
            }
            else if (IsChecked)
            {
                return CheckedBackground;
            }
            else if (IsMouseOver)
            {
                return HoverBackground;
            }
            else
            {
                return Background;
            }
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        _isPressed = true;
        base.OnMouseDown(e);
        e.Handled = true;
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        _isPressed = false;
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Draw.Fill(Bounds, Tint, CornerRadius);
    }
}
