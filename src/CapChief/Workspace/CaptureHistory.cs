namespace CapChief.Workspace;

/// <summary>
/// Represents the captures the user saved to open again later, newest first. An entry shares its
/// frozen screen and its clip's file with the capture it was saved from and with every capture
/// opened from it, so the history decides when they are freed, even for the open capture.
/// </summary>
public sealed class CaptureHistory
{
    public const int MaxEntries = 10;

    private readonly List<Capture> _entries = new();

    public event Action? Changed;

    public IReadOnlyList<Capture> Entries
    {
        get
        {
            return _entries;
        }
    }

    public Capture? EntryOf(Capture capture)
    {
        return _entries.Find(entry => entry.Picture == capture.Picture);
    }

    public bool IsUpToDate(Capture capture)
    {
        return EntryOf(capture) is { } saved && saved.Crop == capture.Crop && saved.Marks.All.SequenceEqual(capture.Marks.All) && EditedAlike(saved.Clip, capture.Clip);
    }

    public void Save(Capture capture)
    {
        if (EntryOf(capture) is { } saved)
        {
            _entries.Remove(saved);
        }
        _entries.Insert(0, capture.Copy());
        while (_entries.Count > MaxEntries)
        {
            _entries[^1].Free();
            _entries.RemoveAt(_entries.Count - 1);
        }
        Changed?.Invoke();
    }

    public void FreeUnlessSaved(Capture capture)
    {
        if (EntryOf(capture) is null)
        {
            capture.Free();
        }
    }

    public void Delete(Capture capture)
    {
        if (EntryOf(capture) is not { } saved)
        {
            capture.Free();
            return;
        }
        _entries.Remove(saved);
        saved.Free();
        Changed?.Invoke();
    }

    public void Clear()
    {
        foreach (Capture entry in _entries)
        {
            entry.Free();
        }
        _entries.Clear();
        Changed?.Invoke();
    }

    private static bool EditedAlike(Clip? saved, Clip? open)
    {
        if (saved is null)
        {
            return open is null;
        }
        else
        {
            return open is not null && saved.HasSameEdits(open);
        }
    }
}
