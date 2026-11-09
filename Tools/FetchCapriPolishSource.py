"""Fetch only the author's public, free CC0 animation files. No account/purchase."""
import urllib.request, urllib.parse, http.cookiejar, re, json, zipfile
from pathlib import Path
base='https://kaylousberg.itch.io/kaykit-character-animations'
folder=Path('Tools/CapriPolishSource');folder.mkdir(exist_ok=True)
opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
page=opener.open(base+'/purchase').read().decode()
csrf=re.search(r'<meta name="csrf_token" value="([^"]+)"',page).group(1)
req=urllib.request.Request(base+'/download_url',data=urllib.parse.urlencode({'csrf_token':csrf}).encode(),headers={'Referer':base+'/purchase'})
data=json.loads(opener.open(req).read())
page=opener.open(data['url']).read().decode()
uploads=re.findall(r'data-upload_id="(\d+)"[^>]*>(.*?)</div>',page,re.S)
csrf=re.search(r'<meta name="csrf_token" value="([^"]+)"',page).group(1)
upload=re.search(r'data-upload_id="(\d+)"',page).group(1)
req=urllib.request.Request(base+'/file/'+upload,data=urllib.parse.urlencode({'csrf_token':csrf}).encode(),headers={'Referer':data['url']})
result=json.loads(opener.open(req).read())
archive=folder/'KayKit-Character-Animations-Free-1.1.zip'
with opener.open(result['url']) as src,archive.open('wb') as dst:dst.write(src.read())
with zipfile.ZipFile(archive) as z:
    selected=[n for n in z.namelist() if n.endswith('.fbx') and ('CombatMelee' in n or 'CombatRanged' in n or 'General' in n or 'Mannequin_Medium' in n) and ('Rig_Medium' in n or 'Mannequin_Medium' in n)]
    selected += [n for n in z.namelist() if 'license' in n.lower() and not n.endswith('/')]
    target=Path('Assets/ThirdParty/CapriPolishKayKit');target.mkdir(exist_ok=True)
    for name in selected:
        (target/Path(name).name).write_bytes(z.read(name))
    print('Downloaded author Free 1.1 archive; imported:',[Path(n).name for n in selected])
