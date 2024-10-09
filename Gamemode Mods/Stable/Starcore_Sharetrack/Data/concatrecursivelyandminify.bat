@echo off
setlocal enabledelayedexpansion
chcp 65001 
REM ^^ this is to change the encoding to UTF-8, apparently

echo Starting operation...

set TEMP_OUTPUT=temp_concatenated.txt
set FINAL_OUTPUT=minified_output.txt
set /a COUNT=0
set /a ERRORS=0

if exist "%TEMP_OUTPUT%" (
  echo Existing temporary file found. Deleting...
  del "%TEMP_OUTPUT%"
)

if exist "%FINAL_OUTPUT%" (
  echo Existing output file found. Deleting...
  del "%FINAL_OUTPUT%"
)

for /r %%i in (*.cs) do (
  echo Processing: "%%i"
  type "%%i" >> "%TEMP_OUTPUT%"
  if errorlevel 1 (
    echo Error processing "%%i".
    set /a ERRORS+=1
  ) else (
    set /a COUNT+=1
  )
)

echo Concatenation completed.
echo Total files processed: %COUNT%
if %ERRORS% gtr 0 (
  echo There were %ERRORS% errors during the concatenation.
) else (
  echo No errors encountered during concatenation.
)

echo Minifying concatenated file...
csmin < "%TEMP_OUTPUT%" > "%FINAL_OUTPUT%"
if errorlevel 1 (
  echo Error occurred during minification.
  set /a ERRORS+=1
) else (
  echo Minification completed successfully.
)

echo Cleaning up temporary file...
del "%TEMP_OUTPUT%"

echo Operation completed.
if %ERRORS% gtr 0 (
  echo There were %ERRORS% errors during the entire operation.
) else (
  echo No errors encountered during the entire operation.
)
pause