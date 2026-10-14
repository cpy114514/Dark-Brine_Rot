"""Compare native impact poses and encode all actual Unity samples at 60 FPS."""
import json,math,subprocess
from pathlib import Path
import imageio_ffmpeg
root=Path(r'D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012')
folder=root/'native-continuous-v051'
old=json.loads((root/'native-continuous-v040/native-samples.json').read_text(encoding='utf-8'))
new=json.loads((folder/'native-samples.json').read_text(encoding='utf-8'))
def impact(samples,name):return min((s for s in samples if s['name']==name),key=lambda s:abs(s['time']-.63))
before=impact(old,'CH1_SahurSlash_v040');after=impact(new,'CH1_SahurSlash_v051')
def relative_hand(s):return s['rightHand'][1]-min(s['leftFoot'][1],s['rightFoot'][1])
def angle(a,b):return math.degrees(math.acos(max(-1,min(1,sum(x*y for x,y in zip(a,b))))))
report=dict(stage='FIRST PASS',samples=len(new),seconds=new[-1]['time'],original_avatar=True,
            unchanged_contact_seconds=.63,old_shaft_y=before['shaftDirection'][1],new_shaft_y=after['shaftDirection'][1],
            old_hand_above_ankles=relative_hand(before),new_hand_above_ankles=relative_hand(after),
            max_shaft_degrees_per_frame=max(angle(a['shaftDirection'],b['shaftDirection']) for a,b in zip(new,new[1:])),
            paired_contacts='not yet reviewed')
for view in ('front','side','three-quarter'):
    output=folder/(view+'-review.mp4')
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-framerate','60','-i',str(folder/('CH1_SahurSlash_v051-'+view+'-%03d.png')),'-vf','format=yuv420p','-c:v','libx264','-preset','fast','-crf','21','-movflags','+faststart',str(output)],check=True)
    report[view]=str(output)
(folder/'review-metrics.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
