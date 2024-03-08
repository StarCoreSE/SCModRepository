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

:: Copying non-symbolic link folders to the desktop
for /d %%i in ("%targetDir%\*") do (
    set "folderName=%%~nxi"
    fsutil reparsepoint query "%%i" > nul 2>&1
    if errorlevel 1 (
        robocopy "%%i" "%desktopDir%\!folderName!" /E /NFL /NDL /NJH /NJS /nc /ns /np
    )
)

:: Removing symbolic links from the mod folder
for /d %%i in ("%targetDir%\*") do (
    set "folderName=%%~nxi"
    fsutil reparsepoint query "%%i" > nul 2>&1
    if not errorlevel 1 (
        rmdir /s /q "%%i"
        echo Removed symlink in "%%i"
    )
)

pause
