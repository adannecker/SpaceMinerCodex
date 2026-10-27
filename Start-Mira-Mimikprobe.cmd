@echo off
cd /d "%~dp0"
if not exist "Builds\MiraDialogueDemo\MiraDialogueDemo.exe" (
  echo Mira-Dialogstudio noch nicht gebaut. Siehe docs\MiraAvatar.md.
  pause
  exit /b 1
)
if not exist "Logs" mkdir "Logs"
start "Mira Mimikprobe" "Builds\MiraDialogueDemo\MiraDialogueDemo.exe" -miraExpressivePreview -screen-fullscreen 0 -screen-width 1600 -screen-height 1000 -force-d3d11 -logFile "Logs\mira-expression-preview.log"
