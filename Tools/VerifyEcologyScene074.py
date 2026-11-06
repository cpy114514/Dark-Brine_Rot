"""Compare native Unity saves to the actual dirty-workspace baseline, not Git HEAD."""
import hashlib
import json
import re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
WORK=ROOT/'.codex/island-ecology074'

def blocks(path):
    result={}
    header=None
    lines=[]
    with path.open(encoding='utf-8-sig') as stream:
        for line in stream:
            if line.startswith('--- !u!'):
                if header:
                    value=''.join(lines)
                    result[header]=(hashlib.sha256(value.encode()).hexdigest(),value)
                header=line.strip()
                lines=[]
            else:
                lines.append(line)
    if header:
        value=''.join(lines)
        result[header]=(hashlib.sha256(value.encode()).hexdigest(),value)
    return result

before=blocks(WORK/'backup/Environment.disk-before.unity')
after=blocks(ROOT/'Assets/Scenes/First Island/Environment.unity')
removed=sorted(set(before)-set(after))
added=sorted(set(after)-set(before))
changed=[key for key in before.keys() & after.keys() if before[key][0]!=after[key][0]]
details=[]
for key in sorted(changed):
    a=before[key][1].splitlines()
    b=after[key][1].splitlines()
    import difflib
    diff=list(difflib.unified_diff(a,b,n=1))
    details.append({'block':key,'diff':diff[:100]})
report={'beforeBlocks':len(before),'afterBlocks':len(after),'removed':removed,'added':added,'changed':details}
report['meshRemapping']=[]
for filter_id in (367158186,558624518):
    key=next(k for k in before if k.endswith('&'+str(filter_id)))
    old_id=int(re.search(r'm_Mesh: \{fileID: (\d+)',before[key][1])[1])
    new_id=int(re.search(r'm_Mesh: \{fileID: (\d+)',after[key][1])[1])
    old_key=next((k for k in before if k.endswith('&'+str(old_id))),None)
    new_key=next((k for k in after if k.endswith('&'+str(new_id))),None)
    if old_key and new_key:
        a=before[old_key][1].splitlines();b=after[new_key][1].splitlines()
        report['meshRemapping'].append({'before':old_id,'after':new_id,'identicalPayload':a==b,'changedFields':[{'field':x.split(':',1)[0].strip(),'beforeLength':len(x),'afterLength':len(y)} for x,y in zip(a,b) if x!=y][:30]})
(WORK/'serialized-scene-diff.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print(json.dumps({'beforeBlocks':len(before),'afterBlocks':len(after),'removed':len(removed),'added':len(added),'changed':len(changed),'details':details,'meshes':report['meshRemapping']},indent=2)[:14000])
