@echo off
setlocal
pushd "%~dp0"
if not exist "Builds\MiraDialogueGame\SpaceMiner.exe" (
  echo Der Mira-Questspielbuild fehlt. Siehe docs\MiraAvatar.md.
  pause
  popd
  exit /b 1
)
if not exist "Logs\MiraQuestPreview" mkdir "Logs\MiraQuestPreview"
start "" "Builds\MiraDialogueGame\SpaceMiner.exe" -miraQuestPreview -screen-fullscreen 0 -screen-width 1600 -screen-height 1000 -logFile "%~dp0Logs\MiraQuestPreview\player.log"
popd
