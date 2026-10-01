@echo off
setlocal
cd /d "%~dp0"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Bootstrap-TelegramAndroid.ps1"
set "EXITCODE=%ERRORLEVEL%"

echo.
if "%EXITCODE%"=="0" (
  echo ==========================================
  echo Telegram Android bootstrap SUCCESSFUL.
  echo ==========================================
) else (
  echo ==========================================
  echo Telegram Android bootstrap FAILED.
  echo Exit code: %EXITCODE%
  echo ==========================================
)
echo.
pause
exit /b %EXITCODE%
