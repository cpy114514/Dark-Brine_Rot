"""Independent native deck isolation evidence and physics contract aggregation."""
from pathlib import Path
import json,sys,numpy as np
label=sys.argv[1] if len(sys.argv)>1 else 'deck-v249-tracked'
folder=Path('.codex/opening-charge-20261023')/label
data=json.loads((folder/'deck-clearance.json').read_text());rows=data['rows']
full=json.loads((folder/'result.json').read_text());charge=json.loads((folder/'charge-acceptance.json').read_text());geometry=json.loads((folder/'acceptance.json').read_text())
checks={
 'actual_native_reservations_observed':len(rows)>300,
 'no_other_fragment_in_reserved_footprint':all(r['overlaps']==0 for r in rows),
 'horizontal_clearance_retained':all(r['gap']>=1.23 for r in rows),
 'reserved_deck_keeps_dynamic_physics':all(not r['kinematic'] for r in rows),
 'actual_shark_fractures_remain':max(r['fractures'] for r in rows)>0,
 'native_opening_completes':full['rows'][-1]['stage']=='Finished',
 'nonlethal_health_preserved':full['health']==70,
 'tutorial_and_charge_contracts':charge['passed'],
 'shark_wood_geometry_no_new_intersections':geometry['intersecting_wood_samples']==0,
 'auditor_did_not_fail':not (folder/'deck-error.txt').exists(),
}
stats={}
for stage in sorted(set(r['stage'] for r in rows)):
 rr=[r for r in rows if r['stage']==stage]
 stats[stage]=dict(samples=len(rr),minimumDeckHeight=min(r['deckHeight'] for r in rr),minimumGap=min(r['gap'] for r in rr))
cost=np.array([r['costNs']/1e6 for r in rows[60:] if r['costNs']>=0 and r.get('rate',1)>0])
result=dict(passed=all(checks.values()),checks=checks,samples=len(rows),minimumHorizontalGap=min(r['gap'] for r in rows),overlapSamples=sum(r['overlaps']>0 for r in rows),separations=max(r['SeparationCount'] for r in rows),stages=stats,markerMedianMs=float(np.median(cost)) if len(cost) else None,markerP95Ms=float(np.percentile(cost,95)) if len(cost) else None,laterFilmContinuityPassed=geometry['passed'],laterFilmUnresolved=[c['text'] for c in geometry['checks'] if not c['passed']],scope='Actual native simulation and physics-pose mesh bounds. Water height depends on waves; arbitrary layout, low-end FPS and full artistic approval are not certified.')
(folder/'deck-acceptance.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2));sys.exit(0 if result['passed'] else 1)
