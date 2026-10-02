using static CapChief.Tests.Fixtures;

namespace CapChief.Tests;

/// <summary>
/// Tests that recordings are written as correct GIF files: the compression round trip, the color
/// tables, the frame delays, the frames that only carry what changed, trimming and speed changes,
/// and a whole recording of a test screen read back frame by frame.
/// </summary>
public sealed class GifTests
{
    private const int MadeFrames = 9;
    private const int MadeWidth = 40;
    private const int MadeHeight = 24;

    [Fact]
    public void CompressionRoundTrips()
    {
        RoundTrip("all the same", Pixels.Bgra(64, 64, _ => (7, 7, 7)), 64, 64, wantClears: 0);
        RoundTrip("all different", Pixels.Bgra(64, 64, i => Pixels.Spread(i % 254)), 64, 64, wantClears: 0);

        uint seed = 12345;
        byte[] noise = Pixels.Bgra(256, 256, _ =>
        {
            seed = seed * 1664525u + 1013904223u;
            return Pixels.Spread((int)((seed >> 24) % ColorTable.MaxColors));
        });
        RoundTrip("noise, past the 4096-code reset", noise, 256, 256, wantClears: 1);
    }

    [Fact]
    public void AFewColorsAreKeptExactly()
    {
        const int n = ColorTable.MaxColors;
        ColorTable t = Pixels.TableOf(Pixels.Bgra(32, 32, i => Pixels.Spread(i % n)), 32, 32);
        Assert.True(t.IsExact, $"a {n}-color frame is kept exactly");
        Eq(t.Count, n, "with one entry for each color");

        int wrong = 0;
        for (int i = 0; i < n; i++)
        {
            (int r, int g, int b) = Pixels.Spread(i);
            t.Entry(t.IndexOf(r, g, b), out int er, out int eg, out int eb);
            if (er != r || eg != g || eb != b)
            {
                wrong++;
            }
        }
        Eq(wrong, 0, "colors that do not map to themselves");

        ColorTable over = Pixels.TableOf(Pixels.Bgra(32, 32, i => Pixels.Spread(i % (n + 1))), 32, 32);
        Assert.False(over.IsExact, "one color more than that falls to the median cut");
        Assert.True(over.Count <= n, $"which never hands back more than {n} entries, leaving the last for see-through");
    }

    [Fact]
    public void ATableTakesTheSmallestSizeThatHoldsIt()
    {
        foreach ((int colors, int size) in new[] { (1, 2), (2, 4), (3, 4), (4, 8), (127, 128), (128, 256), (255, 256) })
        {
            ColorTable t = Pixels.TableOf(Pixels.Bgra(16, 16, i => Pixels.Spread(i % colors)), 16, 16);
            Eq(t.Count, colors, $"{colors} colors are kept as {colors} entries");
            Eq(t.Size, size, $"in a table of {size}, the smallest power of two with an entry left over");
            Eq(t.Transparent, size - 1, $"and the entry left over, the last of the {size}, is see-through");
        }
    }

    [Fact]
    public void TheMainColorKeepsItsOwnEntry()
    {
        const int w = 500;
        const int h = 200;
        byte[] px = Pixels.Bgra(w, h, i =>
        {
            if (i % 100 < 74)
            {
                return (255, 255, 255);
            }
            else
            {
                return Pixels.Dull(i / 100 % 400);
            }
        });
        ColorTable t = Pixels.TableOf(px, w, h);
        Assert.False(t.IsExact, "a 401-color frame needs the median cut");
        t.Entry(t.IndexOf(255, 255, 255), out int r, out int g, out int b);
        Assert.True(r == 255 && g == 255 && b == 255, $"white, at 74 % of the frame, lands on {r:X2}{g:X2}{b:X2}, which has to be FFFFFF");
        double err = Pixels.MeanError(t, px, w, h);
        Assert.True(err < 1.0, $"mean error over that frame is {err:0.00} of 255");
    }

