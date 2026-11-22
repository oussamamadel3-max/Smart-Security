# OPS-T-01 local TLS smoke (layer B). Uses test CA only.
# Does not claim G1-03 closed. Does not enable production UseTls.

[CmdletBinding()]
param(
    [string]$HostAddress = "host.docker.internal",
    [int]$Port = 8883
)

$ErrorActionPreference = "Stop"
$opsRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
Set-Location $opsRoot

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

function Convert-ToDockerMount {
    param([string]$Path)
    $mount = $Path -replace '\\', '/'
    if ($mount -match '^[A-Za-z]:') {
        $drive = $mount.Substring(0, 1).ToLower()
        $mount = "/$drive" + $mount.Substring(2)
    }
    return $mount
}

$envMap = Read-DotEnv (Join-Path $opsRoot ".env")
$pwSrv = $envMap["MQTT_SRV_PASSWORD"]
$deviceA = if ($envMap["DEVICE_A_ID"]) { $envMap["DEVICE_A_ID"] } else { "11111111-1111-4111-8111-111111111111" }
$scriptsMount = Convert-ToDockerMount (Join-Path $opsRoot "scripts")
$tlsMount = Convert-ToDockerMount (Join-Path $opsRoot "tls\server")
$pythonImage = "python:3.12-alpine"

function Invoke-TlsProbe {
    param([string]$CaFileInContainer, [string[]]$ExtraDockerArgs = @())
    $argList = [System.Collections.Generic.List[string]]::new()
    $argList.AddRange([string[]]@("run", "--rm", "-e", "MQTT_USERNAME=srv", "-e", "MQTT_PASSWORD=$pwSrv"))
    foreach ($a in $ExtraDockerArgs) { $argList.Add($a) }
    $argList.AddRange([string[]]@(
            "-v", "${scriptsMount}:/scripts:ro",
            $pythonImage,
            "sh", "-c",
            "pip install -q --root-user-action=ignore paho-mqtt >/dev/null && python /scripts/mqtt_acl_probe.py --host $HostAddress --port $Port --topic 'devices/$deviceA/commands' --action publish --message tls-probe --wait 5 --cafile $CaFileInContainer"
        ))
    $prev = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & docker @($argList.ToArray()) 2>&1 | ForEach-Object { "$_" } | Out-String
    $code = $LASTEXITCODE
    $ErrorActionPreference = $prev
    $detail = (($output -split "`n") | Where-Object { $_ -match '^(PUBACK|CONNACK|CONNECT_ERROR|PUBACK_TIMEOUT|CONNACK_TIMEOUT)' }) -join " | "
    return [pscustomobject]@{ ExitCode = $code; Detail = $detail; Raw = $output.Trim() }
}

Write-Host "TLS publish with trust store (expect allow)..."
$ok = Invoke-TlsProbe -CaFileInContainer "/certs/ca.crt" -ExtraDockerArgs @("-v", "${tlsMount}:/certs:ro")
if ($ok.ExitCode -ne 0 -or $ok.Detail -notmatch 'PUBACK (0|16)\b') {
    throw "TLS publish with trusted CA failed: $($ok.Detail)"
}
Write-Host "PASS trusted CA ($($ok.Detail))"

Write-Host "TLS publish without CA (expect deny)..."
$prev = $ErrorActionPreference
$ErrorActionPreference = "Continue"
# No --cafile and no TLS config => plaintext to TLS port should fail
$noCa = & docker run --rm -e "MQTT_USERNAME=srv" -e "MQTT_PASSWORD=$pwSrv" -v "${scriptsMount}:/scripts:ro" $pythonImage `
    sh -c "pip install -q --root-user-action=ignore paho-mqtt >/dev/null && python /scripts/mqtt_acl_probe.py --host $HostAddress --port $Port --topic 'devices/$deviceA/commands' --action publish --message tls-no-ca --wait 4" 2>&1 | ForEach-Object { "$_" } | Out-String
$codeNoCa = $LASTEXITCODE
$ErrorActionPreference = $prev
if ($codeNoCa -eq 0 -and $noCa -match 'PUBACK (0|16)\b') {
    throw "TLS publish without CA unexpectedly succeeded"
}
Write-Host "PASS missing CA rejected"

$badDir = Join-Path $opsRoot "tmp\bad-ca"
New-Item -ItemType Directory -Force -Path $badDir | Out-Null
$badMount = Convert-ToDockerMount $badDir
docker run --rm --entrypoint sh -v "${badMount}:/work" alpine/openssl:latest -c "openssl req -x509 -newkey rsa:2048 -nodes -keyout /work/bad.key -out /work/bad.crt -days 1 -subj '/CN=Untrusted Local CA'" | Out-Null

Write-Host "TLS publish with untrusted CA (expect deny)..."
$bad = Invoke-TlsProbe -CaFileInContainer "/bad/bad.crt" -ExtraDockerArgs @("-v", "${badMount}:/bad:ro")
if ($bad.ExitCode -eq 0 -and $bad.Detail -match 'PUBACK (0|16)\b') {
    throw "TLS publish with untrusted CA unexpectedly succeeded: $($bad.Detail)"
}
Write-Host "PASS untrusted CA rejected ($($bad.Detail))"

Write-Host "Local TLS smoke passed (not G1 closure, not production)."
exit 0
