"""Review actual native-camera opening captures; never generates animation frames."""
import argparse
import json
from pathlib import Path
from PIL import Image, ImageDraw
import imageio_ffmpeg
import subprocess
import math
import re


def review(folder, movie=False):
    folder = Path(folder)
    rows = json.loads((folder / 'review.json-frames.json').read_text(encoding='utf-8-sig'))
    images = sorted((folder / 'review.json-images').glob('*.jpg'))
    assert len(rows) == len(images), (len(rows), len(images))
    for stage, lo, hi in [('Pursuit', 0, 12.5), ('Duel', 0, 10), ('Duel', 10, 19.5), ('Duel', 19.5, 25)]:
        candidates = [i for i, r in enumerate(rows) if r['stage'] == stage and lo <= r['t'] < hi]
        if not candidates:
            continue
        picks = [candidates[round(j * (len(candidates)-1)/15)] for j in range(16)]
        canvas = Image.new('RGB', (1440, 892), '#101318')
        draw = ImageDraw.Draw(canvas)
        for j, i in enumerate(picks):
            x, y = (j % 4) * 360, (j // 4) * 223
            with Image.open(images[i]) as im:
                canvas.paste(im.resize((360, 203)), (x, y + 20))
            r = rows[i]
            draw.text((x+4, y+3), f"f{i} {stage} {r['t']:.2f}s shot {r['shot']}", fill='#f3d17e')
        canvas.save(folder / f'{stage}-{lo:g}-{hi:g}.jpg', quality=92)
    if movie:
        first = next(i for i, r in enumerate(rows) if r['stage'] == 'Pursuit')
        out = folder / 'SharkFight.mp4'
        subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), '-hide_banner', '-loglevel', 'error', '-y',
                        '-framerate', '30', '-start_number', str(first), '-i', str(folder/'review.json-images/%05d.jpg'),
                        '-c:v', 'libx264', '-crf', '20', '-preset', 'fast', '-pix_fmt', 'yuv420p',
                        '-movflags', '+faststart', str(out)], check=True)
    print(json.dumps({'folder': str(folder), 'frames': len(rows), 'shots': list(dict.fromkeys((r['stage'],r['shot']) for r in rows))}))


def metrics(folder):
    folder = Path(folder)
    rows = json.loads((folder / 'review.json-frames.json').read_text(encoding='utf-8-sig'))
    result = json.loads((folder / 'review.json').read_text(encoding='utf-8-sig'))
    film = [r for r in rows if r['stage'] in ('Pursuit', 'Duel')]
    pursuit = [r for r in film if r['stage'] == 'Pursuit' and r['t'] > .05]
    def visible(r):
        x, y, z = r['headView']
        return z > 0 and .05 <= x <= .95 and .05 <= y <= .95
    def vector(s):
        return [float(x) for x in re.findall(r'-?\d+(?:\.\d+)?', s)]
    steps = []
    for a,b in zip(film,film[1:]):
        if a['stage'] == b['stage'] and b['time'] > a['time']:
            distance = math.dist(vector(a['nose']), vector(b['nose']))
            steps.append(dict(stage=b['stage'], t=b['t'], distance=distance, speed=distance/(b['time']-a['time'])))
    contact_events = [e for e in result['events'] if 'contact' in e.lower() and ('Overhead' in e or 'Thrown' in e or 'Final tail' in e)]
    values = dict(frames=result['frame'], passed=result['passed'], health=result['health'], contacts=result['contacts'],
                  tutorialHits=result['tutorialHits'], errors=result['errors'], contactEvents=contact_events,
                  filmShots=list(dict.fromkeys((r['stage'],r['shot']) for r in film)),
                  pursuitHeadInFrame=sum(visible(r) for r in pursuit)/len(pursuit),
                  pursuitHeadAboveLocalWater=sum(r['headWater'] > 0 for r in pursuit)/len(pursuit),
                  pursuitHeadWaterMinimum=min(r['headWater'] for r in pursuit),
                  climbEntryMaximumVerticalStep=max(abs(vector(b['hero'])[1]-vector(a['hero'])[1]) for a,b in zip(pursuit,pursuit[1:]) if 8.9 <= b['t'] <= 9.2),
                  filmLensWaterMinimum=min(r['lensWater'] for r in film),
                  filmLensInsideWoodFrames=sum(r['lensInsideWood'] for r in film),
                  filmSharkObstructedFrames=sum(r.get('sharkObstruction',False) for r in film) if any('sharkObstruction' in r for r in film) else None,
                  filmFocusObstructedFrames=sum(r['woodObstruction'] > .1 for r in film),
                  duelSlowMotionSeconds=sum(r['stage']=='Duel' and r['rate']<.99 for r in film)/30,
                  worstNoseSteps=sorted(steps,key=lambda s:s['distance'],reverse=True)[:10])
    audit=folder/'independent-film-geometry.json'
    if audit.exists():
        geometry=json.loads(audit.read_text(encoding='utf-8-sig'))
        values['geometrySamples']=len(geometry)
        values['geometryMinBodyGap']=min(g['bodyGap'] for g in geometry)
        values['geometryWoodCrossings']=[g for g in geometry if g['wood']]
        values['geometryContactSamples']=[g for g in geometry if g['contact']]
    (folder/'metrics.json').write_text(json.dumps(values,indent=2),encoding='utf-8')
    print(json.dumps(values,indent=2))


