"""Preserve local work and prepare a reviewable UVCS three-way import."""
from pathlib import Path
import concurrent.futures, hashlib, json, shutil, subprocess, sys

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'.codex/release051'
REPO='Dark Brine_Rot/Dark Brine_Rot@3574351988897@cloud'
def run(args):
    p=subprocess.run(args,cwd=ROOT,capture_output=True)
    if p.returncode: raise RuntimeError(p.stderr.decode('utf-8','replace')+p.stdout.decode('utf-8','replace'))
    return p.stdout
def save(name,obj):
    p=OUT/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(obj,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def norm(b):return b.replace(b'\r\n',b'\n')
def backup():
    if (OUT/'preservation.json').exists():raise RuntimeError('Snapshot already exists')
    OUT.mkdir(parents=True,exist_ok=True)
    names=run(['git','ls-files','-m','-o','--exclude-standard','-z']).decode().split('\0')
    hashes={}
    for path in sorted(set(names)-{''}):
        p=ROOT/path
        if not p.is_file():continue
        target=OUT/'local-before'/path;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,target)
        hashes[path]=hashlib.sha256(p.read_bytes()).hexdigest()
    shutil.copytree(ROOT/'.plastic',OUT/'plastic-before')
    save('preservation.json',hashes)
    (OUT/'git-before.txt').write_bytes(run(['git','status','--short','--branch']))
    print('Preserved local authored files:',len(hashes))
def export():
    data=run(['cm','diff',f'cs:221@{REPO}',f'cs:224@{REPO}','--format={status}|{type}|{path}|{baserevid}|{revid}','--repositorypaths']).decode('utf-8-sig')
    (OUT/'cloud-diff.txt').write_text(data,encoding='utf-8')
    entries=[];jobs=[]
    for line in data.splitlines():
        status,kind,path,base,rev=line.split('|');path=path.strip('"').lstrip('/').replace('\\','/')
        e=dict(status=status,kind=kind,path=path,base=base,rev=rev);entries.append(e)
        if kind=='D' or status=='D':continue
        jobs.append((rev,OUT/'incoming'/path))
        if status=='C':jobs.append((base,OUT/'base'/path))
    def download(job):
        rev,dest=job;dest.parent.mkdir(parents=True,exist_ok=True)
        if not dest.exists():run(['cm','cat',f'revid:{rev}@rep:{REPO}',f'--file={dest}'])
    with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:list(pool.map(download,jobs))
    save('cloud-entries.json',entries);print('Exported revisions:',len(jobs))
def plan():
    entries=json.loads((OUT/'cloud-entries.json').read_text(encoding='utf-8'));apply=[];conflicts=[];skip=[];same=[]
    for e in entries:
        path=e['path'];p=ROOT/path;inc=OUT/'incoming'/path;dst=OUT/'candidate'/path
        if e['kind']=='D' or not path.startswith(('Assets/','ArtSource/','Tools/','Documentation/')) or path.startswith(('Assets/_Recovery/','Assets/Temp')):
            skip.append(path);continue
        if e['status']=='D':skip.append(path);continue
        # The second-island scene has an intentional local folder move with its GUID intact.
        if path=='Assets/Scenes/Second Island.unity' and not p.exists():
            path='Assets/Scenes/Second Island/Second Island.unity';p=ROOT/path;dst=OUT/'candidate'/path
        incoming=inc.read_bytes();local=p.read_bytes() if p.exists() else None
        if local is not None and norm(local)==norm(incoming):same.append(path);continue
        base=(OUT/'base'/e['path']).read_bytes() if e['status']=='C' else None
        if local is None or base is not None and norm(local)==norm(base):candidate=incoming
        elif base is not None and norm(incoming)==norm(base):same.append(path);continue
        elif base is not None and p.suffix=='.cs':
            merged=subprocess.run(['git','merge-file','-p',str(p),str(OUT/'base'/e['path']),str(inc)],capture_output=True)
            candidate=merged.stdout
            if merged.returncode:conflicts.append(path)
        else:
            candidate=incoming;conflicts.append(path)
        dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(candidate);apply.append(path)
        if p.exists():
            before=OUT/'import-before'/path;before.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,before)
    save('merge-plan.json',dict(apply=apply,conflicts=conflicts,skip=skip,alreadyIntegrated=same))
    print('Candidate files:',len(apply),'conflicts:',conflicts,'already integrated:',len(same),'excluded:',len(skip))
if __name__=='__main__':
    globals()[sys.argv[1]]()
