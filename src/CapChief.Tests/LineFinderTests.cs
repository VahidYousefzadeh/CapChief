using static CapChief.Tests.Fixtures;

namespace CapChief.Tests;

/// <summary>
/// Tests that straight lines are found in a picture, and that text, faint noise and patterns are
/// not.
/// </summary>
public sealed class LineFinderTests
{
    private const int Reach = 6;

    private const int Inside = 255, Border = 229, Gap = 243, CornerRadius = 3;

    private const int ButtonsWidth = 235, ButtonsHeight = 63;

    private static readonly Rect[] Buttons =
    [
        R(-40, 0, 17, 63),
        R(19, 0, 126, 63),
        R(128, 0, 235, 63),
    ];

    [Fact]
    public void ALeftEdgeLandsOnTheBorderOfTheButtonToItsRight()
    {
        var lines = new LineFinder(ButtonsPicture(transposed: false), ButtonsWidth, ButtonsHeight, 1f);
        for (int at = 13; at <= 26; at++)
        {
            Eq(lines.NearestLine(Edges.West, at, 0, ButtonsHeight, Reach), 19, $"a left edge at {at} lands on the left border of button 2, never the right border of button 1 3 px before it");
        }
        Eq(lines.NearestLine(Edges.West, 12, 0, ButtonsHeight, Reach), 16, "7 px from it, it takes the right border of button 1, the only line in reach");
    }

    [Fact]
    public void ABorderWithNoOtherLineInReachIsTakenFromEitherSide()
    {
        byte[] picture = Pixels.Bgra(100, 100, i =>
        {
            int x = i % 100;
            int y = i / 100;
            bool inBox = x >= 50 && y >= 20 && y < 80;
            bool onBorder = x == 50 || y is 20 or 79;
            int tone;
            if (inBox)
            {
                if (onBorder)
                {
                    tone = Border;
                }
                else
                {
                    tone = Inside;
                }
            }
            else
            {
                tone = Gap;
            }
            return Gray(tone);
        });
        var lines = new LineFinder(picture, 100, 100, 1f);
        Eq(lines.NearestLine(Edges.West, 46, 0, 100, Reach), 50, "a left edge takes the left border of the box to its right");
        Eq(lines.NearestLine(Edges.East, 46, 0, 100, Reach), 51, "and a right edge outside the box takes it too, though its corners bend away");
    }

    [Fact]
    public void ARightEdgeLandsJustPastTheBorderOfTheButtonToItsLeft()
    {
        var lines = new LineFinder(ButtonsPicture(transposed: false), ButtonsWidth, ButtonsHeight, 1f);
        for (int at = 10; at <= 23; at++)
        {
            Eq(lines.NearestLine(Edges.East, at, 0, ButtonsHeight, Reach), 17, $"a right edge at {at} keeps the right border of button 1 inside, and never takes button 2's");
        }
    }

    [Fact]
    public void TopAndBottomEdgesFindLinesTheSameWay()
    {
        var lines = new LineFinder(ButtonsPicture(transposed: true), ButtonsHeight, ButtonsWidth, 1f);
        Eq(lines.NearestLine(Edges.North, 15, 0, ButtonsHeight, Reach), 19, "a top edge lands on the top border below it");
        Eq(lines.NearestLine(Edges.South, 21, 0, ButtonsHeight, Reach), 17, "a bottom edge keeps the bottom border above it");
    }

    [Fact]
    public void TextIsNotALine()
    {
        byte[] picture = ButtonsPicture(transposed: false);
        for (int y = 25; y < 38; y++)
        {
            for (int x = 70; x < 72; x++)
            {
                Paint(picture, ButtonsWidth, x, y, 50);
            }
        }
        var lines = new LineFinder(picture, ButtonsWidth, ButtonsHeight, 1f);
        Eq(lines.NearestLine(Edges.West, 70, 0, ButtonsHeight, Reach), null, "the 13 px stroke of a digit is too short to be a line");
    }

    [Fact]
    public void AFaintLineCountsFromEightLevels()
    {
        Eq(VerticalLine(Gap - 6).NearestLine(Edges.West, 48, 0, 100, Reach), null, "a line 6 levels from its background is not found");
        Eq(VerticalLine(Gap - 8).NearestLine(Edges.West, 48, 0, 100, Reach), 50, "a line 8 levels from it is");
    }

