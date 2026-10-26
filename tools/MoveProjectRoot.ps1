param([switch]$CheckOnly)
$ErrorActionPreference = 'Stop'
$oldRoot = 'C:\Users\achim.dannecker\source\repos\SpaceMinerCodex'
$newRoot = 'C:\temp\SpaceMinerCodex'
$python = 'C:\Users\achim.dannecker\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
$apps = @(Get-Process ChatGPT,Codex -ErrorAction SilentlyContinue)
if ($CheckOnly) {
    [ordered]@{sourceExists=(Test-Path -LiteralPath $oldRoot);targetExists=(Test-Path -LiteralPath $newRoot);appProcesses=$apps.Count;canRunNow=($apps.Count -eq 0);mode='Read-only readiness check'} | ConvertTo-Json
    exit 0
}
if ($apps.Count -gt 0) { throw 'ChatGPT/Codex vollstaendig beenden, dann diesen Starter erneut ausfuehren.' }
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Unity vor dem Umzug schliessen.' }
if (Test-Path -LiteralPath $newRoot) { throw 'Ziel existiert bereits. Keine Zusammenfuehrung oder Ueberschreibung.' }
if (!(Test-Path -LiteralPath (Join-Path $oldRoot '.git') -PathType Container)) { throw 'Erwarteter Quell-Checkout fehlt.' }
Set-Location -LiteralPath 'C:\temp'
& $python (Join-Path $oldRoot 'tools/PrepareProjectRootMove.py')
if ($LASTEXITCODE -ne 0) { throw 'Dateisicherung fehlgeschlagen; nichts verschoben.' }
$moveBody = [scriptblock]::Create((Get-Content -LiteralPath (Join-Path $PSScriptRoot 'NativeMove.ps1') -Raw))
$specs = @(Get-Content -LiteralPath (Join-Path $oldRoot 'Logs/ProjectRoot-20261026/services-before.json') -Raw | ConvertFrom-Json)
$viewerCopies = @()
foreach ($relative in @('Design/Models/OperatorArm/Viewer','Design/Models/TouchConsole/ConsoleViewer')) {
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $oldRoot $relative) -File) {
        $viewerCopies += @{Relative=($relative+'/'+$file.Name);Bytes=[IO.File]::ReadAllBytes($file.FullName)}
    }
}
$stopped = @()
try {
    foreach ($spec in $specs) {
        $port = if ($spec.Label -eq 'i75') {8769} elseif ($spec.Label -eq 'operator') {8786} elseif ($spec.Label -eq 'console-r03') {8771} elseif ($spec.Label -eq 'console-r04') {8772} elseif ($spec.Label -eq 'console-8773') {8773} else {8770}
        $listeners = @(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue)
        foreach ($listener in $listeners) {
            $process = Get-CimInstance Win32_Process -Filter ('ProcessId='+$listener.OwningProcess)
            $scriptName = [IO.Path]::GetFileName($spec.Script)
            if (!$process -or $process.ExecutablePath -ne $spec.Exe -or !$process.CommandLine.Contains($scriptName)) { throw "Unerwarteter Dienst auf Port $port; nichts beendet." }
            $native = Get-Process -Id $process.ProcessId
            Stop-Process -Id $native.Id
            $native.WaitForExit()
        }
        $stopped += $spec
    }
    & $moveBody
    & $python (Join-Path $PSScriptRoot 'VerifyProjectRootMove.py') $newRoot
    if ($LASTEXITCODE -ne 0) { throw 'Dateien verschoben, aber Nachpruefung fehlgeschlagen. Siehe Umzugsprotokoll; nichts loeschen.' }
    $utf8 = [Text.UTF8Encoding]::new($false)
    $note = "`r`n`r`n## Lokaler Projektordner nach Umzug`r`n`r`nAuf diesem Rechner liegt der gemeinsame physische Checkout unter ``C:\temp\SpaceMinerCodex``. ``C:\Users\achim.dannecker\source\repos\SpaceMinerCodex`` ist nur noch eine NTFS-Junction auf denselben Checkout. Keine zweite Arbeitskopie anlegen. Bei Rechnerwechsel den dort vorhandenen Checkout verwenden und den tatsaechlichen Pfad pruefen.`r`n"
    [IO.File]::AppendAllText((Join-Path $newRoot 'AGENTS.md'),$note,$utf8)
    $status = "<!-- ROOT_MOVE_STATUS_BEGIN -->`n**Umzug abgeschlossen:** physischer Checkout ``C:\temp\SpaceMinerCodex``; alter Pfad als NTFS-Junction erhalten. SHA256, HEAD, Arbeitsstand und Git-Konnektivitaet im Helfer geprueft; ``Logs/ProjectRoot-20261026/after.json`` enthaelt den tatsaechlichen Nachweis. Anschliessend nur diese Ordnerdokumentation aktualisiert.`n<!-- ROOT_MOVE_STATUS_END -->"
    $doc = Join-Path $newRoot 'docs/Projektordner-Umzug-2026-10-26.md'
    $text = [IO.File]::ReadAllText($doc)
    [IO.File]::WriteAllText($doc,[regex]::Replace($text,'(?s)<!-- ROOT_MOVE_STATUS_BEGIN -->.*?<!-- ROOT_MOVE_STATUS_END -->',$status),$utf8)
    [IO.File]::AppendAllText((Join-Path $newRoot 'docs/Projektmemory.md'),"`r`n- Projektordner-Umzug durch separaten Helfer abgeschlossen: physisch C:\temp\SpaceMinerCodex, bisheriger Source-Pfad als NTFS-Junction auf denselben Checkout. Verifizierter Nachweis in Logs/ProjectRoot-20261026/after.json; HEAD/Arbeitsstand und Projekt-/Git-Dateien erhalten, danach AGENTS/Umzugsdokumentation aktualisiert. Keine App-Datenbank-/Chat-ID-/Saveformat-/Unity-Versionsaenderung und kein Commit/Push.`r`n",$utf8)
    Write-Host 'UMZUG UND DATEIPRUEFUNG ERFOLGREICH: C:\temp\SpaceMinerCodex' -ForegroundColor Green
} finally {
    $activeRoot = if (Test-Path -LiteralPath (Join-Path $newRoot '.git')) {$newRoot} else {$oldRoot}
    foreach ($spec in $stopped) {
        $exe = $spec.Exe.Replace($oldRoot,$activeRoot)
        $serverArgs = @('-u',('"'+(Join-Path $activeRoot $spec.Script)+'"'))+@($spec.Extra)
        Start-Process -FilePath $exe -ArgumentList $serverArgs -WorkingDirectory $activeRoot -WindowStyle Hidden -RedirectStandardOutput (Join-Path $activeRoot ('Logs/ProjectRoot-20261026/'+$spec.Label+'-manual-stdout.log')) -RedirectStandardError (Join-Path $activeRoot ('Logs/ProjectRoot-20261026/'+$spec.Label+'-manual-stderr.log')) | Out-Null
    }
    # Viewer startup regenerates derived HTML/JS; preserve the exact previous files.
    Start-Sleep -Seconds 2
    foreach ($copy in $viewerCopies) { [IO.File]::WriteAllBytes((Join-Path $activeRoot $copy.Relative),$copy.Bytes) }
}
