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

REM Define the root Space Engineers save directory
set saveRootDir="%APPDATA%\SpaceEngineers\Saves"

REM Check if the root save directory exists
if not exist !saveRootDir! (
    echo Save directory does not exist: !saveRootDir!
    echo Please ensure that the Space Engineers game has created save directories.
    pause
    exit /b
)

REM Initialize variables to track the latest modified Steam ID folder
set latestSteamID=
set latestModifiedDate=0

REM Search for directories that match the 17-digit Steam ID pattern
echo Searching for valid Steam ID directories in: !saveRootDir!
for /d %%i in (!saveRootDir!\*) do (
    REM Get the folder name (Steam ID)
    set folderName=%%~nxi

    REM Check if the folder name is exactly 17 characters long and only contains digits
    set folderLength=0
    for /l %%a in (0, 1, 16) do (
        if "!folderName:~%%a,1!" GEQ "0" if "!folderName:~%%a,1!" LEQ "9" set /a folderLength+=1
    )

    REM If folder name length is 17 and only contains digits, consider it a valid Steam ID
    if !folderLength! == 17 (
        REM Get the modification date of the current folder
        for %%j in (%%i) do (
            set modifiedDate=%%~tj

            REM Convert the modified date to a comparable format (YYYYMMDDHHMMSS)
            set modifiedDate=!modifiedDate: =!
            set modifiedDate=!modifiedDate:/=!
            set modifiedDate=!modifiedDate::=!

            REM Compare the current folder's modified date to the latest found so far
            if !modifiedDate! GTR !latestModifiedDate! (
                set latestSteamID=!folderName!
                set latestModifiedDate=!modifiedDate!
            )
        )
    )
)

REM Verify if we found a valid Steam ID folder
if "!latestSteamID!"=="" (
    echo No valid Steam ID folders found in: !saveRootDir!
    pause
    exit /b
)

REM Set the path to the latest modified Steam ID folder
set targetDir=%saveRootDir%\!latestSteamID!
echo Using latest Steam ID directory: !targetDir!

REM Change to the target directory
cd /d "!targetDir!"

REM List only symbolic links in the target Steam ID directory
echo.
echo Searching for symbolic links in: !targetDir!
set foundSymlink=false
for /f "tokens=*" %%l in ('dir /AL /B') do (
    set foundSymlink=true
    echo Found symlink: %%l
)

REM Check if any symbolic links were found
if "!foundSymlink!"=="false" (
    echo No symbolic links found in: !targetDir!
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

REM Remove all symlinks in the target directory
echo.
echo Deleting all symbolic links in: !targetDir!
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
echo All symbolic links have been removed from: !targetDir!
pause
