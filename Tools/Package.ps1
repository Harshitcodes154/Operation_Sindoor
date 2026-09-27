$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$buildRoot = Join-Path $projectRoot 'Builds\Windows'
$resultPath = Join-Path $projectRoot 'Artifacts\build-result.txt'
$testPath = Join-Path $projectRoot 'Artifacts\campaign-validation.txt'
if (-not (Test-Path -LiteralPath $resultPath) -or (Get-Content -LiteralPath $resultPath -Raw) -notmatch '^Succeeded') { throw 'Build Windows successfully before packaging.' }
if (-not (Test-Path -LiteralPath $testPath) -or (Get-Content -LiteralPath $testPath -Raw) -notmatch 'CAMPAIGN_TEST_PASS') { throw 'Run Tools/Playtest.ps1 successfully before packaging.' }
if ((Get-Item -LiteralPath $testPath).LastWriteTimeUtc -lt (Get-Item -LiteralPath $resultPath).LastWriteTimeUtc) { throw 'The campaign test predates the build. Run it again before packaging.' }
foreach ($validation in @(@('asset-review-validation.txt','ASSET_REVIEW_PASS'), @('gameplay-preservation.txt','GAMEPLAY_PRESERVATION_PASS'))) {
    $path = Join-Path $projectRoot ('Artifacts\' + $validation[0])
    if (-not (Test-Path -LiteralPath $path) -or (Get-Content -LiteralPath $path -Raw) -notmatch $validation[1]) { throw ('Required validation is missing or failed: ' + $validation[0]) }
    if ((Get-Item -LiteralPath $path).LastWriteTimeUtc -lt (Get-Item -LiteralPath $resultPath).LastWriteTimeUtc) { throw ('Validation predates build: ' + $validation[0]) }
}
$archivePath = Join-Path $projectRoot 'Builds\OperationSindoor-Windows.zip'
$tempArchive = Join-Path $projectRoot 'Builds\OperationSindoor-Windows.pending.zip'
Add-Type -AssemblyName System.IO.Compression
$stream = [System.IO.File]::Open($tempArchive, [System.IO.FileMode]::Create)
$archive = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Create)
$entries = [System.Collections.Generic.List[object]]::new()
try {
    $files = @(Get-ChildItem -LiteralPath $buildRoot -File -Recurse | Where-Object { $_.FullName -notmatch '_BackUpThisFolder_ButDontShipItWithYourGame[\\/]' -and $_.Extension -ne '.pdb' })
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($buildRoot.Length + 1).Replace('\', '/')
        $entries.Add(@{ Source = $file.FullName; Name = $relative })
    }
    foreach ($name in @('README.md', 'DEVELOPMENT_NOTES.md', 'ASSET_LICENSES.md', 'ATTRIBUTIONS.md')) {
        $entries.Add(@{ Source = (Join-Path $projectRoot $name); Name = $name })
    }
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Documentation') -File -Recurse) {
        $entries.Add(@{ Source = $file.FullName; Name = $file.FullName.Substring($projectRoot.Length + 1).Replace('\', '/') })
    }
    foreach ($name in @('build-result.txt', 'editor-validation.txt', 'campaign-validation.txt', 'visual-asset-validation.txt', 'asset-review-validation.txt', 'gameplay-preservation.txt')) {
        $entries.Add(@{ Source = (Join-Path $projectRoot "Artifacts\$name"); Name = "Validation/$name" })
    }
    foreach ($item in $entries) {
        $entry = $archive.CreateEntry('OperationSindoor/' + $item.Name, [System.IO.Compression.CompressionLevel]::Optimal)
        $inputStream = [System.IO.File]::OpenRead($item.Source)
        $outputStream = $entry.Open()
        try { $inputStream.CopyTo($outputStream) } finally { $outputStream.Dispose(); $inputStream.Dispose() }
    }
} finally { $archive.Dispose(); $stream.Dispose() }
# Verify required runtime files are actually in the archive before replacing a release.
$verify = [System.IO.Compression.ZipFile]::OpenRead($tempArchive)
try {
    foreach ($name in @('OperationSindoor.exe', 'UnityPlayer.dll', 'OperationSindoor_Data/Managed/Assembly-CSharp.dll', 'README.md')) {
        if ($null -eq $verify.GetEntry('OperationSindoor/' + $name)) { throw "Archive is missing $name" }
    }
} finally { $verify.Dispose() }
Move-Item -LiteralPath $tempArchive -Destination $archivePath -Force
$hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash
Set-Content -LiteralPath ($archivePath + '.sha256') -Value ($hash + '  OperationSindoor-Windows.zip')
Get-Item -LiteralPath $archivePath | Select-Object FullName, Length
Write-Output "SHA256: $hash"
