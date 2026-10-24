"""Retained Sahur rig: single-hand club spin and weapon-specific jump chops."""
import bpy, sys, math, json
from pathlib import Path
from mathutils import Vector, Quaternion

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(Path(__file__).parent))
from common import curve_records, slot_for
from validate_animation import validate
from render_animation_preview import render
from export_unity_fbx import export

env = {'__file__': str(Path(__file__).with_name('author_pose_revision_v215.py'))}
exec(Path(env['__file__']).read_text().split('def reviewed_previews')[0], env)
OUT = ROOT / 'ArtSource/Combat/v003'
MODE = sys.argv[-1]
SOURCE = ROOT / 'ArtSource/Combat/v001/sahur-combinations-v001.blend'

def rotate(rig, name, axis, angle):
    env['rotate_world'](rig.pose.bones[name], Quaternion(axis, math.radians(angle)))

def arm(rig, side, position, pole, rotation=None):
    env['limb'](rig, side, 'arm', position, pole, rotation)

if MODE == 'build':
    OUT.mkdir(parents=True, exist_ok=True)
    dest = OUT / 'sahur-melee-completion-v003.blend'
    if dest.exists(): raise FileExistsError(dest)
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    rig = bpy.data.objects['Armature']
    rest = {b.name: tuple(b.matrix_local) for b in rig.data.bones}
    specs, audits = [], []
    near = Vector((-3.6591, 12.8643, 2.1512))
    shaft_local = Vector((16.7918, 58.718, -96.8423)) - near
    for label, parent in [('ClubSpinOneHand', 'ClubSpin'), ('ClubJumpChop', 'JumpChop'), ('BladeJumpChop', 'JumpChop')]:
        source = bpy.data.actions['PLAYER_' + parent + '_v001']
        action = source.copy(); action.name = 'PLAYER_' + label + '_v003'; action.use_fake_user = True
        # Sample the retained whole action before replacing this revision's keys.
        rig.animation_data.action = source; rig.animation_data.action_slot = slot_for(source, rig)
        count = 55 if parent == 'ClubSpin' else 52
        poses = []
        for frame in range(1, count + 1):
            bpy.context.scene.frame_set(frame)
            poses.append({p.name: (p.location.copy(), p.rotation_quaternion.copy(), p.scale.copy()) for p in rig.pose.bones})
        for _, curve in curve_records(action): curve.keyframe_points.clear()
        rig.animation_data.action = action; rig.animation_data.action_slot = slot_for(action, rig)
        grip_errors = []
        for i, pose in enumerate(poses):
            t = i / 60; u = i / (count - 1)
            bpy.context.scene.frame_set(i + 1); env['restore'](rig, pose)
            chest = rig.pose.bones['Spine2'].matrix.translation.copy()
            if parent == 'ClubSpin':
                # Keep the original pivot/torso/weapon arc, release the support
                # hand into a clear counterbalance. No runtime bone overrides.
                arm(rig, 'Left', chest + Vector((35, 13, -23)), chest + Vector((62, 8, -25)))
            else:
                lift = env['curve'](t, [(0, 0), (.2, 1), (.3, 1), (.47, 0), (.6, -.2), (.85, 0)])
                impact = env['curve'](t, [(0, 0), (.3, 0), (.47, 1), (.62, .7), (.85, 0)])
                if label == 'BladeJumpChop':
                    # Parallel, separate blade lanes: raise -> accelerate down
                    # and forward -> low follow-through -> balanced recovery.
                    blend = env['curve'](t, [(0, 0), (.13, 1), (.66, 1), (.85, 0)])
                    for side, sign in [('Right', -1), ('Left', 1)]:
                        hand = rig.pose.bones[side + 'Hand']
                        start_p, start_q = hand.matrix.translation.copy(), hand.matrix.to_quaternion()
                        target = chest + Vector((sign * (27 + 10 * impact), -25 + 12 * lift - 12 * impact, -3 + 40 * lift - 36 * impact))
                        shaft = Vector((sign * .12, -.65 + .55 * lift, .25 + .85 * lift - 1.05 * impact)).normalized()
                        q = env['basis_rotation']((sign * .18, .41, -.89), shaft, (0, 1, 0), (1, 0, 0))
                        arm(rig, side, start_p.lerp(target, blend), chest + Vector((sign * 65, -3, -12)), start_q.slerp(q, blend))
                # Small tuck/re-extension belongs to this full authored pose;
                # CharacterController alone owns the physical jump trajectory.
                tuck = max(0, lift) * .55
                for side in ('Right', 'Left'):
                    foot = rig.pose.bones[side + 'Foot']
                    p, q = foot.matrix.translation.copy(), foot.matrix.to_quaternion()
                    p += Vector((0, 10 * tuck, 10 * tuck))
                    env['limb'](rig, side, 'leg', p, p + Vector((0, -38, 38)), q)
            for bone in rig.pose.bones:
                for channel in ('location', 'rotation_quaternion', 'scale'):
                    bone.keyframe_insert(data_path=channel, frame=i + 1, group=bone.name)
        for _, curve in curve_records(action):
            for key in curve.keyframe_points: key.interpolation = 'LINEAR'
        manifest = dict(blend=str(dest), rig=rig.name, action=action.name, unity_clip=action.name,
            revision='combat-v003', frame_start=1, frame_end=count, fps=60, loop=parent == 'ClubSpin',
            root_motion='in_place', root_bone='root', rig_type='Humanoid', stage='SECOND PASS',
            design_status='approved', design_reference='Documentation/MeleeCompletion-20261022.md',
            approval_reference='2026-10-22 human: jump chop must support both melee weapons; club only needs one hand',
            review_status='pending', review_reference='', avatar='Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx')
        if parent != 'ClubSpin': manifest['gameplay'] = dict(startup=[1, 18], active=[18, 35], recovery=[35, count], hitbox=[18, 35])
        audit = validate(rig, action, manifest, slot_for(action, rig))
        if not audit['passed']: raise ValueError(audit)
        specs.append(manifest); audits.append(dict(action=action.name, validation=audit))
    assert rest == {b.name: tuple(b.matrix_local) for b in rig.data.bones}
    bpy.context.scene.render.fps = 60; bpy.context.scene.render.fps_base = 1
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(dest))
    (OUT / 'index.json').write_text(json.dumps(specs, indent=2))
    (OUT / 'source-audit.json').write_text(json.dumps(dict(rest_preserved=True, actions=audits), indent=2))
