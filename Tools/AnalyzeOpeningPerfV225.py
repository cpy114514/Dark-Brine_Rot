import json,statistics
from pathlib import Path
root=Path('.codex/animation-performance-20261019')
def stats(values):
 values=sorted(values);return dict(n=len(values),median=statistics.median(values),p95=values[int((len(values)-1)*.95)],maximum=max(values),mean=statistics.mean(values)) if values else None
out={}
for name in ['before-30','instrumented-before-30','first-after-30','second-after-30','third-after-30','fourth-after-30','fifth-after-30','final-after-30']:
 p=root/name/'profile.json'
 if not p.exists():continue
 d=json.loads(p.read_text());runs={}
 for stage in ['Crush','Tutorial','Pursuit','Duel','Drift']:
  rows=[r for r in d['rows'] if r['stage']==stage]
  if not rows:continue
  markers={n:stats([r['counters'][i]/(1 if n=='GC Allocated In Frame' else 1e6) for r in rows]) for i,n in enumerate(d['names'])}
  runs[stage]=dict(solidMs=stats([r['solidMs'] for r in rows]),wallMs=stats([r['wallMs'] for r in rows]),markers=markers)
 out[name]=dict(stage=d['stage'],frames=d['frames'],stages=runs)
(root/'profile-comparison.json').write_text(json.dumps(out,indent=2))
for name,r in out.items():
 print(name,r['stage'],r['frames'])
 for stage in ['Pursuit','Duel']:
  s=r['stages'][stage];print(stage,'native',*[round(s['solidMs'][k],3) for k in ['median','p95','maximum']],'main',*[round(s['markers'].get('Main Thread',s['solidMs'])[k],3) for k in ['median','p95','maximum']])
