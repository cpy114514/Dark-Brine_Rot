import json,sys,statistics
from pathlib import Path
p=Path('.codex/encounter-redesign-20261017')/sys.argv[1]
r=json.loads((p/'result.json').read_text()); a=json.loads((p/'film-solids.json').read_text()); rows=r['rows']
bad_wood=[x for x in a if x['wood']]
bad_hero=[x for x in a if x['minimum']<-.002]
bad_timber=[x for x in a if (x.get('heroWoodDepth',0)>.001 if 'heroWoodDepth' in x else x.get('heroWoodCost',0)>.00001)]
route=json.loads((p/'adaptive-obstacle.json').read_text()) if (p/'adaptive-obstacle.json').exists() else None
performance={}
for stage in ['Tutorial','Pursuit','Duel','Drift']:
 s=sorted(x['RedesignSolidMs'] for x in a if x['stage']==stage)
 if s:performance[stage]={'samples':len(s),'median_ms':statistics.median(s),'p95_ms':s[min(len(s)-1,int(len(s)*.95))],'max_ms':s[-1]}
result={'stage':r['stage'],'frames':r['frames'],'native_samples':len(a),'contacts':rows[-1]['RedesignContacts'],'health':rows[-1]['currentHealth'],'fractures':max([x['RedesignSolidFractures'] for x in a]or[0]),'shark_wood_overlaps':len(bad_wood),'shark_hero_overlaps':len(bad_hero),'hero_timber_overlaps':len(bad_timber),'minimum_hero_gap':min(x['minimum']for x in a),'scale_unchanged':all(x['scale']==[3,3,3]for x in a),'adaptive_obstacle':route,'input_checks':r.get('inputChecks',[]),'performance_in_capture':performance,'failed_samples':(bad_wood+bad_hero+bad_timber)[:15],'events':[e for e in r.get('RedesignEvents',[])if 'success;' in e or 'Overhead contact' in e or 'Final tail contact' in e or 'fractured native plank' in e]}
result['passed']=r['stage']=='Finished'and result['contacts']==3 and not(bad_wood or bad_hero or bad_timber)and result['scale_unchanged'] and route is not None and route['difference']>.5 and route['travel']>=1.5
(p/'contact-metrics.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
