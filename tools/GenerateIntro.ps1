param([string]$Voice = 'Microsoft Hedda Desktop')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$taskProject = Split-Path -Parent $PSScriptRoot
$taskSource = Join-Path $taskProject 'docs\Dialoge\01_Intro_Erwachen.md'
$taskDestination = Join-Path $taskProject 'Assets\SpaceMiner\Resources\Intro'
New-Item -ItemType Directory -Force -Path $taskDestination | Out-Null
$taskMarkdown = Get-Content -LiteralPath $taskSource -Raw -Encoding UTF8
$taskSections = [regex]::Matches($taskMarkdown, '(?ms)^## ([1-4])\. ([^\r\n]+)\r?\n(.*?)(?=^## |\z)')
if ($taskSections.Count -ne 4) { throw 'Das Intro muss vier nummerierte Szenen enthalten.' }
$taskSynth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$taskCues = @()
try {
    $taskSynth.SelectVoice($Voice)
    if ($taskSynth.Voice.Culture.Name -ne 'de-DE' -or $taskSynth.Voice.Gender -ne 'Female') {
        throw 'Für Mira wird eine deutsche weibliche Stimme benötigt.'
    }
    $taskSynth.Rate = -1
    $taskSynth.Volume = 85
    $taskFormat = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo -ArgumentList 22050, ([System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen), ([System.Speech.AudioFormat.AudioChannel]::Mono)
    $taskHeadings = @('NOTFALLSYSTEM AKTIV', 'SCHIFFSSTATUS: NOTBETRIEB', 'ASTEROIDENGÜRTEL · EINE DROHNE EINSETZBAR', 'ERSTES ZIEL: WASSER SICHERN')
    foreach ($taskSection in $taskSections) {
        $taskStage = [int]$taskSection.Groups[1].Value - 1
        $taskBody = $taskSection.Groups[3].Value
        $taskParagraphs = [regex]::Matches($taskBody, '(?m)^>[^\S\r\n]+([^\r\n]+)')
        if ($taskParagraphs.Count -eq 0) { throw 'Sprechertext fehlt in einer Intro-Szene.' }
        foreach ($taskParagraph in $taskParagraphs) {
            $taskIndex = $taskCues.Count
            $taskName = 'mira_' + $taskIndex.ToString('00')
            $taskText = $taskParagraph.Groups[1].Value.Trim()
            $taskAudio = Join-Path $taskDestination ($taskName + '.wav')
            $taskSynth.SetOutputToWaveFile($taskAudio, $taskFormat)
            $taskSynth.Speak($taskText)
            $taskSynth.SetOutputToNull()
            $taskCues += [pscustomobject]@{
                Stage = $taskStage
                Heading = $taskHeadings[$taskStage]
                Text = $taskText
                Audio = 'Intro/' + $taskName
            }
        }
    }
    $taskData = [pscustomobject]@{ Voice = $taskSynth.Voice.Name; Cues = $taskCues }
    $taskJson = ConvertTo-Json -InputObject $taskData -Depth 5
    [System.IO.File]::WriteAllText((Join-Path $taskDestination 'intro.json'), $taskJson, (New-Object System.Text.UTF8Encoding($false)))
    Write-Output ('Mira-Intro erzeugt: ' + $taskCues.Count + ' Abschnitte, Stimme ' + $taskSynth.Voice.Name)
} finally { $taskSynth.Dispose() }
