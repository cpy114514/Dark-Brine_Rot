"""Acceptance against recorded final poses, actual contact probes and the native route."""
import json
import math
import sys
from pathlib import Path

root = Path('.codex/shark-animation-polish-20261021')
folder = root / sys.argv[1]
data = json.loads((folder / 'result.json').read_text())
rows = data['rows']
checks = []

def check(ok, text):
    checks.append(dict(passed=bool(ok), text=text))

check(rows[-1]['stage'] == 'Finished', 'Native route reaches unconscious drift and shore handoff')
check(data['health'] > 0 and data['health'] == 70, 'Approved bounded damage leaves Sahur alive at 70HP')
check(all(tuple(r['scale']) == (3, 3, 3) for r in rows), 'Original shark scale retained throughout')
check(max(r['contacts'] for r in rows) == 3, 'Exactly three cinematic hit contacts')
check(max(r['RedesignTutorialConfirmedHits'] for r in rows) == 2, 'Both real tutorial strikes confirm')
check(max(r['RedesignSolidFractures'] for r in rows) > 0, 'Shark still fractures real native wood')
check(any('Player swimming handed to pursuit' in e for e in data['events']), 'Actual swimming input hands off to pursuit')
for error in ('error.txt', 'native-audit-error.txt', 'film-audit-error.txt'):
    check(not (folder / error).exists(), 'No observer failure: ' + error)
native = json.loads((folder / 'independent-native-contacts.json').read_text())
check(len(native) == 2 and all(r.get('passed') for r in native), 'Fresh native shark/weapon/feet/timber probes pass at both tutorial contacts')
film = json.loads((folder / 'independent-film-geometry.json').read_text())
check(len(film) >= 50, 'Independent final posed-mesh samples cover the later film')
check(all(r['bodyGap'] >= -.005 for r in film), 'Independent posed shark/body envelope samples remain outside penetration')
check(all(not r['wood'] for r in film), 'Independent posed shark/native wood triangle samples remain separated')

def max_step(stage, low, high):
    return max((math.dist(a['nose'], b['nose']) for a, b in zip(rows, rows[1:]) if b['stage'] == stage and low-1e-4 <= b['localTime'] <= high+1e-4), default=None)

seams = []
for stage, lo, hi in [('Pursuit', 0, .1), ('Pursuit', 3.98, 4.1), ('Pursuit', 7.48, 7.6), ('Duel', 0, .1),
                       ('Duel', 6.78, 6.9), ('Duel', 9.98, 10.1), ('Duel', 13, 15.5), ('Duel', 15.48, 15.6), ('Duel', 19.48, 19.6)]:
    step = max_step(stage, lo, hi)
    seams.append(dict(stage=stage, low=lo, high=hi, maximum_nose_step=step))
    check(step is not None and step < 4 * 30 / data['fps'], f'Continuous shark movement through {stage} {lo}-{hi}s')

result = dict(passed=all(c['passed'] for c in checks), checks=checks, seams=seams, health=data['health'], frames=data['frame'],
              native_samples=len(film), minimum_independent_body_gap=min(r['bodyGap'] for r in film),
              intersecting_wood_samples=sum(bool(r['wood']) for r in film),
              measurement='Existing Editor final camera/poses, conservative body envelopes and independent exact shark/timber triangles; simulation Hz is not hardware FPS.')
(folder / 'acceptance.json').write_text(json.dumps(result, indent=2))
print(json.dumps({k: v for k, v in result.items() if k != 'checks'}, indent=2))
for failed in (c for c in checks if not c['passed']):
    print('FAIL:', failed['text'])
if not result['passed']:
    raise SystemExit(1)
