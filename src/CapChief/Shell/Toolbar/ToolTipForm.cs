namespace CapChief.Shell;

/// <summary>
/// Displays a tooltip window for the toolbar buttons.
/// </summary>
public sealed class ToolTipForm : Form
{
    private const string ClassName = "CCTip";

    public ToolTipForm(AppIcon appIcon)
        : base(new WindowKind(ClassName, appIcon, doubleClicks: false, redrawOnResize: false, ownsBackground: false, tracksLeaving: false))
    {
        Label.Height = Theme.TipHeight;
        Label.Padding = Thickness.Sides(Theme.TipPad);

        var panel = new ShadedPanel(Theme.TipRadius);
        panel.Add(Label);
        Content = panel;
    }

    public Label Label { get; } = new();

    public void Create(Window owner)
    {
        Open(FloatingSpec with { TakesFocus = false, ClickThrough = true }, owner);
    }
}
