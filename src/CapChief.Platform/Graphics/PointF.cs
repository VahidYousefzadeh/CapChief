using System.Runtime.InteropServices;

namespace CapChief.Graphics;

/// <summary>
/// Represents a point with single-precision floating-point X and Y coordinates.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct PointF(float X, float Y);
