@echo off
setlocal
pushd "%~dp0"
if not exist "Builds\RingSimulator\SpaceMiner.exe" (
  echo Die Ringbau-Testanwendung fehlt. Siehe docs\Ringstation-Ausbau.md.
  pause
  popd
  exit /b 1
)
if not exist "Logs" mkdir "Logs"
start "" "Builds\RingSimulator\SpaceMiner.exe" -ringSimulator -screen-fullscreen 0 -screen-width 1600 -screen-height 1000 -logFile "%~dp0Logs\ring-simulator-player.log"
popd
