@echo off
cd /d "%~dp0"
if not exist "Builds\ConsoleStudio\ConsoleStudio.exe" (
  echo Konsolenstudio noch nicht gebaut. Siehe docs\Konsolenstudio.md.
  pause
  exit /b 1
)
if not exist "Logs" mkdir "Logs"
start "SpaceMiner Konsolenstudio" "Builds\ConsoleStudio\ConsoleStudio.exe" -screen-fullscreen 0 -screen-width 1600 -screen-height 1000 -force-d3d11 -logFile "Logs\console-studio-player.log"
