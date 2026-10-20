"""Summarize actual island recordings and encode the native-frame preview."""
import json
import subprocess
from pathlib import Path
import imageio_ffmpeg

root=Path('.codex/nailong-height-20261020')
summary={}
for label in ['island-final']:
    rows=json.loads((root/label/'motion.json').read_text(encoding='utf-8'))
    setup=json.loads((root/label/'setup.json').read_text(encoding='utf-8'))
    summary[label]={
        'frames':len(rows),'duration':rows[-1]['clock'],'camera':setup,
        'minimumHealthPerAction':[min(r['currentHealth'] for r in rows if r['phase']==phase) for phase in range(3)],
        'maxConcurrentGlobs':max(len(r['projectiles']) for r in rows),
    }
    assert summary[label]['minimumHealthPerAction']==[84,55,84]
    assert summary[label]['maxConcurrentGlobs']==1
    if label=='island-final':
        shots=[s for r in rows for s in r['projectiles'] if not s['HasImpacted']]
        summary[label]['collisionRadiusMetres']=shots[0]['CollisionRadius']
        summary[label]['diameterPixels']=[min(s['diameterPixels'] for s in shots),max(s['diameterPixels'] for s in shots)]
sections=[s for r in rows for w in r['waves'] for s in w['sections'] if s['height']>3.0]
assert sections,'No full-height pressure front reached native rendering'
summary['verticalSpanMetres']=[min(s['height'] for s in sections),max(s['height'] for s in sections)]
summary['projectedHeightPixels']=[min(s['screenHeight'] for s in sections),max(s['screenHeight'] for s in sections)]
summary['contracts']=json.loads((root/'contracts.json').read_text(encoding='utf-8'))['passed']
summary['captureSamplingHz']=10
(root/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
video=root/'Nailong_Height_v231.mp4'
if video.exists():raise RuntimeError('Preserve previous preview')
command=[imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-framerate','10','-start_number','2',
         '-i',str(root/'island-final'/'frame-%04d.png'),'-vf','scale=960:540',
         '-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(video)]
subprocess.run(command,check=True,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0))
print(json.dumps({'summary':str(root/'summary.json'),'preview':str(video),'bytes':video.stat().st_size}))
