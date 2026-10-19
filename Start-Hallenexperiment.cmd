@echo off
setlocal
pushd "%~dp0"
if not exist "Builds\HallExperiment\HallExperiment.exe" (
  echo Das Hallenexperiment fehlt. Siehe docs\Hallenexperiment.md.
  pause
  popd
  exit /b 1
)
if not exist "Logs" mkdir "Logs"
start "" "Builds\HallExperiment\HallExperiment.exe" -screen-fullscreen 0 -logFile "%~dp0Logs\hall-experiment-player.log"
popd
