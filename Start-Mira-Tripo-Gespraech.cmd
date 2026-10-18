@echo off
setlocal
pushd "%~dp0"
if not exist "Builds\MiraTripoDemo\MiraTripoDemo.exe" (
  echo Die Mira-Tripo-Dialogvorschau fehlt. Siehe docs\Mira3D.md.
  pause
  popd
  exit /b 1
)
if not exist "Logs" mkdir "Logs"
start "" "Builds\MiraTripoDemo\MiraTripoDemo.exe" -screen-fullscreen 0 -logFile "%~dp0Logs\mira-tripo-dialog-player.log"
popd
