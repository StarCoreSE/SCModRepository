@echo off
setlocal enabledelayedexpansion

REM Check if the script is running as administrator
>nul 2>&1 "%SystemRoot%\system32\cacls.exe" "%SystemRoot%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    echo.
    echo ====================================================
    echo This script must be run as an administrator to create symlinks!
    echo Right-click the script and select "Run as administrator".
    echo ====================================================
    echo.
    pause
    exit /b
)

REM Set the script's working directory to the location of this script
cd /d "%~dp0"

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

REM Define the script's current directory as the root search path for worlds
set currentDir=%cd%
echo Searching for world folders in: !currentDir!

REM Search the current directory and subdirectories for Sandbox.sbc and Sandbox_config.sbc files
set foundWorldFolder=false
for /d /r %%d in (*) do (
    if exist "%%d\Sandbox.sbc" if exist "%%d\Sandbox_config.sbc" (
        echo Found world folder: %%~dpd
        set foundWorldFolder=true

        REM Move to the parent folder of the world directory
        set sourceFolder=%%~dpd

        REM Set the destination path in the target Steam ID folder with the world folder name
        set worldName=%%~nxd
        set destinationFolder=!targetDir!\!worldName!

        REM Check if the destination already has a symlink or directory with the same name
        if exist "!destinationFolder!" (
            echo A directory or symlink with the name !worldName! already exists at the destination. Skipping...
        ) else (
            REM Create the symlink from the source folder to the target Steam ID save directory
            echo Creating symlink from "!sourceFolder!" to "!destinationFolder!"
            mklink /D "!destinationFolder!" "!sourceFolder!"
            if !errorlevel! neq 0 (
                echo Failed to create symlink for: !sourceFolder! Check permissions or path issues.
            ) else (
                echo Successfully created symlink: !worldName!
            )
        )
    )
)

REM Check if no world folders were found
if "!foundWorldFolder!"=="false" (
    echo No valid world folders found containing both Sandbox.sbc and Sandbox_config.sbc files.
) else (
    echo Operation completed successfully.
)

REM Ensure the script window stays open regardless of the outcome
pause
