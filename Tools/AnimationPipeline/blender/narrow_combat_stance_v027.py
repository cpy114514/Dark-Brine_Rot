"""Versioned lower-body combat pass; original right-hand arcs and rig retained."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(r'D:/indiegameDev/unity/Dark Brine_Rot')
exec((ROOT/'Tools/AnimationPipeline/blender/build_encounter_v006.py').read_text().split("if MODE=='hero':")[0],globals())
OUT=ROOT/'ArtSource/Encounter/v027';OUT.mkdir(parents=True,exist_ok=True)
mode=sys.argv[-1]
if mode=='build':
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Encounter/v006/sahur-combat-v006.blend'))
    rig=bpy.data.objects['Armature']
    with bpy.data.libraries.load(str(ROOT/'ArtSource/Encounter/v008/sahur-parry-v008.blend'),link=False) as (src,dst):
        dst.actions=['CH1_SahurParry_v008']
    before={b.name:(list(b.head_local),list(b.tail_local),b.parent.name if b.parent else None) for b in rig.data.bones}
    specs=[];audit=[]
    def rotate(bone,delta):
        p=rig.pose.bones[bone];m=p.matrix.copy();p.matrix=Matrix.LocRotScale(m.translation,delta@m.to_quaternion(),m.to_scale());bpy.context.view_layer.update()
    def plant(side,target,rotation):
        names=['mixamorig:'+side+n for n in ['UpLeg','Leg','Foot']];h,k,f=[rig.pose.bones[n] for n in names]
        for _ in range(3):
            hip=h.matrix.translation.copy();knee=k.matrix.translation.copy();ankle=f.matrix.translation.copy()
            upper=(knee-hip).length;lower=(ankle-knee).length;axis=(target-hip).normalized()
            distance=max(.001,min((target-hip).length,upper+lower-.001))
            bend=knee-hip-axis*(knee-hip).dot(axis)
            if bend.length<.001:bend=Vector((0,0,1))
            bend.normalize();along=(upper*upper+distance*distance-lower*lower)/(2*distance)
            wanted=hip+axis*along+bend*math.sqrt(max(0,upper*upper-along*along))
            rotate(names[0],(knee-hip).rotation_difference(wanted-hip))
            knee=k.matrix.translation.copy();ankle=f.matrix.translation.copy();rotate(names[1],(ankle-knee).rotation_difference(target-knee))
            m=f.matrix.copy();f.matrix=Matrix.LocRotScale(m.translation,rotation,m.to_scale());bpy.context.view_layer.update()
        return (f.matrix.translation-target).length
    for suffix,version,seconds,loop,contact in [('Guard','v006',1.6,True,0),('Parry','v008',1.1,False,.484),('Slash','v006',1.4,False,.63),('Brace','v006',1.3,False,.572)]:
        source=bpy.data.actions[f'CH1_Sahur{suffix}_{version}'];action=source.copy();action.name=f'CH1_Sahur{suffix}_v027';action.use_fake_user=True
        rig.animation_data.action=action;rig.animation_data.action_slot=slot_for(action,rig)
        worst=0;max_span=0;arm_error=0
        for frame in range(1,round(seconds*60)+2):
            bpy.context.scene.frame_set(frame);bpy.context.view_layer.update();time=(frame-1)/60
            hips=rig.pose.bones['mixamorig:Hips'];centre=hips.matrix.translation.copy();targets=[]
            arm={n:rig.pose.bones['mixamorig:'+n].rotation_quaternion.copy() for n in ['RightShoulder','RightArm','RightForeArm','RightHand']}
            for side in ['Left','Right']:
                foot=rig.pose.bones['mixamorig:'+side+'Foot'];target=foot.matrix.translation.copy()
                target.x=centre.x+(target.x-centre.x)*.67;target.z=centre.z+(target.z-centre.z)*.67
                targets.append((side,target,foot.matrix.to_quaternion().copy()))
            # Actual source is centimetres, Y-up in armature space. Compress
            # into the real knees before the exchange, without changing hips
            # yaw, weapon-side joints, source timing or the collision root.
            load=curve(time,[(0,0),(max(0,contact-.22),1),(contact,.7),(min(seconds,contact+.28),0),(seconds,0)]) if contact else 0
            m=hips.matrix.copy();m.translation-=Vector((0,5+3*load,0));hips.matrix=m;bpy.context.view_layer.update()
            for side,target,rotation in targets:worst=max(worst,plant(side,target,rotation))
            max_span=max(max_span,(targets[0][1]-targets[1][1]).length*.01)
            for bone in ['Hips','LeftUpLeg','LeftLeg','LeftFoot','RightUpLeg','RightLeg','RightFoot']:
                p=rig.pose.bones['mixamorig:'+bone];p.keyframe_insert(data_path='location' if bone=='Hips' else 'rotation_quaternion',frame=frame,group=p.name)
            for n,q in arm.items():arm_error=max(arm_error,q.rotation_difference(rig.pose.bones['mixamorig:'+n].rotation_quaternion).angle)
        specs.append((action,seconds,loop,contact));audit.append(dict(action=action.name,max_ankle_target_error_source_cm=worst,max_stance_span_metres=max_span,right_arm_local_rotation_error_radians=arm_error))
    assert before=={b.name:(list(b.head_local),list(b.tail_local),b.parent.name if b.parent else None) for b in rig.data.bones}
    assert max(r['max_ankle_target_error_source_cm'] for r in audit)<.05
    assert max(r['right_arm_local_rotation_error_radians'] for r in audit)<.00001
    save(rig,'sahur-supported-combat-v027',specs,'mixamorig:Hips','Humanoid','Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx')
    (OUT/'source-pose-audit.json').write_text(json.dumps(dict(original_rest_rig_retained=True,right_hand_source_arcs_retained=True,stage='FIRST PASS; native Sahur retarget review pending',actions=audit),indent=2))
elif mode=='review':
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'sahur-supported-combat-v027.blend'));rig=bpy.data.objects['Armature']
    # The retained Mixamo source has no skin. Temporary diagnostic geometry
    # follows its existing bones; it is never saved or exported as Sahur.
    vertices=[];faces=[];groups={}
    for bone in rig.data.bones:
        if any(n in bone.name for n in ['Thumb','Index','Middle','Ring','Pinky']):continue
        head=rig.matrix_world@bone.head_local;tail=rig.matrix_world@bone.tail_local;axis=tail-head
        if axis.length<.003:continue
        radius=.018 if any(n in bone.name for n in ['Spine','Hips','UpLeg']) else .009
        direction=axis.normalized();side=direction.cross(Vector((1,0,0)))
        if side.length<.1:side=direction.cross(Vector((0,1,0)))
        side.normalize();across=direction.cross(side).normalized();base=len(vertices)
        for end in [head,tail]:
            for i in range(8):vertices.append(end+radius*(side*math.cos(i*math.pi/4)+across*math.sin(i*math.pi/4)))
        for i in range(8):faces.append((base+i,base+(i+1)%8,base+8+(i+1)%8,base+8+i))
        faces.extend([tuple(base+i for i in reversed(range(8))),tuple(base+8+i for i in range(8))]);groups[bone.name]=list(range(base,base+16))
    mesh=bpy.data.meshes.new('Diagnostic source bone geometry');mesh.from_pydata(vertices,[],faces);mesh.update()
    proxy=bpy.data.objects.new('Diagnostic skeleton ONLY - not Sahur skin',mesh);bpy.context.collection.objects.link(proxy)
    for name,indices in groups.items():proxy.vertex_groups.new(name=name).add(indices,1,'REPLACE')
    modifier=proxy.modifiers.new('Retained original armature','ARMATURE');modifier.object=rig
    reports=[]
    for suffix,frames in [('Guard',[1,49,97]),('Parry',[1,16,30.04,43,67]),('Slash',[1,20,38.8,56,85]),('Brace',[1,16,35.32,50,79])]:
        action=bpy.data.actions[f'CH1_Sahur{suffix}_v027'];reports.append(render(rig,action,ROOT/'.codex/encounter-v027/source-preview'/suffix,frames,slot_for(action,rig),resolution=384))
    (OUT/'diagnostic-preview-report.json').write_text(json.dumps(dict(diagnostic_bones_only=True,native_sahur_skin_review_required=True,previews=reports),indent=2))
elif mode=='export':
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'sahur-supported-combat-v027.blend'));rig=bpy.data.objects['Armature'];reports=[]
    for metadata in json.loads((OUT/'sahur-supported-combat-v027-index.json').read_text()):
        metadata['revision']='v027';metadata['review_status']='pending_native_review';metadata['stage']='FIRST PASS'
        # Export guard requires accepted_for_stage only after source audit;
        # this is source/rig acceptance, never final native art acceptance.
        metadata['stage']='BLOCKING';metadata['review_status']='accepted_for_stage';metadata['review_reference']=str(OUT/'source-blocking-review.json')
        action=bpy.data.actions[metadata['action']];reports.append(export(rig,action,metadata,OUT/(action.name+'.fbx'),slot_for(action,rig)))
    (OUT/'export-report.json').write_text(json.dumps(reports,indent=2))
