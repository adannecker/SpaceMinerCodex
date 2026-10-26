@echo off
setlocal
pushd "%~dp0"
if not exist "Builds\MiraDialogueDemo\MiraDialogueDemo.exe" (
  echo Das Mira-Dialogstudio ist noch nicht gebaut. Siehe docs\MiraAvatar.md.
  pause
  popd
  exit /b 1
)
if not exist "Logs\MiraDialogue" mkdir "Logs\MiraDialogue"
start "" "Builds\MiraDialogueDemo\MiraDialogueDemo.exe" -miraDialogueDemo -screen-fullscreen 0 -screen-width 1440 -screen-height 900 -logFile "%~dp0Logs\MiraDialogue\demo-player.log"
popd
