"""Inspect a downloaded Mixamo FBX in a fresh Blender process; never alter project rigs."""
import bpy, json, sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent))
from common import curve_records

src = Path(sys.argv[sys.argv.index('--') + 1]).resolve()
dst = Path(sys.argv[sys.argv.index('--') + 2]).resolve()
dst.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(src), use_anim=True)
rigs = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
report = {'source': str(src), 'fps': bpy.context.scene.render.fps / bpy.context.scene.render.fps_base,
          'objects': [(o.name, o.type) for o in bpy.context.scene.objects], 'rigs': [], 'actions': []}
for a in bpy.data.actions:
    report['actions'].append({'name': a.name, 'start': a.frame_range[0], 'end': a.frame_range[1],
                              'slots': [s.identifier for s in a.slots],
                              'curves': [{'path': c.data_path, 'index': c.array_index,
                                          'keys': len(c.keyframe_points),
                                          'first': list(c.keyframe_points[0].co) if c.keyframe_points else None,
                                          'last': list(c.keyframe_points[-1].co) if c.keyframe_points else None}
                                         for _,c in curve_records(a)]})
for rig in rigs:
    data = {'name': rig.name, 'bones': [], 'samples': []}
    for b in rig.data.bones:
        data['bones'].append({'name': b.name, 'parent': b.parent.name if b.parent else None})
    for action in bpy.data.actions:
        if not action.slots: continue
        rig.animation_data_create(); rig.animation_data.use_nla = False; rig.animation_data.action = action
        rig.animation_data.action_slot = action.slots[0]
        a, z = action.frame_range
        for f in range(int(a), int(z)+1):
            bpy.context.scene.frame_set(int(f), subframe=f-int(f))
            bpy.context.view_layer.update()
            data['samples'].append({'action': action.name, 'frame': f,
                'points': {b.name: list(rig.matrix_world @ b.head) for b in rig.pose.bones},
                'rotations': {b.name: list(b.rotation_quaternion) for b in rig.pose.bones}})
    report['rigs'].append(data)
(dst / 'inspection.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('FPS', report['fps'], 'ACTIONS', [(a['name'], a['start'], a['end'], len(a['curves'])) for a in report['actions']])
print('RIGS', [(r['name'], len(r['bones'])) for r in report['rigs']])
