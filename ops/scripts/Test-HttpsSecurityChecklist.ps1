# OPS-T-03 — configuration checklist for HTTPS/Cookie/CSRF staging templates.
# Does NOT claim Staging Runtime Validated.
$ErrorActionPreference = 'Stop'
$ops = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$failed = 0
function Fail([string]$m) { Write-Host "FAIL: $m" -ForegroundColor Red; $script:failed++ }
function Pass([string]$m) { Write-Host "PASS: $m" -ForegroundColor Green }

$web = Get-Content -Raw (Join-Path $ops 'environments\staging\web-origins.example.json') | ConvertFrom-Json
if ($web.httpsRequiredForCookieChannel -ne $true) { Fail 'staging web-origins must require HTTPS for cookies' }
else { Pass 'staging requires HTTPS for cookie channel' }

if ($web.cookie.secure -ne $true) { Fail 'staging cookie.secure must be true' }
else { Pass 'staging cookie.secure is true' }

$origins = @($web.corsAllowedOrigins)
if ($origins | Where-Object { $_ -match '^http://' }) { Fail 'staging origins must not use plain http://' }
else { Pass 'staging origins are not plain http' }

$caddy = Get-Content -Raw (Join-Path $ops 'staging\Caddyfile')
if ($caddy -match 'local_certs' -or $caddy -match 'tls\s+internal' -or $caddy -match 'tls\s') { Pass 'Caddyfile configures TLS' }
else { Fail 'Caddyfile missing tls directive' }

if ($caddy -match 'insecure_skip_verify|tls_insecure') { Fail 'Caddyfile must not skip TLS verify' }
else { Pass 'Caddyfile does not skip TLS verification' }

if ($caddy -match 'X-Forwarded-Proto') { Pass 'Caddyfile forwards X-Forwarded-Proto' }
else { Fail 'Caddyfile should set X-Forwarded-Proto for ASP.NET' }

if ($caddy -match 'header_up\s+-X-Forwarded-') { Pass 'Caddyfile strips client Forwarded headers before set' }
else { Fail 'Caddyfile should strip client-supplied X-Forwarded-* then set trusted values' }

$compose = Get-Content -Raw (Join-Path $ops 'staging\docker-compose.staging.yml')
if ($compose -match 'ForwardedHeaders__TrustPrivateNetworkProxies') { Pass 'staging compose enables TrustPrivateNetworkProxies' }
else { Fail 'staging compose missing ForwardedHeaders__TrustPrivateNetworkProxies' }

# API host publish would allow spoofing when trusting private nets — must stay commented/absent.
$apiPublish = [regex]::Match($compose, '(?ms)api:\s.*?networks:')
if ($apiPublish.Success -and ($apiPublish.Value -match '(?m)^\s+ports:\s*$')) {
    Fail 'API service must not publish host ports by default (proxy-only edge)'
} else { Pass 'API service is proxy-only (no default host ports)' }

$httpsDoc = Join-Path $ops 'staging\HTTPS-ACCEPTANCE.md'
if (-not (Test-Path $httpsDoc)) { Fail 'HTTPS-ACCEPTANCE.md missing' }
else {
    $txt = Get-Content -Raw $httpsDoc
    foreach ($id in @('H1', 'H2', 'H3', 'H4', 'H5', 'H6', 'H7')) {
        if ($txt -notmatch $id) { Fail "HTTPS-ACCEPTANCE missing scenario $id" }
    }
    Pass 'HTTPS-ACCEPTANCE lists H1–H7'
}

Write-Host 'NOTE: Live HTTPS on public staging host = NOT VERIFIED (external dependency).' -ForegroundColor Yellow

if ($failed -gt 0) { Write-Host "HTTPS checklist FAILED ($failed)" -ForegroundColor Red; exit 1 }
Write-Host 'HTTPS security checklist PASSED (configuration only)' -ForegroundColor Green
exit 0
