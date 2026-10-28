import json, hashlib
from pathlib import Path
root=Path('.codex/tutorial-cap-v263')
read=lambda p:json.loads(p.read_text(encoding='utf-8'))
r=read(root/'opening-final/review.json')
o=read(root/'observation.json')
checks=[]
def check(v,n):
 if not v: raise AssertionError(n)
 checks.append(n)
check(r['passed'], 'Complete final opening replay passes without runtime errors')
check(r['includeHelm'] and r['helmSeconds']>=18, 'Natural automatic cinematic takeover preserves at least 18 seconds of helm time')
e='\n'.join(r['events'])
for s in ('Gate Move','Accepted Move','Gate Sprint','Accepted Sprint','Gate Special','Accepted Special','Special skill: club released','Special skill: club caught','Shark fractured native plank on contact','Final tail contact','Unconscious splash'):
 check(s in e,s)
check(r['tutorialHits']==2,'Basic and heavy tutorial strikes still confirm real contact')
check(r['health']>0 and r['sawTail'] and r['sawFlight'] and r['sawSplash'],'Nonlethal finale still launches Sahur and reaches the drifting state')
check(len(o['idle'])>0 and all(x['scale']==0 for x in o['idle']), 'Idle observations occur while the world is paused')
for lesson in ('Sprint','Attack','Dodge','Special'):
 rows=[x for x in o['idle'] if x['lesson']==lesson]
 check(len(rows)>2 and all(x['clip']=='Rig|Idle_Loop' for x in rows[1:]) and max(x['clipTime'] for x in rows)-min(x['clipTime'] for x in rows)>.1 and len(set(x['hand'] for x in rows))>1,lesson+' waiting idle advances its clip and visible hand pose while the world is paused')
check(o['movement'] and all(not x['Swimming'] and x['constrained'] for x in o['movement']), 'Walking and sprinting keep the real player on the protected tutorial deck')
check(any(x['IsSprinting'] for x in o['movement'] if x['lesson']=='Sprint'), 'Sprint lesson exercises the real sprint controller')
check(o['helm'] and max(x['t'] for x in o['helm'])>17.5,'Final replay observes live sailing until the end of the control interval')
check(all(x['fidget']==0 for x in o['helm'] if x['t']<17.5),'The extended voyage does not loop the three-second club fidget')
save=Path('C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json')
check(save.read_bytes()==(root/'save-preserved.json').read_bytes(),'Campaign save remains byte-identical throughout isolated verification')
(root/'acceptance.json').write_text(json.dumps(dict(passed=True,checks=checks,bossChecks=len(read(root/'boss-result.json')['checks']),nativeOpeningFrames=r['frame'],takeoverSeconds=r['helmSeconds']),indent=2),encoding='utf-8')
print(json.dumps(dict(passed=True,openingChecks=len(checks),bossChecks=len(read(root/'boss-result.json')['checks']),nativeOpeningFrames=r['frame'],takeoverSeconds=r['helmSeconds'])))
