# OPS-T-03 — validate staging compose templates (no destructive actions).
$ErrorActionPreference = 'Stop'
$staging = (Resolve-Path (Join-Path $PSScriptRoot '..\staging')).Path
$failed = 0
function Fail([string]$m) { Write-Host "FAIL: $m" -ForegroundColor Red; $script:failed++ }
function Pass([string]$m) { Write-Host "PASS: $m" -ForegroundColor Green }

$compose = Join-Path $staging 'docker-compose.staging.yml'
$envExample = Join-Path $staging '.env.staging.example'
if (-not (Test-Path $compose)) { Fail 'docker-compose.staging.yml missing'; exit 1 }
if (-not (Test-Path $envExample)) { Fail '.env.staging.example missing' }

$raw = Get-Content -Raw $compose
if ($raw -notmatch '127\.0\.0\.1:') { Fail 'staging compose must bind published ports to 127.0.0.1' }
else { Pass 'published ports bound to 127.0.0.1' }

if ($raw -match '0\.0\.0\.0:') { Fail 'staging compose must not bind 0.0.0.0' }
else { Pass 'no 0.0.0.0 port binds' }

if ($raw -notmatch 'smartsecurity_stg_pgdata') { Fail 'staging must use separate postgres volume name' }
else { Pass 'separate staging postgres volume' }

if ($raw -match 'smartsecurity_pgdata[^\w]') { Fail 'staging compose must not reuse OPS-T-01 volume smartsecurity_pgdata' }
else { Pass 'does not reuse OPS-T-01 pg volume name' }

foreach ($needle in @('Mqtt__Inbound__Enabled: "false"', 'Mqtt__Outbound__LivePublishEnabled: "false"', 'Mqtt__Outbox__Enabled: "false"')) {
    if ($raw -notmatch [regex]::Escape($needle)) { Fail "API service missing hard-disable: $needle" }
}
Pass 'API MQTT/Outbox hard-disabled in compose'

if ($raw -notmatch 'health/live') { Fail 'API healthcheck should probe /health/live' }
else { Pass 'API healthcheck uses /health/live' }

if ($raw -notmatch 'ForwardedHeaders__TrustPrivateNetworkProxies') {
    Fail 'staging compose must set ForwardedHeaders__TrustPrivateNetworkProxies'
} else { Pass 'ForwardedHeaders trust flag present' }

if ($raw -match '(?m)^\s+api:[\s\S]*?(?=\n  [a-z])') {
    $apiBlock = [regex]::Match($raw, '(?ms)^  api:\r?\n.*?(?=\r?\n  [a-z]|\r?\nnetworks:)').Value
    if ($apiBlock -match '(?m)^\s+ports:\s*$') { Fail 'API must not declare host ports by default' }
    else { Pass 'API has no default host ports (proxy-only)' }
}

$envTxt = Get-Content -Raw $envExample
if ($envTxt -notmatch 'LivePublishEnabled=false' -or $envTxt -notmatch 'Outbox__Enabled=false') {
    Fail '.env.staging.example must keep MQTT publish/outbox false'
} else { Pass '.env.staging.example keeps MQTT disabled' }

if ($envTxt -match 'eyJ[A-Za-z0-9_-]+\.') { Fail '.env.staging.example contains JWT-like secret' }
else { Pass 'no JWT blob in staging env example' }

# docker compose config (requires env file — use example copy in temp)
$tmpEnv = Join-Path $env:TEMP ("ops-stg-env-" + [guid]::NewGuid().ToString('N') + '.env')
try {
    Copy-Item $envExample $tmpEnv
    # Satisfy required vars with placeholders already in example
    Push-Location $staging
    docker compose --env-file $tmpEnv -f docker-compose.staging.yml config 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { Fail 'docker compose config failed for staging file' }
    else { Pass 'docker compose config OK' }
}
finally {
    Pop-Location -ErrorAction SilentlyContinue
    Remove-Item -Force $tmpEnv -ErrorAction SilentlyContinue
}

foreach ($f in @('Dockerfile.api', 'Caddyfile', 'SERVICES.md', 'ROLLBACK.md', 'HTTPS-ACCEPTANCE.md', 'README.md')) {
    if (-not (Test-Path (Join-Path $staging $f))) { Fail "missing $f" } else { Pass "$f present" }
}

if ($failed -gt 0) { Write-Host "Staging compose config FAILED ($failed)" -ForegroundColor Red; exit 1 }
Write-Host 'Staging compose config PASSED' -ForegroundColor Green
exit 0
