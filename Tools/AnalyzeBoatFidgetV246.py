"""Native deck-space stance/visibility checks; not artistic FINAL approval."""
import json,math,sys
from pathlib import Path
import numpy as np
root=Path('.codex/boat-fidget-20261024')
folder=root/(sys.argv[1] if len(sys.argv)>1 else 'final-30')
data=json.loads((folder/'result.json').read_text())
setup=json.loads((folder/'setup.json').read_text())
rows=data['rows'];voyage=[r for r in rows if not r['active']];warning=[r for r in rows if r['active']]
scale=np.array(setup['shipScale'])
fidget=[r for r in rows if not r['active'] or r.get('pose')=='CH1_SahurStickIdle_v247']
def excursion(rs,key):
    return float(np.max(np.linalg.norm((np.array([r[key] for r in rs])-np.array(rs[0][key]))*scale,axis=1)))
def angular_step(rs,key):
    q=np.array([r[key] for r in rs]);q/=np.linalg.norm(q,axis=1)[:,None]
    return float(np.max(2*np.degrees(np.arccos(np.clip(np.abs(np.sum(q[1:]*q[:-1],axis=1)),0,1)))))
checks={
 'native_take_completes':data['complete'],
 'requested_fidget_duration':(2.95 <= max(r['idle'] for r in voyage)+max(r['clock'] for r in warning if r.get('pose')=='CH1_SahurStickIdle_v247') <= 3.08) if 'three-second' in str(folder) else len(voyage)>264,
 'correct_imported_fidget':setup['clip']=='CH1_SahurStickIdle_v247',
 'local_animation_ownership':all(not r['applyRootMotion'] and r['speed']==0 for r in voyage),
 'voyage_root_stays_with_deck':excursion(voyage,'root')<.01,
 'voyage_left_support_planted':excursion(voyage,'left')<.012,
 'voyage_right_support_planted':excursion(voyage,'right')<.012,
 'warning_no_standing_root_slide':excursion(warning,'root')<.003,
 'warning_left_support_planted':excursion(warning,'left')<.012,
 'warning_right_support_planted':excursion(warning,'right')<.012,
 'fidget_visible_in_existing_closeup':sum(r.get('pose')=='CH1_SahurStickIdle_v247' and r['shot']==1 for r in warning)>=30,
 'alert_follows_fidget':any(r.get('pose')=='CH1_SahurGuard_v050' and r['clock']>2.7 for r in warning),
 'wrist_no_single_frame_snap':angular_step(rows,'wrist')<30,
 'head_no_single_frame_snap':angular_step(rows,'head')<15,
 'actual_fidget_tip_motion':float(np.linalg.norm(np.ptp(np.array([r['tip'] for r in fidget]),axis=0))*3)>.7,
}
out=dict(passed=all(checks.values()),checks=checks,frames=len(rows),fidgetDurationSeconds=max(r['idle'] for r in voyage)+max((r['clock'] for r in warning if r.get('pose')=='CH1_SahurStickIdle_v247'),default=0),voyageRootExcursionM=excursion(voyage,'root'),voyageLeftFootExcursionM=excursion(voyage,'left'),voyageRightFootExcursionM=excursion(voyage,'right'),warningRootExcursionM=excursion(warning,'root'),wristMaxFrameDegrees=angular_step(rows,'wrist'),headMaxFrameDegrees=angular_step(rows,'head'),scope='Native actor/prop and ship-relative support; held-fps capture is not performance measurement. Ordered source/native/gameplay views require separate visual review.')
(folder/'acceptance.json').write_text(json.dumps(out,indent=2))
print(json.dumps(out,indent=2))
sys.exit(0 if out['passed'] else 1)
