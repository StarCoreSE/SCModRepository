@echo off
setlocal enabledelayedexpansion

echo Starting operation...

set OUTPUT=newfile.txt
set /a COUNT=0
set /a ERRORS=0

if exist "%OUTPUT%" (
  echo Existing output file found. Deleting...
  del "%OUTPUT%"
)

for /r %%i in (*.cs) do (
  echo Processing: "%%i"
  type "%%i" >> "%OUTPUT%"
  if errorlevel 1 (
    echo Error processing "%%i".
    set /a ERRORS+=1
  ) else (
    set /a COUNT+=1
  )
)

echo Operation completed.
echo Total files processed: %COUNT%
if %ERRORS% gtr 0 (
  echo There were %ERRORS% errors during the operation.
) else (
  echo No errors encountered.
)
pause