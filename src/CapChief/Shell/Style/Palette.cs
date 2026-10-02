namespace CapChief.Shell;

/// <summary>
/// Defines the colors the app draws with.
/// </summary>
public static class Palette
{
    private const uint AccentRgb = 0x0E9E6Eu;

    public const uint AccentArgb = 0xFF000000u | AccentRgb;

    public const uint AccentHoverArgb = 0xFF13B681u;

    public const uint AccentPressArgb = 0xFF0A8459u;

    public static readonly uint AccentRef = Argb.SwapRedBlue(AccentRgb);

    private const uint PanelTopArgb = 0xFF1F3429u;

    private const uint PanelBottomArgb = 0xFF13221Bu;

    private const float PanelRimTop = 0.3f;

    private const float PanelRimBottom = 0.04f;

    public const uint OnPanelArgb = 0xFFF1F5F2u;

    public static readonly uint OnPanelRef = Argb.SwapRedBlue(OnPanelArgb);

    public static readonly uint KeyTextArgb = Argb.Over(Argb.WhiteWash(0x99), PanelAt(0.5f));

    public const uint LabelTextRef = 0x00F5F5F5u;

    public const uint DarkRef = 0x00141414u;

    public const byte DimA = 0x80;

    private const uint StopRgb = 0xE5484Du;

    public static readonly uint StopRef = Argb.SwapRedBlue(StopRgb);

    public static readonly uint HoverArgb = Argb.WhiteWash(0x1F);

    public static readonly uint ActiveArgb = Argb.WhiteWash(0x2E);

    public static readonly uint PressArgb = Argb.WhiteWash(0x3A);

    public static readonly uint SepArgb = Argb.WhiteWash(0x1F);

    public const uint TimelineWellArgb = 0x66000000u;

    public static readonly uint TimelineKeptArgb = Argb.WhiteWash(0x38);

    public static PanelLook Panel
    {
        get
        {
            return new()
            {
                TopArgb = PanelTopArgb,
                BottomArgb = PanelBottomArgb,
                RimTop = PanelRimTop,
                RimBottom = PanelRimBottom,
            };
        }
    }

    public static uint PanelAt(float v)
    {
        return PanelRaster.Between(PanelTopArgb, PanelBottomArgb, v);
    }
}
