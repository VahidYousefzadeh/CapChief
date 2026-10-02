namespace CapChief.Shell;

/// <summary>
/// Calculates line thicknesses, hit-test distances and fonts for the scale of the monitor being
/// captured.
/// </summary>
public sealed class Metrics
{
    private const float StepShare = 0.58f;

    private const int StepFloorPx = 7;

    private const int LabelDip = 15, TextDip = 24, DigitDip = 19;

    private readonly Desktop _desk;
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

    public TextFont Text { get; private set; }

    public TextFont Digit { get; private set; }

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
        Text.Free();
        Digit.Free();
        _step.Free();
        Label = Text = Digit = _step = default;
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
        Text = Font(TextDip);
        Digit = Font(DigitDip);

        float strokeWidth = Math.Max(4f, 4f * scale);
        int gripRadius = Length.AtLeast(4f, 5f).Px(scale);
        BorderWidth = 2f * scale;
        LabelMargin = Length.AtLeast(2f, 2f).Px(scale);
        SnapDistance = Length.AtLeast(4f, 6f).Px(scale);

        Sizes = new MarkSizes
        {
            StrokeWidth = strokeWidth,
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
