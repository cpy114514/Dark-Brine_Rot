from PIL import Image
import numpy as np,json
from pathlib import Path
p=Path('.codex/environment-styles'); result={}
for style in ['realistic','dark-fairytale']:
 a=np.asarray(Image.open(p/('pixel-'+style+'.png')).convert('RGB'))
 b=a.reshape(180,3,320,3,3)
 result[style]=int(np.any(b!=b[:,0:1,:,0:1,:],axis=(1,3,4)).sum())
print(result)
assert max(result.values())==0
(p/'pixel-checks.json').write_text(json.dumps(dict(passed=True,mixedBlocks=result,blocksPerFrame=57600),indent=2))
sheet=Image.new('RGB',(1920,1080))
for x,y,filename in [(0,0,'native-realistic.png'),(960,0,'native-dark-fairytale.png'),(0,540,'pixel-realistic.png'),(960,540,'pixel-dark-fairytale.png')]:
 sheet.paste(Image.open(p/filename),(x,y))
sheet.save(p/'comparison.png')
