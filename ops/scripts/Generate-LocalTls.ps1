# Generates LOCAL TEST TLS material for OPS-T-01 Mosquitto (layer A/B only).
# Uses a disposable openssl container so host openssl is not required.
# Output: ops/tls/server/{ca.crt,ca.key,server.crt,server.key} — gitignored.
# Never copy these into staging/production. Never disable certificate validation as a shortcut.

[CmdletBinding()]
param(
    [string]$OutDir = (Join-Path $PSScriptRoot "..\tls\server")
)

$ErrorActionPreference = "Stop"
$OutDir = [System.IO.Path]::GetFullPath($OutDir)
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$opensslImage = "alpine/openssl:latest"
Write-Host "Pulling $opensslImage (if needed)..."
docker pull $opensslImage | Out-Null

$work = Join-Path $OutDir ".gen"
New-Item -ItemType Directory -Force -Path $work | Out-Null

$cnf = @"
[req]
distinguished_name = req_distinguished_name
x509_extensions = v3_ca
prompt = no

[req_distinguished_name]
CN = SmartSecurity Local Test CA

[v3_ca]
basicConstraints = critical,CA:TRUE
keyUsage = critical,keyCertSign,cRLSign
subjectKeyIdentifier = hash

[server_req]
distinguished_name = server_dn
req_extensions = v3_req
prompt = no

[server_dn]
CN = localhost

[v3_req]
basicConstraints = CA:FALSE
keyUsage = digitalSignature,keyEncipherment
extendedKeyUsage = serverAuth
subjectAltName = @alt_names

[alt_names]
DNS.1 = localhost
DNS.2 = mosquitto
DNS.3 = smartsecurity-mosquitto
DNS.4 = host.docker.internal
IP.1 = 127.0.0.1
IP.2 = ::1
"@
Set-Content -Path (Join-Path $work "openssl.cnf") -Value $cnf -Encoding ascii

$mount = $work -replace '\\', '/'
if ($mount -match '^[A-Za-z]:') {
    $drive = $mount.Substring(0, 1).ToLower()
    $mount = "/$drive" + $mount.Substring(2)
}

$script = @'
set -e
cd /work
openssl genrsa -out ca.key 2048
openssl req -new -x509 -days 730 -key ca.key -out ca.crt -config openssl.cnf -extensions v3_ca
openssl genrsa -out server.key 2048
openssl req -new -key server.key -out server.csr -config openssl.cnf -section server_req
openssl x509 -req -in server.csr -CA ca.crt -CAkey ca.key -CAcreateserial -out server.crt -days 365 -extensions v3_req -extfile openssl.cnf
'@
$scriptPath = Join-Path $work "gen.sh"
# LF line endings for sh
[System.IO.File]::WriteAllText($scriptPath, ($script -replace "`r`n", "`n"))

# Image ENTRYPOINT is `openssl`; override so the shell script can run multiple commands.
docker run --rm --entrypoint sh -v "${mount}:/work" $opensslImage /work/gen.sh
if ($LASTEXITCODE -ne 0) { throw "openssl generation failed (exit $LASTEXITCODE)" }

Copy-Item (Join-Path $work "ca.crt") (Join-Path $OutDir "ca.crt") -Force
Copy-Item (Join-Path $work "ca.key") (Join-Path $OutDir "ca.key") -Force
Copy-Item (Join-Path $work "server.crt") (Join-Path $OutDir "server.crt") -Force
Copy-Item (Join-Path $work "server.key") (Join-Path $OutDir "server.key") -Force
Remove-Item -Recurse -Force $work

Write-Host "Wrote LOCAL TEST TLS material to $OutDir"
Write-Host "Files: ca.crt ca.key server.crt server.key (gitignored). Production use forbidden."
