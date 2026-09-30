@echo off
setlocal
cd /d "%~dp0"

echo ==========================================
echo BDFR Messenger - Verify Telegram Desktop
echo ==========================================
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Verify-TelegramDesktop.ps1"
set "EXITCODE=%ERRORLEVEL%"

echo.
if "%EXITCODE%"=="0" (
  echo ==========================================
  echo Verification SUCCESSFUL.
  echo ==========================================
) else (
  echo ==========================================
  echo Verification FAILED.
  echo Exit code: %EXITCODE%
  echo ==========================================
)

echo.
pause
exit /b %EXITCODE%
