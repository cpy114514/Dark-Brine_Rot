<#
.SYNOPSIS
    Mavis stdio <-> Unity MCP Bridge V2 (named pipe).

.DESCRIPTION
    Reads newline-delimited JSON from stdin and forwards each line to the
    Unity MCP Bridge V2 named pipe whose path is in the latest connection JSON
    under %USERPROFILE%\.unity\mcp\connections\bridge-*.json.  Reads from the
    pipe and writes each line back to stdout.  Reconnects automatically if the
    pipe drops.

    Logging goes to stderr only so stdout stays a clean JSON channel.

    The Unity MCP Bridge V2 protocol is NOT standard JSON-RPC.  Messages are
    newline-delimited JSON objects shaped like:

        {"type": "<command>", "params": {...}, "requestId": "..."}

    The server sends {"type":"handshake","protocol":"unity-mcp","version":"2.0",
    "tools":[...],"toolsHash":"..."} as the very first line, then responds to
    each command with {"status":"success|error","result":{...}|"error":"...",
    "requestId":"..."}.

    Register with Mavis:
        mavis mcp create unity stdio \
          --command "powershell" \
          --args "-NoProfile","-ExecutionPolicy","Bypass","-File","C:\Unity\Dark Brine_Rot\Tools\unity-mcp-bridge.ps1"
#>

$ErrorActionPreference = 'Continue'

# Force-load types PowerShell 5.1 lazy-skips.
[void][System.Reflection.Assembly]::LoadWithPartialName('System.Core')
[void][System.Reflection.Assembly]::LoadWithPartialName('System.IO.Pipes')
[void][System.Reflection.Assembly]::LoadWithPartialName('System.Management.Automation')
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::InputEncoding  = [System.Text.Encoding]::UTF8
$OutputEncoding          = [System.Text.Encoding]::UTF8

$ConnDir = Join-Path $env:USERPROFILE '.unity\mcp\connections'
if (-not (Test-Path $ConnDir)) {
    [Console]::Error.WriteLine("[bridge] connection dir not found: $ConnDir")
    exit 1
}

function Log([string]$m) {
    $ts = (Get-Date).ToString('HH:mm:ss.fff')
    [Console]::Error.WriteLine("[$ts] [bridge] $m")
    [Console]::Error.Flush()
}

# [Console]::In.ReadLine() returns null immediately when stdin is a .NET-managed
# anonymous pipe; use an explicit StreamReader on the raw stdin stream instead.
$stdinStream = [Console]::OpenStandardInput()
$stdinReader = New-Object System.IO.StreamReader(
    $stdinStream, [System.Text.Encoding]::UTF8, $false, 4096, $true)

# Shared state.  Plain script-scope variables don't cross PowerShell runspace
# boundaries, so we use a PSCustomObject (reference type) passed as a parameter.
$state = [PSCustomObject]@{
    Pipe        = $null
    Reader      = $null
    Writer      = $null
    Shutdown    = $false
    ReaderAsync = $null
    ReaderPs    = $null
}

function Disconnect-Pipe {
    if ($state.Reader) { try { $state.Reader.Dispose() } catch {} ; $state.Reader = $null }
    if ($state.Writer) { try { $state.Writer.Dispose() } catch {} ; $state.Writer = $null }
    if ($state.Pipe -and $state.Pipe.IsConnected) {
        try { $state.Pipe.Close() } catch {}
    }
    $state.Pipe = $null
}