def accept(folder):
    folder=Path(folder)
    result=json.loads((folder/'review.json').read_text(encoding='utf-8-sig'))
    geometry=json.loads((folder/'independent-film-geometry.json').read_text(encoding='utf-8-sig'))
    checks={
        'complete_real_input_route':result['passed'] and result['sawTail'] and result['sawFlight'] and result['sawSplash'],
        'three_real_film_contacts':result['contacts']==3,
        'two_real_tutorial_hits':result['tutorialHits']==2,
        'nonlethal_health':result['health']==70,
        'no_runtime_errors':not result['errors'],
        'full_eight_second_helm':8 <= result['helmSeconds'] < 8.1,
        'independent_contact_coverage':sum(g['contact'] for g in geometry)==3,
        'sampled_body_clearance':all(g['bodyGap']>=-.001 for g in geometry),
        'sampled_timber_clearance':all(not g['wood'] for g in geometry),
        'original_shark_scale':all(all(abs(s-3)<.001 for s in g['scale']) for g in geometry),
        'native_timber_fracture':max(g['RedesignSolidFractures'] for g in geometry)>0,
    }
    out={'checks':checks,'passed':all(checks.values()),'geometrySamples':len(geometry)}
    (folder/'acceptance.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
    print(json.dumps(out,indent=2))
    assert out['passed'], out


def compare(before, after):
    before, after = Path(before), Path(after)
    takes = [json.loads((p/'review.json-frames.json').read_text(encoding='utf-8-sig')) for p in (before,after)]
    cues = [('Pursuit',2.47),('Pursuit',6.0),('Duel',12.1),('Duel',21.3)]
    canvas=Image.new('RGB',(1280,1536),'#101318')
    draw=ImageDraw.Draw(canvas)
    for row,(stage,t) in enumerate(cues):
        for col,(folder,frames) in enumerate(zip((before,after),takes)):
            i=min((i for i,r in enumerate(frames) if r['stage']==stage),key=lambda i:abs(frames[i]['t']-t))
            x,y=col*640,row*384
            with Image.open(folder/'review.json-images'/f'{i:05}.jpg') as im:
                canvas.paste(im.resize((640,360)),(x,y+24))
            draw.text((x+8,y+5),f"{'BEFORE' if col==0 else 'AFTER'} | {stage} {frames[i]['t']:.2f}s",fill='#f3d17e')
    canvas.save(after/'BeforeAfter.jpg',quality=93)


if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('folder')
    p.add_argument('--movie', action='store_true')
    p.add_argument('--metrics', action='store_true')
    p.add_argument('--compare')
    p.add_argument('--accept', action='store_true')
    args = p.parse_args()
    if args.accept:
        accept(args.folder)
    elif args.compare:
        compare(args.compare,args.folder)
    elif args.metrics:
        metrics(args.folder)
    else:
        review(args.folder, args.movie)
