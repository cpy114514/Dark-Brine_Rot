"""Verify final native poses, phase continuity and fresh contact probes."""
import json
import math
import sys
from pathlib import Path

folder = Path('.codex/tutorial-facing-20261023') / sys.argv[1]
data = json.loads((folder / 'result.json').read_text())
rows = data['rows']
walk = [r for r in rows if r['stage'] == 'Tutorial' and r['staging'] and r['poseName'] == 'PLAYER_Walk_v006']
contacts = json.loads((folder / 'independent-native-contacts.json').read_text())
steps = json.loads((folder / 'walk-audit.json').read_text())

def alignment(row):
    f, v = row['facing'], row['step']
    return (f[0]*v[0] + f[2]*v[2]) / math.hypot(v[0], v[2])

checks = {
    'reaches_pursuit': rows[-1]['stage'] == 'Pursuit',
    'both_staging_lessons_observed': {r['lesson'] for r in walk} == {'Attack', 'ChargeHold'},
    'walk_faces_travel': bool(walk) and all(alignment(r) > .99 for r in walk),
    'both_native_contacts_pass': len(contacts) == 2 and all(r.get('passed') for r in contacts),
    'no_independent_backpedal': bool(steps) and all(r['alignment'] > 0 for r in steps),
    'nonlethal_tutorial': data['health'] == 88,
    'original_shark_scale': all(r['scale'] == [3, 3, 3] for r in rows),
    'no_observer_errors': not any((folder / e).exists() for e in ['error.txt', 'walk-audit-error.txt', 'native-audit-error.txt'])
}
for lesson in ['Attack', 'ChargeHold']:
    staging = [r for r in rows if r['staging'] and r['lesson'] == lesson]
    moving = [r for r in staging if r['poseName'] == 'PLAYER_Walk_v006']
    checks[lesson + '_uninterrupted_gait'] = bool(moving) and all(
        r['poseName'] == 'PLAYER_Walk_v006' for r in staging if moving[0]['frame'] <= r['frame'] <= moving[-1]['frame'])
    checks[lesson + '_committed_phase_durations'] = bool(staging) and len({
        (r['turnIn'], r['walkDuration'], r['turnOut']) for r in staging}) == 1

result = dict(passed=all(checks.values()), checks=checks, frames=data['frame'], walk_frames=len(walk),
              minimum_facing_path_dot=min(map(alignment, walk), default=None), independent_movement_samples=len(steps),
              minimum_independent_dot=min((r['alignment'] for r in steps), default=None), health=data['health'])
(folder / 'acceptance.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result, indent=2))
if not result['passed']:
    raise SystemExit(1)
