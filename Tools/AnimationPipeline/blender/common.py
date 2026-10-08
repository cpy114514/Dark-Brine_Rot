"""Shared Blender 4.2+/5.x inspection and scoped Action utilities."""
import argparse
from contextlib import contextmanager
import json
from pathlib import Path
import sys
import bpy

def arguments(parser):
    argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
    return parser.parse_args(argv)

def parser(description):
    p=argparse.ArgumentParser(description=description)
    p.add_argument('--rig',help='Armature object name; autodetect only when unambiguous')
    p.add_argument('--action',help='Exact Action datablock name')
    p.add_argument('--slot',help='Explicit layered Action slot identifier if ambiguous')
    p.add_argument('--output',help='JSON report path; existing reports are not overwritten')
    return p

def rig_object(name=None):
    rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
    if name:
        obj=bpy.context.scene.objects.get(name)
        if obj is None or obj.type!='ARMATURE':raise ValueError(f'Armature not found: {name}')
        return obj
    if len(rigs)!=1:raise ValueError(f'Choose --rig; found {len(rigs)} armatures: {[r.name for r in rigs]}')
    return rigs[0]

def action_object(name=None,rig=None):
    if name:
        action=bpy.data.actions.get(name)
        if action is None:raise ValueError(f'Action not found: {name}')
        return action
    if rig and rig.animation_data and rig.animation_data.action:return rig.animation_data.action
    if len(bpy.data.actions)==1:return bpy.data.actions[0]
    raise ValueError('Choose --action; active Action is missing or selection is ambiguous')

def curve_records(action):
    records=[]
    layers=getattr(action,'layers',())
    for layer in layers:
        for strip in layer.strips:
            for bag in getattr(strip,'channelbags',()):
                for curve in bag.fcurves:records.append((getattr(bag,'slot_handle',None),curve))
    if not records and hasattr(action,'fcurves'):
        for curve in action.fcurves:records.append((None,curve))
    return records

def slot_for(action,rig,identifier=None):
    slots=list(getattr(action,'slots',()))
    if not slots:return None
    if identifier:
        matches=[s for s in slots if s.identifier==identifier]
    elif rig.animation_data and rig.animation_data.action==action and rig.animation_data.action_slot:
        matches=[rig.animation_data.action_slot]
    else:matches=slots
    if len(matches)!=1:raise ValueError(f'Choose --slot; Action has slots {[s.identifier for s in slots]}')
    slot=matches[0]
    if getattr(slot,'target_id_type','OBJECT')!='OBJECT':raise ValueError('Action slot does not target an Object')
    return slot

def effective_fps(scene=None):
    scene=scene or bpy.context.scene
    return scene.render.fps/scene.render.fps_base

def report(data,output=None):
    text=json.dumps(data,indent=2,ensure_ascii=False,allow_nan=False)
    if output:
        path=Path(output).resolve();path.parent.mkdir(parents=True,exist_ok=True)
        with path.open('x',encoding='utf-8') as stream:stream.write(text+'\n')
    print(text)
    return data

@contextmanager
def scoped_action(rig,action,slot=None):
    """Restore scene/pose/selection state after preview, sampling or export."""
    scene=bpy.context.scene
    had_animation=rig.animation_data is not None
    animation=rig.animation_data_create()
    if getattr(animation,'use_tweak_mode',False):raise ValueError('Exit NLA tweak mode before scoped export/preview')
    saved_action=animation.action;saved_slot=getattr(animation,'action_slot',None)
    use_nla=animation.use_nla
    frame=scene.frame_current;subframe=scene.frame_subframe
    basis=rig.matrix_basis.copy();poses={p.name:p.matrix_basis.copy() for p in rig.pose.bones}
    active=bpy.context.view_layer.objects.active;selection=list(bpy.context.selected_objects)
    mode=active.mode if active else 'OBJECT'
    try:
        if active and mode!='OBJECT':bpy.ops.object.mode_set(mode='OBJECT')
        animation.action=action
        if slot:animation.action_slot=slot
        animation.use_nla=False
        yield
    finally:
        animation.action=saved_action
        if saved_slot and saved_action:animation.action_slot=saved_slot
        animation.use_nla=use_nla
        rig.matrix_basis=basis
        for name,matrix in poses.items():rig.pose.bones[name].matrix_basis=matrix
        scene.frame_set(frame,subframe=subframe)
        for obj in bpy.context.selected_objects:obj.select_set(False)
        for obj in selection:
            if obj.name in bpy.context.view_layer.objects:obj.select_set(True)
        bpy.context.view_layer.objects.active=active
        if active and mode!='OBJECT':bpy.ops.object.mode_set(mode=mode)
        if not had_animation:rig.animation_data_clear()

def action_summary(action):
    records=curve_records(action)
    keys=[float(k.co.x) for _,c in records for k in c.keyframe_points]
    return {'name':action.name,'users':action.users,'fake_user':action.use_fake_user,
            'frame_range':list(action.frame_range),'key_range':[min(keys),max(keys)] if keys else None,
            'curves':len(records),'keys':sum(len(c.keyframe_points) for _,c in records),
            'slots':[{'identifier':s.identifier,'target':s.target_id_type} for s in getattr(action,'slots',())]}
