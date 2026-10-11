@echo off
cd /d "%~dp0"
if not exist "Builds\MaintenanceShowcase\SpaceMiner.exe" (
  echo Der Wartungsdrohnen-Showcase wurde noch nicht gebaut.
  pause
  exit /b 1
)
start "" /D "%~dp0" "Builds\MaintenanceShowcase\SpaceMiner.exe" -maintenanceShowcase -screen-fullscreen 0 -screen-width 1600 -screen-height 1000 -logFile "%~dp0Logs\maintenance-showcase-player.log"
