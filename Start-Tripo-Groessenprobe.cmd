@echo off
cd /d "%~dp0"
if not exist "Builds\TripoModels\SpaceMiner.exe" (
  echo Die Tripo-Groessenprobe wurde noch nicht gebaut.
  pause
  exit /b 1
)
start "" /D "%~dp0" "Builds\TripoModels\SpaceMiner.exe" -tripoModelPreview -screen-fullscreen 0 -screen-width 1600 -screen-height 1000 -logFile "%~dp0Logs\tripo-preview-player.log"
