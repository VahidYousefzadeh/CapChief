using System.Numerics;

namespace CapChief.Media;

/// <summary>
/// Provides the constants and block codes of the GIF89a file format.
/// </summary>
public static class Gif89a
{
    public const string Signature = "GIF89a";

    public const int HeaderBytes = 13;

    public const byte Extension = 0x21;

    public const byte GraphicControlLabel = 0xF9;

    public const byte GraphicControlSize = 4;

    public const byte ApplicationLabel = 0xFF;

    public const byte ApplicationSize = 11;

    public const string NetscapeId = "NETSCAPE2.0";

    public const byte LoopBlockSize = 3;

    public const byte LoopBlockId = 1;

    public const byte ImageDescriptor = 0x2C;

    public const byte Trailer = 0x3B;

    public const byte BlockEnd = 0x00;

    public const byte TransparentFlag = 0x01;

    public const byte LeaveInPlace = 0x04;

    public const byte FramePacked = LeaveInPlace | TransparentFlag;

    public const byte NoLocalTable = 0x00;

    public const int GlobalTableFlag = 0x80;

    public const int LocalTableFlag = 0x80;

    public const int ColorResolution = 0x70;

    public const int MaxCodes = 4096;

    public const int MaxCodeBits = 12;

    public static int TableEntries(int packed)
    {
        return 1 << ((packed & 7) + 1);
    }

    public static int TableSizeBits(int entries)
    {
        return BitOperations.Log2((uint)entries) - 1;
    }
}
