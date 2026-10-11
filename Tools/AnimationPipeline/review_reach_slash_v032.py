import json,math
from pathlib import Path
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[2]/'.codex/encounter-v032/native-preview'
samples=json.loads((root/'native-samples.json').read_text())
old=[r for r in samples if r['name'].endswith('v027')];new=[r for r in samples if r['name'].endswith('v032')]
assert len(old)==len(new)==16
def delta(a,b):return [y-x for x,y in zip(a,b)]
def length(v):return math.sqrt(sum(x*x for x in v))
rows=[]
for a,b in zip(old,new):
    rows.append(dict(seconds=b['time'],right_hand_delta_metres=delta(a['rightHand'],b['rightHand']),
        left_foot_delta_metres=length(delta(a['leftFoot'],b['leftFoot'])),right_foot_delta_metres=length(delta(a['rightFoot'],b['rightFoot']))))
report=dict(staged_only=True,actual_original_avatar=True,original_walk_run_roll_unchanged=True,contact=rows[8],
    maximum_native_foot_difference=max(max(r['left_foot_delta_metres'],r['right_foot_delta_metres']) for r in rows),frames=rows)
(root/'native-comparison.json').write_text(json.dumps(report,indent=2))
for view in ('front','side','three-quarter'):
    sheet=Image.new('RGB',(1920,686),(18,22,28));draw=ImageDraw.Draw(sheet)
    for row,action in enumerate(('CH1_SahurSlash_v027','CH1_SahurSlash_v032')):
        for column,index in enumerate((0,3,6,8,11,14)):
            image=Image.open(root/f'{action}-{view}-{index:02}.png').convert('RGB').resize((320,320))
            x=column*320;y=row*343;sheet.paste(image,(x,y+23));draw.text((x+8,y+6),f'{action} {new[index]["time"]:.3f}s',fill='white')
    sheet.save(root/(view+'-comparison.jpg'))
print(json.dumps({k:v for k,v in report.items() if k!='frames'},indent=2))