    [Fact]
    public void AScreenLosesLittleToTheMedianCut()
    {
        const int w = 480;
        const int h = 360;
        byte[] px = Pixels.Bgra(w, h, i =>
        {
            int x = i % w;
            int y = i / w;
            int b = 38 + 24 * (w - x) / w;
            int g = 56 + 42 * y / h;
            int r = 10 + 26 * x / w;
            if (((x >> 2) + (y >> 2) & 1) == 0)
            {
                b += 44;
                g += 22;
                r += 38;
            }
            if (x % 128 < 12)
            {
                (r, g, b) = (0xF0, 0xC8, 0x20);
            }
            if (y % 96 < 8)
            {
                (r, g, b) = (0x30, 0x40, 0xE0);
            }
            return (Math.Min(r, 255), Math.Min(g, 255), Math.Min(b, 255));
        });
        ColorTable t = Pixels.TableOf(px, w, h);
        double err = Pixels.MeanError(t, px, w, h);
        Assert.False(t.IsExact, "a screen of gradients needs the median cut");
        Assert.True(err < 4.0, $"mean error over {w}x{h} of it is {err:0.00} of 255");
    }

    [Fact]
    public void DelaysCarryWhatTheyRoundAway()
    {
        const int n = 30;
        var steady = new Centiseconds();
        long total = 0;
        long cs = 0;
        long truncated = 0;
        for (int i = 0; i < n; i++)
        {
            total += 105;
            cs += steady.Next(105);
            truncated += 105 / 10;
        }
        long unaccounted = total - cs * 10;
        Assert.True(unaccounted >= 0 && unaccounted < 10, $"30 ticks of 105 ms sum to {cs * 10} ms of {total}");
        Assert.True(truncated * 10 < total - 10, $"which truncating alone would not, at {truncated * 10} ms");

        uint seed = 999;
        var jittered = new Centiseconds();
        total = 0;
        cs = 0;
        for (int i = 0; i < n; i++)
        {
            seed = seed * 1664525u + 1013904223u;
            int ms = 90 + (int)(seed >> 24) % 21;
            total += ms;
            cs += jittered.Next(ms);
        }
        unaccounted = total - cs * 10;
        Assert.True(unaccounted >= 0 && unaccounted < 10, $"30 uneven ticks sum to {cs * 10} ms of {total}, inside one centisecond");
    }

    [Fact]
    public void AFrameCarriesOnlyWhatChanged()
    {
        const int w = 64;
        const int h = 64;
        byte[] a = Pixels.Bgra(w, h, i => Pixels.Dull(i % 37));

        GifProbe.GifHeader still = GifProbe.Decode(Pixels.Encode(w, h, (a, 10), (a, 10)));
        Assert.True(still.Error is null && still.Frames.Count == 2, $"two frames decode ({still.Error})");
        Eq(still.Subs[1], R(0, 0, 1, 1), "with nothing changed the frame is one pixel at the corner");
        Eq(still.Cells[1][0], still.SeeThrough[1], "and that pixel is see-through");

        byte[] b = (byte[])a.Clone();
        Pixels.Paint(b, 20 * w + 10, (200, 10, 10));
        Pixels.Paint(b, 25 * w + 40, (10, 200, 10));
        GifProbe.GifHeader got = GifProbe.Decode(Pixels.Encode(w, h, (a, 10), (b, 10)));
        Assert.True(got.Error is null && got.Frames.Count == 2, $"two frames decode ({got.Error})");
        Eq(got.Subs[1], R(10, 20, 41, 26), "two changed pixels give the box around them");
        Eq(got.Cells[1][1], got.SeeThrough[1], "and an unchanged pixel inside the box is see-through");
        Eq(Pixels.Differs(b, got.Frames[1]), 0, "and drawn over the first it is the second frame");
    }

