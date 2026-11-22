# OPS-T-03 — build/run staging-like stack on loopback. No VPS. No docker compose down -v.
# Evidence level: Local Runtime Validated (lab). NOT Staging Runtime Validated (public HTTPS).
param(
    [switch]$SkipBuild,
    [switch]$LeaveRunning
)
$ErrorActionPreference = 'Stop'
$staging = (Resolve-Path (Join-Path $PSScriptRoot '..\staging')).Path
$failed = 0
function Fail([string]$m) { Write-Host "FAIL: $m" -ForegroundColor Red; $script:failed++ }
function Pass([string]$m) { Write-Host "PASS: $m" -ForegroundColor Green }
function Note([string]$m) { Write-Host "NOTE: $m" -ForegroundColor Yellow }

$envFile = Join-Path $staging '.env.staging'
$example = Join-Path $staging '.env.staging.example'
if (-not (Test-Path $envFile)) {
    Copy-Item $example $envFile
    # Ensure JWT key length for local lab only (gitignored file).
    $txt = Get-Content -Raw $envFile
    $txt = $txt -replace 'REPLACE_STAGING_LOCAL_KEY_MIN_32_CHARS_____', 'stg-local-lab-signing-key-32chars-min!!'
    Set-Content -Path $envFile -Value $txt -NoNewline
    Note 'created gitignored .env.staging from example (lab secrets only)'
}

