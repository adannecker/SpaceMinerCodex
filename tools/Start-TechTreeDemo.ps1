param([int]$Width=1600,[int]$Height=1000)
$ErrorActionPreference='Stop'
$demoRoot=Split-Path -Parent $PSScriptRoot
$demoPlayer=Join-Path $demoRoot 'Builds\TechTreeDemo\SpaceMiner.exe'
if(!(Test-Path -LiteralPath $demoPlayer)){throw 'Demo noch nicht gebaut. Siehe docs/Forschungslabor.md.'}
Start-Process -FilePath $demoPlayer -WorkingDirectory $demoRoot -ArgumentList @('-researchLab','-screen-fullscreen','0','-screen-width',$Width,'-screen-height',$Height,'-logFile',(Join-Path $demoRoot 'Logs\techtree-demo-player.log')) -WindowStyle Normal
