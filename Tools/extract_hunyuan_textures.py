import bpy,sys,json
from pathlib import Path
kind=sys.argv[-1];root=Path.cwd()/'ArtSource/Environment/Hunyuan-20261027-r2';bpy.ops.wm.open_mainfile(filepath=str(root/(kind+'-working.blend')));info=[]
for img in bpy.data.images:
 if not img.packed_file:continue
 name='normal' if 'normal' in img.name.lower() else 'metalrough' if 'metallic' in img.name.lower() else 'base';data=bytes(img.packed_file.data);ext='.png' if data[:8]==b'\x89PNG\r\n\x1a\n' else '.jpg' if data[:2]==b'\xff\xd8' else '.bin';dest=root/(kind+'-'+name+ext);dest.write_bytes(data);info.append({'name':img.name,'file':dest.name,'size':list(img.size)})
(root/(kind+'-textures.json')).write_text(json.dumps(info,indent=2));print(info)
