param([string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$artifactRoot = Join-Path $projectRoot 'Artifacts'
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null
if (-not (Test-Path -LiteralPath $Unity)) { throw "Unity editor not found: $Unity" }
$resultPath = Join-Path $artifactRoot 'build-result.txt'
if (Test-Path -LiteralPath $resultPath) { Remove-Item -LiteralPath $resultPath }
$arguments = @('-batchmode','-nographics','-quit','-projectPath',('"' + $projectRoot + '"'),'-executeMethod','Sindoor.Editor.OperationSindoorSetupWindow.BuildWindows','-logFile',('"' + (Join-Path $artifactRoot 'build.log') + '"'))
$buildProcess = Start-Process -FilePath $Unity -ArgumentList $arguments -WindowStyle Hidden -PassThru
# Unity's shared helper processes may outlive the editor; wait for this editor, not its entire process tree.
$buildProcess.WaitForExit()
$buildProcess.Refresh()
if ($buildProcess.ExitCode -ne 0) { throw "Build failed. See Artifacts/build.log. Exit code: $($buildProcess.ExitCode)" }
if (-not (Test-Path -LiteralPath $resultPath) -or (Get-Content -LiteralPath $resultPath -Raw) -notmatch '^Succeeded') { throw 'Unity did not produce a successful build report. See Artifacts/build.log.' }
Get-Content -LiteralPath $resultPath
