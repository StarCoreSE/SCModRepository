@echo off
setlocal enabledelayedexpansion

:: Check if the script is running with administrative privileges
NET SESSION >NUL 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Please run this script as administrator!
    pause
    exit /b 1
)

set "targetDir=%APPDATA%\SpaceEngineers\Mods"
set "desktopDir=%USERPROFILE%\Desktop\SpaceEngineersModsBackup"

:: Creating desktop directory if it doesn't exist
if not exist "%desktopDir%" (
    mkdir "%desktopDir%"
)

:: Copying contents of mod folder to the desktop
robocopy "%targetDir%" "%desktopDir%" /E /MIR /NFL /NDL /NJH /NJS /nc /ns /np

:: Removing symbolic links from the mod folder
for /f "delims=" %%i in ('dir /AL /b "%targetDir%" 2^>nul') do (
    if exist "%targetDir%\%%i" (
        rmdir /s /q "%targetDir%\%%i"
        echo Removed symlink in "%targetDir%\%%i"
    )
)

pause
