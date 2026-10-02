using System.Globalization;

namespace CapChief.Lab;

/// <summary>
/// Contains the entry point of the diagnostic build, which runs the app over a synthetic screen and
/// the checks and pictures its flags ask for.
/// </summary>
internal static class Program
{
    private static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log.Fatal(e.ExceptionObject);

        var flags = new Flags(args);
        Desktop.MakeProcessDpiAware();
        Desktop desk = Desktop.Measure();
        if (flags.Size is { } size)
        {
            desk = new Desktop(size, size, [size], flags.Scale);
        }
        else if (flags.Scale > 0f)
        {
            desk = new Desktop(desk.ScreenBounds, desk.ScreenPrimary, desk.ScreenMonitors, flags.Scale);
        }
        var screen = new SyntheticScreen(desk) { On = flags.Synthetic || flags.Diagnostic };
        var app = new App(desk, screen, excludedFromCapture: !flags.Capturable);

        if (!flags.Diagnostic && !SingleInstance.Claim(Product.MutexName))
        {
            SingleInstance.HandOver(Overlay.ClassName, AppMessages.ShowToolbar);
            return;
        }

        app.Start();
        if (flags.Scale > 0f)
        {
            Driver.ScaleForms(app, flags.Scale);
        }
        if (!flags.Diagnostic)
        {
            app.Shell.ShowTray();
        }

        int failures = 0;
        var run = new Alarm(() =>
        {
            try
            {
                failures = Run(flags, app, desk, screen);
            }
            catch (Exception e)
            {
                failures = 1;
                Log.Fatal(e);
            }
            finally
            {
                app.Shell.Quit();
            }
        });
        if (flags.Diagnostic)
        {
            run.In(100);
        }

        WindowLoop.Pump();
        app.Shutdown();
        screen.Free();
        if (flags.Diagnostic)
        {
            Environment.Exit(failures);
        }
    }

    private static int Run(Flags flags, App app, Desktop desk, SyntheticScreen screen)
    {
        var driver = new Driver(app, desk);
        if (flags.DumpDir is { } dir)
        {
            return new UiDump(driver, dir).Run();
        }
        if (flags.GifReport is { } gif)
        {
            return new GifCheck(driver, screen).Run(gif);
        }
        if (flags.BenchReport is { } bench)
        {
            return new Bench(driver).Run(bench);
        }
        if (flags.SelfTest)
        {
            return SelfTest.Run(app, screen);
        }
        return 0;
    }
}

/// <summary>
/// Represents the flags the diagnostic build was started with.
/// </summary>
public sealed class Flags
{
    private readonly string[] _args;

    public Flags(string[] args)
    {
        _args = args;
    }

    public string? DumpDir
    {
        get
        {
            return After("--dumpui", Beside("dump"));
        }
    }

    public string? GifReport
    {
        get
        {
            return After("--gifcheck", Beside("gif.txt"));
        }
    }

    public string? BenchReport
    {
        get
        {
            return After("--bench", Beside("bench.txt"));
        }
    }

    public bool SelfTest
    {
        get
        {
            return Has("--selftest");
        }
    }

    public bool Capturable
    {
        get
        {
            return Has("--nohide") || DumpDir is not null || BenchReport is not null;
        }
    }

    public bool Synthetic
    {
        get
        {
            return Has("--synthetic");
        }
    }

    public bool Diagnostic
    {
        get
        {
            return DumpDir is not null || GifReport is not null || BenchReport is not null || SelfTest;
        }
    }

    public float Scale
    {
        get
        {
            string? scale = After("--scale", "");
            if (scale is null)
            {
                return 0f;
            }
            bool percent = scale.EndsWith('%');
            string digits;
            if (percent)
            {
                digits = scale[..^1];
            }
            else
            {
                digits = scale;
            }
            if (float.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out float s) && s > 0f)
            {
                if (percent)
                {
                    return s / 100f;
                }
                else
                {
                    return s;
                }
            }
            else
            {
                return 0f;
            }
        }
    }

    public Rect? Size
    {
        get
        {
            string? size = After("--size", "");
            if (size is null)
            {
                return null;
            }
            string[] parts = size.Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
            {
                return new Rect { Right = w, Bottom = h };
            }
            else
            {
                return null;
            }
        }
    }

    private bool Has(string name)
    {
        return Array.IndexOf(_args, name) >= 0;
    }

    private static string Beside(string file)
    {
        return Path.Combine(AppContext.BaseDirectory, file);
    }

    private string? After(string name, string fallback)
    {
        int i = Array.IndexOf(_args, name);
        if (i < 0)
        {
            return null;
        }
        if (i + 1 < _args.Length && !_args[i + 1].StartsWith("--"))
        {
            return _args[i + 1];
        }
        else
        {
            return fallback;
        }
    }
}
