import json, sys
from pathlib import Path

take = Path(__file__).resolve().parents[2] / '.codex/encounter-redesign-20261017' / sys.argv[1]
def read(name):
    return json.loads((take / name).read_text(encoding='utf-8-sig'))
r = read('result.json')
rows, clear, near, deck = r['rows'], read('clearance.json'), read('near-pass.json'), read('deck-gaps.json')
unique = {x['clock']: x for x in near}
supported = [x for x in deck if x['leftGap'] < 999 and x['rightGap'] < 999]
summary = dict(frames=r['frames'], fps=r['fps'], stage=r['stage'], aborted=r.get('aborted', False),
    inputChecks=r['inputChecks'], contacts=rows[-1]['RedesignContacts'], health=rows[-1]['currentHealth'],
    shots=sorted(set(x['RedesignShot'] for x in rows)),
    lensInsideFrames=sum(x['RedesignCameraLensInsideWood'] for x in clear),
    rigging=clear[-1], nearPassSamples=len(unique),
    nearPassMinimum=min(unique.values(), key=lambda x: x['gap']) if unique else None,
    soleSamples=len(supported), minLeftSole=min((x['leftGap'] for x in supported), default=None),
    minRightSole=min((x['rightGap'] for x in supported), default=None),
    sharkScales=sorted(set(tuple(x['sharkScale']) for x in deck)),
    maxDriftBodyCost=max((x['RedesignWaterBodyPenetration'] for x in rows if x['stage']=='Drift'), default=None))
(take / 'review-summary.json').write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(summary, ensure_ascii=False))