    [Fact]
    public void ColorsThatAppearAfterTheFirstFrameAreKept()
    {
        const int w = 96;
        const int h = 64;
        byte[] gray = Pixels.Bgra(w, h, i =>
        {
            if (i % w < 48)
            {
                return (0xAA, 0xAA, 0xAA);
            }
            else
            {
                return (0x55, 0x55, 0x55);
            }
        });
        byte[] lit = (byte[])gray.Clone();
        for (int y = 10; y < 20; y++)
        {
            for (int x = 10; x < 20; x++)
            {
                Pixels.Paint(lit, y * w + x, (0xFF, 0xAA, 0x55));
                Pixels.Paint(lit, y * w + x + 50, (0xFF, 0xFF, 0x55));
            }
        }

        GifProbe.GifHeader got = GifProbe.Decode(Pixels.Encode(w, h, (gray, 10), (lit, 10), (gray, 10)));
        Assert.True(got.Error is null && got.Frames.Count == 3, $"three frames decode ({got.Error})");
        Eq(got.TableSize, 4, "the file's table is sized for the two grays of the first frame");
        Eq(Pixels.Differs(lit, got.Frames[1]), 0, "an orange and a yellow the first frame did not have come out exact");
        Eq(got.OwnTables[1], 4, "because their frame carries a table of its own, sized for the two of them");
        Eq(Pixels.Differs(gray, got.Frames[2]), 0, "and the grays come back exact after them");
        Eq(got.OwnTables[2], 0, "from the file's table, which holds them");
    }

    [Fact]
    public void AFrameOfManyNewColorsGetsTheBestTableOfItsOwn()
    {
        const int w = 160;
        const int h = 120;
        byte[] gray = Pixels.Bgra(w, h, _ => (0x80, 0x80, 0x80));
        byte[] fade = Pixels.Bgra(w, h, i => (i % w * 255 / w, i / w * 255 / h, 255 - i % w * 255 / w));

        GifProbe.GifHeader got = GifProbe.Decode(Pixels.Encode(w, h, (gray, 10), (fade, 10)));
        Assert.True(got.Error is null && got.Frames.Count == 2, $"two frames decode ({got.Error})");
        Eq(got.OwnTables[1], 256, "a frame of thousands of colors the first frame did not have carries a full table of its own");
        double err = Pixels.MeanError(fade, got.Frames[1]);
        double best = Pixels.MeanError(Pixels.TableOf(fade, w, h), fade, w, h);
        Assert.True(Math.Abs(err - best) < 1e-9, $"and loses {err:0.00} of 255 on average to it, what a table made for that frame alone loses ({best:0.00})");
    }

    [Fact]
    public void ARecordingIsAGifOfTheScreen()
    {
        var screen = new TestScreen();
        var recording = new Recording(screen);
        Crop crop = CropOf(40, 40, 360, 280);
        Assert.True(recording.Start(crop), "the recording starts");
        for (int i = 0; i < 8; i++)
        {
            Assert.True(recording.Frame(), $"frame {i + 1} is taken");
        }
        Clip? clip = recording.Stop();
        Assert.True(clip is { FilePath: not null }, "and stops into a clip written to a file");

        try
        {
            GifProbe.GifHeader got = GifProbe.Decode(File.ReadAllBytes(clip!.FilePath!));
            Assert.True(got.Error is null, $"the file decodes ({got.Error})");
            Eq(got.W, crop.Width, "as wide as the area");
            Eq(got.H, crop.Height, "and as tall");
            Assert.True(got.Loops, "it carries the loop block, so it plays more than once");
            Eq(got.Frames.Count, 9, "the eight frames taken and the one the start took");
            Eq(got.Subs[0], R(0, 0, crop.Width, crop.Height), "the first frame is whole");
            Assert.True(got.Delays.All(cs => cs == 10), "each frame lasts the 100 ms the screen said passed");
            Eq(clip.Count, 9, "and the clip knows its frames");

            for (int i = 0; i < got.Frames.Count; i++)
            {
                byte[] frame = got.Frames[i];
                Rect block = TestScreen.BlockAt(i, crop.Width);
                Eq(Pixels.ColorRefAt(frame, (crop.Height - 1) * crop.Width + crop.Width - 1), TestScreen.Ground, $"frame {i} keeps the ground's color");
                Eq(Pixels.ColorRefAt(frame, (block.Top + 2) * crop.Width + block.Left + 2), TestScreen.BlockColorAt(i), $"frame {i} shows the block where the screen had it, in the color it had, though the first frame never had it");
                if (i > 0 && !block.Overlaps(TestScreen.BlockAt(i - 1, crop.Width)))
                {
                    Rect was = TestScreen.BlockAt(i - 1, crop.Width);
                    Eq(Pixels.ColorRefAt(frame, (was.Top + 2) * crop.Width + was.Left + 2), TestScreen.Ground, $"and frame {i} no longer shows it where it was");
                }
            }
        }
        finally
        {
            clip?.DeleteFile();
        }
    }