    [Fact]
    public void ALineCountsOnlyAlongsideTheEdge()
    {
        LineFinder lines = VerticalLine(Border, height: 200, lineHeight: 100);
        Eq(lines.NearestLine(Edges.West, 48, 120, 200, Reach), null, "a line that ends above the edge is not found");
        Eq(lines.NearestLine(Edges.West, 48, 80, 200, Reach), 50, "one that runs 20 px alongside it is");
        Eq(lines.NearestLine(Edges.East, 48, 80, 200, Reach), 51, "and a right edge keeps it inside too");
    }

    [Fact]
    public void ACheckerboardIsNotLines()
    {
        byte[] picture = Pixels.Bgra(100, 100, i =>
        {
            int tone;
            if (((i % 100 >> 2) + (i / 100 >> 2) & 1) == 0)
            {
                tone = 200;
            }
            else
            {
                tone = 160;
            }
            return Gray(tone);
        });
        var lines = new LineFinder(picture, 100, 100, 1f);
        for (int at = 40; at < 50; at++)
        {
            Eq(lines.NearestLine(Edges.West, at, 0, 100, Reach), null, $"the cell edges at {at} flip every 4 px, so they are no line");
        }
    }

    [Fact]
    public void ATwoPixelLineAtTwiceTheScaleIsOneLine()
    {
        byte[] picture = Pixels.Bgra(100, 100, i =>
        {
            int tone;
            if (i % 100 is 40 or 41)
            {
                tone = Border;
            }
            else
            {
                tone = Gap;
            }
            return Gray(tone);
        });
        var lines = new LineFinder(picture, 100, 100, 2f);
        Eq(lines.NearestLine(Edges.West, 44, 0, 100, 12), 40, "a left edge takes both columns of the line");
        Eq(lines.NearestLine(Edges.East, 38, 0, 100, 12), 42, "and so does a right edge");
    }

    private static LineFinder VerticalLine(int shade, int height = 100, int lineHeight = 100)
    {
        byte[] picture = Pixels.Bgra(100, height, i =>
        {
            int tone;
            if (i % 100 == 50 && i / 100 < lineHeight)
            {
                tone = shade;
            }
            else
            {
                tone = Gap;
            }
            return Gray(tone);
        });
        return new LineFinder(picture, 100, height, 1f);
    }

    private static byte[] ButtonsPicture(bool transposed)
    {
        int width;
        if (transposed)
        {
            width = ButtonsHeight;
        }
        else
        {
            width = ButtonsWidth;
        }
        int height;
        if (transposed)
        {
            height = ButtonsWidth;
        }
        else
        {
            height = ButtonsHeight;
        }
        return Pixels.Bgra(width, height, i =>
        {
            int x = i % width;
            int y = i / width;
            int tone;
            if (transposed)
            {
                tone = ButtonShade(y, x);
            }
            else
            {
                tone = ButtonShade(x, y);
            }
            return Gray(tone);
        });
    }

    private static int ButtonShade(int x, int y)
    {
        foreach (Rect button in Buttons)
        {
            if (!InButton(x, y, button))
            {
                continue;
            }
            bool edge = !InButton(x - 1, y, button) || !InButton(x + 1, y, button) || !InButton(x, y - 1, button) || !InButton(x, y + 1, button);
            if (edge)
            {
                return Border;
            }
            else
            {
                return Inside;
            }
        }
        return Gap;
    }

    private static bool InButton(int x, int y, Rect button)
    {
        if (!button.Contains(P(x, y)))
        {
            return false;
        }
        int cx = Math.Clamp(x, button.Left + CornerRadius, button.Right - 1 - CornerRadius);
        int cy = Math.Clamp(y, button.Top + CornerRadius, button.Bottom - 1 - CornerRadius);
        return (x - cx) * (x - cx) + (y - cy) * (y - cy) <= CornerRadius * CornerRadius;
    }

    private static void Paint(byte[] picture, int width, int x, int y, int shade)
    {
        int i = (y * width + x) * 4;
        picture[i] = picture[i + 1] = picture[i + 2] = (byte)shade;
    }

    private static (int, int, int) Gray(int shade)
    {
        return (shade, shade, shade);
    }
}
