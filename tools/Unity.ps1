param(
    [ValidateSet('Setup', 'Build', 'HundredBuild', 'SpiralBuild', 'MillionBuild', 'Open', 'Play', 'Check', 'AsteroidCheck')]
    [string]$Action = 'Open',
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe',
    [switch]$Visible
)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$logsPath = Join-Path $projectPath 'Logs'
New-Item -ItemType Directory -Force -Path $logsPath | Out-Null

if ($Action -eq 'Play' -or $Action -eq 'Check') {
    $playerPath = Join-Path $projectPath 'Builds\Windows\SpaceMiner.exe'
    if (!(Test-Path -LiteralPath $playerPath)) { throw 'Zuerst bauen: .\tools\Unity.ps1 Build' }
    $arguments = @('-screen-fullscreen', '0', '-screen-width', '1440', '-screen-height', '900', '-logFile', ('"' + (Join-Path $logsPath 'player.log') + '"'))
    if ($Action -eq 'Check') { $arguments += '-spaceMinerSmokeTest' }
    $taskWindowStyle = if ($Action -eq 'Play' -or $Visible) { 'Normal' } else { 'Hidden' }
    $process = Start-Process -FilePath $playerPath -WorkingDirectory $projectPath -ArgumentList $arguments -PassThru -WindowStyle $taskWindowStyle
    if ($Action -eq 'Check') {
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) { throw 'Spieltest fehlgeschlagen. Siehe Logs\player.log und Logs\smoke-test-error.txt.' }
    }
    return
}

if (!(Test-Path -LiteralPath $UnityPath)) { throw "Unity nicht gefunden: $UnityPath. Mit -UnityPath den Editor angeben." }
$arguments = @('-projectPath', ('"' + $projectPath + '"'))
if ($Action -eq 'Open') {
    Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden | Out-Null
    return
}
$method = if ($Action -eq 'MillionBuild') { 'SpaceMiner.Editor.PrototypeSetup.BuildMillionAsteroids' } elseif ($Action -eq 'SpiralBuild') { 'SpaceMiner.Editor.PrototypeSetup.BuildSpiralAsteroids' } elseif ($Action -eq 'HundredBuild') { 'SpaceMiner.Editor.PrototypeSetup.BuildHundredAsteroids' } elseif ($Action -eq 'Build') { 'SpaceMiner.Editor.PrototypeSetup.BuildWindows' } elseif ($Action -eq 'AsteroidCheck') { 'SpaceMiner.Editor.AsteroidValidation.Run' } else { 'SpaceMiner.Editor.PrototypeSetup.Setup' }
$arguments += @('-batchmode', '-quit', '-executeMethod', $method, '-logFile', ('"' + (Join-Path $logsPath ($Action.ToLower() + '.log')) + '"'))
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -PassThru -WindowStyle Hidden
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity $Action fehlgeschlagen. Siehe Logs." }
