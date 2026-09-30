@echo off
setlocal
cd /d "%~dp0.."

echo Starting BDFR Messenger backend...
docker compose -f "Infrastructure\docker-compose.yml" up --build -d
set "DOCKER_EXIT=%ERRORLEVEL%"

if not "%DOCKER_EXIT%"=="0" (
  echo.
  echo Docker Compose failed with exit code %DOCKER_EXIT%.
  echo Review the Docker output above.
  pause
  exit /b %DOCKER_EXIT%
)

echo.
echo Backend build/start command completed.
echo Running API smoke test...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Test-MessengerMvp.ps1" -SkipDockerStart
set "EXITCODE=%ERRORLEVEL%"

if not "%EXITCODE%"=="0" (
  echo.
  echo Script failed with exit code %EXITCODE%.
  pause
)

exit /b %EXITCODE%
