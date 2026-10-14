"""Native continuous guard evidence; no source-generated screenshots substituted."""
import json,math,subprocess
from pathlib import Path
import imageio_ffmpeg
root=Path(r'D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/native-continuous-v050')
samples=json.loads((root/'native-samples.json').read_text(encoding='utf-8'))
def distance(a,b):return math.sqrt(sum((x-y)**2 for x,y in zip(a,b)))
report={"stage":"FIRST PASS","original_avatar":True,"native_samples":len(samples),"seconds":samples[-1]['time'],
        "left_ankle_drift_m":max(distance(s['leftFoot'],samples[0]['leftFoot']) for s in samples),
        "right_ankle_drift_m":max(distance(s['rightFoot'],samples[0]['rightFoot']) for s in samples),
        "right_hand_travel_m":max(distance(s['rightHand'],samples[0]['rightHand']) for s in samples),
        "seam_right_hand_m":distance(samples[-1]['rightHand'],samples[0]['rightHand']),
        "paired_contacts":"not yet reviewed"}
for view in ('front','side','three-quarter'):
    output=root/(view+'-review.mp4')
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate','60','-i',str(root/('CH1_SahurGuard_v050-'+view+'-%03d.png')),'-vf','format=yuv420p','-c:v','libx264','-preset','fast','-crf','21','-movflags','+faststart',str(output)],check=True)
    report[view]=str(output)
(root/'review-metrics.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
