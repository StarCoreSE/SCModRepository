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

for /f "delims=" %%i in ('dir /AL /b "%targetDir%" 2^>nul') do (
    if exist "%targetDir%\%%i" (
        rmdir /s /q "%targetDir%\%%i"
        echo Removed symlink in "%targetDir%\%%i"
    )
)

pause
