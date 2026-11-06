"""Remove only reviewed generated artifacts from this project's local .codex."""
from pathlib import Path
import gzip,json,os,stat,time

workspace=Path.cwd().resolve()
root=(workspace/'.codex').resolve()
if root.name!='.codex' or root.parent!=workspace or (workspace/'.codex').is_symlink() or (workspace/'.codex').is_junction():
    raise RuntimeError('Unexpected cleanup root')
work=root/'cleanup073'
plan=work/'planned-files.jsonl.gz'
summary=json.loads((work/'plan-summary.json').read_text(encoding='utf-8'))
if Path(summary['root']).resolve()!=root or (work/'result.json').exists():
    raise RuntimeError('Wrong root or plan already consumed')

# Validate every parent once before any mutation. Reject junctions/reparse points,
# including links whose resolved destination happens to remain inside .codex.
validated={root}
def validate_parent(parent):
    if parent in validated:return
    parent.relative_to(root)
    validate_parent(parent.parent)
    info=os.lstat(parent)
    if getattr(info,'st_file_attributes',0)&0x400 or not stat.S_ISDIR(info.st_mode):
        raise RuntimeError('Reparse/non-directory parent: '+str(parent))
    if not parent.resolve().is_relative_to(root):raise RuntimeError('Escaped root')
    validated.add(parent)
parents=set()
with gzip.open(plan,'rt',encoding='utf-8') as stream:
    for line in stream:
        row=json.loads(line);rel=Path(row['path'])
        if rel.is_absolute() or '..' in rel.parts:raise RuntimeError('Invalid relative path')
        target=root/rel
        if target==root or target.is_relative_to(work):raise RuntimeError('Protected cleanup record')
        parents.add(target.parent)
for parent in sorted(parents,key=lambda p:len(p.parts)):validate_parent(parent)
print(json.dumps({'validatedRoot':str(root),'validatedParents':len(parents),'plannedFiles':summary['plannedFiles']}),flush=True)

started=time.time();last=started;removed=0;freed=0;missing=0;failures=[];changed=[]
with gzip.open(plan,'rt',encoding='utf-8') as stream:
    for line in stream:
        row=json.loads(line);target=root/row['path']
        try:
            info=os.lstat(target)
            if getattr(info,'st_file_attributes',0)&0x400 or not stat.S_ISREG(info.st_mode):
                raise RuntimeError('Not a regular unlinked file')
            if info.st_size!=row['bytes']:
                changed.append(row['path']);continue
            try:os.unlink(target)
            except PermissionError:
                os.chmod(target,stat.S_IWRITE);os.unlink(target)
            removed+=1;freed+=info.st_size
        except FileNotFoundError:missing+=1
        except Exception as exc:failures.append({'path':row['path'],'error':str(exc)})
        now=time.time()
        if now-last>=10:
            print(json.dumps({'removed':removed,'freedGiB':round(freed/1024**3,2),'elapsedSeconds':round(now-started)}),flush=True);last=now
# Remove only empty validated directories, never a recursive directory delete.
empty=0
for folder in sorted(validated-{root},key=lambda p:len(p.parts),reverse=True):
    if folder==work or folder.is_relative_to(work):continue
    try:os.rmdir(folder);empty+=1
    except OSError:pass
result={'root':str(root),'removedFiles':removed,'freedBytes':freed,'freedGiB':round(freed/1024**3,3),'remainingEstimatedBytes':summary['beforeBytes']-freed,'removedEmptyDirectories':empty,'missing':missing,'changedAndPreserved':changed,'failures':failures,'elapsedSeconds':round(time.time()-started,1)}
(work/'result.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False),flush=True)
