"""High diagonal guard revision after real hull interference during the low parry."""
import bpy,sys,json
from pathlib import Path
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v008';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v001/sahur-encounter-v001.blend'))
rig=bpy.data.objects['Armature'];source=bpy.data.actions['CH1_SahurDiagonalSlash_v001'];initial=sample(rig,source,1)
def modify(t,pose,rot):
    load=curve(t,[(0,0),(.25,.3),(.484,1),(.7,.55),(1.1,0)])
    rot('mixamorig:Spine',x=-load*5,y=load*7)
    rot('mixamorig:Spine1',x=-load*4)
    for name in ('Hips','LeftUpLeg','LeftLeg','LeftFoot','RightUpLeg','RightLeg','RightFoot','LeftShoulder','LeftArm','LeftForeArm','LeftHand'):
        loc,q,s=pose['mixamorig:'+name];a,b,c=initial['mixamorig:'+name];pose['mixamorig:'+name]=(a.copy(),b.copy(),c.copy())
    for side in ('Left','Right'):
        rot('mixamorig:'+side+'UpLeg',x=-5-load*4);rot('mixamorig:'+side+'Leg',x=9+load*6)
    rot('mixamorig:Hips',y=-load*3)
    rot('mixamorig:LeftArm',y=load*10,z=-load*8)
phase=lambda t:curve(t,[(0,1),(.28,39),(.484,39),(.68,35),(.92,12),(1.1,1)])
action=author(rig,source,'CH1_SahurParry_v008',1.1,phase,modify)
save(rig,'sahur-parry-v008',[(action,1.1,False,.484)],'mixamorig:Hips','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx')
m=json.loads((OUT/'sahur-parry-v008-index.json').read_text())[0]
m['revision']='v008';m['review_status']='accepted_for_stage';m['review_reference']=str(OUT/'source-review.json')
export(rig,action,m,OUT/(action.name+'.fbx'),slot_for(action,rig))
