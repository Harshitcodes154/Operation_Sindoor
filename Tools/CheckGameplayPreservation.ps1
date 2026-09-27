$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$baseline = Join-Path $root 'Artifacts\AssetUpgradeBaseline'
if (-not (Test-Path -LiteralPath $baseline)) { throw 'The pre-upgrade baseline is required for this local comparison.' }
function Read-Method([string]$source, [string]$method) {
    $match = [regex]::Match($source, '\b(?:void|bool|float|int|Contact|Vector3)\s+' + [regex]::Escape($method) + '\s*\([^)]*\)\s*(?:=>[^;]*;|\{)')
    if (-not $match.Success) { throw "Cannot find method $method" }
    if ($match.Value.Contains('=>')) { return $match.Value }
    $start = $match.Index; $brace = $source.IndexOf('{', $start); $depth = 1; $i = $brace + 1
    while ($depth -gt 0 -and $i -lt $source.Length) {
        if ($source[$i] -eq '{') { $depth++ }; if ($source[$i] -eq '}') { $depth-- }; $i++
    }
    if ($depth -ne 0) { throw "Unbalanced method $method" }
    return $source.Substring($start, $i - $start)
}
$checks = @{
    'OperationGame.cs' = @('BeginMission','StartFlight','SetStage','TickMission','CompleteMission','Fail','Resume','ToMenu','Radio','TrySave','ClearCombat','ApplySettings','SetWeather')
    'FlightCombat.cs' = @('TickFlight','GroundHeight','ClosestTarget','SelectTarget','FireMissile','FireCannon','Countermeasures','TickMissiles','DistanceToSegment','SpawnOne','SpawnFriendly','SpawnThreats','TickContacts','DamageContact','DamagePlayer','UpdateCamera','CaptureCheckpoint','RestoreCheckpoint')
    'WorldFactory.cs' = @('Height')
}
$lines = [System.Collections.Generic.List[string]]::new()
foreach ($file in $checks.Keys) {
    $before = Get-Content -LiteralPath (Join-Path $baseline $file) -Raw
    $after = Get-Content -LiteralPath (Join-Path $root "Assets\Scripts\$file") -Raw
    foreach ($method in $checks[$file]) {
        if ((Read-Method $before $method) -cne (Read-Method $after $method)) { throw "Gameplay changed: $file / $method" }
        $lines.Add("PASS: $file / $method unchanged")
    }
}
foreach ($file in @('GameHUD.cs','GameUI.cs','CinematicDirector.cs')) {
    $before = Get-FileHash -LiteralPath (Join-Path $baseline $file)
    $after = Get-FileHash -LiteralPath (Join-Path $root "Assets\Scripts\$file")
    if ($before.Hash -ne $after.Hash) { throw "Protected presentation functionality changed: $file" }
    $lines.Add("PASS: $file unchanged")
}
$beforeData = (Get-Content -LiteralPath (Join-Path $baseline 'GameData.cs') -Raw).Split(@('public sealed class AudioDirector'), [System.StringSplitOptions]::None)[0]
$afterData = (Get-Content -LiteralPath (Join-Path $root 'Assets\Scripts\GameData.cs') -Raw).Split(@('public sealed class AudioDirector'), [System.StringSplitOptions]::None)[0]
if ($beforeData -cne $afterData) { throw 'Mission definitions, saves or controls changed' }
$lines.Add('PASS: mission definitions, settings, save format and input bindings unchanged')
$lines.Add('GAMEPLAY_PRESERVATION_PASS')
$lines | Set-Content -LiteralPath (Join-Path $root 'Artifacts\gameplay-preservation.txt')
$lines
