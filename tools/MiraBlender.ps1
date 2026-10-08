param([string]$Script, [switch]$Background)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskTools = Join-Path $taskRoot 'work/AvatarTools'
$env:BLENDER_USER_CONFIG = Join-Path $taskTools 'user/config'
$env:BLENDER_USER_SCRIPTS = Join-Path $taskTools 'user/scripts'
$env:BLENDER_USER_DATAFILES = Join-Path $taskTools 'user/datafiles'
$env:BLENDER_USER_EXTENSIONS = Join-Path $taskTools 'user/extensions'
foreach($taskPath in @($env:BLENDER_USER_CONFIG,$env:BLENDER_USER_SCRIPTS,$env:BLENDER_USER_DATAFILES,$env:BLENDER_USER_EXTENSIONS)) {
    New-Item -ItemType Directory -Force $taskPath | Out-Null
}
$taskExe = Join-Path $taskTools 'blender-4.5.14-windows-x64/blender.exe'
if (!(Test-Path -LiteralPath $taskExe)) { throw 'Portable Blender fehlt in work/AvatarTools.' }
$taskArguments = @()
if ($Background) { $taskArguments += '--background' }
if ($Script) { $taskArguments += @('--python-exit-code', '1', '--python', ('"' + (Join-Path $taskRoot $Script) + '"')) }
$taskStyle = if ($Background) { 'Hidden' } else { 'Normal' }
$taskLogName = if ($Script) { [IO.Path]::GetFileNameWithoutExtension($Script) } else { 'interactive' }
New-Item -ItemType Directory -Force (Join-Path $taskRoot 'Logs') | Out-Null
$taskProcess = Start-Process -FilePath $taskExe -ArgumentList $taskArguments -WindowStyle $taskStyle -PassThru -WorkingDirectory $taskRoot -RedirectStandardOutput (Join-Path $taskRoot "Logs/$taskLogName-blender.log") -RedirectStandardError (Join-Path $taskRoot "Logs/$taskLogName-blender-error.log")
if ($Background) { $taskProcess.WaitForExit(); if ($taskProcess.ExitCode -ne 0) { throw "Blender Exit $($taskProcess.ExitCode)" } }
