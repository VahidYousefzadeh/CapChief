namespace CapChief.Shell;

/// <summary>
/// Calculates line thicknesses, hit-test distances and fonts for the scale of the monitor being
/// captured.
/// </summary>
public sealed class Metrics
{
    private const float StepShare = 0.58f;

    private const int StepFloorPx = 7;

    private const int LabelDip = 15, DigitDip = 19;

    private static readonly TextSize[] TextSizes = Enum.GetValues<TextSize>();

    private readonly Desktop _desk;
    private readonly TextFont[] _texts = new TextFont[TextSizes.Length];
    private int _stepPx;
    private TextFont _step;

    public Metrics(float scale, Desktop desk)
    {
        _desk = desk;
        ScaleTo(scale);
    }

    public float Scale { get; private set; }

    public MarkSizes Sizes { get; private set; }

    public HitTolerance Tolerance { get; private set; }

    public float BorderWidth { get; private set; }

    public int LabelMargin { get; private set; }

    public int SnapDistance { get; private set; }

    public TextFont Label { get; private set; }

    public TextFont Digit { get; private set; }

    /// <summary>
    /// Gives how many times bigger than Normal text of a size is drawn.
    /// </summary>
    public static float TextFactor(TextSize size)
    {
        return DipOf(size) / (float)DipOf(TextSize.Normal);
    }

    public TextFont Text(TextSize size)
    {
        return _texts[(int)size];
    }

    public void ScaleToMonitorAt(Point overlay)
    {
        ScaleTo(_desk.ScaleAt(_desk.ToScreen(overlay)));
    }

    public TextFont Step(int disc)
    {
        int px = Math.Max(StepFloorPx, (int)MathF.Round(disc * StepShare));
        if (px == _stepPx && _step.Exists)
        {
            return _step;
        }
        _step.Free();
        _step = TextFont.Sharp(px, FontWeights.Semibold);
        _stepPx = px;
        return _step;
    }

    public void Free()
    {
        Label.Free();
        foreach (TextFont text in _texts)
        {
            text.Free();
        }
        Digit.Free();
        _step.Free();
        Label = Digit = _step = default;
        Array.Clear(_texts);
        _stepPx = 0;
    }

    private void ScaleTo(float scale)
    {
        if (scale == Scale && Label.Exists)
        {
            return;
        }
        Free();
        Scale = scale;
        Label = Font(LabelDip);
        foreach (TextSize size in TextSizes)
        {
            _texts[(int)size] = Font(DipOf(size));
        }
        Digit = Font(DigitDip);

        float strokeWidth = Math.Max(4f, 4f * scale);
        int gripRadius = Length.AtLeast(4f, 5f).Px(scale);
        BorderWidth = 2f * scale;
        LabelMargin = Length.AtLeast(2f, 2f).Px(scale);
        SnapDistance = Length.AtLeast(4f, 6f).Px(scale);

        Sizes = new MarkSizes
        {
            StrokeWidth = strokeWidth,
            HighlighterWidth = strokeWidth * 4f,
            ArrowHead = 15f * scale,
            HintWidth = Math.Max(1f, scale),
            BadgeRadius = Length.AtLeast(13f, 14f).Px(scale),
            MinSize = Length.AtLeast(8f, 10f).Px(scale),
            RaySpacing = Length.AtLeast(6f, 6f).Px(scale),
            RedactBlock = Length.AtLeast(6f, 9f).Px(scale),
        };

        Tolerance = new HitTolerance
        {
            HitBand = (int)strokeWidth + 4,
            EdgeBand = Length.AtLeast(4f, 6f).Px(scale),
            GripRadius = gripRadius,
        };
    }

    private TextFont Font(int dip)
    {
        return TextFont.Sharp((int)Math.Round(dip * Scale), FontWeights.Semibold);
    }

    private static int DipOf(TextSize size)
    {
        switch (size)
        {
            case TextSize.Small:
                return 16;
            case TextSize.Normal:
                return 24;
            case TextSize.Large:
                return 36;
            case TextSize.Huge:
                return 54;
            default:
                throw new ArgumentOutOfRangeException(nameof(size), size, null);
        }
    }
}

/// <summary>
/// Stores the distances within which the pointer can grab marks and grips.
/// </summary>
public readonly struct HitTolerance
{
    public int HitBand { get; init; }

    public int EdgeBand { get; init; }

    public int GripRadius { get; init; }
}
