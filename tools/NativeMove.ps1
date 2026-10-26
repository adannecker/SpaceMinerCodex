$ErrorActionPreference = 'Stop'
$sourceRoot = [IO.Path]::GetFullPath('C:\Users\achim.dannecker\source\repos\SpaceMinerCodex')
$targetRoot = [IO.Path]::GetFullPath('C:\temp\SpaceMinerCodex')
$sourceParent = 'C:\Users\achim.dannecker\source\repos'
$targetParent = 'C:\temp'
if ([IO.Path]::GetDirectoryName($sourceRoot) -ne $sourceParent -or [IO.Path]::GetDirectoryName($targetRoot) -ne $targetParent) { throw 'Unexpected relocation paths' }
if ([IO.Path]::GetPathRoot($sourceRoot) -ne [IO.Path]::GetPathRoot($targetRoot)) { throw 'Relocation must stay on the same volume' }
$sourceItem = Get-Item -LiteralPath $sourceRoot -Force
if ($sourceItem.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Source must be the original physical directory' }
if ((Get-Item -LiteralPath $targetParent -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Target parent must be a physical directory' }
if (Test-Path -LiteralPath $targetRoot) { throw 'Target already exists; refusing to merge or overwrite' }
if (!(Test-Path -LiteralPath (Join-Path $sourceRoot '.git') -PathType Container)) { throw 'Expected standalone Git repository missing' }
if (!(Test-Path -LiteralPath (Join-Path $sourceRoot 'Logs/ProjectRoot-20261026/before.json'))) { throw 'Required preservation manifest missing' }
$repoProcesses = @(Get-CimInstance Win32_Process | Where-Object { $_.ExecutablePath -and $_.ExecutablePath.StartsWith($sourceRoot,[StringComparison]::OrdinalIgnoreCase) })
if ($repoProcesses.Count -gt 0 -or (Get-Process Unity -ErrorAction SilentlyContinue)) { throw 'Close Unity and project executables before relocation' }
Set-Location -LiteralPath $targetParent
Move-Item -LiteralPath $sourceRoot -Destination $targetRoot
try {
    New-Item -ItemType Junction -Path $sourceRoot -Target $targetRoot | Out-Null
    $alias = Get-Item -LiteralPath $sourceRoot -Force
    if ($alias.LinkType -ne 'Junction' -or [IO.Path]::GetFullPath([string]$alias.Target) -ne $targetRoot) { throw 'Compatibility junction validation failed' }
} catch {
    if (!(Test-Path -LiteralPath $sourceRoot)) { Move-Item -LiteralPath $targetRoot -Destination $sourceRoot }
    throw
}
$receipt = [ordered]@{ physicalRoot=$targetRoot; compatibilityJunction=$sourceRoot; sameVolumeDirectoryMove=$true; copiedProject=$false; oldAppProjectPathRetained=$true; date=(Get-Date).ToString('o') }
$receipt | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $targetRoot 'Logs/ProjectRoot-20261026/move.json') -Encoding UTF8
$receipt | ConvertTo-Json