    [Fact]
    public void TheReaderReadsEveryFrame()
    {
        OnAClip((made, reader, _) =>
        {
            Eq(new Size(reader.Width, reader.Height), new Size(MadeWidth, MadeHeight), "the reader reads the clip at its size");
            for (int i = 0; i < MadeFrames; i++)
            {
                Eq(Pixels.Differs(made.Want[i], reader.Composite(i)), 0, $"frame {i}, pixels out");
            }
        });
    }

    [Fact]
    public void TheReaderSeeksBackwards()
    {
        OnAClip((made, reader, _) =>
        {
            reader.Composite(MadeFrames - 1);
            Eq(Pixels.Differs(made.Want[1], reader.Composite(1)), 0, "seeking back to frame 1 from the end is the same frame 1");
            Eq(Pixels.Differs(made.Want[MadeFrames - 1], reader.Composite(MadeFrames - 1)), 0, "and forwards again to the last frame");
            Eq(Pixels.Differs(made.Want[4], reader.Composite(4)), 0, "and to the middle");
        });
    }

    [Fact]
    public void TheReaderNeverReadsPastTheEndOfAFrame()
    {
        OnAClip((made, _, _) =>
        {
            byte[] data = File.ReadAllBytes(made.Path);
            for (int i = 0; i < MadeFrames; i++)
            {
                GifFrame frame = made.Index[i];
                for (int kept = 1; kept < frame.Length; kept++)
                {
                    byte[] cut = data[..(int)(frame.Offset + kept)];
                    List<GifFrame> frames = [.. made.Index.Take(i), frame with { Length = kept }];
                    var reader = new GifReader();
                    Exception? e = Record.Exception(() =>
                    {
                        if (reader.Open(cut, frames))
                        {
                            reader.Composite(i);
                        }
                    });
                    Assert.True(e is null, $"frame {i} cut to {kept} bytes: {e?.GetType().Name}");
                }
            }
        });
    }

    [Fact]
    public void TheReaderRefusesASizeTooBigToHold()
    {
        OnAClip((made, _, _) =>
        {
            byte[] data = File.ReadAllBytes(made.Path);
            data[6] = data[7] = data[8] = data[9] = 0xFF;
            Assert.False(new GifReader().Open(data, made.Index), "a 65535x65535 header is refused");
        });
    }

    [Fact]
    public void AnUntrimmedClipIsCopied()
    {
        OnAClip((made, _, trim) =>
        {
            var clip = new Clip(GifFrame.DelaysOf(made.Index));
            Assert.True(clip.IsUnedited, "an untrimmed clip at its own speed says it is untouched");
            Assert.True(trim.Write(made.Out, clip), "it writes");
            Assert.True(File.ReadAllBytes(made.Path).AsSpan().SequenceEqual(File.ReadAllBytes(made.Out)), "byte for byte as it was");
        });
    }

