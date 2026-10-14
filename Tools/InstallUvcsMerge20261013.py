from pathlib import Path
import hashlib
import json
import re
import shutil

root=Path(__file__).resolve().parents[1]
out=root/'.codex/uvcs-conflicts-20261013'
items=json.loads((out/'audit.json').read_text())
checks=[]
for item in items:
    relative=item['path']
    current=root/relative
    backup=out/'local-before'/relative
    if current.read_bytes()!=backup.read_bytes():
        raise RuntimeError('File changed since backup: '+relative)
    candidate=out/'merged'/relative
    if item['localEqualsIncoming']:
        candidate=backup
    data=candidate.read_bytes()
    if re.search(rb'^(<<<<<<<|=======|>>>>>>>)(?: |$)',data,re.M):
        raise RuntimeError('Conflict markers in '+relative)
    if relative.endswith('.unity'):
        text=data.decode('utf-8-sig')
        anchors=re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M)
        if len(anchors)!=len(set(anchors)):
            raise RuntimeError('Duplicate scene object IDs: '+relative)
        ids=set(anchors)
        missing=[]
        for reference in re.findall(r'\{fileID: (-?\d+)([^}]*)\}',text):
            value,extra=reference
            if value!='0' and 'guid:' not in extra and value not in ids:
                missing.append(value)
        # Preserve the source baseline's known native/built-in references.
        old=backup.read_text(encoding='utf-8-sig')
        oldids=set(re.findall(r'^--- !u!\d+ &(-?\d+)',old,re.M))
        oldmissing={value for value,extra in re.findall(r'\{fileID: (-?\d+)([^}]*)\}',old) if value!='0' and 'guid:' not in extra and value not in oldids}
        newly_missing=set(missing)-oldmissing
        if newly_missing:
            raise RuntimeError('New missing scene references: '+relative+' '+str(sorted(newly_missing)))
        checks.append({'path':relative,'sceneObjects':len(anchors),'newMissingReferences':0,'existingUnresolvedReferences':len(oldmissing)})
    checks.append({'path':relative,'sha256':hashlib.sha256(data).hexdigest(),'bytes':len(data),'unchanged':data==backup.read_bytes()})
# All files pass before writing any live file.
for item in items:
    if item['localEqualsIncoming']:
        continue
    relative=item['path']
    shutil.copyfile(out/'merged'/relative,root/relative)
(out/'installed-manifest.json').write_text(json.dumps(checks,indent=2))
print(json.dumps({'installed':len(items),'sceneReferencesChecked':True,'checks':checks},indent=2))
