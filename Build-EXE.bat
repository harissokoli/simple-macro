@echo off
cd /d "%~dp0"

echo ============================================================
echo  SimpleMacro - EXE Builder v4
echo  Always-Active Macro Engine
echo ============================================================
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-EXE.ps1"

if errorlevel 1 (
    echo.
    echo BUILD FAILED.
    echo.
    pause
    exit /b 1
)

echo.
echo Starting SimpleMacro.exe...
echo.

start "" "%~dp0SimpleMacro.exe"

pause
