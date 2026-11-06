$ErrorActionPreference='Stop'
$taskWorkspace=(Resolve-Path -LiteralPath (Split-Path $PSScriptRoot)).Path
Push-Location -LiteralPath $taskWorkspace
try {
    $taskBaseline=(& git show 'ca59178:Assets/Scripts/Effects/MainQuestGuidance.cs') -join "`n"
    if($LASTEXITCODE -ne 0){throw 'Retained baseline commit unavailable'}
    $taskBaseline=$taskBaseline.Replace('public sealed class MainQuestGuidance:', 'public sealed class MaintenanceReferenceGuidance:')
    $taskStart=$taskBaseline.IndexOf('namespace Mavis')
    if($taskStart -lt 0){throw 'Unexpected baseline source'}
    $taskVerifier=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'VerifyProjectMaintenance.cs') -Raw
    $taskSource="using UnityEngine.AI;`nusing UnityEngine.Rendering;`n"+$taskVerifier+"`n"+$taskBaseline.Substring($taskStart)
    $taskOutput=Join-Path $taskWorkspace '.codex/project-maintenance-20261103/CombinedVerification.cs'
    New-Item -ItemType Directory -Force (Split-Path $taskOutput) | Out-Null
    [IO.File]::WriteAllText($taskOutput,$taskSource)
    $taskResponseText=& unity command run_script --file $taskOutput --entry VerifyProjectMaintenance.Run --format json
    if($LASTEXITCODE -ne 0){throw 'Native verification failed'}
    $taskResponseText
    $taskResponse=$taskResponseText | ConvertFrom-Json
    if(-not $taskResponse.success -or -not $taskResponse.data.result.success -or -not $taskResponse.data.result.result.passed){
        throw 'Native verifier did not report passing checks'
    }
} finally { Pop-Location }