    [Fact]
    public void TrimmingTheTailKeepsTheFramesBeforeIt()
    {
        OnAClip((made, _, trim) =>
        {
            const int last = MadeFrames - 3;
            var clip = new Clip(GifFrame.DelaysOf(made.Index));
            clip.SetOut(last);
            Assert.False(clip.IsUnedited, "a clip with its tail pulled in is not untouched");
            Assert.True(trim.Write(made.Out, clip), "it writes");

            GifProbe.GifHeader got = GifProbe.Decode(File.ReadAllBytes(made.Out));
            Assert.True(got.Error is null, $"it decodes ({got.Error})");
            Eq(got.Frames.Count, last + 1, "into the frames kept");
            Eq(new Size(got.W, got.H), new Size(MadeWidth, MadeHeight), "at the size it always was");
            for (int i = 0; i < got.Frames.Count; i++)
            {
                Eq(Pixels.Differs(made.Want[i], got.Frames[i]), 0, $"frame {i}, pixels out");
            }
        });
    }

    [Fact]
    public void TrimmingTheHeadRedrawsTheFirstFrameWhole()
    {
        OnAClip((made, _, trim) =>
        {
            const int first = 3;
            var clip = new Clip(GifFrame.DelaysOf(made.Index));
            clip.SetIn(first);
            Eq(ClipTrimWriter.CopyFrom(clip), first + 1, "the in point is encoded again and the copying starts after it");
            Assert.True(trim.Write(made.Out, clip), "it writes");

            GifProbe.GifHeader got = GifProbe.Decode(File.ReadAllBytes(made.Out));
            Assert.True(got.Error is null, $"it decodes ({got.Error})");
            Eq(got.Frames.Count, MadeFrames - first, "into the frames kept");
            Eq(got.Subs[0], R(0, 0, MadeWidth, MadeHeight), "the frame that opens it is whole, as the first frame of a clip has to be");
            for (int i = 0; i < got.Frames.Count; i++)
            {
                Eq(Pixels.Differs(made.Want[first + i], got.Frames[i]), 0, $"frame {i}, pixels out");
            }
        });
    }

    [Fact]
    public void ChangingTheSpeedChangesOnlyTheDelays()
    {
        OnAClip((made, _, trim) =>
        {
            var clip = new Clip(GifFrame.DelaysOf(made.Index));
            clip.SetSpeed(clip.NextSpeed);
            Eq(clip.Speed, ClipSpeed.Double, "the first speed along from as-recorded is twice");
            Assert.True(trim.Write(made.Out, clip), "it writes");

            GifProbe.GifHeader got = GifProbe.Decode(File.ReadAllBytes(made.Out));
            Assert.True(got.Error is null, $"it decodes ({got.Error})");
            long raw = 0;
            long sped = 0;
            for (int i = 0; i < got.Frames.Count; i++)
            {
                Eq(Pixels.Differs(made.Want[i], got.Frames[i]), 0, $"frame {i}, pixels out");
                Eq(got.Delays[i], clip.ScaledDelayCs(i), $"frame {i} lasts what the clip asked for");
                raw += made.Index[i].DelayCs;
                sped += got.Delays[i];
            }
            Eq(sped, raw / 2, "the clip runs half the time it was recorded over");
        });
    }

    [Fact]
    public void TheSizeIsKnownBeforeWriting()
    {
        OnAClip((made, _, trim) =>
        {
            foreach (int first in new[] { 0, 2, 5 })
            {
                var clip = new Clip(GifFrame.DelaysOf(made.Index));
                clip.SetIn(first);
                clip.SetOut(MadeFrames - 2);
                long promised = trim.SizeOf(clip);
                Assert.True(trim.Write(made.Out, clip), $"in at {first}, it writes");
                Eq(new FileInfo(made.Out).Length, promised, $"in at {first}, the bytes promised are the bytes written");
            }
        });
    }

