@echo off
setlocal
set "PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer;%PATH%"
set DOTNET_CLI_TELEMETRY_OPTOUT=1

rem Builds bin\CapChief.msix, the Microsoft Store package, from the app and the files in package\.
set "STAGE=%~dp0obj\package"
set "SDK="
for /d %%v in ("%ProgramFiles(x86)%\Windows Kits\10\bin\10.*") do (
    if exist "%%v\x64\makeappx.exe" set "SDK=%%v\x64"
)
if not defined SDK (
    echo Windows SDK not found!
    pause
    exit /b 1
)

dotnet publish "%~dp0src\CapChief\CapChief.csproj" -c Release -r win-x64 -o "%~dp0obj\publish"
if errorlevel 1 (
    echo Build failed!
    pause
    exit /b 1
)

if exist "%STAGE%" rmdir /s /q "%STAGE%"
mkdir "%STAGE%\Assets"
copy "%~dp0obj\publish\CapChief.exe" "%STAGE%" >nul
copy "%~dp0package\AppxManifest.xml" "%STAGE%" >nul
copy "%~dp0package\Assets\*.png" "%STAGE%\Assets" >nul

rem The resource index lets Windows pick the logo size that fits each place it shows the app.
"%SDK%\makepri.exe" new /pr "%STAGE%" /cf "%~dp0package\priconfig.xml" /mn "%STAGE%\AppxManifest.xml" /of "%STAGE%\resources.pri" /o
if errorlevel 1 (
    echo Resource index failed!
    pause
    exit /b 1
)

"%SDK%\makeappx.exe" pack /d "%STAGE%" /p "%~dp0bin\CapChief.msix" /o
if errorlevel 1 (
    echo Packing failed!
    pause
    exit /b 1
)

echo Wrote bin\CapChief.msix
pause
