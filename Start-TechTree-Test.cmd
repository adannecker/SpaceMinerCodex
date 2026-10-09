@echo off
setlocal
pushd "%~dp0"
if not exist "Builds\TechTreeDemo\SpaceMiner.exe" (
  echo Die Testanwendung fehlt. Hinweise: docs\Forschungslabor.md
  pause
  popd
  exit /b 1
)
if not exist "Logs" mkdir "Logs"
start "" "Builds\TechTreeDemo\SpaceMiner.exe" -techTreeDemo -screen-fullscreen 0 -screen-width 1600 -screen-height 1000 -logFile "%~dp0Logs\techtree-demo-player.log"
popd
