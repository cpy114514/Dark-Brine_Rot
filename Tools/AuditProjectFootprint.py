"""Read-only disk inventory for cleanup; never follows links or removes files."""
import collections, json, os
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'.codex/project-cleanup-20261018'
def scan(root):
    pending=[root]
    while pending:
        try:
            with os.scandir(pending.pop()) as entries:
                for entry in entries:
                    if entry.is_symlink(): continue
                    if entry.is_dir(follow_symlinks=False): pending.append(Path(entry.path))
                    elif entry.is_file(follow_symlinks=False): yield Path(entry.path),entry.stat().st_size
        except (PermissionError,FileNotFoundError): pass
def main():
    top=collections.Counter();sub=collections.Counter();large=[];captures=collections.defaultdict(lambda:[0,0]);resources=collections.Counter()
    for path,size in scan(ROOT):
        rel=path.relative_to(ROOT);parts=rel.parts;top[parts[0]]+=size
        if len(parts)>2:sub['/'.join(parts[:2])]+=size
        if size>100_000_000:large.append(dict(path=rel.as_posix(),bytes=size))
        if parts[0]=='.codex' and path.suffix.lower()=='.png':captures[str(path.parent.relative_to(ROOT))][0]+=1;captures[str(path.parent.relative_to(ROOT))][1]+=size
        if len(parts)>3 and parts[:2]==('Assets','Resources'):resources['/'.join(parts[:min(4,len(parts)-1)])]+=size
    result=dict(top=top.most_common(),sub=sub.most_common(40),large=sorted(large,key=lambda x:x['bytes'],reverse=True)[:50],captures=sorted((dict(path=p,files=v[0],bytes=v[1]) for p,v in captures.items()),key=lambda x:x['bytes'],reverse=True),resources=resources.most_common(40))
    OUT.mkdir(parents=True,exist_ok=True);(OUT/'footprint-before.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps({k:v[:15] for k,v in result.items()},indent=2))
if __name__=='__main__':main()
