import json, math
from pathlib import Path
from PIL import Image, ImageDraw

root=Path(__file__).resolve().parents[2]
folder=root/'.codex/opening-optimization-20261012/native-framed-parry-v171'
samples=json.loads((folder/'native-samples.json').read_text())
clips={name:[s for s in samples if s['name']==name] for name in ('CH1_SahurParry_v090','CH1_SahurParry_v171')}
contact={name:min(rows,key=lambda s:abs(s['time']-.484)) for name,rows in clips.items()}
def distance(a,b):return math.sqrt(sum((x-y)**2 for x,y in zip(a,b)))
baseline=contact['CH1_SahurParry_v090'];candidate=contact['CH1_SahurParry_v171']
report={'native':True,'originalAvatarAndSkin':True,'stage':'FIRST PASS','runtimeActivated':False,
    'images':len(list(folder.glob('*.png'))),'sampleTime':candidate['time'],
    'contactRightHandDelta':[b-a for a,b in zip(baseline['rightHand'],candidate['rightHand'])],
    'contactHandChange':distance(baseline['rightHand'],candidate['rightHand']),
    'contactLeftFootDifference':distance(baseline['leftFoot'],candidate['leftFoot']),
    'contactRightFootDifference':distance(baseline['rightFoot'],candidate['rightFoot']),
    'clips':{},'normalSpeedArtisticApproval':'not observed','movingWreckContactAcceptance':'pending'}
for name,rows in clips.items():
    upper=[distance(s['rightElbow'],s['chest']) for s in rows] # chest is not shoulder; not a limb-length test
    lower=[distance(s['rightHand'],s['rightElbow']) for s in rows]
    report['clips'][name]={'samples':len(rows),'lowerArmLengthRange':max(lower)-min(lower),
        'largestHandFrameStep':max(distance(a['rightHand'],b['rightHand']) for a,b in zip(rows,rows[1:])),
        'footRange':{side:max(distance(rows[0][side],r[side]) for r in rows) for side in ('leftFoot','rightFoot')}}
grid=Image.new('RGB',(3*480,2*510),(24,24,24));draw=ImageDraw.Draw(grid)
for r,name in enumerate(clips):
    i=min(range(len(clips[name])),key=lambda j:abs(clips[name][j]['time']-.484))
    for c,view in enumerate(('front','side','three-quarter')):
        frame=Image.open(folder/f'{name}-{view}-{i:03}.png')
        grid.paste(frame,(c*480,r*510+30));draw.text((c*480+8,r*510+8),f'{name} {view} contact',fill='white')
grid.save(folder/'contact-comparison.jpg',quality=94)
(folder/'native-review.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
