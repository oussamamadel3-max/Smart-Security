# OPS-T-02 — fail if tracked secrets or empty-policy violations are found.
# Exit 0 = PASS. Does not print secret values.
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$opsRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$failed = 0

function Fail([string]$msg) {
    Write-Host "FAIL: $msg" -ForegroundColor Red
    $script:failed++
}
function Pass([string]$msg) {
    Write-Host "PASS: $msg" -ForegroundColor Green
}

Push-Location $repoRoot
try {
    $trackedEnv = git ls-files -- 'ops/.env' '**/.env' 2>$null
    if ($trackedEnv) { Fail "Tracked .env files: $trackedEnv" } else { Pass 'No tracked .env files' }

    $trackedPasswd = git ls-files -- 'ops/mosquitto/passwd' 2>$null
    if ($trackedPasswd) { Fail 'ops/mosquitto/passwd is tracked' } else { Pass 'mosquitto/passwd not tracked' }

    $trackedKeys = git ls-files -- 'ops/tls/**/*.key' 'ops/tls/**/*.pem' 'ops/tls/**/*.pfx' 'ops/tls/**/*.p12' 2>$null
    if ($trackedKeys) { Fail "Tracked TLS private material: $trackedKeys" } else { Pass 'No tracked TLS private keys under ops/tls' }

    $appsettings = Join-Path $repoRoot 'security_backEnd\API\appsettings.json'
    $json = Get-Content -Raw -Path $appsettings | ConvertFrom-Json
    $signingKey = [string]$json.Auth.Jwt.SigningKey
    if (-not [string]::IsNullOrWhiteSpace($signingKey)) {
        Fail 'Auth:Jwt:SigningKey in appsettings.json is not empty'
    } else {
        Pass 'Auth:Jwt:SigningKey empty in committed appsettings.json'
    }

    $mqttInbound = [bool]$json.Mqtt.Inbound.Enabled
    $mqttLive = [bool]$json.Mqtt.Outbound.LivePublishEnabled
    $mqttOutbox = [bool]$json.Mqtt.Outbox.Enabled
    if ($mqttInbound -or $mqttLive -or $mqttOutbox) {
        Fail 'Production MQTT paths enabled in committed appsettings.json'
    } else {
        Pass 'Committed appsettings keeps MQTT Inbound/LivePublish/Outbox disabled'
    }

    # Scan example env files for disallowed high-entropy JWT-looking blobs / eyJ tokens
    $exampleFiles = Get-ChildItem -Path $opsRoot -Recurse -File -Include *.example,*.example.json,*.example.md,*.md |
        Where-Object { $_.FullName -match '\\environments\\|\\docs\\' }
    foreach ($f in $exampleFiles) {
        $text = Get-Content -Raw -Path $f.FullName
        if ($text -match 'eyJ[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{10,}') {
            Fail "Possible JWT blob in $($f.FullName.Substring($repoRoot.Length))"
        }
    }
    Pass 'No JWT-shaped blobs in ops environments/docs examples'

    # Ensure .gitignore covers critical paths
    $gi = Get-Content (Join-Path $opsRoot '.gitignore') -Raw
    foreach ($pat in @('.env', 'mosquitto/passwd', 'tls/server/')) {
        if ($gi -notmatch [regex]::Escape($pat)) { Fail ".gitignore missing pattern hint: $pat" }
    }
    Pass 'ops/.gitignore covers .env, passwd, tls/server/'
}
finally {
    Pop-Location
}

if ($failed -gt 0) {
    Write-Host "Secrets hygiene FAILED ($failed)" -ForegroundColor Red
    exit 1
}
Write-Host 'Secrets hygiene PASSED' -ForegroundColor Green
exit 0
