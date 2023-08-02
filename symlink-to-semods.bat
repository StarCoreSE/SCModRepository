@echo off
setlocal enabledelayedexpansion

:: Check if the script is running with administrative privileges
NET SESSION >NUL 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Please run this script as administrator!
    pause
    exit /b 1
)

set "sourceDir=%~dp0"
set "targetDir=%APPDATA%\SpaceEngineers\Mods"

for /d %%i in ("%sourceDir%*") do (
    if exist "%%i\metadata.mod" (
        set "folderName=%%~nxi"
        set "symlinkPath=!targetDir!\!folderName!"

        mklink /d "!symlinkPath!" "%%i"
        echo Created symlink for "%%i" in "!symlinkPath!"
    )
)

pause
