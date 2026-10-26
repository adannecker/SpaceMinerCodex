@echo off
setlocal
pushd "%~dp0"
if not exist "Builds\Windows\SpaceMiner.exe" (
  echo Der Spielbuild fehlt. Siehe docs\Station-Keilmodule-Umsetzung-2026-10-25.md.
  pause
  popd
  exit /b 1
)
if not exist "Logs" mkdir "Logs"
start "" "Builds\Windows\SpaceMiner.exe" -startStationPreview -droneHallPreview -stationKeilPreview -screen-fullscreen 0 -screen-width 1600 -screen-height 1000 -logFile "%~dp0Logs\keil-station-player.log"
popd
