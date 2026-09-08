# 켜져 있는 유니티 에디터에 명령을 보낸다.
#   .\tools\bridge.ps1 SceneBuilder.BuildPlayground
# 에디터가 꺼져 있으면 아무 일도 일어나지 않는다. 그때는 tools\unity.ps1 을 쓴다.
param([Parameter(Mandatory = $true)][string]$Method)

$root = Split-Path -Parent $PSScriptRoot
$id = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds().ToString()

Set-Content -Path (Join-Path $root "tools\claude_cmd.txt") -Value @($id, $Method) -Encoding utf8
Write-Host "sent id=$id method=$Method"
