from pathlib import Path
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[2]/'.codex/encounter-v020/previews'
for action in ('PLAYER_BoardPaddle_v020','PLAYER_BoardPaddleReverse_v020'):
    sheet=Image.new('RGB',(1536,832),(24,28,32));draw=ImageDraw.Draw(sheet)
    for row,view in enumerate(('side','three-quarter')):
        for i,frame in enumerate((1,24,39,53)):
            path=root/action/f'{view}-{frame:07.2f}.png'
            sheet.paste(Image.open(path),(i*384,row*416+32))
            draw.text((i*384+8,row*416+10),f'{view} frame {frame}',fill='white')
    sheet.save(root/(action+'-sheet.jpg'))
print(root)
