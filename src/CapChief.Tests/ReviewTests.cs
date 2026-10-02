using static CapChief.Tests.Fixtures;

namespace CapChief.Tests;

/// <summary>
/// Tests that trimming, timing and playback speed of a recording and its review controls work.
/// </summary>
public sealed class ReviewTests
{
    private readonly Desktop _desk = Desk();

    [Fact]
    public void AClipStartsAtItsFirstFrameAndEndsAtItsLast()
    {
        Clip c = TenFrames();
        Eq(c.Count, 10, "a clip has the frames it was given");
        Eq(c.In, 0, "it starts at the first");
        Eq(c.Out, 9, "and ends at the last");
        Assert.True(c.IsUnedited, "and says it is untouched");

        c.SetOut(4);
        Eq(c.Out, 4, "the tail comes in");
        Assert.False(c.IsUnedited, "which is a trim");
        c.SetIn(7);
        Eq(c.In, 4, "the head cannot pass the tail");

        c.Seek(9);
        Eq(c.Playhead, 4, "the playhead is held inside the kept run");
        c.SetOut(8);
        c.Seek(6);
        c.SetIn(7);
        Eq(c.Playhead, 7, "and is carried along when the head moves past it");

        c.Advance(-40);
        Eq(c.Playhead, 7, "stepping stops at the head rather than wrapping");
        c.Advance(40);
        Eq(c.Playhead, 8, "and at the tail");

        c.Trim(0, c.Count - 1);
        Eq(c.In, 0, "a reset puts the head back");
        Eq(c.Out, 9, "and the tail");
        Assert.True(c.IsUnedited, "and it is untouched again");
    }

    [Fact]
    public void AClipsLengthIsTheSumOfItsFrameTimes()
    {
        Clip c = TenFrames();
        Eq(c.LengthMs, 1000, "ten frames of ten centiseconds is a second");
        Eq(c.MsOf(0), 0, "the first frame starts at nought");
        Eq(c.MsOf(3), 300, "and the fourth three tenths in");

        Eq(c.FrameAtMs(0), 0, "nought is the first frame");
        Eq(c.FrameAtMs(99), 0, "and so is the last millisecond of it");
        Eq(c.FrameAtMs(100), 1, "a tenth in is the second");
        Eq(c.FrameAtMs(1000), 0, "and a second in has looped round");
        Eq(c.FrameAtMs(1250), 2, "as a GIF does, over and over");

        c.SetIn(4);
        Eq(c.LengthMs, 600, "trimming the head shortens the run");
        Eq(c.MsOf(4), 0, "and the in point is the new nought");
        Eq(c.FrameAtMs(0), 4, "which is where playback starts");
        Eq(c.FrameAtMs(650), 4, "and what it loops back to");

        Clip fast = TenFrames(1);
        Eq(fast.PlayedDelayCs(0), Clip.SlowedDelayCs, "a delay under the floor is shown at the floor");
        Eq(fast.LengthMs, 1000, "so ten such frames still read as a second");
    }

    [Fact]
    public void AClipStartsAtItsRecordedSpeedAndStepsSpeeds()
    {
        Clip c = TenFrames();
        Eq(c.Speed, ClipSpeed.Normal, "a clip starts at the speed it was recorded");
        c.SetSpeed(c.NextSpeed);
        Eq(c.Speed, ClipSpeed.Double, "the first step along is twice");
        Eq(c.ScaledDelayCs(0), 5, "which halves the delays");
        Eq(c.LengthMs, 500, "and the run with them");
        c.SetSpeed(c.NextSpeed);
        Eq(c.Speed, ClipSpeed.Half, "then half");
        Eq(c.ScaledDelayCs(0), 20, "which doubles them");
        c.SetSpeed(c.NextSpeed);
        Eq(c.Speed, ClipSpeed.Normal, "and round to where it started");

        Clip odd = TenFrames(5);
        odd.SetSpeed(ClipSpeed.Double);
        int sum = 0;
        for (int i = 0; i < odd.Count; i++)
        {
            sum += odd.ScaledDelayCs(i);
        }
        Eq(sum, 25, "halving fifty centiseconds of odd frames gives twenty-five, not thirty");
        Eq(odd.ScaledDelayCs(0) + odd.ScaledDelayCs(1), 5, "which it does by alternating the rounding");
    }

    [Fact]
    public void AClipWritesItsHeaderBeforeItsFrames()
    {
        List<GifFrame> index = TenIndex();
        Clip c = TenFrames();
        Eq(ClipTrimWriter.HeaderBytes(index), 800, "the header is everything before the first frame");
        Eq(ClipTrimWriter.CopyBytes(c, index), 1000, "an untrimmed clip copies all ten frames");

        c.SetOut(4);
        Eq(ClipTrimWriter.CopyFrom(c), 0, "with no head trim the copying starts at the first frame");
        Eq(ClipTrimWriter.CopyBytes(c, index), 500, "and stops at the out point");

        c.SetIn(2);
        Eq(ClipTrimWriter.CopyFrom(c), 3, "a head trim re-encodes the in point, so copying starts after it");
        Eq(ClipTrimWriter.CopyBytes(c, index), 200, "which is two of the five frames left");
    }

