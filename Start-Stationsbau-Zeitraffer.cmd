@echo off
setlocal
pushd "%~dp0"
if not exist "Builds\Windows\SpaceMiner.exe" (
  echo Zuerst den Development-Spielbuild erstellen. Siehe docs\Stationsbau-Zeitraffer.md.
  pause
  popd
  exit /b 1
)
if not exist "Logs" mkdir "Logs"
start "" "Builds\Windows\SpaceMiner.exe" -stationConstructionPreview -screen-fullscreen 0 -screen-width 1600 -screen-height 1000 -logFile "%~dp0Logs\station-construction-player.log"
popd
