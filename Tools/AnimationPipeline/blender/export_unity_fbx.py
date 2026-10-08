"""Versioned single-Action FBX export; requires actual design/review provenance."""
import os
import re
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import *
from validate_animation import validate

STAGES={'DESIGN','BLOCKING','FIRST PASS','SECOND PASS','POLISH','FINAL','APPROVED'}
def validate_manifest(data):
    required={'action','unity_clip','revision','frame_start','frame_end','fps','loop','root_motion','root_bone',
              'rig_type','stage','design_status','design_reference','approval_reference','review_status','review_reference'}
    missing=required-set(data)
    if missing:raise ValueError(f'Manifest missing {sorted(missing)}')
    if data['design_status'] not in {'approved','approval_waived'}:raise ValueError('Production export requires approved design or explicit waiver')
    if not data['design_reference'] or not data['approval_reference']:raise ValueError('Record actual design and user approval/waiver evidence')
    if not data['review_reference']:raise ValueError('Record actual review evidence')
    if data['stage'] not in STAGES or data['stage']=='DESIGN':raise ValueError('Invalid production stage for export')
    if data['review_status']!='accepted_for_stage':raise ValueError('Review has not accepted this production stage')
    if data['stage']=='APPROVED' and not data.get('release_approval_reference'):raise ValueError('APPROVED requires actual release approval evidence')
    if data['rig_type'] not in {'Humanoid','Generic'}:raise ValueError('Choose Humanoid or Generic')
    if data['root_motion'] not in {'in_place','root_motion'}:raise ValueError('Choose explicit root motion policy')
    if not isinstance(data['loop'],bool):raise ValueError('loop must be boolean')
    if not all(type(data[k]) is int for k in ('frame_start','frame_end')):raise ValueError('FBX range must use integer sample frames')
    if not re.fullmatch(r'[A-Za-z][A-Za-z0-9_]+',data['unity_clip']):raise ValueError('Unity clip name must be a stable identifier')
    start=data['frame_start'];end=data['frame_end']
    if end<=start or data['fps']<=0:raise ValueError('Invalid duration/FPS')
    for key in ('startup','active','recovery','hitbox','cancel','combo'):
        windows=data.get('gameplay',{}).get(key,[])
        if windows and isinstance(windows[0],(int,float)):windows=[windows]
        for window in windows:
            if len(window)!=2 or not(start<=window[0]<window[1]<=end):raise ValueError(f'Invalid {key} frame window: {window}')

def export(rig,action,manifest,output,slot=None):
    validate_manifest(manifest)
    if action.name!=manifest['action']:raise ValueError('Manifest and selected Action disagree')
    output=Path(output).resolve();metadata=output.with_suffix('.animation.json')
    if output.suffix.lower()!='.fbx':raise ValueError('Output must have .fbx suffix')
    temp=output.with_name('.'+output.stem+'.exporting.fbx')
    if any(p.exists() for p in (output,metadata,temp)):raise FileExistsError('Export/revision already exists; choose a new versioned name')
    result=validate(rig,action,manifest,slot)
    if not result['passed']:raise ValueError('Animation validation failed: '+str(result['errors']))
    output.parent.mkdir(parents=True,exist_ok=True)
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers)]
    scene=bpy.context.scene;saved_range=(scene.frame_start,scene.frame_end)
    try:
        with scoped_action(rig,action,slot):
            scene.frame_start=manifest['frame_start'];scene.frame_end=manifest['frame_end']
            for obj in bpy.context.selected_objects:obj.select_set(False)
            for obj in [rig]+meshes:
                if obj.hide_get() or obj.hide_viewport:raise ValueError(f'Export object hidden; inspect visibility instead of silently changing it: {obj.name}')
                obj.select_set(True)
            bpy.context.view_layer.objects.active=rig
            status=bpy.ops.export_scene.fbx(filepath=str(temp),use_selection=True,object_types={'ARMATURE','MESH'},
                global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=False,
                add_leaf_bones=False,use_armature_deform_only=False,armature_nodetype='NULL',
                bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_use_all_bones=True,
                bake_anim_force_startend_keying=True,bake_anim_step=1,bake_anim_simplify_factor=0,
                axis_forward='-Z',axis_up='Y',path_mode='AUTO',embed_textures=False)
            if 'FINISHED' not in status or not temp.exists() or temp.stat().st_size==0:raise RuntimeError('FBX export failed')
        records=curve_records(action)
        if slot:records=[(h,c) for h,c in records if h is None or h==slot.handle]
        bones=sorted({json.loads(match.group(1)) for _,c in records if (match:=re.match(r'pose\.bones\[("(?:[^"\\]|\\.)*")\]',c.data_path))})
        duration=(manifest['frame_end']-manifest['frame_start'])/manifest['fps']
        timing={}
        for name,windows in manifest.get('gameplay',{}).items():
            if windows and isinstance(windows[0],(int,float)):windows=[windows]
            timing[name]=[{'frames':window,'seconds':[(f-manifest['frame_start'])/manifest['fps'] for f in window],
                           'normalized':[(f-manifest['frame_start'])/(manifest['frame_end']-manifest['frame_start']) for f in window]}
                          for window in windows]
        metadata_value={**manifest,'format_version':1,'duration_seconds':duration,'source_blend':bpy.data.filepath,
                        'blender_version':bpy.app.version_string,'rig':rig.name,'slot':slot.identifier if slot else None,
                        'animated_bones':bones,'selected_meshes':[m.name for m in meshes],
                        'axis_forward':'-Z','axis_up':'Y','unit_scale':scene.unit_settings.scale_length,
                        'validation':result,'gameplay_timing':timing,'fbx':output.name}
        # Link publishes a completed file atomically and refuses an existing destination.
        os.link(temp,output)
        try:
            with metadata.open('x',encoding='utf-8') as stream:json.dump(metadata_value,stream,indent=2,allow_nan=False)
        except Exception:
            # This is only the freshly created export, never a previous production file.
            output.unlink();raise
        return metadata_value
    finally:
        scene.frame_start,scene.frame_end=saved_range
        if temp.exists():temp.unlink()

if __name__=='__main__':
    p=parser('Export one reviewed Action to a new FBX revision; scene state is restored')
    p.add_argument('--manifest',required=True);p.add_argument('--fbx',required=True);a=arguments(p)
    data=json.loads(Path(a.manifest).read_text(encoding='utf-8'));rig=rig_object(a.rig);action=action_object(a.action or data['action'],rig)
    report(export(rig,action,data,a.fbx,slot_for(action,rig,a.slot)),a.output)
