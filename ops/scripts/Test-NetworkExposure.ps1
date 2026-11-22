# OPS-T-03 — assert Compose published ports stay on loopback (dev + staging templates).
$ErrorActionPreference = 'Stop'
$ops = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$failed = 0
function Fail([string]$m) { Write-Host "FAIL: $m" -ForegroundColor Red; $script:failed++ }
function Pass([string]$m) { Write-Host "PASS: $m" -ForegroundColor Green }

function Assert-LoopbackBinds([string]$file) {
    $name = [IO.Path]::GetFileName($file)
    $loopback = @(Select-String -Path $file -Pattern '"127\.0\.0\.1:[^"]+"' |
        ForEach-Object { $_.Matches[0].Value.Trim('"') })
    $bad = @(Select-String -Path $file -Pattern '"(0\.0\.0\.0:[^"]+|[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+:[^"]+)"' |
        ForEach-Object { $_.Matches[0].Value.Trim('"') } |
        Where-Object { $_ -notmatch '^127\.0\.0\.1:' })

    if ($loopback.Count -eq 0) { Fail "${name}: no 127.0.0.1 port publishes found"; return }
    foreach ($b in $bad) { Fail "${name} non-loopback publish: $b" }
    if ($bad.Count -eq 0) { Pass "${name}: $($loopback.Count) loopback publish(es), 0 public binds" }
}

Assert-LoopbackBinds (Join-Path $ops 'docker-compose.yml')
Assert-LoopbackBinds (Join-Path $ops 'staging\docker-compose.staging.yml')

foreach ($cname in @('smartsecurity-postgres', 'smartsecurity-mosquitto')) {
    docker inspect $cname 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) { Write-Host "SKIP: $cname not running"; continue }
    $bindings = docker inspect --format '{{json .HostConfig.PortBindings}}' $cname | ConvertFrom-Json
    foreach ($prop in $bindings.PSObject.Properties) {
        foreach ($b in @($prop.Value)) {
            if ($b.HostIp -and $b.HostIp -ne '127.0.0.1' -and $b.HostIp -ne '') {
                Fail "$cname HostIp=$($b.HostIp) for $($prop.Name)"
            }
        }
    }
    Pass "$cname runtime PortBindings HostIp loopback/empty-ok"
}

if ($failed -gt 0) { Write-Host "Network exposure FAILED ($failed)" -ForegroundColor Red; exit 1 }
Write-Host 'Network exposure PASSED' -ForegroundColor Green
exit 0
