namespace CapChief.Shell;

/// <summary>
/// Defines the sizes and fonts used by the toolbar, its drop-downs and tooltips, the drawn icons,
/// and the overlay's labels and badges.
/// </summary>
public static class Theme
{
    private const float ButtonSideDip = 34f, PadDip = 9f, GapDip = 5f, SepDip = 15f;

    public const float ButtonRadiusFraction = 0.32f;

    public static Length ButtonSide
    {
        get
        {
            return Length.Of(ButtonSideDip);
        }
    }

    public static Length Pad
    {
        get
        {
            return Length.Of(PadDip);
        }
    }

    public static Length Gap
    {
        get
        {
            return Length.Of(GapDip);
        }
    }

    public static Length ToolbarHeight
    {
        get
        {
            return Length.Of(ButtonSideDip + 2f * PadDip);
        }
    }

    public static Length SepWidth
    {
        get
        {
            return Length.Of(SepDip);
        }
    }

    public static Length SepInset
    {
        get
        {
            return Length.Of(SepDip);
        }
    }

    public static Length SepLine
    {
        get
        {
            return Length.AtLeast(1f, 1f);
        }
    }

    public static Length BarRadius
    {
        get
        {
            return Length.Of(15f);
        }
    }

    public static Length PanelMargin
    {
        get
        {
            return Length.AtLeast(2f, 2f);
        }
    }

    public static Length PanelRadius
    {
        get
        {
            return Length.Of(10f);
        }
    }

    public static Length PanelRim
    {
        get
        {
            return Length.AtLeast(1f, 1.3f);
        }
    }

    public static Length CropGap
    {
        get
        {
            return Length.Of(10f);
        }
    }

    public static GlossLook GlossSweep
    {
        get
        {
            return new()
            {
                Lean = 0.5f,
                Half = 0.5f,
                Gain = 1.5f,
                Spec = 0.28f,
                Core = 0.26f,
                Tilt = 0.45f,
            };
        }
    }

    public static Length TimelineMin
    {
        get
        {
            return Length.Of(240f);
        }
    }

    public static Length TimelineMax
    {
        get
        {
            return Length.Of(620f);
        }
    }

    public static Length ReadoutWidth
    {
        get
        {
            return Length.Of(132f);
        }
    }

    public static Length TimelineGap
    {
        get
        {
            return Length.Of(12f);
        }
    }

    public static Length TimelineWell
    {
        get
        {
            return Length.AtLeast(4f, 10f);
        }
    }

    public static Length TimelinePad
    {
        get
        {
            return Length.AtLeast(2f, 4f);
        }
    }

    public static Length TimelineGrab
    {
        get
        {
            return Length.AtLeast(9f, 11f);
        }
    }

    public static Length TimelineHead
    {
        get
        {
            return Length.AtLeast(2f, 3f);
        }
    }

    public static Length TimelineGrip
    {
        get
        {
            return Length.AtLeast(4f, 6f);
        }
    }

    public static Length TimelineGripOut
    {
        get
        {
            return Length.AtLeast(2f, 5f);
        }
    }

    public static Length ThumbnailWidth
    {
        get
        {
            return Length.Of(160f);
        }
    }

    public static Length ThumbnailHeight
    {
        get
        {
            return Length.Of(100f);
        }
    }

    public static Length ThumbnailInset
    {
        get
        {
            return Length.Of(5f);
        }
    }

    public static Length TipHeight
    {
        get
        {
            return Length.Of(25f);
        }
    }

    public static Length TipPad
    {
        get
        {
            return Length.Of(10f);
        }
    }

    public static Length TipRadius
    {
        get
        {
            return Length.Of(8f);
        }
    }

    public static Length TipGap
    {
        get
        {
            return Length.Of(7f);
        }
    }

    public static Length ToastPad
    {
        get
        {
            return Length.Of(20f);
        }
    }

    public static Length LabelPad
    {
        get
        {
            return Length.AtLeast(8f, 8f);
        }
    }

    public static Length LabelGap
    {
        get
        {
            return Length.AtLeast(8f, 8f);
        }
    }

    public static Length LabelRadius
    {
        get
        {
            return Length.Of(9f);
        }
    }

    public static Length RecRadius
    {
        get
        {
            return Length.Of(5f);
        }
    }

    public static Length CountdownRule
    {
        get
        {
            return Length.Of(8f);
        }
    }

    public static Length IconPen
    {
        get
        {
            return Length.AtLeast(1.2f, 1.7f);
        }
    }

    public static Length ChoiceKeyGap
    {
        get
        {
            return Length.Of(24f);
        }
    }

    public static Length ChoiceEndPad
    {
        get
        {
            return Length.Of(12f);
        }
    }

    public static Length GifMarkPad
    {
        get
        {
            return Length.Of(5f);
        }
    }

    public static Length GifMarkRadius
    {
        get
        {
            return Length.Of(3.5f);
        }
    }

    public static Length PauseRadius
    {
        get
        {
            return Length.Of(1.6f);
        }
    }

    public static Length StepBadgePad
    {
        get
        {
            return Length.Of(7f);
        }
    }

    public static Length MosaicGap
    {
        get
        {
            return Length.AtLeast(1f, 0.9f);
        }
    }

    public static Length RedactRadius
    {
        get
        {
            return Length.AtLeast(1f, 1.5f);
        }
    }

    public static Font Label
    {
        get
        {
            return Font.Of(Length.Of(12.5f), FontWeights.Semibold, FontFace.SharpSemibold);
        }
    }

    public static Font StepBadge
    {
        get
        {
            return Font.Of(Length.Of(13.5f), FontWeights.Bold, FontFace.SmoothSemibold);
        }
    }

    public static Font GifMark
    {
        get
        {
            return Font.Of(Length.Of(9.5f), FontWeights.Bold, FontFace.Smooth);
        }
    }
}
