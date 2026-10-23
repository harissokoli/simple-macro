@echo off
cd /d "%~dp0"

if not exist "%~dp0SimpleMacro.exe" (
    echo SimpleMacro.exe does not exist yet.
    echo.
    echo Run Build-EXE.bat first.
    echo.
    pause
    exit /b 1
)

start "" "%~dp0SimpleMacro.exe"