elif MODE in ('preview', 'export'):
    specs = json.loads((OUT / 'index.json').read_text())
    bpy.ops.wm.open_mainfile(filepath=specs[0]['blend']); rig = bpy.data.objects['Armature']
    if MODE == 'preview':
        proxy, floor = env['preview_contact_props'](rig)
        blades = []
        for side, sign in [('Right', -1), ('Left', 1)]:
            shaft = Vector((sign * .18, .41, -.89)).normalized()
            a = Vector((-sign * 4, 12.5, -.5)); b = a + shaft * 70
            across = shaft.cross(Vector((0, 1, 0))).normalized() * 3
            rest = rig.data.bones[side + 'Hand'].matrix_local
            mesh = bpy.data.meshes.new('PreviewBlade' + side)
            mesh.from_pydata([rest @ v for v in [a-across, a+across, b+across*.3, b-across*.3]], [], [(0, 1, 2, 3)]); mesh.update()
            obj = bpy.data.objects.new(mesh.name, mesh); bpy.context.scene.collection.objects.link(obj)
            obj.matrix_world = rig.matrix_world.copy()
            mat = bpy.data.materials.new('PreviewSteel' + side); mat.diffuse_color = (.55, .6, .65, 1)
            mesh.materials.append(mat); obj.vertex_groups.new(name=side + 'Hand').add(list(range(4)), 1, 'REPLACE')
            obj.modifiers.new('PreviewBinding', 'ARMATURE').object = rig; blades.append(obj)
        for manifest in specs:
            is_blade = 'Blade' in manifest['action']; proxy.hide_render = is_blade
            for obj in blades: obj.hide_render = not is_blade
            action = bpy.data.actions[manifest['action']]
            render(rig, action, OUT / 'preview02' / action.name, [1, 13, 23, 30, 40, manifest['frame_end']], slot_for(action, rig), resolution=384)
    else:
        for manifest in specs:
            action = bpy.data.actions[manifest['action']]
            export(rig, action, manifest, OUT / (action.name + '.fbx'), slot_for(action, rig))
else: raise ValueError(MODE)
