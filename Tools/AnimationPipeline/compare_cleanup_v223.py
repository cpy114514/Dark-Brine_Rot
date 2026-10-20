"""Compare measured function costs; captured videos do not benchmark FPS."""
import json,statistics,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
base=ROOT/'.codex/encounter-redesign-20261017'
output={}
for label in ('before','after'):
    folder=base/(sys.argv[1] if label=='after' and len(sys.argv)>1 else f'cleanup-v223-{label}-30')
    result=json.loads((folder/'result.json').read_text())
    rows=json.loads((folder/'tutorial.json').read_text())
    hot=[r for r in rows if r['stage']=='Tutorial' and r['lesson'] in ('Attack','Dodge','ChargeHold','ChargeRelease') and r['RedesignTutorialClearanceMs']>0]
    values=sorted(r['RedesignTutorialClearanceMs'] for r in hot)
    output[label]=dict(samples=len(hot),clearance_median_ms=statistics.median(values),clearance_p95_ms=values[int((len(values)-1)*.95)],clearance_max_ms=max(values),route_max_ms=max(r['RedesignDodgePlanMs'] for r in rows),health=result['rows'][-1]['currentHealth'],stage=result['stage'],input_checks=result['inputChecks'])
native=json.loads((base/(sys.argv[1] if len(sys.argv)>1 else 'cleanup-v223-after-30')/'native-verification.json').read_text())
output['native']=dict(samples=len(native),bake_error=max(r['bakeError'] for r in native),matrix_error=max(r['matrixError'] for r in native),bounded_query_error=max(r['maximumError'] for r in native),dodge_actual_minimum=min(r['minimum'] for r in native if r['lesson']=='Dodge'),exact_query_total_ms=sum(r['exactMs'] for r in native),bounded_query_total_ms=sum(r['cappedMs'] for r in native))
(ROOT/'.codex/project-cleanup-20261018/comparison.json').write_text(json.dumps(output,indent=2),encoding='utf-8')
print(json.dumps(output,indent=2))
