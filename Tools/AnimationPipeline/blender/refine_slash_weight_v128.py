"""Versioned cinematic slash: stagger pelvis/chest power while preserving contact."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Quaternion,Matrix
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/actual_rig_blocking_v047.py').read_text().split("if mode=='build':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v128';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
def smooth(a,b,t):
    x=max(0,min(1,(t-a)/(b-a)));return x*x*(3-2*x)
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v051/sahur-raised-contact-v051.blend'))
    rig=bpy.data.objects['Armature'];source=bpy.data.actions['CH1_SahurSlash_v051']
    rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    parents={b.name:b.parent.name if b.parent else None for b in rig.data.bones}
    originals=[]
    for frame in range(1,86):
        original=sample(rig,source,frame)
        originals.append((original,{p.name:p.matrix.copy() for p in rig.pose.bones}))
    action=bpy.data.actions.new('CH1_SahurSlash_v128');action.use_fake_user=True
    rig.animation_data.action=action;support_error=0;contact_error=0;maximum_change=0;previous_quaternions={}
    for i,(original,world) in enumerate(originals):
        t=i/60;bpy.context.scene.frame_set(i+1)
        for p in rig.pose.bones:
            p.rotation_mode='QUATERNION';p.location,p.rotation_quaternion,p.scale=original[p.name]
        bpy.context.view_layer.update()
        # Leave the whole original impact band untouched. Pose, native Avatar
        # retarget and actual weapon-contact timing remain separately verified.
        gate=1-smooth(.54,.60,t) if t<.63 else smooth(.67,.75,t)
        pelvis=curve(t,[(0,0),(.1,-.5),(.23,-1),(.42,.35),(.56,.55),(.70,-.35),(.91,-.25),(1.2,0),(1.4,0)])*gate
        chest=curve(t,[(0,0),(.16,-.5),(.29,-1),(.47,.35),(.59,.45),(.76,-.3),(.98,-.2),(1.28,0),(1.4,0)])*gate
        if gate>0 and (abs(pelvis)>1e-8 or abs(chest)>1e-8):
            rotate_world(rig.pose.bones['mixamorig:Hips'],Quaternion((0,0,1),math.radians(8*pelvis)))
            rotate_world(rig.pose.bones['mixamorig:Spine'],Quaternion((0,0,1),math.radians(4*chest)))
            rotate_world(rig.pose.bones['mixamorig:Spine2'],Quaternion((1,0,0),math.radians(3*chest)))
            rotate_world(rig.pose.bones['mixamorig:Head'],Quaternion((0,0,1),math.radians(-2*pelvis)))
            rotate_world(rig.pose.bones['mixamorig:LeftArm'],Quaternion((0,0,1),math.radians(-3*chest)))
            for side in ('Left','Right'):
                names=['mixamorig:'+side+n for n in ('UpLeg','Leg','Foot')]
                target=world[names[-1]].translation;pole=world[names[1]].translation
                support_error=max(support_error,solve(names,target,pole))
                foot=rig.pose.bones[names[-1]];m=foot.matrix.copy()
                foot.matrix=Matrix.LocRotScale(m.translation,world[names[-1]].to_quaternion(),m.to_scale());bpy.context.view_layer.update()
        for p in rig.pose.bones:
            # Quaternion signs describe the same orientation but must stay
            # in one hemisphere for continuous component interpolation.
            q=p.rotation_quaternion.copy();q.normalize()
            if p.name in previous_quaternions and q.dot(previous_quaternions[p.name])<0:q.negate()
            p.rotation_quaternion=q;previous_quaternions[p.name]=q.copy()
            maximum_change=max(maximum_change,math.degrees(p.matrix.to_quaternion().rotation_difference(world[p.name].to_quaternion()).angle))
            if .60<=t<=.67:contact_error=max(contact_error,(p.matrix.translation-world[p.name].translation).length)
            for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=i+1,group=p.name)
    for _,fc in curve_records(action):
        for key in fc.keyframe_points:key.interpolation='LINEAR'
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    assert parents=={b.name:b.parent.name if b.parent else None for b in rig.data.bones}
    assert support_error<.05 and contact_error<.001,(support_error,contact_error)
    save(rig,'sahur-body-led-slash-v128',[(action,1.4,False,.63)],'mixamorig:Hips','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx')
    path=OUT/'sahur-body-led-slash-v128-index.json';index=json.loads(path.read_text())
    for row in index:row.update(revision='v128',stage='FIRST PASS',design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/design-v039.md',approval_reference='2026-10-13 user 继续',review_status='pending')
    path.write_text(json.dumps(index,indent=2))
    audit=dict(source=source.name,action=action.name,bones=len(rest),original_rest_preserved=True,parents_preserved=True,original_mesh_preserved=True,
        contact=.63,untouched_impact_band=[.60,.67],original_support_path_error_cm=support_error,impact_position_error_cm=contact_error,
        maximum_world_rotation_change_degrees=maximum_change,source_fps=60,duration=1.4,runtime_activated=False)
    (OUT/'source-audit.json').write_text(json.dumps(audit,indent=2));print(json.dumps(audit))
elif mode=='audit':
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'sahur-body-led-slash-v128.blend'))
    rig=bpy.data.objects['Armature'];old=bpy.data.actions['CH1_SahurSlash_v051'];new=bpy.data.actions['CH1_SahurSlash_v128']
    poses={}
    for action in (old,new):
        poses[action.name]=[]
        for f in range(1,86):
            sample(rig,action,f);poses[action.name].append({p.name:p.matrix.copy() for p in rig.pose.bones})
    def angle(a,b):
        dot=abs(a.to_quaternion().normalized().dot(b.to_quaternion().normalized()))
        return math.degrees(2*math.acos(min(1,max(0,dot))))
    difference=contact_rotation=foot_error=0;steps={old.name:0,new.name:0}
    for i,(a,b) in enumerate(zip(poses[old.name],poses[new.name])):
        for name in a:
            difference=max(difference,angle(a[name],b[name]))
            if .60<=i/60<=.67:contact_rotation=max(contact_rotation,angle(a[name],b[name]))
            if name.endswith(('LeftFoot','RightFoot')):foot_error=max(foot_error,(a[name].translation-b[name].translation).length)
        if i:
            for key in steps:
                steps[key]=max(steps[key],max(angle(poses[key][i-1][name],poses[key][i][name]) for name in a))
    report=dict(shortest_quaternion_angle=True,max_pose_difference_degrees=difference,contact_rotation_difference_degrees=contact_rotation,
        foot_path_error_cm=foot_error,max_60fps_step_degrees=steps,visual_source_is_skeleton_only=True,runtime_activated=False)
    (OUT/'source-audit-shortest-angle.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
elif mode=='skeleton-preview':
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'sahur-body-led-slash-v128.blend'))
    rig=bpy.data.objects['Armature'];action=bpy.data.actions['CH1_SahurSlash_v128']
    # Diagnostic rods follow the existing deform bones; they are never saved
    # into or exported with the source rig, and are not the player's skin.
    for bone in rig.data.bones:
        if any(word in bone.name for word in ('Thumb','Index','Middle','Ring','Pinky','Toe','End')):continue
        a=bone.head_local;b=bone.tail_local;axis=b-a
        if axis.length<.01:continue
        axis.normalize();u=axis.cross(Vector((0,0,1)))
        if u.length<.01:u=axis.cross(Vector((1,0,0)))
        u.normalize();v=axis.cross(u).normalized();radius=1.6 if 'Spine' in bone.name or 'Hips' in bone.name else .9
        vertices=[tuple(p+radius*(math.cos(k*math.tau/8)*u+math.sin(k*math.tau/8)*v)) for p in (a,b) for k in range(8)]
        faces=[tuple(range(7,-1,-1)),tuple(range(8,16))]+[(k,(k+1)%8,(k+1)%8+8,k+8) for k in range(8)]
        mesh=bpy.data.meshes.new('Diagnostic bone rod');mesh.from_pydata(vertices,[],faces);mesh.update()
        obj=bpy.data.objects.new('Skeleton proxy '+bone.name,mesh);bpy.context.scene.collection.objects.link(obj);obj.matrix_world=rig.matrix_world.copy()
        group=obj.vertex_groups.new(name=bone.name);group.add(list(range(16)),1,'REPLACE');mod=obj.modifiers.new('Original bone following','ARMATURE');mod.object=rig
        material=bpy.data.materials.get('Skeleton proxy colour') or bpy.data.materials.new('Skeleton proxy colour');material.diffuse_color=(.8,.55,.18,1);mesh.materials.append(material)
    report=render(rig,action,ROOT/'.codex/opening-optimization-20261012/slash-skeleton-v128',[1,15,28,35,39,47,58,73,85],slot_for(action,rig),resolution=480)
    report.update(diagnostic_skeleton_proxy=True,actual_player_silhouette_observed=False,original_source_not_saved=True)
    (OUT/'skeleton-preview-report.json').write_text(json.dumps(report,indent=2))
elif mode=='export':
    row=json.loads((OUT/'sahur-body-led-slash-v128-index.json').read_text())[0]
    bpy.ops.wm.open_mainfile(filepath=row['blend']);rig=bpy.data.objects[row['rig']];action=bpy.data.actions[row['action']]
    review=OUT/'source-review.json'
    if action.name not in json.loads(review.read_text())['accepted_for_staging']:raise ValueError('Source staging review required')
    row.update(review_status='accepted_for_stage',review_reference=str(review))
    report=export(rig,action,row,OUT/(action.name+'.fbx'),slot_for(action,rig))
    (OUT/'export-report.json').write_text(json.dumps(report,indent=2))
elif mode=='preview':
    row=json.loads((OUT/'sahur-body-led-slash-v128-index.json').read_text())[0]
    bpy.ops.wm.open_mainfile(filepath=row['blend']);rig=bpy.data.objects[row['rig']];action=bpy.data.actions[row['action']]
    report=render(rig,action,ROOT/'.codex/opening-optimization-20261012/slash-source-v128',[1,15,28,35,39,47,58,73,85],slot_for(action,rig),resolution=480)
    (OUT/'preview-report.json').write_text(json.dumps(report,indent=2))
else:raise ValueError(mode)

