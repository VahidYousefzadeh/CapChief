namespace CapChief.Shell;

/// <summary>
/// Provides methods for formatting recording times, speeds and file sizes for display.
/// </summary>
public static class ClipText
{
    private const string Gap = "   ·   ";

    public static string Of(Clip clip, long? bytes)
    {
        string sizeText;
        if (bytes is { } known)
        {
            sizeText = Size(known);
        }
        else
        {
            sizeText = "…";
        }
        return $"{Clock(clip.MsOf(clip.Playhead))} / {Clock(clip.LengthMs)}{Gap}{sizeText}";
    }

    public static string SpeedOf(Clip clip)
    {
        switch (clip.Speed)
        {
            case ClipSpeed.Double:
                return "2x";
            case ClipSpeed.Half:
                return "0.5x";
            default:
                return "1x";
        }
    }

    public static string RecordingTag(int seconds, string stop)
    {
        string time = $"REC {Clock(seconds * 1000L)} / {Clock(Recording.MaxSeconds * 1000L)}";
        if (stop.Length > 0)
        {
            return time + Gap + stop;
        }
        else
        {
            return time;
        }
    }

    private static string Clock(long ms)
    {
        long s = (ms + 500) / 1000;
        return $"{s / 60}:{s % 60:D2}";
    }

    private static string Size(long bytes)
    {
        if (bytes >= 1L << 20)
        {
            return $"{bytes / (double)(1 << 20):0.0} MB";
        }
        if (bytes >= 1L << 10)
        {
            return $"{(bytes + 512) >> 10} KB";
        }
        return $"{bytes} B";
    }
}
