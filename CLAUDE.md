# CapChief

A Windows screen capture and annotation tool. C#, NativeAOT, no WPF or WinForms, drawn with raw Win32.

Four projects under `src/`: `CapChief.Platform` (the Windows plumbing), `CapChief` (the app), `CapChief.Lab` (a diagnostics exe, never shipped) and `CapChief.Tests` (xUnit). The Lab and the tests compile the app's sources.

## Deploying a change: `dotnet build` is not enough

`dotnet build` writes the IL build, which is not the exe that runs. The exe that runs is the NativeAOT one, which only `dotnet publish` refreshes:

```
src/CapChief/bin/Release/net10.0/win-x64/native/CapChief.exe
bin/CapChief.exe
```

`run.bat` does it right: it puts vswhere on PATH (the AOT publish fails without it), kills the running copies, publishes the app to `bin/` and launches it, then publishes the Lab to `bin/` too. The Lab compiles the app's sources, so a Lab that is not republished checks the old code. By hand:

```bash
export PATH="/c/Program Files (x86)/Microsoft Visual Studio/Installer:$PATH"
taskkill //IM CapChief.exe //F
taskkill //IM CapChief.Lab.exe //F
dotnet publish src/CapChief/CapChief.csproj -c Release -r win-x64 -o bin
dotnet publish src/CapChief.Lab/CapChief.Lab.csproj -c Release -r win-x64 -o bin
```

The app is single instance, so a rebuild never replaces a running copy. Kill it first.

## Verifying

```bash
dotnet test src/CapChief.Tests/CapChief.Tests.csproj -c Release
```

`bin/CapChief.Lab.exe` takes these flags:

| Flag | What it does |
|---|---|
| `--dumpui [dir]` | Walks the app through every state and writes a picture of each, plus `dumpui.txt`, to `dir` (default `dump\` beside the exe). The exit code is the number of failed checks |
| `--gifcheck [file]` | Films the real screen to check that the overlay stays out of a recording and that a recording holds what was shown. Writes `file` (default `gif.txt`) |
| `--bench [file]` | Times drags across the whole screen and writes `file` (default `bench.txt`) |
| `--selftest` | Writes one capture with marks to `CapChief_selftest.png` beside the exe. It checks nothing |
| `--size WxH` | Pretends the desktop is one monitor of that size |
| `--scale N` or `N%` | Pretends every monitor is at that scale |
| `--synthetic` | Uses the Lab's made-up screen in a normal run (the four modes above always do) |
| `--nohide` | Lets screen captures see the overlay (`--dumpui` and `--bench` always do) |

Add `--size 3840x2160 --scale 200%` to `--dumpui` to walk a simulated 4K desktop at 200%, without a high-DPI screen.

When the app or the Lab fails, look in `%LOCALAPPDATA%\CapChief\CapChief.log`.
