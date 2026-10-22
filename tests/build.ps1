$ErrorActionPreference = 'Stop'
$buildScript = Join-Path $PSScriptRoot '../build.ps1'
$powershell = (Get-Process -Id $PID).Path

# Run from a different directory to catch accidental dependence on the caller's cwd.
Push-Location $PSScriptRoot
try {
    & $powershell -NoProfile -File $buildScript
    if ($LASTEXITCODE -ne 0) { throw 'Default build failed.' }

    $missing = Join-Path $PSScriptRoot ([Guid]::NewGuid().ToString())
    $output = & $powershell -NoProfile -File $buildScript -GameDir $missing 2>&1
    if ($LASTEXITCODE -eq 0 -or "$output" -notmatch 'Required build reference missing') {
        throw 'Missing game references must fail with a useful error.'
    }

    $output = & $powershell -NoProfile -File $buildScript -RuntimeDir $missing 2>&1
    if ($LASTEXITCODE -eq 0 -or "$output" -notmatch 'Required build reference missing') {
        throw 'Missing runtime references must fail with a useful error.'
    }
    Write-Output 'PASS: build from another directory; missing game/runtime references rejected.'
}
finally {
    Pop-Location
}
