using System.Diagnostics;
using System.Reflection;
using CapChief.Interop;

namespace CapChief.Lab;

/// <summary>
/// Drives the running app the way a user would, by sending mouse and key messages to its overlay,
/// and reads back what reached the screen.
/// </summary>
public sealed class Driver
{
    private const int WheelNotch = 120;

    public Driver(App app, Desktop desk)
    {
        App = app;
        Desk = desk;
    }

    public App App { get; }

    public Desktop Desk { get; }

    private IntPtr Overlay
    {
        get
        {
            return App.Overlay.Window.Handle;
        }
    }

    public static unsafe void ScaleForms(App app, float scale)
    {
        uint dpi = (uint)MathF.Round(scale * Win32.USER_DEFAULT_SCREEN_DPI);
        ToolbarForm toolbar = app.Shell.Toolbar;
        FieldInfo tip = typeof(ToolbarForm).GetField("_tip", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Form[] forms = [toolbar, (Form)tip.GetValue(toolbar)!, app.Shell.HistoryForm, app.Shell.ToolPicker];
        foreach (Form form in forms)
        {
            Rect suggested = default;
            Win32.SendMessageW(form.Window.Handle, Win32.WM_DPICHANGED, (IntPtr)(dpi << 16 | dpi), (IntPtr)(&suggested));
        }
    }

    public void Press(int x, int y, bool shift = false)
    {
        int shiftKey;
        if (shift)
        {
            shiftKey = Win32.MK_SHIFT;
        }
        else
        {
            shiftKey = 0;
        }
        Send(Win32.WM_LBUTTONDOWN, x, y, 1 | shiftKey);
    }

    public void Move(int x, int y, bool held = true)
    {
        int keys;
        if (held)
        {
            keys = 1;
        }
        else
        {
            keys = 0;
        }
        Send(Win32.WM_MOUSEMOVE, x, y, keys);
    }

    public void Release(int x, int y)
    {
        Send(Win32.WM_LBUTTONUP, x, y, 0);
    }

    public void Drag(int x1, int y1, int x2, int y2, int steps = 8)
    {
        Press(x1, y1);
        for (int i = 1; i <= steps; i++)
        {
            Move(x1 + (x2 - x1) * i / steps, y1 + (y2 - y1) * i / steps);
        }
        Release(x2, y2);
    }

    public void Click(int x, int y)
    {
        Press(x, y);
        Release(x, y);
    }

    public void RightClick(int x, int y)
    {
        Send(Win32.WM_RBUTTONUP, x, y, 0);
    }

    public void Wheel(int notches, bool ctrl)
    {
        Turn(Overlay, notches, ctrl);
    }

    public void WheelInTextBox(int notches, bool ctrl)
    {
        Turn(Win32.FindWindowExW(Overlay, IntPtr.Zero, "Edit", null), notches, ctrl);
    }

    public void DoubleClick(int x, int y)
    {
        Click(x, y);
        Send(Win32.WM_LBUTTONDBLCLK, x, y, 1);
        Release(x, y);
    }

    public void Key(int virtualKey)
    {
        Win32.SendMessageW(Overlay, Win32.WM_KEYDOWN, (IntPtr)virtualKey, IntPtr.Zero);
    }

    public void Escape()
    {
        App.Commands.RunKey(new Chord(CapChief.Input.Key.Escape, false, false));
    }

    public void KeyInTextBox(int virtualKey)
    {
        IntPtr edit = Win32.FindWindowExW(Overlay, IntPtr.Zero, "Edit", null);
        Win32.SendMessageW(edit, Win32.WM_KEYDOWN, (IntPtr)virtualKey, IntPtr.Zero);
    }

    public void Type(string text)
    {
        IntPtr edit = Win32.FindWindowExW(Overlay, IntPtr.Zero, "Edit", null);
        foreach (char c in text)
        {
            Win32.SendMessageW(edit, Win32.WM_CHAR, (IntPtr)c, IntPtr.Zero);
        }
    }

    public void Pick(MarkKind kind)
    {
        App.Tools.PickTool(kind);
    }

    public void Draw(MarkKind kind, int x1, int y1, int x2, int y2)
    {
        Pick(kind);
        Drag(x1, y1, x2, y2);
    }

    public Point PointOn(int index)
    {
        Marks marks = App.Capture!.Marks;
        foreach (Point p in Outline(marks[index]))
        {
            if (marks.IndexAt(p, 8) == index && App.Workflow.CursorAt(p) == Cursor.SizeAll)
            {
                return p;
            }
        }
        throw new InvalidOperationException($"mark {index} has no point to take hold of it by");
    }

    private static IEnumerable<Point> Outline(Mark mark)
    {
        Rect b = mark.Bounds;
        if (mark is PenMark { Path: { } path })
        {
            for (int i = path.Count / 2; i < path.Count; i++)
            {
                yield return path[i];
            }
            yield break;
        }
        for (int k = 0; k < 360; k += 7)
        {
            double t = k * Math.PI / 180;
            if (mark is RectangleMark)
            {
                yield return new Point(b.Left + (int)(b.Width * (0.3 + 0.4 * (k / 360.0))), b.Top);
            }
            else
            {
                yield return new Point(b.Center.X + (int)(b.Width / 2 * Math.Cos(t)), b.Center.Y + (int)(b.Height / 2 * Math.Sin(t)));
            }
        }
    }

    public void ClickThumbnail(int index)
    {
        HistoryForm form = App.Shell.HistoryForm;
        FieldInfo field = typeof(HistoryForm).GetField("_thumbnails", BindingFlags.NonPublic | BindingFlags.Instance)!;
        ClickOn(form, ((ThumbnailButton[])field.GetValue(form)!)[index].Bounds.Center);
    }

    public void ClickChoice(MarkKind kind)
    {
        ToolPickerForm form = App.Shell.ToolPicker;
        FieldInfo field = typeof(ToolPickerForm).GetField("_toolChoices", BindingFlags.NonPublic | BindingFlags.Instance)!;
        ClickOn(form, ((Dictionary<MarkKind, ChoiceButton>)field.GetValue(form)!)[kind].Bounds.Center);
    }

    public void ClickSwatch(int index)
    {
        ToolPickerForm form = App.Shell.ToolPicker;
        FieldInfo field = typeof(ToolPickerForm).GetField("_colorChoices", BindingFlags.NonPublic | BindingFlags.Instance)!;
        ClickOn(form, ((ChoiceButton[])field.GetValue(form)!)[index].Bounds.Center);
    }

    public bool ClickActivates(Form form)
    {
        return Win32.SendMessageW(form.Window.Handle, Win32.WM_MOUSEACTIVATE, Overlay, Win32.LParamOf(Win32.HTCLIENT, (int)Win32.WM_LBUTTONDOWN)) != (IntPtr)Win32.MA_NOACTIVATE;
    }

    public void ClickToolbarButton(Command command)
    {
        ToolbarForm toolbar = App.Shell.Toolbar;
        ClickOn(toolbar, toolbar.Window.ToClient(toolbar.ColumnOnScreen(command).Center));
    }

    private static void ClickOn(Form form, Point at)
    {
        Win32.SendMessageW(form.Window.Handle, Win32.WM_LBUTTONDOWN, (IntPtr)1, Win32.LParamOf(at.X, at.Y));
        Win32.SendMessageW(form.Window.Handle, Win32.WM_LBUTTONUP, IntPtr.Zero, Win32.LParamOf(at.X, at.Y));
    }

    private void Send(uint message, int x, int y, int keys)
    {
        Win32.SendMessageW(Overlay, message, (IntPtr)keys, Win32.LParamOf(x, y));
    }

    private static void Turn(IntPtr window, int notches, bool ctrl)
    {
        int keys;
        if (ctrl)
        {
            keys = Win32.MK_CONTROL;
        }
        else
        {
            keys = 0;
        }
        Win32.SendMessageW(window, Win32.WM_MOUSEWHEEL, (IntPtr)(notches * WheelNotch << 16 | keys), IntPtr.Zero);
    }

    public byte[] Screen(Rect area)
    {
        App.Overlay.Window.PaintNow();
        Win32.DwmFlush();
        using var s = new Surface();
        if (!s.OpenDib(area.Width, area.Height))
        {
            return [];
        }
        using (var screen = ScreenDc.Borrow())
        {
            Win32.BitBlt(s.Dc, 0, 0, area.Width, area.Height, screen.Dc, area.Left + Desk.Origin.X, area.Top + Desk.Origin.Y, Win32.SRCCOPY);
        }
        DeviceContext.Flush();
        return s.Pixels.ToArray();
    }

    public byte[] OverlayPixels(Rect area)
    {
        IntPtr window = App.Overlay.Window.Handle;
        App.Overlay.Window.PaintNow();
        Win32.DwmFlush();
        using var s = new Surface();
        if (!s.OpenDib(area.Width, area.Height))
        {
            return [];
        }
        IntPtr dc = Win32.GetDC(window);
        Win32.BitBlt(s.Dc, 0, 0, area.Width, area.Height, dc, area.Left, area.Top, Win32.SRCCOPY);
        Win32.ReleaseDC(window, dc);
        DeviceContext.Flush();
        return s.Pixels.ToArray();
    }

    public Repaint RepaintDifference(Rect area)
    {
        byte[] before = OverlayPixels(area);
        App.Overlay.InvalidateAll();
        byte[] after = OverlayPixels(area);
        int differ = 0;
        Rect where = default;
        for (int i = 0; i + 2 < before.Length && i + 2 < after.Length; i += 4)
        {
            if (before[i] == after[i] && before[i + 1] == after[i + 1] && before[i + 2] == after[i + 2])
            {
                continue;
            }
            differ++;
            int x = area.Left + i / 4 % area.Width;
            int y = area.Top + i / 4 / area.Width;
            where = Rect.UnionIgnoringEmpty(where, new Rect { Left = x, Top = y, Right = x + 1, Bottom = y + 1 });
        }
        return new(differ, where, before, after);
    }

    public AlphaSurface DragImage()
    {
        FieldInfo image = typeof(Overlay).GetField("_dragImage", BindingFlags.NonPublic | BindingFlags.Instance)!;
        FieldInfo pixels = typeof(MarkDragImage).GetField("_pixels", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (AlphaSurface)pixels.GetValue(image.GetValue(App.Overlay))!;
    }

    public Timing TimeMoves(Func<int, Point> path, int steps)
    {
        var times = new List<double>(steps);
        var clock = new Stopwatch();
        for (int i = 1; i <= steps; i++)
        {
            Point p = path(i);
            clock.Restart();
            Move(p.X, p.Y);
            App.Overlay.Window.PaintNow();
            DeviceContext.Flush();
            times.Add(clock.Elapsed.TotalMilliseconds);
        }
        times.Sort();
        return new(times.Average(), times[times.Count / 2], times[(int)(times.Count * 0.95)], times[^1]);
    }
}

/// <summary>
/// Represents how the overlay repainted bit by bit compares with the same overlay repainted in full:
/// how many pixels differ, the area they lie in, and both pictures.
/// </summary>
public sealed record Repaint(int Differ, Rect Where, byte[] Before, byte[] After);

/// <summary>
/// Represents how long a run of pointer moves took to reach the screen, in milliseconds per move.
/// </summary>
public readonly record struct Timing(double Mean, double Median, double P95, double Max)
{
    public override string ToString()
    {
        return $"mean {Mean,6:0.00} ms, median {Median,6:0.00}, p95 {P95,6:0.00}, max {Max,6:0.00}";
    }
}

/// <summary>
/// Represents the outcome of a diagnostic run: each check it made, and notes to read beside them.
/// </summary>
public sealed class Report
{
    private readonly List<string> _lines = new();
    private readonly string _name;
    private int _ran, _failed;

    public Report(string name)
    {
        _name = name;
    }

    public void Check(bool ok, string what)
    {
        _ran++;
        if (!ok)
        {
            _failed++;
        }
        string status;
        if (ok)
        {
            status = "  ok  ";
        }
        else
        {
            status = "  !!  ";
        }
        _lines.Add(status + what);
    }

    public void Note(string line)
    {
        _lines.Add(line);
    }

    public int Write(string path)
    {
        string summary;
        if (_failed == 0)
        {
            summary = $"{_name}: PASS, {_ran} checks";
        }
        else
        {
            summary = $"{_name}: FAIL, {_failed} of {_ran} checks";
        }
        _lines.Insert(0, summary);
        File.WriteAllLines(path, _lines);
        return _failed;
    }
}
