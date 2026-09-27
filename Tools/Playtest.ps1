param([switch]$ShowWindow)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$player = Join-Path $projectRoot 'Builds\Windows\OperationSindoor.exe'
$log = Join-Path $projectRoot 'Artifacts\player-test.log'
if (-not (Test-Path -LiteralPath $player)) { throw 'Build Windows first with Tools/Build.ps1.' }
New-Item -ItemType Directory -Force -Path (Join-Path $projectRoot 'Artifacts') | Out-Null
$resultPath = Join-Path $projectRoot 'Artifacts\campaign-validation.txt'
if (Test-Path -LiteralPath $resultPath) { Remove-Item -LiteralPath $resultPath }
$arguments = @('--campaign-test','-screen-fullscreen','0','-screen-width','1600','-screen-height','900','-logFile',('"' + $log + '"'))
$windowStyle = 'Hidden'
if ($ShowWindow) { $windowStyle = 'Normal' } else { $arguments += '--no-captures' }
$testProcess = Start-Process -FilePath $player -ArgumentList $arguments -WindowStyle $windowStyle -PassThru
$testProcess.WaitForExit()
$testProcess.Refresh()
if ($testProcess.ExitCode -ne 0) { throw "Campaign validation failed: $($testProcess.ExitCode)" }
if (-not (Test-Path -LiteralPath $resultPath) -or (Get-Content -LiteralPath $resultPath -Raw) -notmatch 'CAMPAIGN_TEST_PASS') { throw 'Player did not produce a passing campaign report. See Artifacts/player-test.log.' }
Get-Content -LiteralPath $resultPath
