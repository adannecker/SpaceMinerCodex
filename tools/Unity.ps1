param(
    [ValidateSet('Setup', 'Build', 'BuildExisting', 'HundredBuild', 'SpiralBuild', 'MillionBuild', 'Open', 'Play', 'Check', 'MiningCheck', 'AsteroidCheck')]
    [string]$Action = 'Open',
    [string]$UnityPath = '',
    [switch]$Visible
)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$logsPath = Join-Path $projectPath 'Logs'
New-Item -ItemType Directory -Force -Path $logsPath | Out-Null

if ($Action -eq 'Play' -or $Action -eq 'Check' -or $Action -eq 'MiningCheck') {
    $playerPath = Join-Path $projectPath 'Builds\Windows\SpaceMiner.exe'
    if (!(Test-Path -LiteralPath $playerPath)) { throw 'Zuerst bauen: .\tools\Unity.ps1 Build' }
    $arguments = @('-screen-fullscreen', '0', '-screen-width', '1440', '-screen-height', '900', '-logFile', ('"' + (Join-Path $logsPath 'player.log') + '"'))
    if ($Action -eq 'Check') { $arguments += '-spaceMinerSmokeTest' }
    if ($Action -eq 'MiningCheck') {
        $arguments += '-miningBalanceCheck'
        if (!$Visible) { $arguments += '-miningLogicOnly' }
    }
    $taskWindowStyle = if ($Action -eq 'Play' -or $Visible) { 'Normal' } else { 'Hidden' }
    $process = Start-Process -FilePath $playerPath -WorkingDirectory $projectPath -ArgumentList $arguments -PassThru -WindowStyle $taskWindowStyle
    if ($Action -eq 'Check' -or $Action -eq 'MiningCheck') {
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) { throw 'Spieltest fehlgeschlagen. Siehe Logs\player.log und Logs\smoke-test-error.txt.' }
        if ($Action -eq 'MiningCheck') {
            $taskReportPath = Join-Path $logsPath 'mining-balance-result.json'
            if (!(Test-Path -LiteralPath $taskReportPath) -or !(Get-Content -LiteralPath $taskReportPath -Raw | ConvertFrom-Json).passed) {
                throw 'Keine erfolgreiche Abbau-Simulation. Siehe Logs\mining-balance-error.txt.'
            }
            Write-Output $taskReportPath
        }
    }
    return
}

$versionFile = Join-Path $projectPath 'ProjectSettings\ProjectVersion.txt'
$versionLine = Get-Content -LiteralPath $versionFile | Where-Object { $_ -match '^m_EditorVersion: ' } | Select-Object -First 1
if (!$versionLine) { throw "Keine festgelegte Unity-Version in $versionFile." }
$projectUnityVersion = ($versionLine -replace '^m_EditorVersion: ', '').Trim()
if (!$UnityPath) { $UnityPath = Join-Path 'C:\Program Files\Unity\Hub\Editor' "$projectUnityVersion\Editor\Unity.exe" }
if (!(Test-Path -LiteralPath $UnityPath)) { throw "Projekt benötigt Unity $projectUnityVersion. Nicht gefunden: $UnityPath. Diese Version im Hub installieren oder ihren Pfad mit -UnityPath angeben. Versionswechsel bewusst nach docs/Unity-Versionen.md durchführen." }
$installedUnityVersion = ((Get-Item -LiteralPath $UnityPath).VersionInfo.ProductVersion -split '_')[0]
if ($installedUnityVersion -ne $projectUnityVersion) { throw "Editorversion $installedUnityVersion entspricht nicht der Projektversion $projectUnityVersion. Bewussten Versionswechsel nach docs/Unity-Versionen.md durchführen." }
$arguments = @('-projectPath', ('"' + $projectPath + '"'))
if ($Action -eq 'Open') {
    Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden | Out-Null
    return
}
$method = if ($Action -eq 'MillionBuild') { 'SpaceMiner.Editor.PrototypeSetup.BuildMillionAsteroids' } elseif ($Action -eq 'SpiralBuild') { 'SpaceMiner.Editor.PrototypeSetup.BuildSpiralAsteroids' } elseif ($Action -eq 'HundredBuild') { 'SpaceMiner.Editor.PrototypeSetup.BuildHundredAsteroids' } elseif ($Action -eq 'Build') { 'SpaceMiner.Editor.PrototypeSetup.BuildWindows' } elseif ($Action -eq 'AsteroidCheck') { 'SpaceMiner.Editor.AsteroidValidation.Run' } else { 'SpaceMiner.Editor.PrototypeSetup.Setup' }
if ($Action -eq 'BuildExisting') { $method = 'SpaceMiner.Editor.ResearchSimulationValidation.Build' }
$arguments += @('-batchmode', '-quit', '-executeMethod', $method, '-logFile', ('"' + (Join-Path $logsPath ($Action.ToLower() + '.log')) + '"'))
$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -PassThru -WindowStyle Hidden
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Unity $Action fehlgeschlagen. Siehe Logs." }
