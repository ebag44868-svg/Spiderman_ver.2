# P0 완료 기준 확인. 컴파일 + 프로젝트 무결성.
& (Join-Path $PSScriptRoot "unity.ps1") -Method BuildTools.Verify -Log verify.log
exit $LASTEXITCODE
