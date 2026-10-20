"""Two targeted revisions on the retained Sahur rig; originals remain in v001."""
import bpy, sys, json, math
from pathlib import Path
from mathutils import Vector, Quaternion
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
sys.path.insert(0,str(ROOT/'Tools/AnimationPipeline/blender'))
from common import curve_records, slot_for
from validate_animation import validate
from render_animation_preview import render
from export_unity_fbx import export
env={'__file__':str(ROOT/'Tools/AnimationPipeline/blender/author_pose_revision_v215.py')}
exec((ROOT/'Tools/AnimationPipeline/blender/author_pose_revision_v215.py').read_text().split('def reviewed_previews')[0],env)
OUT=ROOT/'ArtSource/Combat/v002'
MODE=sys.argv[-1]
def assign(rig,action,frame):
    rig.animation_data.action=action;rig.animation_data.action_slot=slot_for(action,rig)
    bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
def snapshot(rig):return {p.name:(p.location.copy(),p.rotation_quaternion.copy(),p.scale.copy()) for p in rig.pose.bones}
def smooth(value):return env['ease'](value)
if MODE=='build':
    OUT.mkdir(parents=True,exist_ok=True);dest=OUT/'sahur-dual-blade-polish-v002.blend'
    if dest.exists():raise FileExistsError(dest)
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Combat/v001/sahur-combinations-v001.blend'))
    rig=bpy.data.objects['Armature'];guard=bpy.data.actions['CH1_SahurGuard_v050']
    assign(rig,guard,1);neutral=snapshot(rig)
    rest={b.name:tuple(b.matrix_local) for b in rig.data.bones};specs=[];audits=[]
    for suffix,seconds,source_name in [('BladeFinisher',1.5,'PLAYER_BladeSpin_v001'),('BladeRush',.75,'PLAYER_BladeRush_v001')]:
        source=bpy.data.actions[source_name];action=source.copy();action.name='PLAYER_'+suffix+'_v002';action.use_fake_user=True
        for _,fc in curve_records(action):fc.keyframe_points.clear()
        measures=[]
        for index in range(round(seconds*60)+1):
            t=index/60;u=t/seconds
            if suffix=='BladeRush':assign(rig,source,index+1);pose=snapshot(rig)
            else:pose=neutral
            assign(rig,action,index+1);env['restore'](rig,pose)
            chest=rig.pose.bones['Spine2'].matrix.translation.copy()
            if suffix=='BladeFinisher':
                extension=smooth(u/.24)*(1-smooth((u-.78)/.22))
                turn=smooth((u-.28)/(.74-.28))
                env['rotate_world'](rig.pose.bones['Spine'],Quaternion((1,0,0),math.radians(5*extension)))
                for side,sign in [('Right',-1),('Left',1)]:
                    shoulder=rig.pose.bones[side+'Arm'].matrix.translation.copy()
                    reach=rig.pose.bones[side+'Arm'].length+rig.pose.bones[side+'ForeArm'].length
                    target=shoulder+Vector((sign*reach*.975,0,0))
                    hand=rig.pose.bones[side+'Hand'];position=hand.matrix.translation.lerp(target,extension)
                    desired=env['basis_rotation']((sign*.18,.41,-.89),(sign,0,0),(0,1,0),(0,0,1))
                    rotation=hand.matrix.to_quaternion().slerp(desired,extension)
                    env['limb'](rig,side,'arm',position,shoulder+Vector((sign*35,-30,-8)),rotation)
                    if .28<=u<=.74:
                        axis=rotation@Vector((sign*.18,.41,-.89)).normalized()
                        measures.append(dict(frame=index+1,side=side,tip_vertical=axis.z,arm_height=position.z-shoulder.z))
                    foot=rig.pose.bones[side+'Foot'];p=foot.matrix.translation.copy()
                    p.z+=3.5*extension*max(0,math.sin(turn*math.tau+(0 if sign<0 else math.pi)))
                    env['limb'](rig,side,'leg',p,p+Vector((0,-30,28)),foot.matrix.to_quaternion())
                env['rotate_world'](rig.pose.bones['root'],Quaternion((0,0,1),math.radians(turn*360)))
            else:
                opening=smooth((t-.15)/.20)
                for side,sign in [('Right',-1),('Left',1)]:
                    hand=rig.pose.bones[side+'Hand'];matrix=hand.matrix.copy()
                    target=chest+Vector((sign*64,16+sign*3,-26))
                    desired=env['basis_rotation']((sign*.18,.41,-.89),(sign*.70,.35,-.62),(0,1,0),(0,-1,0))
                    env['limb'](rig,side,'arm',matrix.translation.lerp(target,opening),chest+Vector((sign*67,-4,-18)),matrix.to_quaternion().slerp(desired,opening))
                    if t>=.36:
                        axis=rig.pose.bones[side+'Hand'].matrix.to_quaternion()@Vector((sign*.18,.41,-.89)).normalized()
                        measures.append(dict(frame=index+1,side=side,tip_vertical=axis.z,tip_backward=axis.y))
            for p in rig.pose.bones:
                for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=index+1,group=p.name)
        for _,fc in curve_records(action):
            for key in fc.keyframe_points:key.interpolation='LINEAR'
        manifest=dict(blend=str(dest),rig=rig.name,action=action.name,unity_clip=action.name,revision='combat-v002',frame_start=1,frame_end=round(seconds*60)+1,fps=60,loop=False,root_motion='in_place',root_bone='root',rig_type='Humanoid',stage='FIRST PASS',design_status='approved',design_reference='Documentation/DualBladePolish-20261019.md',approval_reference='2026-10-19 human: 1.2x blades, combo3 horizontal outstretched arms and one turn, longer rush, down/back blade follow-through, victim recoil',review_status='pending',review_reference='')
        report=validate(rig,action,manifest,slot_for(action,rig));audits.append(dict(action=action.name,validation=report,pose_measurements=measures));specs.append(manifest)
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    bpy.context.scene.render.fps=60;bpy.context.scene.render.fps_base=1;bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(dest));(OUT/'index.json').write_text(json.dumps(specs,indent=2));(OUT/'source-audit.json').write_text(json.dumps(dict(rest_preserved=True,bones=len(rest),actions=audits),indent=2))
elif MODE in ('preview','export'):
    specs=json.loads((OUT/'index.json').read_text());bpy.ops.wm.open_mainfile(filepath=specs[0]['blend']);rig=bpy.data.objects['Armature']
    if MODE=='preview':
        for side,sign in [('Right',-1),('Left',1)]:
            shaft=Vector((sign*.18,.41,-.89)).normalized();a=Vector((-sign*4,12.5,-.5));b=a+shaft*84
            across=shaft.cross(Vector((0,1,0))).normalized()*3.6;rest=rig.data.bones[side+'Hand'].matrix_local
            mesh=bpy.data.meshes.new('PreviewBlade'+side);mesh.from_pydata([rest@v for v in [a-across,a+across,b+across*.3,b-across*.3]],[],[(0,1,2,3)]);mesh.update()
            obj=bpy.data.objects.new(mesh.name,mesh);bpy.context.scene.collection.objects.link(obj);obj.matrix_world=rig.matrix_world.copy()
            mat=bpy.data.materials.new('BladeSteel'+side);mat.diffuse_color=(.6,.65,.72,1);mesh.materials.append(mat)
            obj.vertex_groups.new(name=side+'Hand').add(list(range(4)),1,'REPLACE');obj.modifiers.new('PreviewBinding','ARMATURE').object=rig
        for m in specs:
            frames=[1,round(m['frame_end']*.28),round(m['frame_end']*.5),round(m['frame_end']*.74),m['frame_end']]
            render(rig,bpy.data.actions[m['action']],OUT/'preview01'/m['action'],frames,slot_for(bpy.data.actions[m['action']],rig),resolution=384)
    else:
        for m in specs:export(rig,bpy.data.actions[m['action']],m,OUT/(m['action']+'.fbx'),slot_for(bpy.data.actions[m['action']],rig))
else:raise ValueError(MODE)
