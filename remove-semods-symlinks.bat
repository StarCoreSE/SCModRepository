@echo off
setlocal enabledelayedexpansion

REM Check if the script is running as administrator
>nul 2>&1 "%SystemRoot%\system32\cacls.exe" "%SystemRoot%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    echo.
    echo ====================================================
    echo This script must be run as an administrator to remove symlinks!
    echo Right-click the script and select "Run as administrator".
    echo ====================================================
    echo.
    pause
    exit /b
)

REM Define the root Space Engineers mod directory
set modRootDir="%APPDATA%\SpaceEngineers\Mods"

REM Check if the mod directory exists
if not exist !modRootDir! (
    echo Mod directory does not exist: !modRootDir!
    echo Please ensure that the Space Engineers game has created mod directories.
    pause
    exit /b
)

REM Change to the mod directory
cd /d "!modRootDir!"

REM List only symbolic links in the mod directory
echo.
echo Searching for symbolic links in: !modRootDir!
set foundSymlink=false
for /f "tokens=*" %%l in ('dir /AL /B') do (
    set foundSymlink=true
    echo Found symlink: %%l
)

REM Check if any symbolic links were found
if "!foundSymlink!"=="false" (
    echo No symbolic links found in: !modRootDir!
    pause
    exit /b
)

REM Prompt the user for confirmation once to delete all symbolic links
set /p confirmDelete="Do you want to delete all symlinks in this directory? [Y/N]: "
if /i "!confirmDelete!" NEQ "Y" (
    echo Operation canceled. No symlinks were removed.
    pause
    exit /b
)

REM Remove all symlinks in the mod directory
echo.
echo Deleting all symbolic links in: !modRootDir!
for /f "tokens=*" %%l in ('dir /AL /B') do (
    echo Removing symlink: %%l
    rmdir "%%l"
    if !errorlevel! == 0 (
        echo Successfully removed symlink: %%l
    ) else (
        echo Failed to remove symlink: %%l. Check permissions or path issues.
    )
)

REM Completion message
echo.
echo All symbolic links have been removed from: !modRootDir!
pause
