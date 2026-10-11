@echo off
setlocal
pushd "%~dp0"
if not exist "Builds\RingSimulator\SpaceMiner.exe" (
  echo Der Ring-Rundgang fehlt. Siehe docs\Ringstation-Ausbau.md.
  pause
  popd
  exit /b 1
)
if not exist "Logs" mkdir "Logs"
start "" "Builds\RingSimulator\SpaceMiner.exe" -ringWalkthrough -screen-fullscreen 0 -screen-width 1600 -screen-height 1000 -logFile "%~dp0Logs\ring-walkthrough-player.log"
popd
