namespace CapChief.OS;

/// <summary>
/// Runs a callback once after a specified number of milliseconds.
/// </summary>
public sealed class Alarm
{
    private readonly Action _then;
    private readonly Win32.TimerProc _tick;

    public Alarm(Action then)
    {
        _then = then;
        _tick = OnTimer;
    }

    public void In(uint ms)
    {
        Win32.SetTimer(IntPtr.Zero, UIntPtr.Zero, ms, _tick);
    }

    private void OnTimer(IntPtr h, uint m, UIntPtr id, uint time)
    {
        Win32.KillTimer(IntPtr.Zero, id);
        _then();
    }
}
