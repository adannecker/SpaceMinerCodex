param(
    [string]$SessionDirectory = '',
    [string]$OutputDirectory = ''
)
$ErrorActionPreference = 'Stop'
$taskProjectPath = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot)).TrimEnd('\', '/')
if (!$SessionDirectory) {
    $taskCodexRoot = if ($env:CODEX_HOME) { $env:CODEX_HOME } else { Join-Path $env:USERPROFILE '.codex' }
    $SessionDirectory = Join-Path $taskCodexRoot 'sessions'
}
if (!$OutputDirectory) { $OutputDirectory = Join-Path $taskProjectPath 'docs/Chats' }
if (!(Test-Path -LiteralPath $SessionDirectory)) { throw 'Lokaler Codex-Sitzungsordner fehlt. Bereits exportierte Chats stehen unter docs/Chats.' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

# Only the three project chats explicitly identified in the app are included.
# The export is a readable transcript, not a Codex session database backup.
$taskThreads = @(
    @{ Id='01a10da1-b11d-7472-b6b4-b34be7f814fd'; Title='Unity-Spiel gemeinsam entwickeln'; File='01-Unity-Spiel.md' },
    @{ Id='01a10dd1-fb99-7451-87bf-2d47afa4be7b'; Title='Asteroidenvarianten entwerfen'; File='02-Asteroiden.md' },
    @{ Id='01a10dac-caae-7492-bec8-24aac83ec430'; Title='Story, Dialoge & Bordcomputer'; File='03-Story-und-Mira.md' }
)
$taskFiles = @(Get-ChildItem -LiteralPath $SessionDirectory -Filter '*.jsonl' -File -Recurse)
$taskStamp = [DateTimeOffset]::UtcNow.ToString('yyyy-MM-dd HH:mm:ss') + ' UTC'
$taskIndex = [Collections.Generic.List[string]]::new()
$taskIndex.Add('# Projektchats')
$taskIndex.Add('')
$taskIndex.Add('Textarchive der drei SpaceMiner-Chats. Exportstand: ' + $taskStamp + '.')
$taskIndex.Add('')
$taskIndex.Add('Enthalten sind die lokal gespeicherten Benutzer- und Assistententexte, einschließlich Zwischenmeldungen. Systemanweisungen, interne Überlegungen, Werkzeugprotokolle und automatisch beigefügter Umgebungskontext sind ausgelassen. Bild-/Audioanhänge werden nicht aus Sitzungsdaten rekonstruiert; die Projektassets liegen unter Assets und docs/Art.')
$taskIndex.Add('')
$taskIndex.Add('Dies ist kein importierbares Codex-Sitzungsformat. Auf einem anderen Rechner einen neuen Chat im Projektordner beginnen und docs/Projektuebergabe.md lesen lassen. Die anderen Chats liefen beim ersten Export noch; Archive sind Momentaufnahmen und lassen sich auf dem Ursprungsrechner aktualisieren.')
$taskIndex.Add('')
$taskIndex.Add('Archivierte Aussagen können durch spätere Entscheidungen überholt sein. Der aktuelle Code und docs/Spielidee.md bestimmen den Projektstand.')
$taskIndex.Add('')
foreach ($taskThread in $taskThreads) {
    $taskMatches = @($taskFiles | Where-Object Name -Like ('*' + $taskThread.Id + '.jsonl'))
    if ($taskMatches.Count -ne 1) { throw ('Genau eine lokale Sitzung erwartet: ' + $taskThread.Title) }
    $taskLines = @(Get-Content -LiteralPath $taskMatches[0].FullName -Encoding UTF8)
    $taskMeta = ConvertFrom-Json -InputObject $taskLines[0]
    $taskSourceCwd = [IO.Path]::GetFullPath($taskMeta.payload.cwd).TrimEnd('\', '/')
    if ($taskSourceCwd -ne $taskProjectPath -or $taskMeta.payload.id -ne $taskThread.Id) {
        throw ('Sitzung gehört nicht zu diesem Projekt: ' + $taskThread.Title)
    }
    $taskArchive = [Collections.Generic.List[string]]::new()
    $taskArchive.Add('# ' + $taskThread.Title)
    $taskArchive.Add('')
    $taskArchive.Add('Exportstand: ' + $taskStamp + '. Lesbares Textarchiv; Hinweise in [README](README.md).')
    $taskArchive.Add('')
    $taskCount = 0
    foreach ($taskLine in $taskLines) {
        try { $taskEntry = ConvertFrom-Json -InputObject $taskLine -ErrorAction Stop }
        catch { continue } # An active session may have an unfinished final JSON line.
        if ($taskEntry.type -ne 'response_item' -or $taskEntry.payload.type -ne 'message') { continue }
        $taskMessage = $taskEntry.payload
        if ($taskMessage.role -notin @('user', 'assistant')) { continue }
        $taskParts = @($taskMessage.content | Where-Object { $_.type -in @('input_text', 'output_text', 'text') } | ForEach-Object { $_.text })
        $taskText = $taskParts -join "`n"
        if ($taskMessage.role -eq 'user') {
            foreach ($taskTag in @('environment_context', 'in-app-browser-context', 'external_codex_apps_open_page')) {
                $taskText = [regex]::Replace($taskText, '(?s)<' + $taskTag + '\b[^>]*>.*?</' + $taskTag + '>', '')
            }
            $taskText = [regex]::Replace($taskText, '^\s*## My request:\s*', '')
        }
        $taskText = $taskText.Trim()
        if (!$taskText) { continue }
        $taskWho = if ($taskMessage.role -eq 'user') { 'Nutzer' } else { 'Assistent' }
        $taskArchive.Add('## ' + $taskWho + ' · ' + $taskEntry.timestamp)
        $taskArchive.Add('')
        $taskArchive.Add($taskText)
        $taskArchive.Add('')
        $taskCount++
    }
    if ($taskCount -eq 0) { throw ('Keine Nachrichten exportiert: ' + $taskThread.Title) }
    $taskTarget = Join-Path $OutputDirectory $taskThread.File
    [IO.File]::WriteAllText($taskTarget, ($taskArchive -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
    $taskIndex.Add('- [' + $taskThread.Title + '](' + $taskThread.File + ') — ' + $taskCount + ' Textnachrichten.')
    Write-Output ($taskThread.File + ': ' + $taskCount + ' Textnachrichten')
}
$taskIndex.Add('')
$taskIndex.Add('Aktualisieren auf dem Rechner mit den ursprünglichen lokalen Sitzungen:')
$taskIndex.Add('')
$taskIndex.Add('```powershell')
$taskIndex.Add('.\tools\ExportProjectChats.ps1')
$taskIndex.Add('```')
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'README.md'), ($taskIndex -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
