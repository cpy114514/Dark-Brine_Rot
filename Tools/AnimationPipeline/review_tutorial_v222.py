import json, statistics, subprocess, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'.codex/encounter-redesign-20261017/python-deps'))
import imageio_ffmpeg
folder=ROOT/'.codex/encounter-redesign-20261017'/sys.argv[1]
result=json.loads((folder/'result.json').read_text(encoding='utf-8'))
data=json.loads((folder/'tutorial.json').read_text(encoding='utf-8'))
tutorial=[r for r in data if r['stage']=='Tutorial']
metrics=dict(frames=result['frames'],stage=result['stage'],input_checks=result['inputChecks'],
    minimum_body_gap_by_lesson={lesson:min(r['RedesignTutorialHeroGap'] for r in tutorial if r['lesson']==lesson) for lesson in ['Attack','Dodge','ChargeHold','ChargeRelease']},
    maximum_dodge_wood_cost=max(r['woodCost'] for r in tutorial),
    dodge_plan_max_ms=max(r['RedesignDodgePlanMs'] for r in tutorial),
    clearance_max_ms=max(r['RedesignTutorialClearanceMs'] for r in tutorial),
    clearance_median_ms=statistics.median(r['RedesignTutorialClearanceMs'] for r in tutorial),
    preparation_max_ms=max(r['RedesignTutorialPreparationMs'] for r in data),
    swim_input_seconds=max(r['RedesignSwimControlSeconds'] for r in tutorial),
    maximum_roll_travel=max(r['RedesignDodgeTravel'] for r in tutorial),
    contact_events=[event for event in result['RedesignEvents'] if 'success;' in event],
    final_health=result['rows'][-1]['currentHealth'])
(folder/'tutorial-metrics.json').write_text(json.dumps(metrics,indent=2),encoding='utf-8')
rows=[r for r in result['rows'] if r['stage']=='Tutorial' and r['lesson']!='Move']
first=rows[0]['frame'];last=rows[-1]['frame']
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate',str(result['fps']),'-start_number',str(first),'-i',str(folder/'game-%05d.png'),'-frames:v',str(last-first+1),'-c:v','libx264','-preset','fast','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(folder/'tutorial-review.mp4')],check=True)
print(json.dumps(metrics,indent=2))
