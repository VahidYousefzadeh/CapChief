namespace CapChief.Capturing;

/// <summary>
/// Represents the countdown before a timed capture: the area chosen and the seconds left before
/// the screen is frozen.
/// </summary>
public sealed class CountingDown
{
    public const int Seconds = 3;

    public const uint TickMs = 500;

    private const int TicksPerSecond = 1000 / (int)TickMs;

    private int _ticks;

    public int SecondsLeft
    {
        get
        {
            return (int)Math.Ceiling(_ticks / (double)TicksPerSecond);
        }
    }

    public bool ShowsBorder
    {
        get
        {
            return _ticks % TicksPerSecond == 0;
        }
    }

    public Crop Crop { get; private set; }

    public void Begin(Crop crop)
    {
        Crop = crop;
        _ticks = Seconds * TicksPerSecond;
    }

    public bool Tick()
    {
        return --_ticks > 0;
    }
}
