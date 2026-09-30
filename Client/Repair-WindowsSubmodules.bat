@echo off
setlocal
cd /d "%~dp0"

echo ==========================================
echo BDFR Messenger - Repair Windows Submodules
echo ==========================================
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Repair-WindowsSubmodules.ps1"
set "EXITCODE=%ERRORLEVEL%"

echo.
if "%EXITCODE%"=="0" (
  echo ==========================================
  echo Windows submodule repair SUCCESSFUL.
  echo ==========================================
) else (
  echo ==========================================
  echo Windows submodule repair FAILED.
  echo Exit code: %EXITCODE%
  echo ==========================================
)

echo.
pause
exit /b %EXITCODE%