    [Fact]
    public void ACutRecordingSavesTheAreaAtTheSizeItPromised()
    {
        using var run = new Headless();
        run.Record(R(100, 100, 400, 300), frames: 8);
        run.Drag(P(400, 300), P(300, 250));
        Review review = run.App.Review;
        review.Advance(2);
        review.Trim(head: true);

        string path = Path.Combine(Path.GetTempPath(), $"{Product.FilePrefix}test_{Guid.NewGuid().ToString("N")}.gif");
        try
        {
            Assert.True(review.SaveAs(path), "the cut recording saves");
            GifProbe.GifHeader got = GifProbe.Decode(File.ReadAllBytes(path));
            Assert.True(got.Error is null, $"it decodes ({got.Error})");
            Eq(got.W, 200, "as wide as the area now is");
            Eq(got.H, 150, "and as tall");
            Eq(got.Frames.Count, 7, "with the frames the trim keeps");
            Eq(new FileInfo(path).Length, review.SaveSize, "and the bytes the review promised");

            run.Drag(P(300, 250), P(400, 300));
            Assert.True(review.SaveAs(path), "grown back to what was recorded, it saves");
            Eq(GifProbe.Decode(File.ReadAllBytes(path)).W, 300, "at the width it was recorded");
            Eq(new FileInfo(path).Length, review.SaveSize, "and the bytes promised again");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TrimsAndSpeedUndoInTheOrderMade()
    {
        using var run = new Headless();
        run.Record(R(100, 100, 400, 300), frames: 8);
        Review review = run.App.Review;
        Clip clip = review.Clip!;

        review.Advance(2);
        review.Trim(head: true);
        review.CycleSpeed();
        run.Drag(P(400, 300), P(300, 250));
        review.PressTimeline(new Scrub(TrimGrip.Out, 6, 7));
        review.DragTimeline(new Scrub(TrimGrip.Out, 5, 6));
        review.ReleaseTimeline();
        Eq(clip.Out, 5, "the out handle dragged back trims the tail");

        run.App.Workspace.Undo();
        Eq(clip.Out, clip.Count - 1, "undo puts the whole drag back as one step");
        run.App.Workspace.Undo();
        Eq(run.Capture.Crop, R(100, 100, 400, 300), "then the area");
        run.App.Workspace.Undo();
        Eq(clip.Speed, ClipSpeed.Normal, "then the speed");
        run.App.Workspace.Undo();
        Eq(clip.In, 0, "then the head trim");

        run.App.Workspace.Redo();
        Eq(clip.In, 2, "and redo trims the head again");
        review.Untrim();
        Eq(clip.In, 0, "clearing the trim is a step of its own");
        run.App.Workspace.Undo();
        Eq(clip.In, 2, "which undo takes back");
    }

    [Fact]
    public void TheTimelineTrackFitsItsStripAndCentersItsWell()
    {
        Clip clip = TenFrames();
        Rect area = R(100, 20, 400, 54);
        var t = new TimelineLayout(area, clip, 1f);

        Eq(t.Area.Width, 300, "the track takes the strip it is given");
        Assert.True(t.Well.Height < area.Height, "the well drawn in it is slimmer than the strip");
        Eq((t.Well.Top + t.Well.Bottom) / 2, (area.Top + area.Bottom) / 2, "and centered in it");
        Assert.True(t.Run.Left > t.Well.Left && t.Run.Right < t.Well.Right, "the run stops short of the well, so an end handle has somewhere to sit");

        Eq(t.XOf(0), t.Run.Left, "the first frame starts at the left of the run");
        Eq(t.XEndOf(9), t.Run.Right, "and the last one ends at its right");
        Eq(t.FrameAt(t.Run.Left), 0, "a point at the left is the first frame");
        Eq(t.FrameAt(t.Run.Right), 9, "one at the right is the last");
        Eq(t.FrameAt(t.Run.Left - 500), 0, "and one off either end is clamped");
        Eq(t.FrameAt(t.Run.Right + 500), 9, "at both ends");

        int slot = t.Run.Width / 10;
        Eq(t.BoundaryAt(t.Run.Left), 0, "the left of the run is the boundary in front of the clip");
        Eq(t.BoundaryAt(t.Run.Right), 10, "and its right is the one behind it");
        Eq(t.BoundaryAt(t.XOf(4)), 4, "a point on a boundary is that boundary");
        Eq(t.BoundaryAt(t.XOf(4) + slot / 4), 4, "a quarter past it still is");
        Eq(t.BoundaryAt(t.XOf(4) + slot * 3 / 4), 5, "three quarters past it is the next one");
        Eq(t.BoundaryAt(t.Run.Left - 500), 0, "and it clamps at both ends");
        Eq(t.BoundaryAt(t.Run.Right + 500), 10, "as the frame lookup does");
    }

    [Fact]
    public void TheTimelineHasInAndOutHandlesAtTheRunsEnds()
    {
        Clip clip = TenFrames();
        Rect area = R(100, 20, 400, 54);
        var t = new TimelineLayout(area, clip, 1f);
        int mid = (area.Top + area.Bottom) / 2;

        Eq(t.At(P(t.Run.Left, mid)), TrimGrip.In, "the in handle is at the head of the run");
        Eq(t.At(P(t.Run.Right, mid)), TrimGrip.Out, "the out handle at its tail");
        Eq(t.At(P(t.Run.Left + t.Run.Width / 2, mid)), TrimGrip.Bar, "between them is the track");
        Eq(t.At(P(area.Left - 20, mid)), TrimGrip.None, "and beside the strip is nothing");

        Eq(t.At(P(t.Run.Left, area.Top)), TrimGrip.In, "the grab runs to the top of the strip");
        Eq(t.At(P(t.Run.Left, area.Bottom - 1)), TrimGrip.In, "and to the bottom of it");
        Assert.True(t.InGrip.Top > area.Top && t.InGrip.Bottom < area.Bottom, "while the handle is drawn well inside that");

        clip.SetIn(3);
        clip.SetOut(6);
        var moved = new TimelineLayout(area, clip, 1f);
        Eq(moved.At(P(moved.XOf(3), mid)), TrimGrip.In, "the handles follow the trim");
        Eq(moved.At(P(moved.XEndOf(6), mid)), TrimGrip.Out, "both of them");
        Eq(moved.KeptRegion.Left, moved.XOf(3), "and the kept band runs between them");
        Eq(moved.KeptRegion.Right, moved.XEndOf(6), "from the in point to the far edge of the out one");

        var one = new Clip([10]);
        var tiny = new TimelineLayout(area, one, 1f);
        Eq(tiny.At(P(tiny.Run.Left, mid)), TrimGrip.In, "on a one-frame clip the left is the in point");
        Eq(tiny.At(P(tiny.Run.Right, mid)), TrimGrip.Out, "and the right is still the out one");
    }

    [Fact]
    public void TheToolbarPlacesItselfBesideOrUnderTheCrop()
    {
        ToolbarPlacement Toolbar()
        {
            var f = new ToolbarPlacement(_desk);
            f.Resize(183, 56, 10);
            return f;
        }

        ToolbarPlacement mid = Toolbar();
        mid.Place(R(400, 300, 900, 700));
        Eq(mid.X, 910, "beside a crop there is room beside, the bar goes to its right");
        Eq(mid.Y, 644, "with their bottoms level");

        ToolbarPlacement right = Toolbar();
        right.Place(R(1400, 300, 1920, 700));
        Eq(right.Y, 710, "with no room to the right of the crop, the bar goes under it");
        Eq(right.X, 1737, "right edges level");

        ToolbarPlacement corner = Toolbar();
        corner.Place(R(1400, 700, 1920, 1080));
        Eq(corner.Y, 634, "with no room under it either, the bar goes above the crop");
        Eq(corner.X, 1737, "right edges level again");

        ToolbarPlacement full = Toolbar();
        full.Place(R(0, 0, 1920, 1080));
        Eq(full.X, 1727, "over a full-screen crop the bar goes inside, at its right");
        Eq(full.Y, 10, "at the top");

        Assert.False(Rectangle(mid).Overlaps(R(400, 300, 900, 700)), "the bar beside a crop is clear of it");
        Assert.False(Rectangle(right).Overlaps(R(1400, 300, 1920, 700)), "the bar under a crop is clear of it");
        Assert.False(Rectangle(corner).Overlaps(R(1400, 700, 1920, 1080)), "the bar above a crop is clear of it");

        ToolbarPlacement kept = Toolbar();
        kept.Place(null);
        kept.MoveTo(100, 100);
        kept.KeepAsIdleSpot();
        kept.Place(null);
        Eq(kept.X, 100, "with no crop to sit beside, the bar stays where it was dragged");
        Eq(kept.Y, 100, "in both directions");
        kept.Place(R(400, 300, 900, 700));
        kept.Place(null);
        Eq(P(kept.X, kept.Y), P(100, 100), "and idle again after a capture it goes back there, not beside the capture");
    }

    private static List<GifFrame> TenIndex(int cs = 10)
    {
        var frames = new List<GifFrame>();
        for (int i = 0; i < 10; i++)
        {
            frames.Add(new GifFrame(800 + i * 100, 100, cs));
        }
        return frames;
    }

    private static Clip TenFrames(int cs = 10)
    {
        return new(GifFrame.DelaysOf(TenIndex(cs)));
    }

    private static Rect Rectangle(ToolbarPlacement f)
    {
        return Rect.At(f.X, f.Y, f.Width, f.Height);
    }
}
