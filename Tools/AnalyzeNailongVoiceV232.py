"""Summarize real oral-wave recordings and encode actual captured frames."""
import json,subprocess
from pathlib import Path
import imageio_ffmpeg
root=Path('.codex/nailong-voice-20261020');folder=root/'island-socket-final'
rows=json.loads((folder/'motion.json').read_text(encoding='utf-8'));setup=json.loads((folder/'setup.json').read_text(encoding='utf-8'))
health=[min(r['currentHealth'] for r in rows if r['phase']==p) for p in range(4)]
assert health==[84,55,86,84],health
waves=[w for r in rows for w in r['waves']];assert waves and all(w['FromMouth'] for w in waves)
sections=[s for w in waves for s in w['sections']]
assert all(abs(s['axis'][1])<.21 for s in sections)
assert max(s['travel'] for s in sections)>10 and max(s['radius'] for s in sections)>3
shots=[s for r in rows for s in r['projectiles']];assert max(len(r['projectiles']) for r in rows)==1
summary={'frames':len(rows),'seconds':rows[-1]['clock'],'setup':setup,'minimumHealthPerAction':health,'allPressureWavesFromMouth':True,'maximumVerticalAxisComponent':max(abs(s['axis'][1]) for s in sections),'maximumTravelMetres':max(s['travel'] for s in sections),'maximumRingRadiusMetres':max(s['radius'] for s in sections),'maxConcurrentGlobs':1,'contracts':json.loads((root/'contracts.json').read_text(encoding='utf-8'))['passed'],'previewSamplingHz':10}
(root/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
video=root/'Nailong_MouthWaves_v232_Final.mp4'
if video.exists():raise RuntimeError('Preserve prior preview')
args=[imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-framerate','10','-start_number','2','-i',str(folder/'frame-%04d.png'),'-vf','scale=960:540','-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-movflags','+faststart',str(video)]
subprocess.run(args,check=True,creationflags=getattr(subprocess,'CREATE_NO_WINDOW',0));print(json.dumps({'summary':summary,'video':str(video),'bytes':video.stat().st_size}))
