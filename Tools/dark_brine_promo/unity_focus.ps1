# Focus the Unity editor so it refreshes assets and recompiles.
#
# Unity's asset refresh mode is "Refresh on Focus" in this project, so a new
# editor script only compiles after the editor window really gains focus.
# Windows does not send a focus event when SetForegroundWindow is called on the
# window that is already active, so this script always activates another app
# first and then Unity.
#
#   powershell -ExecutionPolicy Bypass -File unity_focus.ps1
#   powershell -ExecutionPolicy Bypass -File unity_focus.ps1 -Check
param([switch]$Check)

Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public static class WinFocus {
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool attach);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr SetActiveWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    public static string Title(IntPtr h) {
        StringBuilder sb = new StringBuilder(512);
        GetWindowText(h, sb, sb.Capacity);
        return sb.ToString();
    }
}
"@

function Get-UnityWindow {
    $proc = Get-Process -Name Unity -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowTitle -like "*Dark Brine_Rot*" -and $_.MainWindowHandle -ne 0 } |
        Select-Object -First 1
    if ($proc) { return $proc.MainWindowHandle }
    return [IntPtr]::Zero
}

$foreground = [WinFocus]::GetForegroundWindow()
$foregroundTitle = [WinFocus]::Title($foreground)
Write-Output ("FOREGROUND: " + $foregroundTitle)

if ($Check) { exit 0 }

$unity = Get-UnityWindow
if ($unity -eq [IntPtr]::Zero) { Write-Output "UNITY: not found"; exit 1 }
Write-Output ("UNITY: " + [WinFocus]::Title($unity))

# Step away from Unity so the next activation is a real focus transition.
if ($foreground -eq $unity) {
    $other = (Get-Process -Name explorer -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1)
    if ($other) {
        [WinFocus]::ShowWindow($other.MainWindowHandle, 9) | Out-Null
        [WinFocus]::SetForegroundWindow($other.MainWindowHandle) | Out-Null
        Start-Sleep -Milliseconds 700
    }
}

# Activate Unity, attaching to its input thread so the foreground really moves.
# Windows refuses foreground changes from a background process; tapping ALT
# first releases that lock.
[WinFocus]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero)
$unityPid = 0
$thread = [WinFocus]::GetWindowThreadProcessId($unity, [ref]$unityPid)
$current = [WinFocus]::GetCurrentThreadId()
[WinFocus]::AttachThreadInput($current, $thread, $true) | Out-Null
[WinFocus]::ShowWindow($unity, 9) | Out-Null
[WinFocus]::BringWindowToTop($unity) | Out-Null
[WinFocus]::SetForegroundWindow($unity) | Out-Null
[WinFocus]::SetActiveWindow($unity) | Out-Null
[WinFocus]::AttachThreadInput($current, $thread, $false) | Out-Null
[WinFocus]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 1200

Write-Output ("FOREGROUND NOW: " + [WinFocus]::Title([WinFocus]::GetForegroundWindow()))
