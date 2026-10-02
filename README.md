# CapChief

A screen capture and annotation tool for Windows. It is one small native exe: C# compiled with
NativeAOT, drawn with raw Win32 and GDI+, with no WPF, WinForms or runtime to install.

## Features

- **Capture** a region of the screen, right away or after a countdown.
- **Annotate** with arrows, rectangles, ellipses, a pen, text, numbered steps, and mosaic or
  blackout redaction.
- **Record a GIF** of a region, then review it, trim the start and end, and change the speed
  before saving.
- **Copy** to the clipboard or **save** as PNG or GIF, and keep the last 10 captures in a history
  until CapChief quits.

## Shortcuts

| Global          | Action             |
|-----------------|--------------------|
| `Ctrl+Alt+C`    | Capture            |
| `Ctrl+Alt+T`    | Timed capture (press again to cancel the countdown) |
| `Ctrl+Alt+G`    | Start or stop a GIF recording |

While choosing the region:

| Key             | Action                          |
|-----------------|---------------------------------|
| Click / drag    | Take the window or screen under the pointer / draw a region |
| `Ctrl` (held)   | Take the control under the pointer instead of its window (not for timed captures or GIFs) |
| `Enter`         | Take the region shown           |
| `Tab` while holding the button | Type an exact width and height: `Tab` switches between them, `Backspace` erases |
| `Esc` / right-click | Cancel (if you typed a size, the first press only clears it) |

While annotating:

| Key             | Action                          |
|-----------------|---------------------------------|
| `A` `R` `E` `P` `T` `N` | Arrow, rectangle, ellipse, pen, text, numbered step |
| `M` `B`         | Mosaic and blackout redaction   |
| `1`-`6`         | Pick a color (`Ctrl+Left`/`Ctrl+Right` to step through them) |
| Arrow keys      | Nudge the selected mark, or the region if no mark is selected (`Shift` for a bigger step) |
| `Delete` / `Backspace` | Delete the selected mark |
| `Ctrl+Enter` / `Esc` while typing | Finish the text mark |
| `Ctrl+C` / `Enter` | Copy                         |
| `Ctrl+S`        | Save as PNG                     |
| `Ctrl+H`        | Save to history                 |
| `Ctrl+Z` / `Ctrl+Y` | Undo / redo (`Ctrl+Shift+Z` redoes too) |
| `Esc`           | Deselect the selected mark, or close if none is selected |

During a countdown or a recording, `Esc` cancels it, and a cancelled recording is not kept.

While reviewing a GIF:

| Key             | Action                          |
|-----------------|---------------------------------|
| `Space`         | Play or pause                   |
| `Left` / `Right` | Step back or forward a frame (`Shift` for a bigger step) |
| `Home` / `End`  | Jump to the first or last frame kept |
| `I` / `O`       | Trim the start or the end at the current frame |
| `Ctrl+Shift+X`  | Restore the trimmed frames      |
| `S`             | Change the speed: normal, double, half |
| `Ctrl+S`        | Save as GIF                     |
| `Ctrl+H`        | Save to history                 |
| `Ctrl+Z` / `Ctrl+Y` | Undo / redo (`Ctrl+Shift+Z` redoes too) |
| `Esc`           | Close                           |

## Layout

| Project              | What it is                                                     |
|----------------------|----------------------------------------------------------------|
| `CapChief.Platform`  | The Windows plumbing: interop, geometry, GDI/GDI+ drawing, PNG and GIF encoding, screens, windows and a small UI framework |
| `CapChief`           | The app                                                        |
| `CapChief.Tests`     | xUnit tests                                                    |
| `CapChief.Lab`       | A diagnostics exe, never shipped. `CapChief.Lab.exe --dumpui [dir]` walks the app through every state and writes a picture of each |

## License

[MIT](LICENSE)
