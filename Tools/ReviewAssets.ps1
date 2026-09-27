$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$player = Join-Path $projectRoot 'Builds\Windows\OperationSindoor.exe'
$result = Join-Path $projectRoot 'Artifacts\asset-review-validation.txt'
$log = Join-Path $projectRoot 'Artifacts\asset-review.log'
if (Test-Path -LiteralPath $result) { Remove-Item -LiteralPath $result }
# Visible rendering is required for the requested visual and GPU validation.
$arguments = @('--asset-review','-screen-fullscreen','0','-screen-width','1600','-screen-height','900','-logFile',('"' + $log + '"'))
$process = Start-Process -FilePath $player -ArgumentList $arguments -WindowStyle Normal -PassThru
$process.WaitForExit()
$process.Refresh()
if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $result)) { throw 'Asset review failed; inspect Artifacts/asset-review.log.' }
$content = Get-Content -LiteralPath $result -Raw
if ($content -notmatch 'ASSET_REVIEW_PASS') { throw $content }
Write-Output $content
