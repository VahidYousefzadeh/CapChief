namespace CapChief.Windowing;

/// <summary>
/// Runs a repeating timer and reports how far along each cycle is.
/// </summary>
public sealed class Ticker
{
    private const uint TickMs = 15;

    private readonly int _id;
    private readonly uint _cycleMs;
    private readonly int _cycles;

    private long _startedAt;

    public Ticker(int timerId, uint cycleMs, int cycles)
    {
        _id = timerId;
        _cycleMs = cycleMs;
        _cycles = cycles;
    }

    public bool IsRunning { get; private set; }

    public float Progress { get; private set; }

    public void Begin(Window on)
    {
        IsRunning = true;
        Progress = 0f;
        _startedAt = Environment.TickCount64;
        on.StartTimer(_id, TickMs);
    }

    public bool Advance(Window on)
    {
        long gone = Environment.TickCount64 - _startedAt;
        if (gone >= (long)_cycleMs * _cycles)
        {
            End(on);
            return false;
        }
        Progress = gone % _cycleMs / (float)_cycleMs;
        return true;
    }

    public void End(Window on)
    {
        on.StopTimer(_id);
        IsRunning = false;
    }
}
