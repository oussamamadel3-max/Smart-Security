# OPS-T-03 acceptance runner (local/config + optional local runtime). Does not deploy to external VPS.
param(
    [switch]$SkipDevStackRegression,
    [switch]$IncludeLocalRuntime,
    [switch]$LeaveStagingRunning
)
$ErrorActionPreference = 'Stop'
$ops = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $ops

Write-Host '=== OPS-T-03: staging compose config ===' -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'Test-StagingComposeConfig.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '=== OPS-T-03: network exposure ===' -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'Test-NetworkExposure.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '=== OPS-T-03: HTTPS configuration checklist ===' -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'Test-HttpsSecurityChecklist.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '=== OPS-T-03: secrets hygiene (regression) ===' -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'Test-SecretsHygiene.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $SkipDevStackRegression) {
    Write-Host '=== OPS-T-03: OPS-T-01 stack health regression ===' -ForegroundColor Cyan
    if (Test-Path (Join-Path $ops '.env')) {
        $pg = docker inspect --format '{{.State.Health.Status}}' smartsecurity-postgres 2>$null
        $mq = docker inspect --format '{{.State.Health.Status}}' smartsecurity-mosquitto 2>$null
        if ($pg -ne 'healthy' -or $mq -ne 'healthy') {
            Write-Host "FAIL: expected healthy dev stack; pg=$pg mq=$mq" -ForegroundColor Red
            exit 1
        }
        Write-Host 'PASS: OPS-T-01 postgres+mosquitto healthy' -ForegroundColor Green
    } else {
        Write-Host 'SKIP: ops/.env missing' -ForegroundColor Yellow
    }
}

if ($IncludeLocalRuntime) {
    Write-Host '=== OPS-T-03: staging local runtime (build/up/health/restart) ===' -ForegroundColor Cyan
    $rtArgs = @()
    if ($LeaveStagingRunning) { $rtArgs += '-LeaveRunning' }
    & (Join-Path $PSScriptRoot 'Test-StagingLocalRuntime.ps1') @rtArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host 'OPS-T-03 local/config validation suite completed.' -ForegroundColor Green
Write-Host 'SOFTWARE / Local Runtime ≠ public Staging Runtime Validated.' -ForegroundColor Yellow
Write-Host 'H1–H7 on public staging host remain NOT VERIFIED (DEP-OPS-03 / DEP-WEB-01).' -ForegroundColor Yellow
exit 0
