"""Versioned full poses on Sahur's retained rig. No runtime bone posing."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Quaternion,Matrix
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
sys.path.insert(0,str(ROOT/'Tools/AnimationPipeline/blender'))
from common import curve_records,slot_for
from validate_animation import validate
from render_animation_preview import render
from export_unity_fbx import export
env={'__file__':str(ROOT/'Tools/AnimationPipeline/blender/author_pose_revision_v215.py')}
exec((ROOT/'Tools/AnimationPipeline/blender/author_pose_revision_v215.py').read_text().split('def reviewed_previews')[0],env)
OUT=ROOT/'ArtSource/Combat/v001'
SOURCE=ROOT/'ArtSource/Encounter/v050/sahur-guard-breath-v050.blend'
MODE=sys.argv[-1]
def reset(rig,pose):env['restore'](rig,pose)
def rotate(rig,bone,axis,degrees):env['rotate_world'](rig.pose.bones[bone],Quaternion(axis,math.radians(degrees)))
def matrix(rig,bone,p=None,q=None):env['set_matrix'](rig.pose.bones[bone],p,q)
def arm(rig,side,target,pole,q=None):env['limb'](rig,side,'arm',target,pole,q)
def smooth(t):return max(0,min(1,t))**2*(3-2*max(0,min(1,t)))
def read():
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE));rig=bpy.data.objects['Armature'];a=bpy.data.actions['CH1_SahurGuard_v050']
    rig.animation_data.action=a;rig.animation_data.action_slot=slot_for(a,rig);bpy.context.scene.frame_set(1)
    return rig,a
if MODE=='inspect':
    rig,a=read()
    print('COMBAT_RIG',json.dumps(dict(source=str(SOURCE),rig=rig.name,scale=list(rig.scale),fps=bpy.context.scene.render.fps,bones=len(rig.data.bones),pose={n:list(rig.pose.bones[n].matrix.translation) for n in ['root','Hips','Spine2','Head','RightArm','RightHand','LeftArm','LeftHand','RightFoot','LeftFoot']})))
elif MODE=='build':
    OUT.mkdir(parents=True,exist_ok=True);dest=OUT/'sahur-combinations-v001.blend'
    if dest.exists():raise FileExistsError(dest)
    rig,source=read();original={p.name:(p.location.copy(),p.rotation_quaternion.copy(),p.scale.copy()) for p in rig.pose.bones}
    rest={b.name:tuple(b.matrix_local) for b in rig.data.bones};specs=[];audit=[]
    near=Vector((-3.6591,12.8643,2.1512));far=Vector((16.7918,58.718,-96.8423))
    shaftlocal=far-near
    for suffix,seconds,loop in [('ClubSpin',.9,True),('BladeSpin',.9,True),('BladeCharge',.5,False),('BladeRush',.75,False),('BladeGuard',.6,True),('JumpChop',.85,False)]:
        name='PLAYER_'+suffix+'_v001';a=source.copy();a.name=name;a.use_fake_user=True
        for _,fc in curve_records(a):fc.keyframe_points.clear()
        rig.animation_data.action=a;rig.animation_data.action_slot=slot_for(a,rig)
        contact_errors=[]
        for i in range(round(seconds*60)+1):
            t=i/60;u=t/seconds;bpy.context.scene.frame_set(i+1);reset(rig,original)
            # Power is authored through hips/spine/shoulders; no rest/skeleton changes.
            if suffix.endswith('Spin'):
                rotate(rig,'Spine',(1,0,0),8);rotate(rig,'Spine2',(0,0,1),-6*math.sin(u*math.tau))
                chest=rig.pose.bones['Spine2'].matrix.translation.copy()
                if suffix=='ClubSpin':
                    grip=chest+Vector((-23,-28,-3));shaft=Vector((1,0,.025*math.sin(u*math.tau))).normalized()
                    q=env['basis_rotation'](shaftlocal,shaft,(0,1,0),(0,0,1))
                    arm(rig,'Right',grip-q@near,chest+Vector((-62,-12,-10)),q)
                    grip=rig.pose.bones['RightHand'].matrix@near
                    # Supporting palm lies on the actual shaft, separate from the right grip.
                    leftq=env['basis_rotation']((0,0,1),shaft,(0,1,0),(0,0,-1))
                    leftp=grip+shaft*34-leftq@Vector((0,10,0))
                    arm(rig,'Left',leftp,chest+Vector((54,-22,-20)),leftq)
                    contact_errors.append((rig.pose.bones['LeftHand'].matrix@Vector((0,10,0))-(grip+shaft*34)).length)
                else:
                    for side,sign in [('Right',-1),('Left',1)]:
                        shaft=Vector((sign*.96,-.20,.12)).normalized()
                        q=env['basis_rotation']((sign*.18,.41,-.89),shaft,(0,1,0),(0,0,1))
                        arm(rig,side,chest+Vector((sign*58,-12,-8)),chest+Vector((sign*62,6,-28)),q)
                # Alternating pivot steps: feet lift in opposition while the whole pose turns.
                for side,shift in [('Right',0),('Left',.5)]:
                    f=rig.pose.bones[side+'Foot'];p=f.matrix.translation.copy();p.z+=5*max(0,math.sin((u+shift)*math.tau))
                    env['limb'](rig,side,'leg',p,p+Vector((0,-32,35)),f.matrix.to_quaternion())
                rotate(rig,'root',(0,0,1),u*360)
            elif suffix in ('BladeCharge','BladeRush','BladeGuard'):
                raise_amount=smooth(u) if suffix=='BladeCharge' else 1
                open_amount=env['curve'](t,[(0,0),(.15,0),(.34,1),(.46,.96),(.75,.25)]) if suffix=='BladeRush' else 0
                rotate(rig,'Spine',(1,0,0),-7*raise_amount+16*open_amount)
                rotate(rig,'Spine2',(1,0,0),-4*raise_amount+8*open_amount)
                chest=rig.pose.bones['Spine2'].matrix.translation.copy()
                for side,sign in [('Right',-1),('Left',1)]:
                    # X is in front of the face/chest. Blades occupy separate depth planes.
                    held=chest+Vector((sign*(22+24*open_amount),-30+10*open_amount+sign*3,3-14*open_amount))
                    target=rig.pose.bones[side+'Hand'].matrix.translation.lerp(held,raise_amount)
                    shaft=Vector((-sign*(.57-1.5*open_amount),-.12,.82-.55*open_amount)).normalized()
                    q=env['basis_rotation']((sign*.18,.41,-.89),shaft,(0,1,0),(0,-1,0))
                    q=rig.pose.bones[side+'Hand'].matrix.to_quaternion().slerp(q,raise_amount)
                    arm(rig,side,target,chest+Vector((sign*66,-10,-20)),q)
                if suffix=='BladeGuard':
                    # Subtle breathing on the established X pose, no weapon wobble.
                    rotate(rig,'Spine',(1,0,0),math.sin(u*math.tau)*.5)
            else:
                lift=env['curve'](t,[(0,0),(.2,1),(.3,1),(.47,0),(.6,-.2),(.85,0)])
                impact=env['curve'](t,[(0,0),(.3,0),(.47,1),(.62,.7),(.85,0)])
                rotate(rig,'Spine',(1,0,0),-10*lift+21*impact);rotate(rig,'Spine2',(1,0,0),-6*lift+15*impact)
                rotate(rig,'Head',(1,0,0),45*max(0,lift)-10*impact)
                chest=rig.pose.bones['Spine2'].matrix.translation.copy()
                grip=chest+Vector((-15,-25+12*lift,5+35*lift-25*impact))
                shaft=Vector((-.15,-.75+.8*lift,.5+.7*lift-1.05*impact)).normalized()
                q=env['basis_rotation'](shaftlocal,shaft,(0,1,0),(1,0,0))
                arm(rig,'Right',grip-q@near,chest+Vector((-65,4,14)),q)
                arm(rig,'Left',chest+Vector((38,-18,-12+14*lift)),chest+Vector((62,0,-25)))
                contact_errors.append((rig.pose.bones['RightHand'].matrix@near-grip).length)
            for p in rig.pose.bones:
                for channel in ('location','rotation_quaternion','scale'):p.keyframe_insert(data_path=channel,frame=i+1,group=p.name)
        for _,fc in curve_records(a):
            for k in fc.keyframe_points:k.interpolation='LINEAR'
        m=dict(blend=str(dest),rig=rig.name,action=name,unity_clip=name,revision='combat-v001',frame_start=1,frame_end=round(seconds*60)+1,fps=60,loop=loop,root_motion='in_place',root_bone='root',rig_type='Humanoid',stage='BLOCKING',design_status='approved',design_reference='Documentation/CombatCombinations-20261019.md',approval_reference='2026-10-19 human: jump chop, two-handed horizontal club spin on both buttons, dual-blade X charge/rush outward cut, E block, dual-blade spin; continue making',review_status='pending',review_reference='',avatar='Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx')
        report=validate(rig,a,m,slot_for(a,rig));specs.append(m)
        audit.append(dict(action=name,validation=report,max_grip_error_cm=max(contact_errors,default=0)))
    assert rest=={b.name:tuple(b.matrix_local) for b in rig.data.bones}
    bpy.context.scene.render.fps=60;bpy.context.scene.render.fps_base=1;bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(dest));(OUT/'index.json').write_text(json.dumps(specs,indent=2));(OUT/'source-audit.json').write_text(json.dumps(dict(rest_preserved=True,bones=len(rest),actions=audit),indent=2))
elif MODE in ('preview','export'):
    specs=json.loads((OUT/'index.json').read_text());bpy.ops.wm.open_mainfile(filepath=specs[0]['blend']);rig=bpy.data.objects[specs[0]['rig']]
    if MODE=='preview':
        proxy,floor=env['preview_contact_props'](rig)
        blades=[]
        for side,sign in [('Right',-1),('Left',1)]:
            shaft=Vector((sign*.18,.41,-.89)).normalized();a=Vector((-sign*4,12.5,-.5));b=a+shaft*70
            across=shaft.cross(Vector((0,1,0))).normalized()*3
            rest=rig.data.bones[side+'Hand'].matrix_local
            mesh=bpy.data.meshes.new('PreviewBlade'+side);mesh.from_pydata([rest@v for v in [a-across,a+across,b+across*.3,b-across*.3]],[],[(0,1,2,3)]);mesh.update()
            obj=bpy.data.objects.new(mesh.name,mesh);bpy.context.scene.collection.objects.link(obj);obj.matrix_world=rig.matrix_world.copy()
            mat=bpy.data.materials.new('PreviewBladeSteel'+side);mat.diffuse_color=(.55,.6,.65,1);mesh.materials.append(mat)
            obj.vertex_groups.new(name=side+'Hand').add(list(range(4)),1,'REPLACE');obj.modifiers.new('PreviewBinding','ARMATURE').object=rig;blades.append(obj)
        for m in specs:
            is_blade='Blade' in m['action'];proxy.hide_render=is_blade
            for obj in blades:obj.hide_render=not is_blade
            a=bpy.data.actions[m['action']];frames=[1,round(m['frame_end']*.25),round(m['frame_end']*.5),round(m['frame_end']*.75),m['frame_end']]
            result=render(rig,a,OUT/'preview01'/m['action'],frames,slot_for(a,rig),resolution=384)
        print('PREVIEW_DONE')
    else:
        for m in specs:
            result=export(rig,bpy.data.actions[m['action']],m,OUT/(m['action']+'.fbx'),slot_for(bpy.data.actions[m['action']],rig))
        print('EXPORT_DONE')
else:raise ValueError(MODE)
