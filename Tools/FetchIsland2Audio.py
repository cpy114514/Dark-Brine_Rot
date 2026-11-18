"""Fetch public CC0 source packs; stage originals outside Assets until selected."""
from pathlib import Path
from urllib.request import Request, urlopen
from concurrent.futures import ThreadPoolExecutor
import hashlib, html, json, re, zipfile

ROOT = Path('.codex/island2-audio/sources')
ROOT.mkdir(parents=True, exist_ok=True)

def get(url):
    return urlopen(Request(url, headers={'User-Agent':'DarkBrineRot-AudioAuthoring/1.0','Referer':'https://kenney.nl/'}), timeout=45).read()

def fetch_kenney(key):
    page = 'https://kenney.nl/assets/' + key
    data = get(page).decode('utf8')
    if 'creativecommons.org/publicdomain/zero/' not in data: raise RuntimeError('Verify CC0: '+page)
    identifier = re.search(r"data-download-id=['\"]([^'\"]+)", data).group(1)
    response = json.loads(get('https://kenney.nl/download-link/'+identifier))
    download = response.get('url') or response.get('download')
    if not download: raise RuntimeError('No public download link: '+str(response))
    return fetch_file(key, page, download, key+'.zip', 'Kenney', data)

def fetch_file(key, page, url, name, author, page_data=None):
    data = get(url)
    folder = ROOT/key; folder.mkdir(exist_ok=True)
    file = folder/name; file.write_bytes(data)
    if name.endswith('.zip'):
        with zipfile.ZipFile(file) as z:
            for item in z.infolist():
                if not (folder/item.filename).resolve().is_relative_to(folder.resolve()): raise RuntimeError('Unsafe archive path')
            z.extractall(folder)
    meta = {'id':key,'sourcePage':page,'author':author,'license':'CC0-1.0','downloadFile':name,'sha256':hashlib.sha256(data).hexdigest()}
    (folder/'SOURCE.json').write_text(json.dumps(meta,indent=2))
    if page_data: (folder/'source-page.html').write_text(page_data,encoding='utf8')
    return {'id':key,'bytes':len(data),'files':[str(p.relative_to(folder)) for p in folder.rglob('*') if p.suffix.lower() in ['.ogg','.wav','.mp3']][:180]}

if __name__ == '__main__':
    tasks = [lambda k=k:fetch_kenney(k) for k in ['impact-sounds','interface-sounds','rpg-audio']]
    tasks += [lambda:fetch_file('liquid','https://opengameart.org/content/40-cc0-water-splash-slime-sfx','https://opengameart.org/sites/default/files/water-splash-slime-sfx.zip','liquid.zip','rubberduck'),
              lambda:fetch_file('wind','https://opengameart.org/content/wind-whoosh-loop','https://opengameart.org/sites/default/files/wind%20woosh%20loop.ogg','wind.ogg','SketchMan3'),
              lambda:fetch_file('crunch','https://opengameart.org/content/5-break-crunch-impacts','https://opengameart.org/sites/default/files/independent_nu_ljudbank-break_crunch_impact.7z','crunch.7z','Independent.nu (submitted by qubodup)')]
    with ThreadPoolExecutor(max_workers=4) as pool:
        for result in pool.map(lambda f:f(), tasks): print(json.dumps(result))
