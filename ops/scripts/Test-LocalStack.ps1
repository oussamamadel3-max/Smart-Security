# OPS-T-01 local stack validation: compose config, health, postgres, volume persistence, ACL.
# Does not enable Backend MQTT production flags. Does not print secrets.

[CmdletBinding()]
param(
    [switch]$SkipAcl,
    [switch]$SkipRestart
)

$ErrorActionPreference = "Stop"
$opsRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
Set-Location $opsRoot
New-Item -ItemType Directory -Force -Path (Join-Path $opsRoot "tmp") | Out-Null

function Read-DotEnv {
    param([string]$Path)
    $map = @{}
    Get-Content $Path | ForEach-Object {
        $line = $_.Trim()
        if (-not $line -or $line.StartsWith("#")) { return }
        $idx = $line.IndexOf("=")
        if ($idx -lt 1) { return }
        $map[$line.Substring(0, $idx).Trim()] = $line.Substring($idx + 1).Trim()
    }
    return $map
}

if (-not (Test-Path (Join-Path $opsRoot ".env"))) {
    throw "Missing ops/.env — run .\scripts\Bootstrap-LocalStack.ps1 first"
}
$envMap = Read-DotEnv (Join-Path $opsRoot ".env")
$pgUser = $envMap["POSTGRES_USER"]
$pgDb = $envMap["POSTGRES_DB"]
$pgPass = $envMap["POSTGRES_PASSWORD"]
$pgPort = if ($envMap["POSTGRES_HOST_PORT"]) { $envMap["POSTGRES_HOST_PORT"] } else { "5433" }

Write-Host "== compose config =="
docker compose --env-file .env config --quiet
if ($LASTEXITCODE -ne 0) { throw "docker compose config failed" }
Write-Host "PASS compose config"

Write-Host "== up -d =="
docker compose --env-file .env up -d
if ($LASTEXITCODE -ne 0) { throw "docker compose up failed" }

Write-Host "== wait healthy =="
$deadline = (Get-Date).AddMinutes(2)
do {
    Start-Sleep -Seconds 3
    $ps = docker compose --env-file .env ps --format json 2>$null
    $healthy = $false
    try {
        $rows = $ps | ConvertFrom-Json
        if ($rows -isnot [array]) { $rows = @($rows) }
        $healthy = ($rows.Count -ge 2) -and ($rows | Where-Object { $_.Health -and $_.Health -ne "healthy" }).Count -eq 0 -and ($rows | Where-Object { $_.Health -eq "healthy" }).Count -ge 2
    } catch {
        $healthy = $false
    }
    $plain = docker compose --env-file .env ps
    Write-Host $plain
    if ($healthy) { break }
} while ((Get-Date) -lt $deadline)

$status = docker inspect --format "{{.State.Health.Status}}" smartsecurity-postgres 2>$null
$mstatus = docker inspect --format "{{.State.Health.Status}}" smartsecurity-mosquitto 2>$null
Write-Host "postgres health=$status mosquitto health=$mstatus"
if ($status -ne "healthy") { throw "postgres not healthy" }
if ($mstatus -ne "healthy") { throw "mosquitto not healthy" }
Write-Host "PASS healthchecks"

Write-Host "== postgres auth/query =="
$marker = "ops_t01_" + [guid]::NewGuid().ToString("N").Substring(0, 8)
docker exec -e PGPASSWORD=$pgPass smartsecurity-postgres `
    psql -U $pgUser -d $pgDb -v ON_ERROR_STOP=1 -c "CREATE TABLE IF NOT EXISTS ops_t01_smoke(id text primary key, created_at timestamptz default now()); DELETE FROM ops_t01_smoke WHERE id like 'ops_t01_%'; INSERT INTO ops_t01_smoke(id) VALUES ('$marker');"
if ($LASTEXITCODE -ne 0) { throw "postgres write failed" }

# Host port connectivity (127.0.0.1:$pgPort) via dockerised psql client
docker run --rm -e PGPASSWORD=$pgPass postgres:16-alpine `
    psql -h host.docker.internal -p $pgPort -U $pgUser -d $pgDb -c "SELECT id FROM ops_t01_smoke WHERE id='$marker';" | Out-Host
if ($LASTEXITCODE -ne 0) { throw "postgres host-port connect failed" }
Write-Host "PASS postgres connect"

if (-not $SkipRestart) {
    Write-Host "== restart + volume persistence =="
    docker compose --env-file .env restart
    if ($LASTEXITCODE -ne 0) { throw "restart failed" }
    Start-Sleep -Seconds 8
    $deadline2 = (Get-Date).AddMinutes(2)
    do {
        Start-Sleep -Seconds 3
        $status = docker inspect --format "{{.State.Health.Status}}" smartsecurity-postgres
        $mstatus = docker inspect --format "{{.State.Health.Status}}" smartsecurity-mosquitto
        if ($status -eq "healthy" -and $mstatus -eq "healthy") { break }
    } while ((Get-Date) -lt $deadline2)
    $found = docker exec -e PGPASSWORD=$pgPass smartsecurity-postgres `
        psql -U $pgUser -d $pgDb -tAc "SELECT id FROM ops_t01_smoke WHERE id='$marker';"
    if ($found.Trim() -ne $marker) { throw "volume persistence failed after restart (expected $marker got '$found')" }
    Write-Host "PASS volume persistence after restart"
}

if (-not $SkipAcl) {
    Write-Host "== ACL matrix =="
    & (Join-Path $PSScriptRoot "Test-AclMatrix.ps1")
    if ($LASTEXITCODE -ne 0) { throw "ACL matrix failed" }
    Write-Host "PASS ACL matrix"
}

Write-Host ""
Write-Host "OPS-T-01 local stack validation completed successfully."
Write-Host "Stop without deleting volumes: docker compose --env-file .env down"
Write-Host "Destructive wipe (explicit only): docker compose --env-file .env down -v"
