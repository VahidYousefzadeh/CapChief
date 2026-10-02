namespace CapChief.Windowing;

/// <summary>
/// Blinks a caret on a window timer at the rate set in Windows, and reports whether it is in the
/// hidden half of a blink.
/// </summary>
public sealed class CaretBlink
{
    private const uint NeverBlinks = 0xFFFFFFFF;

    private readonly int _id;

    private bool _running;

    public CaretBlink(int timerId)
    {
        _id = timerId;
    }

    public bool IsHidden { get; private set; }

    public void Start(Window on)
    {
        IsHidden = false;
        if (_running)
        {
            return;
        }

        uint ms = Win32.GetCaretBlinkTime();
        if (ms is 0 or NeverBlinks)
        {
            return;
        }
        _running = true;
        on.StartTimer(_id, ms);
    }

    public void Toggle()
    {
        IsHidden = !IsHidden;
    }

    public void Stop(Window on)
    {
        IsHidden = false;
        if (!_running)
        {
            return;
        }
        _running = false;
        on.StopTimer(_id);
    }
}
