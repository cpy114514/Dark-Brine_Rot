"""Summarize native tutorial contact and scoped Editor markers, without claiming Player FPS."""
import argparse,json
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw
p=argparse.ArgumentParser();p.add_argument('label');p.add_argument('--verify',action='store_true');a=p.parse_args()
root=Path('.codex/tutorial-contact-20261021');folder=root/a.label;s=json.loads((folder/'result.json').read_text());rows=s['rows'];results={}
for lesson in ['Attack','ChargeRelease']:
    active=[r for r in rows if r['lesson']==lesson and r['executing'] and not r['TutorialWaiting']]
    hits=[r for r in active if r['impact']]
    first=hits[0] if hits else None
    steps=[float(np.linalg.norm(np.array(y['nose'])-x['nose'])) for x,y in zip(active,active[1:])]
    result={'frames':len(active),'firstImpactFrame':first['frame'] if first else None,'firstImpactSeconds':first['lessonTime'] if first else None,'postSolidWeaponGapAtImpact':first['meshGap'] if first else None,'bodyGapAtImpact':first['RedesignTutorialHeroGap'] if first else None,'maximumNoseStep':max(steps,default=0),'markers':{}}
    for i,name in enumerate(s['names']):
        vals=[r['values'][i]/1e6 for r in active if r['values'][i]>=0]
        if vals:result['markers'][name]={'medianMs':float(np.percentile(vals,50)),'p95Ms':float(np.percentile(vals,95)),'maximumMs':max(vals)}
    results[lesson]=result
    if a.verify:
        assert first is not None,lesson+' never contacts'
        assert -.15<=first['meshGap']<=.055,(lesson,first['meshGap'])
        assert first['RedesignTutorialHeroGap']>=.17,(lesson,first['RedesignTutorialHeroGap'])
        assert steps and max(steps)<(.65 if s['fps']>=30 else 1.7),(lesson,max(steps))
dodge=[r for r in rows if r['lesson']=='Dodge' and r['executing'] and not r['TutorialWaiting']]
dodge_entry=float(np.linalg.norm(np.array(dodge[1]['nose'])-dodge[0]['nose'])) if len(dodge)>1 else None
if a.verify:assert dodge_entry is not None and dodge_entry<(1 if s['fps']>=30 else 2.5),dodge_entry
charge=[r for r in rows if r['lesson']=='ChargeHold' and r['TutorialWaiting'] and r.get('chargeHold',0)>.4]
summary={'label':a.label,'fps':s['fps'],'frames':s['frame'],'health':s.get('health'),'dodgeFirstStepMetres':dodge_entry,'actions':results,'sharkScales':list({tuple(r['scale']) for r in rows}),'maximumFractures':max(r['RedesignSolidFractures'] for r in rows),'events':s['events'],'chargePoseBlendsWhileWorldPaused':bool(charge and all(r['blendWeight']>.99 for r in charge)),'measurement':'Existing Editor markers; prior completed frame. Captures/observer overhead preclude interpreting Main Thread as release FPS.'}
if a.verify:
    assert all(tuple(r['scale'])==(3,3,3) for r in rows)
    assert any('Player swimming handed to pursuit' in e for e in s['events'])
    assert s['health']>0 and summary['maximumFractures']>0
    assert summary['chargePoseBlendsWhileWorldPaused']
    native=json.loads((folder/'independent-native-contacts.json').read_text());assert len(native)==2 and all(r.get('passed') for r in native),native
    assert not (folder/'error.txt').exists() and not (folder/'native-audit-error.txt').exists()
    summary['independentNativeContacts']=native
(folder/'summary.json').write_text(json.dumps(summary,indent=2))
mapping={x['frame']:x['imageIndex'] for x in s['frameMap']};chosen=[]
for lesson in ['Attack','ChargeRelease']:
    active=[r for r in rows if r['lesson']==lesson and r['executing'] and r['frame'] in mapping]
    hits=[r for r in active if r['impact']]
    if not hits:continue
    cue=hits[0]['lessonTime']
    for t in [cue-.1,cue,cue+.1]:chosen.append(min(active,key=lambda r:abs(r['lessonTime']-t)))
if chosen:
    sheet=Image.new('RGB',(1440,588))
    for i,r in enumerate(chosen):
        x=i%3*480;y=i//3*294
        im=Image.open(folder/('frame-%04d.png'%mapping[r['frame']])).resize((480,270));sheet.paste(im,(x,y));ImageDraw.Draw(sheet).text((x+4,y+273),f"{r['lesson']} {r['lessonTime']:.3f}s gap {r['meshGap']:.3f}m",fill='white')
    sheet.save(folder/'contact-sheet.png')
print(json.dumps({k:v for k,v in summary.items() if k not in ['events']},indent=2))
