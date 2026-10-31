@echo off
setlocal

set "BASH="

if exist "%ProgramFiles%\Git\bin\bash.exe" set "BASH=%ProgramFiles%\Git\bin\bash.exe"
if not defined BASH if exist "%ProgramFiles(x86)%\Git\bin\bash.exe" set "BASH=%ProgramFiles(x86)%\Git\bin\bash.exe"
if not defined BASH if exist "%LOCALAPPDATA%\Programs\Git\bin\bash.exe" set "BASH=%LOCALAPPDATA%\Programs\Git\bin\bash.exe"
if not defined BASH for %%I in (bash.exe) do if not defined BASH set "BASH=%%~$PATH:I"

if not defined BASH (
    echo ERROR: bash was not found in PATH or in a standard Git for Windows location.
    echo Install Git for Windows and make bash available, or run the .sh directly from WSL.
    exit /b 1
)

pushd "%~dp0" >nul
"%BASH%" ./generate-tooyioo-diagram.sh %*
set "exit_code=%errorlevel%"
popd >nul
exit /b %exit_code%
