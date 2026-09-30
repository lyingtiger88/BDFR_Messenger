@echo off
setlocal
cd /d "%~dp0.."
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Test-MessengerMvp.ps1"
set "EXITCODE=%ERRORLEVEL%"
if not "%EXITCODE%"=="0" (
  echo.
  echo Script failed with exit code %EXITCODE%.
  pause
)
exit /b %EXITCODE%