Push-Location $staging
try {
    Write-Host '=== compose config ===' -ForegroundColor Cyan
    docker compose --env-file .env.staging -f docker-compose.staging.yml config | Out-Null
    if ($LASTEXITCODE -ne 0) { Fail 'compose config failed'; exit 1 }
    Pass 'docker compose config'

    if (-not $SkipBuild) {
        Write-Host '=== build API image ===' -ForegroundColor Cyan
        docker compose --env-file .env.staging -f docker-compose.staging.yml build api
        if ($LASTEXITCODE -ne 0) { Fail 'API image build failed'; exit 1 }
        Pass 'API image build'
    }

    Write-Host '=== up (no -v) ===' -ForegroundColor Cyan
    docker compose --env-file .env.staging -f docker-compose.staging.yml up -d
    if ($LASTEXITCODE -ne 0) { Fail 'compose up failed'; exit 1 }
    Pass 'compose up -d'

    $deadline = (Get-Date).AddMinutes(4)
    $apiHealthy = $false
    $proxyUp = $false
    while ((Get-Date) -lt $deadline) {
        $api = docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' smartsecurity-stg-api 2>$null
        $pg = docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' smartsecurity-stg-postgres 2>$null
        $proxy = docker inspect --format '{{.State.Status}}' smartsecurity-stg-proxy 2>$null
        if ($api -eq 'healthy' -and $pg -eq 'healthy' -and $proxy -eq 'running') {
            $apiHealthy = $true
            $proxyUp = $true
            break
        }
        Start-Sleep -Seconds 5
    }
    if (-not $apiHealthy) { Fail "API/postgres not healthy in time (api=$api pg=$pg proxy=$proxy)" }
    else { Pass 'postgres + api healthy; proxy running' }

    # API must not be published on host (Forwarded Headers spoofing guard).
    $apiPorts = docker port smartsecurity-stg-api 2>$null
    if ([string]::IsNullOrWhiteSpace($apiPorts)) { Pass 'API has no host port publish' }
    else { Fail "API should not publish host ports; got: $apiPorts" }

    Write-Host '=== HTTPS lab probes via Caddy (tls internal) ===' -ForegroundColor Cyan
    # Local test cert only — NOT a substitute for public H1–H7.
    $liveCode = ''
    for ($i = 0; $i -lt 12; $i++) {
        # SNI must be a hostname. Windows Schannel does not send SNI for raw IP URLs.
        $liveCode = curl.exe --resolve localhost:8443:127.0.0.1 -sk --http1.1 -o NUL -w '%{http_code}' --max-time 10 https://localhost:8443/health/live
        if ($liveCode -eq '200') { break }
        Start-Sleep -Seconds 3
    }
    if ($liveCode -eq '200') { Pass '/health/live HTTP 200 via local HTTPS proxy' }
    else { Fail "/health/live unexpected code=$liveCode" }

    $codeReady = curl.exe --resolve localhost:8443:127.0.0.1 -sk --http1.1 -o NUL -w '%{http_code}' --max-time 15 https://localhost:8443/health/ready
    if ($codeReady -eq '200') { Pass '/health/ready HTTP 200 via local HTTPS proxy' }
    else { Fail "/health/ready code=$codeReady" }

    $redir = curl.exe -s -o NUL -w '%{http_code}' --max-time 10 http://127.0.0.1:8080/health/live
    if ($redir -eq '308' -or $redir -eq '301' -or $redir -eq '302') { Pass "HTTP->HTTPS redirect code=$redir" }
    else { Note "HTTP redirect code=$redir (non-fatal for lab)" }

    Write-Host '=== volume persistence + restart ===' -ForegroundColor Cyan
    docker exec smartsecurity-stg-postgres psql -U ss_stg -d esp32_security_staging -c "CREATE TABLE IF NOT EXISTS ops_t03_probe(id int PRIMARY KEY); INSERT INTO ops_t03_probe(id) VALUES (42) ON CONFLICT DO NOTHING;" | Out-Null
    if ($LASTEXITCODE -ne 0) { Fail 'failed to write probe row' }
    else { Pass 'wrote ops_t03_probe marker row' }

    docker compose --env-file .env.staging -f docker-compose.staging.yml restart api postgres proxy
    if ($LASTEXITCODE -ne 0) { Fail 'compose restart failed' }
    else { Pass 'compose restart api/postgres/proxy' }

    $deadline2 = (Get-Date).AddMinutes(3)
    while ((Get-Date) -lt $deadline2) {
        $api2 = docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' smartsecurity-stg-api 2>$null
        $pg2 = docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' smartsecurity-stg-postgres 2>$null
        if ($api2 -eq 'healthy' -and $pg2 -eq 'healthy') { break }
        Start-Sleep -Seconds 4
    }
    $api2 = docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' smartsecurity-stg-api 2>$null
    if ($api2 -ne 'healthy') { Fail "API not healthy after restart: $api2" }
    else { Pass 'API healthy after restart' }

    $count = docker exec smartsecurity-stg-postgres psql -U ss_stg -d esp32_security_staging -tAc "SELECT count(*) FROM ops_t03_probe WHERE id=42;"
    if (($count | ForEach-Object { $_.Trim() }) -eq '1') { Pass 'postgres volume retained probe row after restart' }
    else { Fail "probe row missing after restart; count='$count'" }

    $codeReady2 = curl.exe --resolve localhost:8443:127.0.0.1 -sk --http1.1 -o NUL -w '%{http_code}' --max-time 15 https://localhost:8443/health/ready
    if ($codeReady2 -eq '200') { Pass 'ready still 200 after restart via HTTPS' }
    else { Fail "ready after restart code=$codeReady2" }

    if (-not $LeaveRunning) {
        Write-Host '=== stop without deleting volumes ===' -ForegroundColor Cyan
        docker compose --env-file .env.staging -f docker-compose.staging.yml down
        if ($LASTEXITCODE -ne 0) { Fail 'compose down failed' }
        else { Pass 'compose down (volumes preserved — no -v)' }

        $vol = docker volume inspect smartsecurity_stg_pgdata --format '{{.Name}}' 2>$null
        if ($vol -eq 'smartsecurity_stg_pgdata') { Pass 'smartsecurity_stg_pgdata still exists' }
        else { Fail 'staging postgres volume missing after down' }
    } else {
        Note 'LeaveRunning set — stack left up; volumes untouched'
    }
}
finally {
    Pop-Location
}

Note 'Public staging H1–H7 HTTPS Cookie/CSRF/CORS remain NOT VERIFIED (DEP-OPS-03 / DEP-WEB-01).'
if ($failed -gt 0) {
    Write-Host "Staging local runtime FAILED ($failed)" -ForegroundColor Red
    exit 1
}
Write-Host 'Staging local runtime PASSED (Local Runtime Validated — not public Staging Validated).' -ForegroundColor Green
exit 0
