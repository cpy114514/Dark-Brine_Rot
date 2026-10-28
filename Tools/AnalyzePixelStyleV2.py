"""Check native captures: coarse world/UI blocks and unchanged native text."""
import json
from pathlib import Path
import numpy as np
from PIL import Image

root=Path('.codex/pixel-style-v2')
normal=np.asarray(Image.open(root/'normal.png').convert('RGB'))
pixel=np.asarray(Image.open(root/'pixel-style.png').convert('RGB'))
world=np.asarray(Image.open(root/'world-pixel.png').convert('RGB'))
assert normal.shape==pixel.shape==world.shape==(1080,1920,3)
def blocks(image,x0,y0,x1,y1):
    x0=((x0+5)//6)*6;y0=((y0+5)//6)*6;x1=(x1//6)*6;y1=(y1//6)*6
    data=image[y0:y1,x0:x1].reshape((y1-y0)//6,6,(x1-x0)//6,6,3)
    mixed=np.any(data!=data[:,0:1,:,0:1,:],axis=(1,3,4))
    return dict(blocks=int(mixed.size),mixed=int(mixed.sum()))
result=dict(world=blocks(world,0,0,1920,1080),ui_photo=blocks(pixel,240,200,800,550),
    ui_artwork=blocks(pixel,240,680,800,890),ui_photo_normal=blocks(normal,240,200,800,550),
    text_crop_mean_difference=float(np.abs(normal[215:285,1070:1710].astype(float)-pixel[215:285,1070:1710].astype(float)).mean()))
native=json.loads((root/'native.json').read_text())
result['passed']=native['passed'] and all(result[key]['mixed']==0 for key in ['world','ui_photo','ui_artwork']) and result['ui_photo_normal']['mixed']>0 and result['text_crop_mean_difference']==0
(root/'pixel-analysis.json').write_text(json.dumps(result,indent=2))
(root/'acceptance.json').write_text(json.dumps(dict(passed=result['passed'],native_checks=len(native['checks']),image_checks=5,analysis=result),indent=2))
print(json.dumps(result,indent=2))
assert result['passed']
