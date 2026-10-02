namespace CapChief.Workspace;

/// <summary>
/// Stores the marks drawn on a screenshot in drawing order.
/// </summary>
public sealed class Marks
{
    private readonly List<Mark> _marks = new();

    public Marks() { }

    public Marks(IEnumerable<Mark> marks)
    {
        _marks.AddRange(marks);
    }

    public int Count
    {
        get
        {
            return _marks.Count;
        }
    }

    public Mark this[int i]
    {
        get
        {
            return _marks[i];
        }
    }

    public IReadOnlyList<Mark> All
    {
        get
        {
            return _marks;
        }
    }

    public int NextStepNumber
    {
        get
        {
            return StepNumberAt(_marks.Count);
        }
    }

    public bool HasIndex(int i)
    {
        return i >= 0 && i < _marks.Count;
    }

    public int StepNumberAt(int i)
    {
        int n = 1;
        for (int k = 0; k < i && k < _marks.Count; k++)
        {
            if (_marks[k] is StepMark)
            {
                n++;
            }
        }
        return n;
    }

    public int StepNumberOf(int i)
    {
        if (HasIndex(i) && _marks[i] is StepMark)
        {
            return StepNumberAt(i);
        }
        else
        {
            return 0;
        }
    }

    public int IndexAt(Point p, int band, int skip = -1)
    {
        for (int i = _marks.Count - 1; i >= 0; i--)
        {
            if (i != skip && _marks[i].IsHit(p, band))
            {
                return i;
            }
        }
        return -1;
    }

    public int TextIndexAt(Point p, int band)
    {
        for (int i = _marks.Count - 1; i >= 0; i--)
        {
            if (_marks[i] is TextMark && _marks[i].IsHit(p, band))
            {
                return i;
            }
        }
        return -1;
    }

    public void Insert(int i, Mark mark)
    {
        _marks.Insert(Math.Clamp(i, 0, _marks.Count), mark);
    }

    public void RemoveAt(int i)
    {
        if (HasIndex(i))
        {
            _marks.RemoveAt(i);
        }
    }

    public void Replace(int i, Mark mark)
    {
        if (HasIndex(i))
        {
            _marks[i] = mark;
        }
    }
}
