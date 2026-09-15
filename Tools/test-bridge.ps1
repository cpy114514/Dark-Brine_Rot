# test-bridge.ps1 - smoke test for unity-mcp-bridge.ps1 (final)
$ErrorActionPreference = 'Stop'

$bridgeScript = "C:\Unity\Dark Brine_Rot\Tools\unity-mcp-bridge.ps1"

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "powershell.exe"
$psi.Arguments = '-NoProfile','-ExecutionPolicy','Bypass','-File',"`"$bridgeScript`""
$psi.RedirectStandardInput  = $true
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError  = $true
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true
$psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8
$psi.StandardErrorEncoding  = [System.Text.Encoding]::UTF8

$proc = New-Object System.Diagnostics.Process
$proc.StartInfo = $psi
[void]$proc.Start()
Write-Host "Bridge PID: $($proc.Id)"

# Unity MCP Bridge V2 protocol: newline-delimited JSON, type/params/requestId.
$msgs = @(
    '{"type":"set_client_info","params":{"name":"mavis-bridge-test","version":"0.1","title":"Mavis"},"requestId":"1"}'
    '{"type":"get_available_tools","params":{},"requestId":"2"}'
    '{"type":"ping","requestId":"3"}'
    '{"type":"Unity_GetConsoleLogs","params":{"maxEntries":3},"requestId":"4"}'
)

$writer = $proc.StandardInput
$writer.AutoFlush = $true
foreach ($m in $msgs) {
    $writer.WriteLine($m)
    Start-Sleep -Milliseconds 400
}
Start-Sleep -Seconds 3
$writer.Close()

$stdout = $proc.StandardOutput.ReadToEnd()
$stderr = $proc.StandardError.ReadToEnd()
$proc.WaitForExit(8000) | Out-Null
if (-not $proc.HasExited) {
    Write-Host "TIMEOUT, killing"
    $proc.Kill()
}

Write-Host ""
Write-Host "=== Exit: $($proc.ExitCode) ==="
Write-Host ""
Write-Host "=== STDERR ==="
if ($stderr) { $stderr } else { Write-Host "(empty)" }
Write-Host ""
Write-Host "=== STDOUT (parsed summary) ==="
if ($stdout) {
    foreach ($line in ($stdout -split "`n" | Where-Object { $_ })) {
        try {
            $obj = $line | ConvertFrom-Json -ErrorAction Stop
            if ($obj.type -eq 'handshake') {
                $toolCount = if ($obj.tools) { @($obj.tools).Count } else { 0 }
                Write-Host "[handshake] protocol=$($obj.protocol) v=$($obj.version) tools=$toolCount"
            } elseif ($obj.type -eq 'command_in_progress') {
                Write-Host "[in_progress] $($obj.message)"
            } elseif ($obj.status -eq 'success') {
                $res = $obj.result
                if ($res.tools) {
                    $names = ($res.tools | ForEach-Object { $_.name }) -join ', '
                    Write-Host "[tools] hash=$($res.hash.Substring(0,12))... count=$(@($res.tools).Count)"
                    Write-Host "        $names"
                } elseif ($res.message) {
                    Write-Host "[success] $($res.message) (reqId=$($obj.requestId))"
                } else {
                    Write-Host "[success reqId=$($obj.requestId)] $line"
                }
            } elseif ($obj.status -eq 'error') {
                Write-Host "[error reqId=$($obj.requestId)] $($obj.error)"
            } else {
                Write-Host "[other] $line"
            }
        } catch {
            Write-Host "[raw] $line"
        }
    }
} else {
    Write-Host "(empty)"
}