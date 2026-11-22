# OPS-T-02 acceptance runner (local). Does not enable production MQTT or mutate volumes.
param(
    [switch]$SkipLocalStackRegression,
    [switch]$SkipAcl
)
$ErrorActionPreference = 'Stop'
$opsRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $opsRoot

Write-Host '=== OPS-T-02: secrets hygiene ===' -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'Test-SecretsHygiene.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '=== OPS-T-02: environment isolation templates ===' -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'Test-EnvIsolationDocs.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $SkipLocalStackRegression) {
    Write-Host '=== OPS-T-02: OPS-T-01 local stack regression (health) ===' -ForegroundColor Cyan
    if (-not (Test-Path (Join-Path $opsRoot '.env'))) {
        Write-Host 'SKIP: ops/.env missing — cannot regress Compose health' -ForegroundColor Yellow
    } else {
        docker compose --env-file .env ps --format '{{.Name}} {{.Status}}'
        $pg = docker inspect --format '{{.State.Health.Status}}' smartsecurity-postgres 2>$null
        $mq = docker inspect --format '{{.State.Health.Status}}' smartsecurity-mosquitto 2>$null
        if ($pg -ne 'healthy' -or $mq -ne 'healthy') {
            Write-Host "FAIL: expected healthy postgres/mosquitto; got pg=$pg mq=$mq" -ForegroundColor Red
            exit 1
        }
        Write-Host 'PASS: Compose postgres + mosquitto healthy (OPS-T-01 intact)' -ForegroundColor Green

        if (-not $SkipAcl) {
            Write-Host '=== OPS-T-02: ACL matrix regression ===' -ForegroundColor Cyan
            & (Join-Path $PSScriptRoot 'Test-AclMatrix.ps1')
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        }
    }
}

Write-Host 'OPS-T-02 local validation suite completed.' -ForegroundColor Green
exit 0
