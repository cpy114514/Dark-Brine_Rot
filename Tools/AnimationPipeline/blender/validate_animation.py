import math
import re
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import *
from mathutils import Vector

def validate(rig,action,manifest,slot=None):
    errors=[];warnings=[];checks={}
    start=manifest['frame_start'];end=manifest['frame_end'];fps=manifest['fps']
    if not all(math.isfinite(v) for v in (start,end,fps)) or end<=start or fps<=0:raise ValueError('Invalid frame range/FPS')
    if abs(effective_fps()-fps)>.001:errors.append('Scene FPS does not match manifest; inspect timing rather than silently changing FPS')
    if re.search(r'(^Action(?:\.\d+)?$|final_final|attackNEW)',action.name,re.I):errors.append('Non-production Action name')
    records=curve_records(action)
    if slot:records=[(h,c) for h,c in records if h is None or h==slot.handle]
    if not records:errors.append('Action/slot has no animation curves')
    for _,curve in records:
        for key in curve.keyframe_points:
            if not all(math.isfinite(v) for p in (key.co,key.handle_left,key.handle_right) for v in p):errors.append(f'Non-finite curve: {curve.data_path}');break
        try:rig.path_resolve(curve.data_path)
        except ValueError:errors.append(f'Unresolvable animated property: {curve.data_path}')
    root=manifest.get('root_bone')
    if not root or root not in rig.pose.bones:errors.append('Manifest must identify a valid root_bone')
    contacts=manifest.get('contacts',[])
    position_tolerance=manifest.get('loop_position_tolerance_m',.002)
    rotation_tolerance=manifest.get('loop_rotation_tolerance_deg',.5)
    with scoped_action(rig,action,slot):
        def sample(frame):
            whole=math.floor(frame);bpy.context.scene.frame_set(whole,subframe=frame-whole)
            return {b.name:b.matrix.copy() for b in rig.pose.bones}
        first=sample(start);first_world=rig.matrix_world.copy()
        last=sample(end);last_world=rig.matrix_world.copy()
        unit=bpy.context.scene.unit_settings.scale_length
        if root in first:
            delta=last[root].translation-first[root].translation
            sample(start);origin=(rig.matrix_world@rig.pose.bones[root].matrix.translation)*unit
            axes=manifest.get('in_place_axes',['X','Y']) # Blender Z up; vertical bob is not horizontal travel
            if not axes or any(a not in ('X','Y','Z') for a in axes):raise ValueError('Invalid in_place_axes')
            axis_indices=[('X','Y','Z').index(a) for a in axes]
            trajectory=[]
            for frame in range(math.ceil(start),math.floor(end)+1):
                sample(frame)
                point=(rig.matrix_world@rig.pose.bones[root].matrix.translation)*unit
                trajectory.append(point)
            maximum=max(Vector(tuple((point-origin)[i] for i in axis_indices)).length for point in trajectory)
            checks['root_displacement_world_m']=list(trajectory[-1]-origin)
            checks['in_place_max_excursion_m']=maximum
            checks['in_place_axes']=axes
            if manifest['root_motion']=='in_place' and maximum>manifest.get('root_position_tolerance_m',.002):
                errors.append('In-place Action has root/object travel within the clip, not only at its endpoints')
        else:delta=None
        if manifest.get('loop'):
            max_position=0;max_rotation=0;max_scale=0
            first_root=first_world@first[root] if root in first else None
            last_root=last_world@last[root] if root in last else None
            for name,matrix in first.items():
                a=first_world@matrix;b=last_world@last[name]
                if first_root is not None and manifest['root_motion']=='root_motion':
                    a=first_root.inverted_safe()@a;b=last_root.inverted_safe()@b
                displacement=b.translation-a.translation
                max_position=max(max_position,displacement.length*unit)
                max_rotation=max(max_rotation,math.degrees(a.to_quaternion().rotation_difference(b.to_quaternion()).angle))
                max_scale=max(max_scale,(a.to_scale()-b.to_scale()).length)
            max_scale=max(max_scale,(first_world.to_scale()-last_world.to_scale()).length)
            checks['loop_seam']={'position_m':max_position,'rotation_deg':max_rotation,'scale_delta':max_scale}
            if max_position>position_tolerance or max_rotation>rotation_tolerance or max_scale>manifest.get('loop_scale_tolerance',.001):errors.append('Loop endpoint seam exceeds tolerances')
        drift=[]
        for contact in contacts:
            bone=contact['bone'];lo=contact['start'];hi=contact['end']
            if bone not in first or not(start<=lo<hi<=end):raise ValueError(f'Invalid contact interval: {contact}')
            points=[]
            for frame in range(math.ceil(lo),math.floor(hi)+1):
                pose=sample(frame)[bone];point=(rig.matrix_world@pose.translation)*unit
                velocity=contact.get('gameplay_velocity_mps',(0,0,0))
                point+=Vector(velocity)*((frame-start)/fps);points.append(point)
            maximum=max((p-points[0]).length for p in points) if points else 0
            drift.append({'bone':bone,'frames':[lo,hi],'max_drift_m':maximum})
            if maximum>contact.get('max_drift_m',.02):errors.append(f'Contact sliding: {bone}, {maximum:.4f}m')
        checks['contacts']=drift if contacts else 'not observed: no contact intervals supplied'
    if abs(rig.scale.x-rig.scale.y)>.001 or abs(rig.scale.x-rig.scale.z)>.001:warnings.append('Non-uniform armature scale; inspect skin and Unity import')
    return {'passed':not errors,'errors':errors,'warnings':warnings,'checks':checks,
            'artistic_review':'required; numeric validation does not establish silhouette, weight or clipping quality'}

if __name__=='__main__':
    p=parser('Validate numeric animation quality, loop seams, root displacement and supplied contacts')
    p.add_argument('--manifest',required=True);a=arguments(p)
    manifest=json.loads(Path(a.manifest).read_text(encoding='utf-8'));rig=rig_object(a.rig)
    action=action_object(a.action or manifest['action'],rig);slot=slot_for(action,rig,a.slot)
    result=validate(rig,action,manifest,slot);report(result,a.output)
    if not result['passed']:raise RuntimeError('Animation validation failed: '+'; '.join(result['errors']))
