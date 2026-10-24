"""Actual opening input/pose/contact verification of the shared gameplay charge."""
import json
import math
import sys
from pathlib import Path

root = Path('.codex/opening-charge-20261023')
folder = root / sys.argv[1]
data = json.loads((folder / 'result.json').read_text())
contract = json.loads((root / 'gameplay-contract.json').read_text())
rows = data['rows']
checks = {}
active = [r for r in rows if r['gameplayChargeWeight'] > .99]
checks['native_uses_real_gameplay_animation'] = len(active) > 40 and all(r['gameplayChargeClip'] == contract['clip'] for r in active)
checks['same_full_charge_duration'] = all(abs(r['gameplayFullCharge']-contract['fullChargeTime']) < 1e-5 for r in active)
holds = [r for r in rows if r['stage'] == 'Tutorial' and r['TutorialWaiting'] and r['lesson'] in ('ChargeHold', 'ChargeRelease') and r['gameplayChargeWeight'] > 0]
checks['native_hold_reaches_gameplay_full_charge'] = bool(holds) and max(r['chargeHold'] for r in holds) >= contract['fullChargeTime']


def hold_phase(r):
    u = min(1, max(0, r['chargeHold']/contract['fullChargeTime']))
    return (u*u*(3-2*u))*contract['maxChargePosePhase']


checks['held_pose_follows_gameplay_phase_curve'] = bool(holds) and max(abs(r['gameplayChargePhase']-hold_phase(r)) for r in holds) < .001
hits = [b for a, b in zip(rows, rows[1:]) if b['RedesignTutorialConfirmedHits'] > a['RedesignTutorialConfirmedHits']]
heavy_hits = [r for r in hits if r['lesson'] == 'ChargeRelease']
checks['real_heavy_hit_is_inside_gameplay_damage_window'] = len(heavy_hits) == 1 and all(contract['heavySwingWindowStart'] <= r['gameplayChargePhase'] <= contract['heavySwingWindowEnd'] for r in heavy_hits)
checks['tutorial_recovery_finishes'] = any(r['stage'] == 'Tutorial' and r['lesson'] == 'ChargeRelease' and r['gameplayChargePhase'] > .98 for r in rows)
counter = [r for r in active if r['stage'] == 'Duel' and 3 < r['localTime'] < 6.8]
checks['cinematic_counter_reuses_hold_and_release'] = bool(counter) and min(r['gameplayChargePhase'] for r in counter) < contract['maxChargePosePhase'] and max(r['gameplayChargePhase'] for r in counter) > .98
feint = [r for r in active if r['stage'] == 'Duel' and 19.5 < r['localTime'] < 21.6]
checks['feint_uses_same_gameplay_windup'] = bool(feint) and max(r['gameplayChargePhase'] for r in feint) >= contract['maxChargePosePhase']-.001 and max(r['gameplayChargePhase'] for r in feint) <= contract['maxChargePosePhase']+.001
checks['charge_overlay_releases_for_flight_and_drift'] = all(r['gameplayChargeWeight'] == 0 for r in rows if r['pose'] == 'CH1_SahurAirHit_v214' and r['tailHit'] or r['stage'] == 'Drift')
tail_index = next((i for i, r in enumerate(rows) if r['tailHit']), None)
checks['tail_contact_keeps_actual_charge_layer'] = tail_index is not None and rows[tail_index]['gameplayChargeWeight'] > .99
if tail_index is not None:
    a, b = rows[tail_index-1], rows[tail_index]
    jumps = [math.degrees(2*math.acos(min(1, abs(sum(p*q for p, q in zip(x['rotation'], y['rotation']))))))
             for x, y in zip(a['bones'], b['bones']) if x['name'] in ('RightUpperArm', 'RightHand')]
    checks['tail_contact_has_no_preview_pose_jump'] = max(jumps) < 5
    checks['tail_contact_restores_live_collision_envelope'] = bool(a['bodyRadii']) and len(a['bodyRadii']) == len(b['bodyRadii']) and max(abs(x-y) for x, y in zip(a['bodyRadii'], b['bodyRadii'])) < .001
    following = rows[tail_index+1]
    checks['actual_flight_retains_prepared_fall_envelope'] = following['pose'] == 'CH1_SahurAirHit_v214' and len(following['bodyRadii']) == len(b['bodyRadii']) and all(x >= y-.001 for x, y in zip(following['bodyRadii'], b['bodyRadii']))
else:
    checks['tail_contact_has_no_preview_pose_jump'] = False
    checks['tail_contact_restores_live_collision_envelope'] = False
    checks['actual_flight_retains_prepared_fall_envelope'] = False
checks['both_actual_tutorial_hits_once'] = max(r['RedesignTutorialConfirmedHits'] for r in rows) == 2
checks['three_actual_film_contacts_once'] = max(r['contacts'] for r in rows) == 3
checks['original_shark_size'] = all(r['scale'] == [3, 3, 3] for r in rows)
checks['wood_breaking_and_nonlethal_finish'] = max(r['RedesignSolidFractures'] for r in rows) > 0 and rows[-1]['stage'] == 'Finished' and data['health'] == 70
result = dict(passed=all(checks.values()), checks=checks, frames=data['frame'],
              active_charge_frames=len(active), gameplay_clip=contract['clip'],
              actual_hold_seconds=max(r['chargeHold'] for r in holds),
              heavy_hit_phases=[r['gameplayChargePhase'] for r in heavy_hits],
              heavy_contact_seconds=active[0]['gameplayContactSeconds'], health=data['health'])
(folder / 'charge-acceptance.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result, indent=2))
if not result['passed']:
    raise SystemExit(1)