function Connect-Pipe {
    $files = Get-ChildItem -Path $ConnDir -Filter 'bridge-*.json' -ErrorAction SilentlyContinue |
             Sort-Object LastWriteTime -Descending
    if (-not $files -or $files.Count -eq 0) {
        return $false
    }
    $info = Get-Content -Raw -LiteralPath $files[0].FullName | ConvertFrom-Json
    $full = [string]$info.connection_path
    if (-not $full) { return $false }
    $name = $full -replace '^\\\\\.\\pipe\\', ''

    try {
        $state.Pipe = New-Object System.IO.Pipes.NamedPipeClientStream(
            '.', $name,
            [System.IO.Pipes.PipeDirection]::InOut,
            [System.IO.Pipes.PipeOptions]::Asynchronous
        )
        $state.Pipe.Connect(30000)
        $state.Reader = New-Object System.IO.StreamReader(
            $state.Pipe, [System.Text.Encoding]::UTF8, $false, 4096, $true)
        $state.Writer = New-Object System.IO.StreamWriter(
            $state.Pipe, [System.Text.Encoding]::UTF8, 4096, $true)
        $state.Writer.AutoFlush = $true
        Log "connected to $full"
        return $true
    } catch {
        Log "connect failed: $($_.Exception.Message)"
        Disconnect-Pipe
        return $false
    }
}

# Hook Ctrl+C (no-op when stdio is redirected).
try {
    $handler = [Console]::CancelKeyPress
    if ($null -ne $handler) {
        $null = $handler.Add({ $state.Shutdown = $true })
    }
} catch {
    Log "CancelKeyPress hook failed (non-fatal): $($_.Exception.Message)"
}

# Reader runspace: pipe -> stdout.  PowerShell script blocks cannot run on a raw
# System.Threading.Thread (the engine needs to be initialized in the runspace);
# we run them through [System.Management.Automation.PowerShell]::Create() instead.
$readerScript = {
    param($st)
    while (-not $st.Shutdown) {
        try {
            $r = $st.Reader
            if (-not $r) {
                Start-Sleep -Milliseconds 100
                continue
            }
            $line = $r.ReadLine()
            if ($null -eq $line) {
                if ($null -ne $st.Reader) { try { $st.Reader.Dispose() } catch {} }
                $st.Reader = $null
                Start-Sleep -Milliseconds 500
            } else {
                [Console]::Out.WriteLine($line)
            }
        } catch [System.IO.IOException] {
            if ($null -ne $st.Reader) { try { $st.Reader.Dispose() } catch {} }
            $st.Reader = $null
            Start-Sleep -Milliseconds 1000
        } catch {
            if (-not $st.Shutdown) {
                $ts = (Get-Date).ToString('HH:mm:ss.fff')
                [Console]::Error.WriteLine("[$ts] [bridge] reader error: $($_.Exception.GetType().FullName): $($_.Exception.Message)")
                [Console]::Error.Flush()
            }
        }
    }
}

$state.ReaderPs = [System.Management.Automation.PowerShell]::Create()
$null = $state.ReaderPs.AddScript($readerScript).AddArgument($state)
$state.ReaderAsync = $state.ReaderPs.BeginInvoke()

# Main thread: stdin -> pipe.
try {
    while (-not $state.Shutdown) {
        if (-not $state.Pipe -or -not $state.Pipe.IsConnected) {
            if (-not (Connect-Pipe)) {
                Start-Sleep -Seconds 2
                continue
            }
        }
        try {
            $line = $stdinReader.ReadLine()
            if ($null -eq $line) {
                Log 'stdin EOF, exiting'
                $state.Shutdown = $true
                break
            }
            $state.Writer.WriteLine($line)
        } catch {
            Log "stdin error: $($_.Exception.Message)"
            Disconnect-Pipe
        }
    }
} finally {
    $state.Shutdown = $true
    Disconnect-Pipe
    if ($null -ne $state.ReaderAsync -and -not $state.ReaderAsync.IsCompleted) {
        # Give the reader up to 2s to drain its pipe buffer.
        $null = [System.Threading.Thread]::Sleep(2000)
    }
    if ($null -ne $state.ReaderAsync) {
        try {
            $null = $state.ReaderPs.EndInvoke($state.ReaderAsync)
        } catch {
            Log "reader EndInvoke: $($_.Exception.Message)"
        }
    }
    if ($null -ne $state.ReaderPs) {
        $state.ReaderPs.Dispose()
    }
}