@echo off
setlocal
set "PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer;%PATH%"
set DOTNET_CLI_TELEMETRY_OPTOUT=1

taskkill /IM CapChief.exe /F >nul 2>&1
taskkill /IM CapChief.Lab.exe /F >nul 2>&1

dotnet publish "%~dp0src\CapChief\CapChief.csproj" -c Release -r win-x64 -o "%~dp0bin"
if errorlevel 1 (
    echo Build failed!
    pause
    exit /b 1
)

start "" "%~dp0bin\CapChief.exe"

rem The Lab compiles the app's sources, so republish it or it keeps checking the old code.
dotnet publish "%~dp0src\CapChief.Lab\CapChief.Lab.csproj" -c Release -r win-x64 -o "%~dp0bin"
if errorlevel 1 (
    echo Lab build failed!
    pause
    exit /b 1
)
