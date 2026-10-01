@echo off
setlocal
cd /d "%~dp0"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Verify-TelegramAndroid.ps1"
set "EXITCODE=%ERRORLEVEL%"

echo.
if "%EXITCODE%"=="0" (
  echo ==========================================
  echo Android source verification SUCCESSFUL.
  echo ==========================================
) else (
  echo ==========================================
  echo Android source verification FAILED.
  echo Exit code: %EXITCODE%
  echo ==========================================
)
echo.
pause
exit /b %EXITCODE%
