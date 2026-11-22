# OPS-T-01 bootstrap: .env + Mosquitto passwd + local TLS material.
# Does not start containers. Does not touch staging/production.

[CmdletBinding()]
param(
    [switch]$ForcePasswd,
    [switch]$ForceTls
)

$ErrorActionPreference = "Stop"
$opsRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
Set-Location $opsRoot

function Read-DotEnv {
    param([string]$Path)
    $map = @{}
    if (-not (Test-Path $Path)) { return $map }
    Get-Content $Path | ForEach-Object {
        $line = $_.Trim()
        if (-not $line -or $line.StartsWith("#")) { return }
        $idx = $line.IndexOf("=")
        if ($idx -lt 1) { return }
        $key = $line.Substring(0, $idx).Trim()
        $val = $line.Substring($idx + 1).Trim()
        $map[$key] = $val
    }
    return $map
}

$envExample = Join-Path $opsRoot ".env.example"
$envFile = Join-Path $opsRoot ".env"
if (-not (Test-Path $envFile)) {
    Copy-Item $envExample $envFile
    Write-Host "Created .env from .env.example — edit passwords before sharing the host."
} else {
    Write-Host ".env already present (not overwritten)."
}

$envMap = Read-DotEnv $envFile
foreach ($required in @(
        "POSTGRES_PASSWORD", "MQTT_SRV_PASSWORD", "MQTT_DEVA_PASSWORD", "MQTT_DEVB_PASSWORD"
    )) {
    if (-not $envMap.ContainsKey($required) -or [string]::IsNullOrWhiteSpace($envMap[$required])) {
        throw "Missing $required in ops/.env"
    }
    if ($envMap[$required] -match "change-me") {
        Write-Warning "$required still uses the example placeholder. Acceptable for isolated local lab only."
    }
}

$passwdPath = Join-Path $opsRoot "mosquitto\passwd"
if ((Test-Path $passwdPath) -and -not $ForcePasswd) {
    Write-Host "mosquitto/passwd exists (use -ForcePasswd to regenerate)."
} else {
    $image = if ($envMap["MOSQUITTO_IMAGE"]) { $envMap["MOSQUITTO_IMAGE"] } else { "eclipse-mosquitto:2" }
    $srv = $envMap["MQTT_SRV_PASSWORD"]
    $deva = $envMap["MQTT_DEVA_PASSWORD"]
    $devb = $envMap["MQTT_DEVB_PASSWORD"]
    $tmpDir = Join-Path $opsRoot "tmp"
    New-Item -ItemType Directory -Force -Path $tmpDir | Out-Null
    $tmpPass = Join-Path $tmpDir "passwd"
    if (Test-Path $tmpPass) { Remove-Item $tmpPass -Force }

    $mount = $tmpDir -replace '\\', '/'
    if ($mount -match '^[A-Za-z]:') {
        $drive = $mount.Substring(0, 1).ToLower()
        $mount = "/$drive" + $mount.Substring(2)
    }

    # Create hashed password file inside the official image (no plaintext commit).
    # Mode 644: eclipse-mosquitto runs as user `mosquitto` and must read the bind-mounted file.
    docker run --rm -v "${mount}:/out" -e SRV_PW="$srv" -e DEVA_PW="$deva" -e DEVB_PW="$devb" $image `
        sh -c 'mosquitto_passwd -b -c /out/passwd srv "$SRV_PW" && mosquitto_passwd -b /out/passwd deva "$DEVA_PW" && mosquitto_passwd -b /out/passwd devb "$DEVB_PW" && chmod 644 /out/passwd'
    if ($LASTEXITCODE -ne 0) { throw "mosquitto_passwd failed" }
    Move-Item -Force $tmpPass $passwdPath
    Write-Host "Wrote mosquitto/passwd (gitignored)."
}

$tlsServer = Join-Path $opsRoot "tls\server"
$needTls = $ForceTls -or -not (Test-Path (Join-Path $tlsServer "server.crt")) -or -not (Test-Path (Join-Path $tlsServer "server.key")) -or -not (Test-Path (Join-Path $tlsServer "ca.crt"))
if ($needTls) {
    & (Join-Path $PSScriptRoot "Generate-LocalTls.ps1") -OutDir $tlsServer
} else {
    Write-Host "TLS material already present (use -ForceTls to regenerate)."
}

Write-Host ""
Write-Host "Bootstrap complete. Next:"
Write-Host "  docker compose --env-file .env up -d"
Write-Host "  .\scripts\Test-LocalStack.ps1"
