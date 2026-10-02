using System.Runtime.InteropServices;

namespace CapChief.Windowing;

/// <summary>
/// Provides methods for asking the user where to save a file.
/// </summary>
public static class SaveFileDialog
{
    private const int MaxPath = 260;

    public static unsafe string? Ask(Window owner, string filter, string title, string ext, string suggested, string initialFolder)
    {
        var file = new char[MaxPath];
        suggested.CopyTo(0, file, 0, Math.Min(suggested.Length, MaxPath - 1));

        fixed (char* f = filter, t = title, e = ext, d = initialFolder, buf = file)
        {
            IntPtr dir = IntPtr.Zero;
            if (initialFolder.Length > 0)
            {
                dir = (IntPtr)d;
            }

            var ofn = new Win32.OPENFILENAME
            {
                lStructSize = (uint)Marshal.SizeOf<Win32.OPENFILENAME>(),
                hwndOwner = owner.Handle,
                lpstrFilter = (IntPtr)f,
                nFilterIndex = 1,
                lpstrFile = (IntPtr)buf,
                nMaxFile = MaxPath,
                lpstrInitialDir = dir,
                lpstrTitle = (IntPtr)t,
                lpstrDefExt = (IntPtr)e,
                Flags = Win32.OFN_OVERWRITEPROMPT | Win32.OFN_PATHMUSTEXIST | Win32.OFN_NOCHANGEDIR | Win32.OFN_EXPLORER,
            };

            if (!Win32.GetSaveFileNameW(ref ofn))
            {
                return null;
            }
            if (new string(buf) is { Length: > 0 } path)
            {
                return path;
            }
            else
            {
                return null;
            }
        }
    }
}
