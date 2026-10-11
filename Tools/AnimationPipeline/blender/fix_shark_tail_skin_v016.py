"""Correct dorsal-fin overlay leaking into the caudal fin on a versioned copy."""
import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'ArtSource/Encounter/v016'
OUT.mkdir(parents=True,exist_ok=True)
target=OUT/'shark-skin-v016.blend'
if target.exists():raise FileExistsError(target)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v006/shark-v006.blend'))
skin=bpy.data.objects['Tralalero_Skin'];rig=bpy.data.objects['Tralalero_Rig']
original_bones=[(b.name,tuple(b.head_local),tuple(b.tail_local)) for b in rig.data.bones]
dorsal=skin.vertex_groups['Dorsal'];rows=[]
chain=[('Spine_Rear',4.5),('Tail_Base',6.8),('Tail_Mid',9.2),('Tail_Tip',11.5)]
for v in skin.data.vertices:
    entries={skin.vertex_groups[g.group].name:g.weight for g in v.groups}
    leaking=entries.get('Dorsal',0)
    if v.co.y<=5.5 or leaking<.0001:continue
    weights=dict(entries);weights.pop('Dorsal')
    if v.co.y>=11.5:base={'Tail_Tip':1}
    else:
        for (a,lo),(b,hi) in zip(chain,chain[1:]):
            if lo<=v.co.y<=hi:
                t=(v.co.y-lo)/(hi-lo);t=t*t*(3-2*t);base={a:1-t,b:t};break
    for name,w in base.items():weights[name]=weights.get(name,0)+leaking*w
    dorsal.remove([v.index])
    for name,w in weights.items():skin.vertex_groups[name].add([v.index],w,'REPLACE')
    # The retained FBX mesh uses the reflected X coordinate, with the same
    # rest Y/Z coordinates. Unity installation validates every correspondence.
    rows.append({'x':-v.co.x,'y':v.co.y,'z':v.co.z,'weights':weights})
assert len(rows)>100
assert original_bones==[(b.name,tuple(b.head_local),tuple(b.tail_local)) for b in rig.data.bones]
bpy.ops.wm.save_as_mainfile(filepath=str(target))
(OUT/'tail-weights-v016.json').write_text(json.dumps({'source':'ArtSource/Encounter/v006/shark-v006.blend','revision':'v016','reason':'Dorsal overlay incorrectly bound the upper caudal fin to Spine_Mid','bones_preserved':len(original_bones),'updated_vertices':rows},indent=2))
print('Revised caudal vertices:',len(rows),'Bones preserved:',len(original_bones))
