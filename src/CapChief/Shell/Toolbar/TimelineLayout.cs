namespace CapChief.Shell;

/// <summary>
/// Calculates the positions of the trim grips and markers on the recording timeline.
/// </summary>
public readonly struct TimelineLayout
{
    public Rect Area { get; }
    public Rect Well { get; }
    public Rect Run { get; }
    private readonly Clip _clip;
    private readonly int _count;
    private readonly int _grab;
    private readonly int _headW;
    private readonly int _gripW;
    private readonly int _gripOut;

    public TimelineLayout(Rect area, Clip clip, float scale)
    {
        Area = area;
        int wellH = Theme.TimelineWell.Px(scale);
        int top = (area.Top + area.Bottom) / 2 - wellH / 2;
        Well = Rect.At(area.Left, top, area.Width, wellH);

        int pad = Theme.TimelinePad.Px(scale);
        Run = Well with { Left = Well.Left + pad, Right = Well.Right - pad };

        _clip = clip;
        _count = Math.Max(1, clip.Count);
        _grab = Theme.TimelineGrab.Px(scale);
        _headW = Theme.TimelineHead.Px(scale);
        _gripW = Theme.TimelineGrip.Px(scale);
        _gripOut = Theme.TimelineGripOut.Px(scale);
    }

    public Rect KeptRegion
    {
        get
        {
            return Between(XOf(_clip.In), XEndOf(_clip.Out));
        }
    }

    public Rect PlayedRegion
    {
        get
        {
            return Between(XOf(_clip.In), XOf(_clip.Playhead));
        }
    }

    public Rect PlayheadRegion
    {
        get
        {
            return Column(XOf(_clip.Playhead), _headW, Well.Top - _gripOut / 2, Well.Bottom + _gripOut / 2);
        }
    }

    public Rect InGrip
    {
        get
        {
            return GripRectAt(XOf(_clip.In));
        }
    }

    public Rect OutGrip
    {
        get
        {
            return GripRectAt(XEndOf(_clip.Out));
        }
    }

    public int XOf(int i)
    {
        return Run.Left + (int)((long)Math.Clamp(i, 0, _count) * Run.Width / _count);
    }

    public int XEndOf(int i)
    {
        return XOf(i + 1);
    }

    public int FrameAt(int x)
    {
        if (Run.Width <= 0)
        {
            return 0;
        }
        long i = (long)(x - Run.Left) * _count / Run.Width;
        return (int)Math.Clamp(i, 0, _count - 1);
    }

    public int BoundaryAt(int x)
    {
        if (Run.Width <= 0)
        {
            return 0;
        }
        // Adding half the divisor rounds to the nearest boundary.
        long b = ((long)(x - Run.Left) * _count * 2 + Run.Width) / (Run.Width * 2L);
        return (int)Math.Clamp(b, 0, _count);
    }

    private Rect Between(int left, int right)
    {
        return Well with { Left = left, Right = Math.Max(left, right) };
    }

    private Rect GripRectAt(int x)
    {
        return Column(x, _gripW, Well.Top - _gripOut, Well.Bottom + _gripOut);
    }

    public TrimGrip At(Point p)
    {
        if (!Area.Contains(p))
        {
            return TrimGrip.None;
        }
        bool onIn = GrabRectAt(XOf(_clip.In)).Contains(p);
        bool onOut = GrabRectAt(XEndOf(_clip.Out)).Contains(p);
        if (onIn && onOut)
        {
            if (Math.Abs(p.X - XOf(_clip.In)) <= Math.Abs(p.X - XEndOf(_clip.Out)))
            {
                return TrimGrip.In;
            }
            else
            {
                return TrimGrip.Out;
            }
        }
        if (onIn)
        {
            return TrimGrip.In;
        }
        if (onOut)
        {
            return TrimGrip.Out;
        }
        return TrimGrip.Bar;
    }

    private Rect GrabRectAt(int x)
    {
        return Column(x, _grab, Area.Top, Area.Bottom);
    }

    private static Rect Column(int x, int width, int top, int bottom)
    {
        return new()
        {
            Left = x - width / 2,
            Top = top,
            Right = x - width / 2 + width,
            Bottom = bottom,
        };
    }
}
