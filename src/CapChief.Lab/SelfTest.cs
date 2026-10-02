namespace CapChief.Lab;

/// <summary>
/// Provides the self test: a capture of the synthetic screen with one mark of most kinds, written
/// the way Save writes it, to a PNG file beside the exe.
/// </summary>
public static class SelfTest
{
    public static int Run(App app, SyntheticScreen screen)
    {
        Size size = app.Desktop.Size;
        using var picture = new Surface();
        if (!picture.OpenCompatible(size.Width, size.Height))
        {
            return 1;
        }
        screen.Capture(picture);

        if (!Crop.TryCreate(new Rect { Right = Math.Min(200, size.Width), Bottom = Math.Min(120, size.Height) }, out Crop crop))
        {
            return 1;
        }
        var capture = new Capture(picture, crop);
        Mark[] marks =
        [
            new ArrowMark { Color = Tools.ColorAt(0), A = P(10, 10), B = P(90, 60) },
            new RectangleMark { Color = Tools.ColorAt(1), A = P(20, 20), B = P(120, 80) },
            new MosaicMark { A = P(100, 30), B = P(180, 100) },
            new BlackoutMark { Color = Tools.ColorAt(4), A = P(20, 92), B = P(110, 112) },
            new TextMark
            {
                Color = Tools.ColorAt(5),
                A = P(50, 50),
                B = P(150, 90),
                Text = "Hello",
            },
            new StepMark { Color = Tools.ColorAt(0), A = P(150, 4), B = P(178, 32) },
        ];
        var document = new Document();
        document.Open(capture);
        foreach (Mark mark in marks)
        {
            document.AddMark(mark);
        }

        using var image = new Surface();
        string path = Path.Combine(AppContext.BaseDirectory, $"{Product.FilePrefix}selftest.png");
        if (capture.Render(image, app.MarkPainter) && Export.ToFile(image, path))
        {
            return 0;
        }
        else
        {
            return 1;
        }
    }

    private static Point P(int x, int y)
    {
        return new(x, y);
    }
}
