using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CapChief.OS;

/// <summary>
/// Finds the user interface elements inside a window with UI Automation, on a background thread.
/// </summary>
public sealed class ElementScan
{
    private const int MaxSiblings = 3000;

    private const int MaxFound = 3000;

    private const int MaxElements = 6000;

    private const int BudgetMs = 400;

    private const int DeadZone = 4;

    private readonly int _minSize;
    private readonly Uia _uia = new();
    private readonly object _gate = new();
    private readonly ManualResetEventSlim _wake = new(false);
    private readonly Dictionary<IntPtr, List<Rect>> _found = new();
    private Thread? _worker;
    private Action? _whenScanned;
    private volatile int _generation;
    private volatile bool _closing;
    private volatile bool _unavailable;
    private volatile bool _forgetFound;
    private ScanRequest? _request;
    private ScannedElements? _result;

    public ElementScan(int minSize)
    {
        _minSize = minSize;
    }

    public static bool IsSelectable(Rect r, Rect parent, int minSize)
    {
        return r.Width >= minSize && r.Height >= minSize && r != parent;
    }

    public void WhenScanned(Action post)
    {
        _whenScanned = post;
    }

    public void Request(IntPtr window, Point at)
    {
        if (_unavailable || window == IntPtr.Zero)
        {
            return;
        }
        lock (_gate)
        {
            _request = new ScanRequest(window, at);
            _generation++;
        }
        StartWorker();
        _wake.Set();
    }

    public ScannedElements? TakeResult()
    {
        lock (_gate)
        {
            ScannedElements? result = _result;
            _result = null;
            return result;
        }
    }

    public void Cancel()
    {
        lock (_gate)
        {
            _generation++;
            _request = null;
            _result = null;
            _forgetFound = true;
        }
        _wake.Set();
    }

    public void Close()
    {
        if (_worker is not { } worker)
        {
            return;
        }
        _closing = true;
        _generation++;
        _wake.Set();
        worker.Join(1000);
        _worker = null;
    }

    private void StartWorker()
    {
        if (_worker is not null)
        {
            return;
        }
        _worker = new Thread(Run) { IsBackground = true, Name = "elements" };
        _worker.Start();
    }

    private void Run()
    {
        if (!_uia.Start())
        {
            _unavailable = true;
            return;
        }

        while (!_closing)
        {
            _wake.Reset();
            if (_forgetFound)
            {
                _forgetFound = false;
                _found.Clear();
            }

            ScanRequest? request;
            int generation;
            lock (_gate)
            {
                request = _request;
                generation = _generation;
                _request = null;
            }

            if (request is not { } asked)
            {
                _wake.Wait();
                continue;
            }

            Rect innermost = Scan(asked.Window, asked.At, generation);
            lock (_gate)
            {
                if (generation != _generation)
                {
                    continue;
                }
                _result = new ScannedElements(asked.Window, [.. FoundIn(asked.Window)], innermost);
            }
            _whenScanned?.Invoke();
        }

        _found.Clear();
        _uia.Stop();
    }

    private List<Rect> FoundIn(IntPtr window)
    {
        if (!_found.TryGetValue(window, out List<Rect>? l))
        {
            _found[window] = l = [];
        }
        return l;
    }

    private Rect Scan(IntPtr window, Point at, int generation)
    {
        List<Rect> found = FoundIn(window);
        IntPtr root = _uia.FromHandle(window);

        if (root == IntPtr.Zero)
        {
            return default;
        }

        if (!Win32.GetWindowRect(window, out Rect clip))
        {
            clip = default;
        }

        var stack = new Stack<PendingElement>();
        stack.Push(new PendingElement(root, clip));
        var clock = Stopwatch.StartNew();
        int visited = 0;

        while (stack.Count > 0)
        {
            PendingElement pending = stack.Pop();
            visited += VisitChildren(pending, at, clip, found, stack, clock);
            Uia.Release(pending.Element);
            if (OutOfBudget(generation, clock, visited))
            {
                break;
            }
        }

        while (stack.Count > 0)
        {
            Uia.Release(stack.Pop().Element);
        }

        Rect innermost = Rect.SmallestContaining(CollectionsMarshal.AsSpan(found), at);
        if (innermost.IsEmpty)
        {
            return Rect.Around(at, DeadZone);
        }
        else
        {
            return innermost;
        }
    }

    private bool OutOfBudget(int generation, Stopwatch clock, int visited)
    {
        return generation != _generation || _closing || visited >= MaxElements || clock.ElapsedMilliseconds >= BudgetMs;
    }

    private int VisitChildren(PendingElement pending, Point at, Rect clip, List<Rect> found, Stack<PendingElement> stack, Stopwatch clock)
    {
        int visited = 0;
        IntPtr child = _uia.FirstChild(pending.Element);
        while (child != IntPtr.Zero)
        {
            IntPtr next = _uia.NextSibling(child);
            Rect r = ClippedBounds(child, clip, pending.Parent, found);

            if (r.Contains(at))
            {
                stack.Push(new PendingElement(child, r));
            }
            else
            {
                Uia.Release(child);
            }

            visited++;
            if (visited >= MaxSiblings || clock.ElapsedMilliseconds >= BudgetMs)
            {
                Uia.Release(next);
                break;
            }
            child = next;
        }
        return visited;
    }

    private Rect ClippedBounds(IntPtr element, Rect clip, Rect parent, List<Rect> found)
    {
        if (!_uia.BoundsOf(element, out Rect raw))
        {
            return default;
        }
        Rect r = Rect.Intersect(raw, clip);
        if (IsSelectable(r, parent, _minSize))
        {
            Remember(found, r);
        }
        return r;
    }

    private static void Remember(List<Rect> found, Rect r)
    {
        if (found.Count < MaxFound && !found.Contains(r))
        {
            found.Add(r);
        }
    }

    /// <summary>
    /// Represents a window to scan and the point, in screen coordinates, to walk toward.
    /// </summary>
    private readonly record struct ScanRequest(IntPtr Window, Point At);

    /// <summary>
    /// Represents an element still to be visited and the bounds of its parent.
    /// </summary>
    private readonly record struct PendingElement(IntPtr Element, Rect Parent);
}

/// <summary>
/// Represents what one scan found in a window, in screen coordinates: every element seen in that
/// window so far, and the innermost one at the point the scan walked toward.
/// </summary>
public sealed record ScannedElements(IntPtr Window, IReadOnlyList<Rect> Elements, Rect Innermost);
