"""Versioned opening stick fidget on Sahur's retained actual rig."""
import bpy, sys, json, math
from pathlib import Path
from mathutils import Vector, Quaternion
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
sys.path.insert(0,str(ROOT/'Tools/AnimationPipeline/blender'))
from common import curve_records,slot_for
from validate_animation import validate
from render_animation_preview import render
from export_unity_fbx import export
env={'__file__':str(ROOT/'Tools/AnimationPipeline/blender/author_pose_revision_v215.py')}
exec((ROOT/'Tools/AnimationPipeline/blender/author_pose_revision_v215.py').read_text().split('def reviewed_previews')[0],env)
OUT=ROOT/'ArtSource/Encounter/v221'
NAME='CH1_SahurStickIdle_v221'
BLEND=OUT/'sahur-stick-idle-v221.blend'
MODE=sys.argv[-1]
SOURCE=ROOT/'ArtSource/Encounter/v050/sahur-guard-breath-v050.blend'
if MODE=='inspect':
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    rig=bpy.data.objects['Armature'];a=bpy.data.actions['CH1_SahurGuard_v050']
    rig.animation_data.action=a;rig.animation_data.action_slot=slot_for(a,rig);bpy.context.scene.frame_set(1)
    print('RIG_AUDIT',json.dumps(dict(filepath=bpy.data.filepath,rig=rig.name,scale=list(rig.scale),units=bpy.context.scene.unit_settings.scale_length,
        fps=bpy.context.scene.render.fps,source=a.name,range=list(a.frame_range),
        bones=[dict(name=p.name,parent=p.parent.name if p.parent else None,at=list(p.matrix.translation),constraints=[c.type for c in p.constraints]) for p in rig.pose.bones],
        meshes=[o.name for o in bpy.context.scene.objects if o.type=='MESH'])))
elif MODE=='build':
    if BLEND.exists():raise FileExistsError(BLEND)
    OUT.mkdir(parents=True,exist_ok=True)
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE));rig=bpy.data.objects['Armature'];source=bpy.data.actions['CH1_SahurGuard_v050']
    rig.animation_data.action=source;rig.animation_data.action_slot=slot_for(source,rig);bpy.context.scene.frame_set(1)
    original={p.name:(p.location.copy(),p.rotation_quaternion.copy(),p.scale.copy()) for p in rig.pose.bones}
    rest={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    feet={s:rig.pose.bones[s+'Foot'].matrix.translation.copy() for s in ['Left','Right']}
    action=source.copy();action.name=NAME;action.use_fake_user=True
    for _,fc in curve_records(action):fc.keyframe_points.clear()
    rig.animation_data.action=action;rig.animation_data.action_slot=slot_for(action,rig)
    maximum_foot_drift=0;rows=[]
    for f in range(193):
        bpy.context.scene.frame_set(f+1);t=f/60;p=t/3.2*math.tau
        env['restore'](rig,original)
        # Low-key, one-handed rolling inspection: no detached aerial spin.
        # Fixed pelvis/legs retain the actual broad stance and planted soles.
        env['rotate_world'](rig.pose.bones['Spine'],Quaternion((1,0,0),math.radians(-5)))
        env['rotate_world'](rig.pose.bones['Spine2'],Quaternion((1,0,0),math.radians(-3+math.sin(p)*.6)))
        chest=rig.pose.bones['Spine2'].matrix.translation.copy()
        grip=chest+Vector((-32+5*math.sin(p),-36-4*math.sin(p),-22+3*(1-math.cos(p))))
        # Shaft stays outside the torso and above the deck throughout the roll.
        shaft=Vector((.35+.35*math.sin(p),-.94,-.12+.48*math.sin(p-.25))).normalized()
        q=env['basis_rotation'](Vector((16.7918,58.718,-96.8423))-Vector((-3.6591,12.8643,2.1512)),shaft,(0,1,0),(1,0,0))
        q=q@Quaternion((Vector((16.7918,58.718,-96.8423))-Vector((-3.6591,12.8643,2.1512))).normalized(),math.radians(18*math.sin(p)))
        near=Vector((-3.6591,12.8643,2.1512));target=grip-q@near
        env['limb'](rig,'Right','arm',target,chest+Vector((-70,-10,-10)),q)
        # Free hand hangs loosely and counterbalances, without pretending to grip.
        left=chest+Vector((40,-7,-55+2*math.sin(p+.5)))
        env['limb'](rig,'Left','arm',left,chest+Vector((68,-7,-20)))
        env['rotate_world'](rig.pose.bones['Head'],Quaternion((1,0,0),math.radians(8+2*math.sin(p))))
        env['rotate_world'](rig.pose.bones['Head'],Quaternion((0,0,1),math.radians(-8+4*math.sin(p))))
        bpy.context.view_layer.update()
        hand=rig.pose.bones['RightHand'].matrix
        a=hand@near;b=hand@Vector((16.7918,58.718,-96.8423))
        maximum_foot_drift=max(maximum_foot_drift,*[(rig.pose.bones[s+'Foot'].matrix.translation-feet[s]).length for s in feet])
        rows.append(dict(frame=f+1,grip=list(a),tip=list(b),head=list(rig.pose.bones['Head'].matrix.translation)))
        for bone in rig.pose.bones:
            for channel in ('location','rotation_quaternion','scale'):bone.keyframe_insert(data_path=channel,frame=f+1,group=bone.name)
    for _,fc in curve_records(action):
        for k in fc.keyframe_points:k.interpolation='LINEAR'
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    bpy.context.scene.render.fps=60;bpy.context.scene.render.fps_base=1;bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=193
    m=dict(blend=str(BLEND),rig=rig.name,action=NAME,unity_clip=NAME,revision='v221',frame_start=1,frame_end=193,fps=60,loop=True,
        root_motion='in_place',root_bone='root',rig_type='Humanoid',stage='BLOCKING',design_status='approved',
        design_reference='Documentation/AnimationWorkflow/Actions/CH1_SahurSharkEncounter_v001/revision-v221-20261018.md',
        approval_reference='2026-10-18 user: change the opening to Sahur playing with his stick; lower shark jump slightly',
        review_status='pending',review_reference='',avatar='Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx',contact_seconds=0,
        contacts=[dict(bone=s+'Foot',start=1,end=193,max_drift_m=.002) for s in feet])
    report=validate(rig,action,m,slot_for(action,rig))
    bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(BLEND))
    (OUT/'manifest.json').write_text(json.dumps(m,indent=2))
    (OUT/'source-audit.json').write_text(json.dumps(dict(rest_retained=True,foot_drift_source_units=maximum_foot_drift,validation=report,rows=rows),indent=2))
elif MODE in ['preview','export']:
    m=json.loads((OUT/'manifest.json').read_text());bpy.ops.wm.open_mainfile(filepath=str(BLEND));rig=bpy.data.objects[m['rig']];a=bpy.data.actions[NAME]
    if MODE=='preview':
        env['preview_contact_props'](rig)
        result=render(rig,a,OUT/'preview01',frames=list(range(1,194,16)),slot=slot_for(a,rig),resolution=384)
        (OUT/'preview01.json').write_text(json.dumps(result,indent=2))
    else:
        result=export(rig,a,m,OUT/(NAME+'.fbx'),slot_for(a,rig));(OUT/'export-report.json').write_text(json.dumps(result,indent=2))
else:raise ValueError(MODE)
