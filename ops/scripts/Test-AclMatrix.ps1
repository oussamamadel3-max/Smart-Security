# OPS-T-01 ACL matrix against local Mosquitto (plaintext 1883).
# Uses MQTT v5 reason codes via paho-mqtt (mosquitto_pub treats PUBACK 135 as success).
# Does not print passwords. Exit 0 only if all expected allow/deny outcomes match.

[CmdletBinding()]
param(
    [string]$HostAddress = "host.docker.internal",
    [int]$Port = 1883
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
$deviceA = if ($envMap["DEVICE_A_ID"]) { $envMap["DEVICE_A_ID"] } else { "11111111-1111-4111-8111-111111111111" }
$deviceB = if ($envMap["DEVICE_B_ID"]) { $envMap["DEVICE_B_ID"] } else { "22222222-2222-4222-8222-222222222222" }
$pwSrv = $envMap["MQTT_SRV_PASSWORD"]
$pwA = $envMap["MQTT_DEVA_PASSWORD"]
$pwB = $envMap["MQTT_DEVB_PASSWORD"]

$scriptsMount = Convert-ToDockerMount (Join-Path $opsRoot "scripts")
$pythonImage = "python:3.12-alpine"

function Invoke-Probe {
    param(
        [string]$Action,
        [string]$User,
        [string]$Password,
        [string]$Topic,
        [string]$Message = "probe",
        [double]$Wait = 4.0,
        [switch]$Anonymous
    )

    # Pass credentials via env (-e) to avoid shell-quoting breakage on Windows.
    $argList = [System.Collections.Generic.List[string]]::new()
    $argList.AddRange([string[]]@("run", "--rm", "-v", "${scriptsMount}:/scripts:ro"))
    if (-not $Anonymous) {
        $argList.AddRange([string[]]@("-e", "MQTT_USERNAME=$User", "-e", "MQTT_PASSWORD=$Password"))
    }
    $argList.AddRange([string[]]@(
            $pythonImage,
            "sh", "-c",
            "pip install -q --root-user-action=ignore paho-mqtt >/dev/null && python /scripts/mqtt_acl_probe.py --host $HostAddress --port $Port --topic '$Topic' --action $Action --message '$Message' --wait $Wait"
        ))

    $prev = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $output = & docker @($argList.ToArray()) 2>&1 | ForEach-Object { "$_" } | Out-String
    $code = $LASTEXITCODE
    $ErrorActionPreference = $prev
    # Keep only probe result lines in Detail (not pip noise)
    $detail = (($output -split "`n") | Where-Object { $_ -match '^(PUBACK|CONNACK|MESSAGE|NO_MESSAGE|CONNECT_ERROR|PUBACK_TIMEOUT|CONNACK_TIMEOUT|PUBLISH_FALLBACK)' }) -join " | "
    if (-not $detail) { $detail = $output.Trim() }
    return [pscustomobject]@{
        ExitCode = $code
        Output   = $detail
        Raw      = $output.Trim()
    }
}

$script:results = [System.Collections.Generic.List[object]]::new()
function Add-Result {
    param($Id, $Expected, $Actual, $Detail)
    $pass = ($Expected -eq $Actual)
    $script:results.Add([pscustomobject]@{
            Id       = $Id
            Expected = $Expected
            Actual   = $Actual
            Pass     = $pass
            Detail   = $Detail
        }) | Out-Null
    $mark = if ($pass) { "PASS" } else { "FAIL" }
    Write-Host "[$mark] $Id — expected=$Expected actual=$Actual ($Detail)"
}

Write-Host "Preparing python probe image (paho-mqtt)..."
docker pull $pythonImage | Out-Null

# 1) Anonymous rejected (CONNACK fail)
$anon = Invoke-Probe -Action publish -Anonymous -Topic "devices/$deviceA/state" -Message "anon"
Add-Result -Id "anonymous_publish_rejected" -Expected "deny" -Actual ($(if ($anon.ExitCode -ne 0) { "deny" } else { "allow" })) -Detail $anon.Output

# 2) Device A publish own state
$aOwn = Invoke-Probe -Action publish -User "deva" -Password $pwA -Topic "devices/$deviceA/state" -Message "a-state"
function Test-PublishAllowed([string]$Output, [int]$ExitCode) {
    return ($ExitCode -eq 0) -and ($Output -match 'PUBACK (0|16)\b')
}
function Test-PublishDenied([string]$Output, [int]$ExitCode) {
    return ($Output -match 'PUBACK 135\b') -or (($ExitCode -ne 0) -and -not ($Output -match 'PUBACK (0|16)\b'))
}

Add-Result -Id "deva_publish_own_state" -Expected "allow" -Actual ($(if (Test-PublishAllowed $aOwn.Output $aOwn.ExitCode) { "allow" } else { "deny" })) -Detail $aOwn.Output

# 3) Device A publish Device B state denied (PUBACK 135)
$aCross = Invoke-Probe -Action publish -User "deva" -Password $pwA -Topic "devices/$deviceB/state" -Message "cross"
Add-Result -Id "deva_publish_devb_state_denied" -Expected "deny" -Actual ($(if (Test-PublishDenied $aCross.Output $aCross.ExitCode) { "deny" } else { "allow" })) -Detail $aCross.Output

# 4) Device A cannot publish commands
$aCmd = Invoke-Probe -Action publish -User "deva" -Password $pwA -Topic "devices/$deviceA/commands" -Message "bad-cmd"
Add-Result -Id "deva_publish_commands_denied" -Expected "deny" -Actual ($(if (Test-PublishDenied $aCmd.Output $aCmd.ExitCode) { "deny" } else { "allow" })) -Detail $aCmd.Output

# 5) Server publish + device receives
$marker = "acl-cmd-" + [guid]::NewGuid().ToString("N").Substring(0, 8)
$subJob = Start-Job -ScriptBlock {
    param($ScriptsMount, $PythonImage, $HostAddress, $Port, $Password, $Topic, $Wait)
    $ErrorActionPreference = "Continue"
    docker run --rm -e "MQTT_USERNAME=deva" -e "MQTT_PASSWORD=$Password" -v "${ScriptsMount}:/scripts:ro" $PythonImage `
        sh -c "pip install -q --root-user-action=ignore paho-mqtt >/dev/null && python /scripts/mqtt_acl_probe.py --host $HostAddress --port $Port --topic '$Topic' --action subscribe --wait $Wait" 2>&1 | ForEach-Object { "$_" }
} -ArgumentList $scriptsMount, $pythonImage, $HostAddress, $Port, $pwA, "devices/$deviceA/commands", 10.0
Start-Sleep -Seconds 4
$srvPub = Invoke-Probe -Action publish -User "srv" -Password $pwSrv -Topic "devices/$deviceA/commands" -Message $marker
$subOut = (Receive-Job $subJob -Wait -AutoRemoveJob | Out-String).Trim()
$delivered = $subOut.Contains($marker)
Add-Result -Id "srv_publish_deva_command" -Expected "allow" -Actual ($(if (Test-PublishAllowed $srvPub.Output $srvPub.ExitCode) { "allow" } else { "deny" })) -Detail $srvPub.Output
Add-Result -Id "deva_receives_own_command" -Expected "allow" -Actual ($(if ($delivered) { "allow" } else { "deny" })) -Detail $subOut

# 6) Cross-subscribe isolation (no delivery of B commands to A)
$markerB = "acl-b-" + [guid]::NewGuid().ToString("N").Substring(0, 8)
$crossJob = Start-Job -ScriptBlock {
    param($ScriptsMount, $PythonImage, $HostAddress, $Port, $Password, $Topic, $Wait)
    $ErrorActionPreference = "Continue"
    docker run --rm -e "MQTT_USERNAME=deva" -e "MQTT_PASSWORD=$Password" -v "${ScriptsMount}:/scripts:ro" $PythonImage `
        sh -c "pip install -q --root-user-action=ignore paho-mqtt >/dev/null && python /scripts/mqtt_acl_probe.py --host $HostAddress --port $Port --topic '$Topic' --action subscribe --wait $Wait" 2>&1 | ForEach-Object { "$_" }
} -ArgumentList $scriptsMount, $pythonImage, $HostAddress, $Port, $pwA, "devices/$deviceB/commands", 6.0
Start-Sleep -Seconds 4
[void](Invoke-Probe -Action publish -User "srv" -Password $pwSrv -Topic "devices/$deviceB/commands" -Message $markerB)
$crossOut = (Receive-Job $crossJob -Wait -AutoRemoveJob | Out-String).Trim()
$leaked = $crossOut.Contains($markerB)
Add-Result -Id "deva_does_not_receive_devb_commands" -Expected "deny" -Actual ($(if (-not $leaked) { "deny" } else { "allow" })) -Detail $crossOut

# 7) Device B own events
$bOwn = Invoke-Probe -Action publish -User "devb" -Password $pwB -Topic "devices/$deviceB/events" -Message "b-event"
Add-Result -Id "devb_publish_own_events" -Expected "allow" -Actual ($(if (Test-PublishAllowed $bOwn.Output $bOwn.ExitCode) { "allow" } else { "deny" })) -Detail $bOwn.Output

# 8) Bad password
$bad = Invoke-Probe -Action publish -User "deva" -Password "definitely-wrong-password" -Topic "devices/$deviceA/state" -Message "x"
Add-Result -Id "bad_password_rejected" -Expected "deny" -Actual ($(if ($bad.ExitCode -ne 0) { "deny" } else { "allow" })) -Detail $bad.Output

$failed = @($script:results | Where-Object { -not $_.Pass })
Write-Host ""
Write-Host ("ACL matrix: {0} passed, {1} failed, {2} total" -f ($script:results.Count - $failed.Count), $failed.Count, $script:results.Count)
$script:results | ConvertTo-Csv -NoTypeInformation | Set-Content -Path (Join-Path $opsRoot "tmp\acl-matrix-result.csv") -Encoding utf8
if ($failed.Count -gt 0) { exit 1 }
exit 0
