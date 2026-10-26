@echo off
setlocal
pushd "%~dp0"
if not exist "Builds\Windows\SpaceMiner.exe" (
  echo Der Spielbuild fehlt.
  pause
  popd
  exit /b 1
)
if not exist "Logs" mkdir "Logs"
start "" "Builds\Windows\SpaceMiner.exe" -startStationPreview -stationOperationsPreview -indoorTrafficPreview -screen-fullscreen 0 -screen-width 1600 -screen-height 1000 -logFile "%~dp0Logs\innendrohnen-fahrprobe-player.log"
popd
