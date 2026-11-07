import pathlib, re, json, difflib
root = pathlib.Path(__file__).resolve().parents[1]
work = root / '.codex/island-decoration'
def blocks(path):
    text = path.read_text(encoding='utf-8-sig')
    pieces = re.split(r'(?m)^--- !u!', text)
    return {p.split('\n', 1)[0].split('&')[1].split()[0]: p for p in pieces[1:]}
before = blocks(work / 'before/Environment.unity')
after = blocks(root / 'Assets/Scenes/First Island/Environment.unity')
changed = [key for key in before.keys() & after.keys() if before[key] != after[key]]
result = dict(removed=list(before.keys()-after.keys()),added=len(after.keys()-before.keys()),changed=[])
for key in changed:
    diff=list(difflib.unified_diff(before[key].splitlines(),after[key].splitlines(),n=1))
    result['changed'].append(dict(id=key,type=before[key].splitlines()[1],diff=diff[:45]))
result['otherScenesUnchanged']={p.name:p.read_bytes()==(root/'Assets/Scenes/First Island'/p.name).read_bytes() for p in (work/'before').glob('*.unity') if p.name!='Environment.unity'}
result['regeneratedMeshPayloads']=[]
for component in ['367158186','558624518']:
    old=re.search(r'm_Mesh: \{fileID: (\d+)',before[component]).group(1)
    new=re.search(r'm_Mesh: \{fileID: (\d+)',after[component]).group(1)
    if new not in after: continue
    a=before[old].split('\n',1)[1]; b=after[new].split('\n',1)[1]
    result['regeneratedMeshPayloads'].append(dict(old=old,new=new,identical=a==b,diff=list(difflib.unified_diff(a.splitlines(),b.splitlines(),n=0))[:10] if a!=b else []))
(work/'scene-comparison.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2)[:7000])
