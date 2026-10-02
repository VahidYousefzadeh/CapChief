namespace CapChief.Shell;

/// <summary>
/// Represents the timeline control used to play and trim a recording.
/// </summary>
public sealed class Timeline : Control
{
    private const uint HeadArgb = Palette.OnPanelArgb;

    private readonly Review _review;

    private bool _scrubbing;

    public Timeline(Review review)
    {
        _review = review;

        HorizontalAlignment = HorizontalAlignment.Stretch;
        MinWidth = Theme.TimelineMin;
        MaxWidth = Theme.TimelineMax;
        Height = Theme.ButtonSide;
    }

    public override CursorShape? CursorAt(Point p)
    {
        if (_scrubbing)
        {
            return CursorShape.SizeWE;
        }
        switch (GripAt(p))
        {
            case TrimGrip.In or TrimGrip.Out:
                return CursorShape.SizeWE;
            case TrimGrip.Bar:
                return CursorShape.Hand;
            default:
                return CursorShape.Arrow;
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        e.Handled = true;
        if (_review.Clip is not { } clip || !_review.PressTimeline(Scrubbed(clip, e.Location)))
        {
            return;
        }
        _scrubbing = true;
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        if (_scrubbing && _review.Clip is { } clip)
        {
            _review.DragTimeline(Scrubbed(clip, e.Location));
        }
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        if (!_scrubbing)
        {
            return;
        }
        _scrubbing = false;
        _review.ReleaseTimeline();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_review.Clip is not { } clip)
        {
            return;
        }
        DrawContext d = e.Draw;
        TimelineLayout t = Of(clip);

        float r = t.Well.Height * 0.5f;
        Fill(d, t.Well, Palette.TimelineWellArgb, r);
        Fill(d, t.KeptRegion, Palette.TimelineKeptArgb, r);
        Fill(d, t.PlayedRegion, Palette.AccentArgb, r);

        float hr = t.InGrip.Width * 0.45f;
        Fill(d, t.InGrip, Palette.AccentArgb, hr);
        Fill(d, t.OutGrip, Palette.AccentArgb, hr);

        Fill(d, t.PlayheadRegion, HeadArgb, t.PlayheadRegion.Width * 0.5f);
    }

    private TrimGrip GripAt(Point p)
    {
        if (_review.Clip is { } clip)
        {
            return Of(clip).At(p);
        }
        else
        {
            return TrimGrip.None;
        }
    }

    private TimelineLayout Of(Clip clip)
    {
        return new(Bounds, clip, Scale);
    }

    private Scrub Scrubbed(Clip clip, Point p)
    {
        TimelineLayout t = Of(clip);
        return new(t.At(p), t.FrameAt(p.X), t.BoundaryAt(p.X));
    }

    private static void Fill(DrawContext d, Rect r, uint argb, float radius)
    {
        if (!r.IsEmpty)
        {
            d.FillRound(r.Left, r.Top, r.Width, r.Height, radius, Color.FromArgb(argb));
        }
    }
}