    [Fact]
    public void CroppingKeepsWhatIsInsideTheArea()
    {
        OnAClip((made, reader, _) =>
        {
            foreach (Rect area in new[] { R(3, 2, 31, 20), R(0, 10, 30, 20) })
            {
                using var copy = new MemoryStream();
                List<GifFrame>? frames = GifCrop.Write(reader, area, copy);
                Assert.True(frames is { Count: MadeFrames }, $"{area}: every frame is written");
                byte[] cut = copy.ToArray();

                GifProbe.GifHeader got = GifProbe.Decode(cut);
                Assert.True(got.Error is null, $"{area}: it decodes ({got.Error})");
                Eq(new Size(got.W, got.H), area.Size, $"{area}: at the size of the area");
                Assert.True(got.Loops, $"{area}: it still loops");
                for (int i = 0; i < MadeFrames; i++)
                {
                    Eq(Pixels.Differs(Cut(made.Want[i], MadeWidth, area), got.Frames[i]), 0, $"{area}: frame {i}, pixels out");
                    Eq(got.Delays[i], made.Index[i].DelayCs, $"{area}: frame {i} keeps its delay");
                }
                Eq(frames![^1].Offset + frames[^1].Length + 1, cut.Length, $"{area}: the index ends where the trailer starts");
            }
        });
    }

    private static void RoundTrip(string what, byte[] px, int w, int h, int wantClears)
    {
        GifProbe.GifHeader got = GifProbe.Decode(Pixels.Encode(w, h, (px, 10)));
        Assert.True(got.Error is null, $"{what}: decodes without error ({got.Error})");
        Eq(got.Frames.Count, 1, $"{what}: one frame decodes");
        Eq(Pixels.Differs(px, got.Frames[0]), 0, $"{what}: pixels whose color differs after a round trip");
        Assert.True(got.Clears >= wantClears, $"{what}: {got.Clears} clear codes, at least {wantClears} expected");
    }

    private static byte[] Cut(byte[] bgra, int w, Rect area)
    {
        var cut = new byte[area.Width * area.Height * 4];
        for (int y = 0; y < area.Height; y++)
        {
            Array.Copy(bgra, ((area.Top + y) * w + area.Left) * 4, cut, y * area.Width * 4, area.Width * 4);
        }
        return cut;
    }

    private static void OnAClip(Action<MadeClip, GifReader, ClipTrimWriter> check)
    {
        MadeClip made = MadeClip.Write();
        var reader = new GifReader();
        try
        {
            Assert.True(reader.Open(File.ReadAllBytes(made.Path), made.Index), "the reader opens the clip");
            check(made, reader, new ClipTrimWriter(reader));
        }
        finally
        {
            reader.Close();
            File.Delete(made.Path);
            File.Delete(made.Out);
        }
    }

    /// <summary>
    /// Represents a small clip written for the tests, the frames it was written from, and a path
    /// beside it to write a trimmed copy to.
    /// </summary>
    private sealed class MadeClip
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Product.FilePrefix}test_{Guid.NewGuid().ToString("N")}.gif");

        public string Out
        {
            get
            {
                return $"{Path}.out.gif";
            }
        }

        public List<GifFrame> Index { get; } = new();

        public List<byte[]> Want { get; } = new();

        public static MadeClip Write()
        {
            var made = new MadeClip();
            using var file = new FileStream(made.Path, FileMode.Create, FileAccess.Write);
            var gif = new GifWriter(file);
            for (int i = 0; i < MadeFrames; i++)
            {
                byte[] cur = Pixels.Bgra(MadeWidth, MadeHeight, at => Pixels.Dull(at % 7));
                for (int y = 2; y < 8; y++)
                {
                    for (int x = i * 3; x < i * 3 + 5 && x < MadeWidth; x++)
                    {
                        Pixels.Paint(cur, y * MadeWidth + x, Pixels.Spread(200 + i));
                    }
                }
                Pixels.Paint(cur, (MadeHeight - 2) * MadeWidth + MadeWidth - 2, Pixels.Spread(100 + i));

                if (i == 0)
                {
                    Assert.True(gif.Begin(MadeWidth, MadeHeight, cur), "the writer opens");
                }
                Assert.True(gif.Frame(cur, 5 + i % 3, out GifFrame frame), $"frame {i} writes");
                made.Index.Add(frame);
                made.Want.Add(cur);
            }
            gif.End();
            return made;
        }
    }
}
