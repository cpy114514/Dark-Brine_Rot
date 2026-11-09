from pathlib import Path
import json, hashlib, re

root=Path.cwd()
stage=root/'.codex/execution-removal-20261108/staged'
stage.mkdir(parents=True,exist_ok=True)
manifest=[]
def once(s,a,b=''):
    assert s.count(a)==1,(a[:100],s.count(a))
    return s.replace(a,b,1)
def block(s,header):
    start=s.index(header); opening=s.index('{',start); depth=0
    for n in range(opening,len(s)):
        if s[n]=='{':depth+=1
        elif s[n]=='}':
            depth-=1
            if depth==0:return s[:start]+s[n+1:].lstrip('\n')
    raise AssertionError(header)
def write(path,change):
    p=root/path;before=p.read_bytes();s=before.decode('utf-8-sig').replace('\r\n','\n');updated=change(s)
    assert updated!=s,path
    out=stage/path;out.parent.mkdir(parents=True,exist_ok=True);out.write_bytes(updated.replace('\n','\r\n').encode('utf-8'))
    manifest.append(dict(path=path,sha256=hashlib.sha256(before).hexdigest()))

def health(s):
    s=once(s,'currentHealth <= 0f && !BossExecution.DefersDeath(this)','currentHealth <= 0f')
    s=s.replace('IsDead || BossExecution.DefersDeath(this) ||','IsDead ||')
    s=once(s,'            var execution = GetComponent<BossExecution>();\n            bool deferred = amount >= currentHealth && execution && execution.CanDefer;\n')
    s=once(s,'amount>=currentHealth&&!deferred?','amount>=currentHealth?')
    s=once(s,'            if (deferred) execution.EnterPending(hitPoint);\n')
    s=once(s,'            if (deferred) { execution.PresentPending(); return; }\n')
    start=s.index('        internal void CompleteExecution(')
    s=s[:start]+'    }\n}\n'
    return s
write('Assets/Scripts/Enemy/Health.cs',health)
write('Assets/Game/Prefabs/Bosses/Nailong/Scripts/NailongHealth.cs',health)

def movement(s):
    s=once(s,'    float executionCameraYaw;\n')
    s=once(s,'    public bool ExecutionControlLock { get; set; }\n    public float ExecutionLocomotionSpeed { get; set; }\n')
    s=once(s,'get => NarrativeControlLock || ExecutionControlLock;','get => NarrativeControlLock;')
    s=once(s,'        if (Application.isPlaying && (gameObject.scene.path == "Assets/Scenes/First Island/Gameplay.unity" ||\n            gameObject.scene.path == "Assets/Scenes/Second Island/Second Island.unity") &&\n            !GetComponent<Mavis.SahurExecution>()) gameObject.AddComponent<Mavis.SahurExecution>();\n')
    s=once(s,'        // Execution alignment owns capsule travel; no residual run velocity or\n        // locomotion state changes may fight the paired animation.\n')
    s=block(s,'        if (ExecutionControlLock)\n')
    s=once(s,'        executionCameraYaw = Mathf.MoveTowards(executionCameraYaw, ExecutionControlLock ? 18f : 0f, 90f * Time.deltaTime);\n')
    s=once(s,'Quaternion.Euler(pitch, yaw + executionCameraYaw, 0f)','Quaternion.Euler(pitch, yaw, 0f)')
    return s
write('Assets/Game/Prefabs/Characters/Sahur/Scripts/ThirdPersonPlayerController.cs',movement)
write('Assets/Game/Prefabs/Characters/Sahur/Scripts/PlayerHealth.cs',lambda s:once(s,'movement && movement.ExecutionControlLock || '))
def boomerang(s):
    s=once(s,'            var execution = GetComponent<SahurExecution>();\n            if (execution && (execution.IsActive || execution.ConsumedSpecialThisFrame)) return;\n')
    return once(s,'            if (GetComponent<SahurExecution>() is SahurExecution execution && execution.IsActive) return false;\n')
write('Assets/Game/Prefabs/Characters/Sahur/Scripts/SahurBoomerang.cs',boomerang)
def rifle(s):
    start=s.index('        bool executionActive;');end=s.index('        public bool DeathPoseActive',start);s=s[:start]+s[end:]
    s=block(s,'            if (executionActive)\n')
    s=once(s,'            EndExecution();\n')
    start=s.index('        // Execution owns only presentation.');end=s.index('        public void ResetDeath()',start);s=s[:start]+s[end:]
    s=block(s,'            if (executionActive)\n')
    s=once(s,'void AlignToAim(Vector3 target, bool executionAim = false)','void AlignToAim(Vector3 target)')
    s=once(s,'Vector3 cameraDirection = executionAim ? direction :\n                movement.IsSprinting','Vector3 cameraDirection = movement.IsSprinting')
    s=once(s,'if (nearAimBlend > 0 && !executionAim)','if (nearAimBlend > 0)')
    s=block(s,'            if (executionAim && executionSequence)\n')
    return s
write('Assets/Game/Prefabs/Characters/Sahur/Scripts/SahurRifleAnimation.cs',rifle)
(stage.parent/'source-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(f'Staged {len(manifest)} focused source edits; live source untouched.')
