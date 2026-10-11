@echo off
cd /d "%~dp0"
if not exist "Builds\TripoMining\SpaceMiner.exe" (
  echo Die Mining-Drohne wurde noch nicht gebaut.
  pause
  exit /b 1
)
start "" /D "%~dp0" "Builds\TripoMining\SpaceMiner.exe" -tripoMiningShowcase -screen-fullscreen 0 -screen-width 1600 -screen-height 1000 -logFile "%~dp0Logs\tripo-mining-player.log"
