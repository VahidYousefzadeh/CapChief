using System.Runtime.InteropServices;

namespace CapChief.Geometry;

/// <summary>
/// Represents the width and height of a rectangular area in pixels.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct Size(int Width, int Height);
