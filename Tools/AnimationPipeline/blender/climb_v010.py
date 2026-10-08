"""Authored ledge climb on the retained UAL2 rig, in a new source revision."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Quaternion,Matrix
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v010';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v006/sahur-board-v006.blend'))
rig=bpy.data.objects['Armature'];source=next(a for a in bpy.data.actions if a.name.endswith('|Idle_No_Loop'));base=sample(rig,source,1)
def turn(p,q):
    at=p.head.copy();p.matrix=Matrix.Translation(at)@q.to_matrix().to_4x4()@Matrix.Translation(-at)@p.matrix;bpy.context.view_layer.update()
def solve(a,b,c,target,bend):
    upper,lower,end=[rig.pose.bones[n] for n in (a,b,c)];h=upper.head.copy();mid=lower.head.copy();at=end.head.copy();l1=(mid-h).length;l2=(at-mid).length
    axis=(target-h).normalized();distance=min(l1+l2-.001,max(abs(l1-l2)+.001,(target-h).length));v=Vector(bend);v=(v-axis*v.dot(axis)).normalized();along=(l1*l1+distance*distance-l2*l2)/(2*distance)
    knee=h+axis*along+v*math.sqrt(max(0,l1*l1-along*along));turn(upper,(mid-h).rotation_difference(knee-h));turn(lower,(end.head-lower.head).rotation_difference(target-lower.head))
def build():
    action=bpy.data.actions.new('PLAYER_Climb_v010');action.use_fake_user=True;rig.animation_data.action=action
    for i in range(70):
        t=i/69;scene=bpy.context.scene;scene.frame_set(i+1)
        for p in rig.pose.bones:p.location,p.rotation_quaternion,p.scale=base[p.name];p.rotation_mode='QUATERNION'
        bpy.context.view_layer.update()
        # Hang/reach, shoulder-led pull, compressed chest-over-edge, then stand.
        load=curve(t,[(0,.15),(.16,.4),(.42,1),(.65,.8),(.83,.45),(1,0)])
        for bone,amp in [('spine_01',18),('spine_02',13),('spine_03',8)]:turn(rig.pose.bones[bone],Quaternion((1,0,0),math.radians(amp*load)))
        turn(rig.pose.bones['neck_01'],Quaternion((1,0,0),math.radians(-16*load)))
        for side,sign in [('l',1),('r',-1)]:
            upper=rig.pose.bones['upperarm_'+side];grab=curve(t,[(0,.7),(.1,1),(.62,1),(.78,.7 if side=='l' else 1),(.9,0),(1,0)])
            press=curve(t,[(0,0),(.3,.1),(.55,.6),(.75,1),(1,1)])
            target=upper.head+Vector((sign*.10,-.46,-.02-.45*press));idle=rig.pose.bones['hand_'+side].head.copy();target=idle.lerp(target,grab)
            solve('upperarm_'+side,'lowerarm_'+side,'hand_'+side,target,(sign*.8,.15,-.5))
            # Left knee gets onto the deck first; right leg follows after the trunk clears.
            step=curve(t,[(0,0),(.28 if side=='l' else .5,0),(.57 if side=='l' else .76,1),(.8 if side=='l' else .94,.7),(1,0)])
            hip=rig.pose.bones['thigh_'+side];target=hip.head+Vector((sign*.035,-.15-step*.38,-.84+step*.5))
            settle=ease((t-.8)/.2);idle=Vector((hip.head.x,base['foot_'+side][0].y,0.10));target=target.lerp(idle,settle)
            solve('thigh_'+side,'calf_'+side,'foot_'+side,target,(0,-1,.1))
        # Preserve the source skeleton and key the actual solved bone poses.
        for p in rig.pose.bones:
            p.rotation_mode='QUATERNION'
            for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=i+1,group=p.name)
    for _,fc in curve_records(action):
        for key in fc.keyframe_points:key.interpolation='LINEAR'
    save(rig,'sahur-climb-v010',[(action,1.15,False,.55)],'root','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay/QuaterniusUAL2/UAL2_Standard.fbx')
    render(rig,action,ROOT/'.codex/encounter-v010/source-climb',[1,10,22,35,47,58,70],slot_for(action,rig),resolution=384)
if sys.argv[-1]=='export':
    path=OUT/'sahur-climb-v010-index.json';m=json.loads(path.read_text())[0];bpy.ops.wm.open_mainfile(filepath=m['blend']);rig=bpy.data.objects[m['rig']];action=bpy.data.actions[m['action']]
    review=OUT/'source-review.json'
    if not review.exists():raise ValueError('Review the rendered source before export')
    m['revision']='v010';m['review_status']='accepted_for_stage';m['review_reference']=str(review);m['stage']='SECOND PASS';export(rig,action,m,OUT/(action.name+'.fbx'),slot_for(action,rig))
else:build()

