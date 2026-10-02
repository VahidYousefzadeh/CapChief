namespace CapChief.Graphics;

/// <summary>
/// Draws a translucent dark tint over a rectangle to dim what lies beneath it.
/// </summary>
public sealed class DimTile
{
    private const int Side = 16;

    private readonly Surface _tile = new();
    private readonly byte _alpha;

    public DimTile(byte alpha)
    {
        _alpha = alpha;
    }

    public void Over(DeviceContext dst, Rect r)
    {
        if (r.IsEmpty || !Build())
        {
            return;
        }
        dst.AlphaBlend(r, _tile.DeviceContext, _tile.Bounds);
    }

    public void Free()
    {
        _tile.Free();
    }

    private unsafe bool Build()
    {
        if (_tile.IsOpen)
        {
            return true;
        }
        if (!_tile.OpenDib(Side, Side))
        {
            return false;
        }
        byte* p = (byte*)_tile.Bits;
        for (int i = 0; i < Side * Side; i++)
        {
            p[i * 4 + 3] = _alpha;
        }
        return true;
    }
}
