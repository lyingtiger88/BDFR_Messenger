@echo off
setlocal
cd /d "%~dp0"

echo ==========================================
echo BDFR Messenger - Telegram Desktop Bootstrap
echo ==========================================
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Bootstrap-TelegramDesktop.ps1"
set "EXITCODE=%ERRORLEVEL%"

echo.
if "%EXITCODE%"=="0" (
  echo ==========================================
  echo Telegram Desktop source prepared SUCCESSFULLY.
  echo ==========================================
  echo.
  echo Source location:
  echo %~dp0TelegramDesktop
  echo.
  echo Next step: return to ChatGPT and send me the final output.
) else (
  echo ==========================================
  echo Telegram Desktop bootstrap FAILED.
  echo Exit code: %EXITCODE%
  echo ==========================================
)

echo.
pause
exit /b %EXITCODE%
