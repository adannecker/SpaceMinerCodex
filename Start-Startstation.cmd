@echo off
setlocal
pushd "%~dp0"
if not exist "Builds\WindowsRelease\SpaceMiner.exe" (
  echo Der Spielbuild fehlt. Siehe README.md.
  pause
  popd
  exit /b 1
)
if not exist "Logs" mkdir "Logs"
start "" "Builds\WindowsRelease\SpaceMiner.exe" -startStationPreview -screen-fullscreen 0 -screen-width 1600 -screen-height 1000 -logFile "%~dp0Logs\startstation-player.log"
popd
