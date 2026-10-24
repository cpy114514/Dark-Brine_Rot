"""Rendered skeleton direction and complete-action checks, separate from root headings.

Use with the native input/camera recorder and independent board/geometry probes.
These assertions do not certify visual polish or hardware frame rate.
"""
import json
import math
import sys
from pathlib import Path

root = Path('.codex/shark-direction-20261023')
folder = root / sys.argv[1]
data = json.loads((folder / 'result.json').read_text())
rows = data['rows']
checks = {}


def dot(a, b):
    return sum(x*y for x, y in zip(a, b))


def yaw_angle(a, b):
    aa, bb = math.hypot(a[0], a[2]), math.hypot(b[0], b[2])
    return math.degrees(math.acos(max(-1, min(1, (a[0]*b[0]+a[2]*b[2])/max(1e-8, aa*bb)))))


walk = [r for r in rows if r['pose'] == 'PLAYER_Walk_v241' and r['blendWeight'] > .99]
checks['walk_body_faces_root_after_blend'] = len(walk) >= 10 and min(dot(r['chestForward'], r['rootForward']) for r in walk) > .85
independent = json.loads((folder / 'walk-audit.json').read_text())
checks['both_staging_lessons_independently_observed'] = {r['lesson'] for r in independent} == {'Attack', 'ChargeHold'}
checks['actual_body_faces_actual_board_relative_travel'] = len(independent) >= 8 and min(r['visualAlignment'] for r in independent) > .8

climb = [r for r in rows if r['stage'] == 'Pursuit' and 9.3 <= r['localTime'] <= 12.3]
checks['whole_climb_has_uninterrupted_ownership'] = len(climb) >= 50 and all(r['pose'] == 'PLAYER_Climb_v241' and r['blendWeight'] > .99 for r in climb)
checks['climbing_body_faces_onto_board'] = bool(climb) and min(dot(r['chestForward'], r['routeForward']) for r in climb) > .6
seam_steps = [yaw_angle(a['rootForward'], b['rootForward']) for a, b in zip(rows, rows[1:])
              if b['stage'] == 'Pursuit' and 8.98 <= b['localTime'] <= 9.3
              or b['stage'] == 'Duel' and b['localTime'] <= .75]
checks['no_half_turn_at_climb_handoffs'] = bool(seam_steps) and max(seam_steps) < 35

roll = [r for r in rows if r['stage'] == 'Tutorial' and r['lesson'] == 'Dodge' and r['pose'] == 'Rig|Roll']
checks['authored_roll_recovery_is_not_cut_off'] = bool(roll) and max(r['poseSeconds'] for r in roll) > 1.40
roll_steps = [yaw_angle(a['rootForward'], b['rootForward']) for a, b in zip(rows, rows[1:])
              if a['stage'] == b['stage'] == 'Tutorial' and a['lesson'] == b['lesson'] == 'Dodge']
checks['roll_heading_returns_without_snap'] = bool(roll_steps) and max(roll_steps) < 60

# Bowing for an overhead and turning face-up during unconscious drift are
# deliberate three-dimensional poses, not backwards locomotion failures.
checks['original_shark_scale'] = all(r['scale'] == [3, 3, 3] for r in rows)
checks['real_tutorial_hits_once_each'] = max(r['RedesignTutorialConfirmedHits'] for r in rows) == 2
checks['real_three_cinematic_contacts'] = max(r['contacts'] for r in rows) == 3
checks['actual_timber_fracture_retained'] = max(r['RedesignSolidFractures'] for r in rows) > 0
checks['nonlethal_route_completes'] = rows[-1]['stage'] == 'Finished' and data['health'] == 70

source = json.loads((root / 'normalized/audit.json').read_text())
for name in ('PLAYER_Walk_v241', 'PLAYER_Climb_v241'):
    poses = [r for r in source['rows'] if r['name'] == name]
    checks[name+'_native_pose_forward'] = len(poses) == 9 and min(r['chestForward'][2] for r in poses) > .8

baseline = json.loads((root / 'baseline-30/result.json').read_text())['rows']
old_climb = [r for r in baseline if r['stage'] == 'Pursuit' and r['pose'] == 'PLAYER_Climb_v010']
report = dict(passed=all(checks.values()), checks=checks, frames=data['frame'],
              walk_frames=len(walk), independent_walk_samples=len(independent),
              minimum_visible_walk_alignment=min(r['visualAlignment'] for r in independent),
              old_climb_frames=len(old_climb), old_climb_maximum_weight=max(r['blendWeight'] for r in old_climb),
              new_climb_frames=len(climb), new_climb_minimum_weight=min(r['blendWeight'] for r in climb),
              maximum_climb_handoff_yaw_step=max(seam_steps), maximum_dodge_yaw_step=max(roll_steps),
              maximum_roll_seconds=max(r['poseSeconds'] for r in roll), health=data['health'],
              scope='Final native skeleton and camera observations; direction/ownership and contact regression, not artistic FINAL or hardware FPS certification.')
(folder / 'direction-acceptance.json').write_text(json.dumps(report, indent=2))
print(json.dumps(report, indent=2))
if not report['passed']:
    raise SystemExit(1)
