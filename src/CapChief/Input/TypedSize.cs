namespace CapChief.Input;

/// <summary>
/// Represents the width and height the user types while selecting, and which of the two the next
/// digit goes into; a side left at zero still follows the pointer.
/// </summary>
public readonly record struct TypedSize(int Width, int Height, bool HeightFocused)
{
    private const int Max = 9999;

    public bool HasDigits
    {
        get
        {
            return Width > 0 || Height > 0;
        }
    }

    public bool IsFocusedFieldEmpty
    {
        get
        {
            return Focused == 0;
        }
    }

    private int Focused
    {
        get
        {
            if (HeightFocused)
            {
                return Height;
            }
            else
            {
                return Width;
            }
        }
    }

    public TypedSize FocusOtherField()
    {
        return this with { HeightFocused = !HeightFocused };
    }

    public TypedSize AppendDigit(int d)
    {
        int v = Focused * 10 + d;
        if (v == 0 || v > Max)
        {
            return this;
        }
        else
        {
            return WithFocused(v);
        }
    }

    public TypedSize RemoveLastDigit()
    {
        return WithFocused(Focused / 10);
    }

    private TypedSize WithFocused(int v)
    {
        if (HeightFocused)
        {
            return this with { Height = v };
        }
        else
        {
            return this with { Width = v };
        }
    }
}
