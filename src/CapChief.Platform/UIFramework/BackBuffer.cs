namespace CapChief.UIFramework;

/// <summary>
/// Represents an off-screen bitmap that a form draws into before displaying it.
/// </summary>
public sealed class BackBuffer
{
    private readonly Surface _surf = new();
    private readonly Sheet _sheet = new();
    private byte[] _base = [];

    public Sheet Sheet
    {
        get
        {
            return _sheet;
        }
    }

    public int Width
    {
        get
        {
            return _surf.Width;
        }
    }

    public int Height
    {
        get
        {
            return _surf.Height;
        }
    }

    public bool IsOpen
    {
        get
        {
            return _surf.IsOpen && _sheet.IsOpen;
        }
    }

    public Span<byte> Pixels
    {
        get
        {
            return _surf.Pixels;
        }
    }

    public void Resize(int w, int h)
    {
        if (w == _surf.Width && h == _surf.Height && _surf.IsOpen)
        {
            return;
        }
        Free();
        if (!_surf.OpenDib(w, h))
        {
            return;
        }

        _sheet.Open(_surf);
        _base = new byte[w * h * 4];
    }

    public void Free()
    {
        _sheet.Free();
        _surf.Free();
        _base = [];
    }

    public void Rebase()
    {
        if (!_surf.IsOpen)
        {
            return;
        }
        _sheet.Flush();
        _surf.Pixels.CopyTo(_base);
    }

    public void ResetToBase()
    {
        if (_surf.IsOpen)
        {
            _surf.WriteFrom(_base);
        }
    }

    public void Text(string s, Rect r, Color color, TextFont font)
    {
        if (!_surf.IsOpen || s.Length == 0)
        {
            return;
        }
        _sheet.Flush();
        _surf.DeviceContext.DrawText(s, r, color.ColorRef, font, Win32.DT_CENTER | Win32.DT_VCENTER | Win32.DT_SINGLELINE);
        RestoreAlpha(r);
    }

    public void CenteredText(string s, Rect r, Color color, TextFont font)
    {
        if (!_surf.IsOpen || s.Length == 0)
        {
            return;
        }
        _sheet.Flush();
        CapChief.Graphics.CenteredText.Draw(_surf.DeviceContext, r, s, font, color.ColorRef);
        RestoreAlpha(r);
    }

    public void Image(Surface image, Rect at)
    {
        if (!_surf.IsOpen || !image.IsOpen)
        {
            return;
        }
        _sheet.Flush();
        _surf.DeviceContext.Blit(at, image.DeviceContext, 0, 0);
        RestoreAlpha(at);
    }

    public void FadeToBase(Rect r, float keep)
    {
        if (!_surf.IsOpen)
        {
            return;
        }
        _sheet.Flush();
        Rect c = Rect.Intersect(r, _surf.Bounds);
        int k = (int)(keep * 256);
        Span<byte> px = _surf.Pixels;
        for (int y = c.Top; y < c.Bottom; y++)
        {
            int row = y * _surf.Width * 4;
            for (int i = row + c.Left * 4; i < row + c.Right * 4; i++)
            {
                px[i] = (byte)(_base[i] + ((px[i] - _base[i]) * k >> 8));
            }
        }
    }

    public void Flush()
    {
        _sheet.Flush();
    }

    public void Push(Window window, int x, int y)
    {
        if (_surf.IsOpen)
        {
            window.PushPixels(_surf, x, y);
        }
    }

    private void RestoreAlpha(Rect r)
    {
        Rect c = Rect.Intersect(r, _surf.Bounds);
        Span<byte> px = _surf.Pixels;
        for (int y = c.Top; y < c.Bottom; y++)
        {
            int row = y * _surf.Width * 4;
            for (int x = c.Left; x < c.Right; x++)
            {
                px[row + x * 4 + 3] = _base[row + x * 4 + 3];
            }
        }
    }
}
