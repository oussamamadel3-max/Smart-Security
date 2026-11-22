# OPS-T-02 — validate environment isolation templates exist and are well-formed.
$ErrorActionPreference = 'Stop'
$opsRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$failed = 0

function Fail([string]$msg) { Write-Host "FAIL: $msg" -ForegroundColor Red; $script:failed++ }
function Pass([string]$msg) { Write-Host "PASS: $msg" -ForegroundColor Green }

$envs = @('development', 'staging', 'production')
foreach ($e in $envs) {
    $dir = Join-Path $opsRoot "environments\$e"
    if (-not (Test-Path $dir)) { Fail "Missing environments/$e"; continue }

    $backend = Join-Path $dir 'backend.env.example'
    if (-not (Test-Path $backend)) { Fail "Missing $e/backend.env.example" }
    else {
        $txt = Get-Content -Raw $backend
        foreach ($needle in @(
                'ASPNETCORE_ENVIRONMENT',
                'ConnectionStrings__DefaultConnection',
                'Auth__Jwt__SigningKey',
                'Auth__Jwt__Issuer',
                'Mqtt__Outbound__LivePublishEnabled=false',
                'Mqtt__Outbox__Enabled=false'
            )) {
            if ($txt -notmatch [regex]::Escape($needle)) { Fail "$e backend.env.example missing $needle" }
        }
        if ($txt -match 'eyJ[A-Za-z0-9_-]+\.') { Fail "$e backend.env.example contains JWT-like value" }
        Pass "$e/backend.env.example structure OK"
    }

    $web = Join-Path $dir 'web-origins.example.json'
    if (-not (Test-Path $web)) { Fail "Missing $e/web-origins.example.json" }
    else {
        $j = Get-Content -Raw $web | ConvertFrom-Json
        if (-not $j.corsAllowedOrigins -or $j.corsAllowedOrigins.Count -lt 1) {
            Fail "$e web-origins missing corsAllowedOrigins"
        }
        if (-not $j.cookie -or -not $j.csrf) { Fail "$e web-origins missing cookie/csrf" }
        if ($e -eq 'production' -or $e -eq 'staging') {
            if ($j.httpsRequiredForCookieChannel -ne $true) {
                Fail "$e must require HTTPS for cookie channel"
            }
            $origins = @($j.corsAllowedOrigins)
            if ($origins | Where-Object { $_ -match 'localhost' }) {
                Fail "$e cors must not include localhost"
            }
        }
        Pass "$e/web-origins.example.json OK"
    }
}

foreach ($e in @('staging', 'production')) {
    $fw = Join-Path $opsRoot "environments\$e\network-firewall.example.md"
    if (-not (Test-Path $fw)) { Fail "Missing $e/network-firewall.example.md" }
    else { Pass "$e/network-firewall.example.md present" }
}

$docs = @(
    'docs\ENV-ISOLATION.md',
    'docs\SECRETS-AND-JWT.md',
    'docs\SECRETS-CHECKLIST.md',
    'docs\WEB-SECURITY-PER-ENV.md',
    'docs\FIREWALL-AND-NETWORK.md',
    'environments\README.md'
)
foreach ($d in $docs) {
    $p = Join-Path $opsRoot $d
    if (-not (Test-Path $p)) { Fail "Missing $d" } else { Pass "$d present" }
}

# Production template must keep MQTT disabled
$prod = Get-Content -Raw (Join-Path $opsRoot 'environments\production\backend.env.example')
if ($prod -notmatch 'LivePublishEnabled=false' -or $prod -notmatch 'Inbound__Enabled=false') {
    Fail 'production template must keep MQTT disabled'
} else {
    Pass 'production template keeps MQTT disabled'
}

# Dev CORS should allow localhost; prod/staging must not share identical cookie names with each other ideally
$devWeb = Get-Content -Raw (Join-Path $opsRoot 'environments\development\web-origins.example.json') | ConvertFrom-Json
$stgWeb = Get-Content -Raw (Join-Path $opsRoot 'environments\staging\web-origins.example.json') | ConvertFrom-Json
$prdWeb = Get-Content -Raw (Join-Path $opsRoot 'environments\production\web-origins.example.json') | ConvertFrom-Json
if ($devWeb.cookie.name -eq $stgWeb.cookie.name -or $stgWeb.cookie.name -eq $prdWeb.cookie.name) {
    Fail 'cookie names should differ across environments'
} else {
    Pass 'cookie names differ across environments'
}

if ($failed -gt 0) {
    Write-Host "Env isolation docs FAILED ($failed)" -ForegroundColor Red
    exit 1
}
Write-Host 'Env isolation docs PASSED' -ForegroundColor Green
exit 0
