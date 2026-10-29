function Restore-Qa019NativePreferences {
    param([Parameter(Mandatory=$true)][string]$SnapshotPath)
    $qa019Snapshot=Import-Clixml -LiteralPath $SnapshotPath
    $qa019ExpectedPath='Software\DefaultCompany\Dark Brine_Rot'
    if($qa019Snapshot.Path -cne $qa019ExpectedPath){throw 'Unexpected native preference scope'}
    if(Get-Process|Where-Object ProcessName -Like 'Dark*'){throw 'Preserve concurrent Player preferences'}
    if(-not ('Qa019RawRegistry' -as [type])){
        Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class Qa019RawRegistry {
    [DllImport("advapi32.dll", EntryPoint="RegSetValueExW", CharSet=CharSet.Unicode, ExactSpelling=true)]
    public static extern int Write(IntPtr key, string name, uint reserved, uint type, byte[] data, uint bytes);
}
'@
    }
    $qa019Key=[Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($qa019ExpectedPath)
    $qa019Values=@($qa019Snapshot.Values)
    $qa019Names=@($qa019Values|ForEach-Object Name)
    try {
        foreach($qa019Name in $qa019Key.GetValueNames()){if($qa019Names -notcontains $qa019Name){$qa019Key.DeleteValue($qa019Name)}}
        foreach($qa019Value in $qa019Values){
            $qa019Kind=[Microsoft.Win32.RegistryValueKind]$qa019Value.Kind
            $qa019Data=$qa019Value.Value
            if($qa019Value.Kind -eq 'DWord' -and $qa019Data -is [long]){
                # These observed legacy Unity floats are tagged REG_DWORD but contain
                # eight raw bytes. Managed SetValue rejects that representation.
                # Restore the exact observed type/length/bits through the native API.
                $qa019Bytes=[BitConverter]::GetBytes([long]$qa019Data)
                $qa019Code=[Qa019RawRegistry]::Write($qa019Key.Handle.DangerousGetHandle(),$qa019Value.Name,0,4,$qa019Bytes,8)
                if($qa019Code -ne 0){throw ('Raw preference restoration failed: '+$qa019Code)}
                continue
            }
            switch($qa019Value.Kind){
                'DWord' {
                    $qa019Bits=[long]$qa019Data
                    if($qa019Bits -lt -2147483648 -or $qa019Bits -gt 4294967295){throw 'Invalid saved DWORD'}
                    # Ordinary four-byte DWORDs use boxed Int32. The observed
                    # eight-byte legacy payloads were handled above without conversion.
                    $qa019Data=if($qa019Bits -gt 2147483647){[int]($qa019Bits-4294967296)}else{[int]$qa019Bits}
                }
                'QWord' {$qa019Data=[long]$qa019Data}
                'Binary' {$qa019Data=[byte[]]$qa019Data}
                'None' {$qa019Data=[byte[]]$qa019Data}
                'MultiString' {$qa019Data=[string[]]$qa019Data}
                default {$qa019Data=[string]$qa019Data}
            }
            $qa019Key.SetValue($qa019Value.Name,$qa019Data,$qa019Kind)
        }
        $qa019Valid=$qa019Key.ValueCount -eq $qa019Values.Count
        foreach($qa019Value in $qa019Values){
            $qa019Actual=$qa019Key.GetValue($qa019Value.Name,$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
            switch($qa019Value.Kind){
                'DWord' {if($qa019Value.Value -is [long]){$qa019Equal=$qa019Actual -is [long] -and [long]$qa019Actual -eq [long]$qa019Value.Value}else{$qa019Equal=(([long]$qa019Actual -band 4294967295) -eq ([long]$qa019Value.Value -band 4294967295))}}
                'Binary' {$qa019Equal=[Convert]::ToBase64String([byte[]]$qa019Actual) -eq [Convert]::ToBase64String([byte[]]$qa019Value.Value)}
                'None' {$qa019Equal=[Convert]::ToBase64String([byte[]]$qa019Actual) -eq [Convert]::ToBase64String([byte[]]$qa019Value.Value)}
                'MultiString' {$qa019Equal=($qa019Actual|ConvertTo-Json -Compress) -eq ($qa019Value.Value|ConvertTo-Json -Compress)}
                default {$qa019Equal=[string]$qa019Actual -ceq [string]$qa019Value.Value}
            }
            if(!$qa019Equal -or $qa019Key.GetValueKind($qa019Value.Name).ToString() -ne $qa019Value.Kind){$qa019Valid=$false}
        }
        $qa019Empty=$qa019Key.ValueCount -eq 0 -and $qa019Key.SubKeyCount -eq 0
    } finally {$qa019Key.Close()}
    if(!$qa019Snapshot.Exists -and $qa019Empty){[Microsoft.Win32.Registry]::CurrentUser.DeleteSubKey($qa019ExpectedPath)}
    return $qa019Valid
}
