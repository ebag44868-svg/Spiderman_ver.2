# Unity 배치모드 공통 러너.
#   .\tools\unity.ps1 BuildTools.Verify
#   .\tools\unity.ps1 SceneBuilder.BuildPlayground -Log scene.log
param(
    [Parameter(Mandatory = $true)][string]$Method,
    [string]$Log = "batch.log"
)
$ErrorActionPreference = "Stop"
$unity = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"
$proj  = Split-Path -Parent $PSScriptRoot
$logPath = Join-Path $proj "tools\$Log"

if (Test-Path $logPath) { Remove-Item $logPath }
& $unity -batchmode -nographics -projectPath $proj -executeMethod $Method -logFile $logPath
$code = $LASTEXITCODE

if (Test-Path $logPath) {
    Write-Host "----- log tail -----"
    Get-Content $logPath -Tail 40
}
Write-Host "----- exit code: $code -----"
exit $code
