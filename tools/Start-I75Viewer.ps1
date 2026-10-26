param([switch]$OpenBrowser)
$ErrorActionPreference='Stop'
$viewerRoot=Split-Path -Parent $PSScriptRoot
$viewerUrl='http://127.0.0.1:8769/I75Viewer/'
try {$viewerHealth=Invoke-RestMethod 'http://127.0.0.1:8769/health' -TimeoutSec 2} catch {$viewerHealth=$null}
if($viewerHealth -and $viewerHealth.service -ne 'spaceminer-i75-viewer'){throw 'Port8769 is already used by another service.'}
if(-not $viewerHealth){
    $viewerPython=(Get-Command python.exe).Source
    $viewerProcess=Start-Process -FilePath $viewerPython -WorkingDirectory $viewerRoot -ArgumentList @('-u',('"'+(Join-Path $PSScriptRoot 'ServeI75Viewer.py')+'"')) -WindowStyle Hidden -RedirectStandardOutput (Join-Path $viewerRoot 'Logs/i75-viewer-server.log') -RedirectStandardError (Join-Path $viewerRoot 'Logs/i75-viewer-requests.log') -PassThru
    Set-Content -LiteralPath (Join-Path $viewerRoot 'Logs/i75-viewer-server-pid.txt') -Value $viewerProcess.Id
    for($viewerAttempt=0;$viewerAttempt -lt 20;$viewerAttempt++){
        try {$viewerHealth=Invoke-RestMethod 'http://127.0.0.1:8769/health' -TimeoutSec 1;if($viewerHealth.service -eq 'spaceminer-i75-viewer'){break}} catch {}
        Start-Sleep -Milliseconds 200
    }
    if($viewerHealth.service -ne 'spaceminer-i75-viewer'){throw 'Local I75 viewer did not start.'}
}
if($OpenBrowser){Start-Process $viewerUrl}
Write-Output $viewerUrl
