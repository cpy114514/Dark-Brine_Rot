from pathlib import Path
import concurrent.futures
import difflib
import hashlib
import json
import shutil
import subprocess
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
out = root / '.codex/uvcs-conflicts-20261013'
progress = json.loads((root / '.plastic/plastic.incomingchangesprogress').read_text(encoding='utf-8-sig'))
conflicts = progress[0]['FileConflicts']
paths = {c['Path'].lstrip('/') for c in conflicts}
for change in ET.parse(out / 'status-before.xml').findall('.//Change'):
    if change.findtext('Type') in ('CO', 'CH'):
        paths.add(change.findtext('Path').replace('\\', '/'))
manifest = []
for relative in sorted(paths):
    source = root / relative
    target = out / 'local-before' / relative
    if source.is_file():
        target.parent.mkdir(parents=True, exist_ok=True)
        if not target.exists():
            shutil.copy2(source, target)
        manifest.append({'path': relative, 'sha256': hashlib.sha256(target.read_bytes()).hexdigest(), 'bytes': target.stat().st_size})
save = Path('C:/Users/pinyu/AppData/LocalLow/DefaultCompany/Dark Brine_Rot/dark-brine-rot-save.json')
if save.exists() and not (out / 'save-before.json').exists():
    shutil.copy2(save, out / 'save-before.json')
(out / 'backup-manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')

def download(job):
    conflict, version, revision = job
    relative = conflict['Path'].lstrip('/')
    target = out / version / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    if not target.exists():
        spec = f'revid:{revision}@rep:Dark Brine_Rot/Dark Brine_Rot@repserver:3574351988897@cloud'
        result = subprocess.run(['C:/Program Files/PlasticSCM5/client/cm.exe', 'cat', spec, '--file='+str(target)], capture_output=True, text=True)
        if result.returncode:
            raise RuntimeError(relative + ': ' + result.stdout + result.stderr)
    return relative, version, target.stat().st_size

jobs = [(c, version, c[key]) for c in conflicts for version, key in [('base', 'BaseRevId'), ('incoming', 'SourceRevId')]]
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
    for result in pool.map(download, jobs):
        print(json.dumps({'download': result}), flush=True)

def normalized(path):
    return path.read_text(encoding='utf-8-sig').replace('\r\n', '\n')

report = []
for conflict in conflicts:
    relative = conflict['Path'].lstrip('/')
    local, incoming, base = [normalized(out / directory / relative) for directory in ['local-before', 'incoming', 'base']]
    row = {'path': relative, 'localEqualsIncoming': local == incoming, 'localEqualsBase': local == base, 'incomingEqualsBase': incoming == base}
    for label, left, right in [('local-vs-incoming', local, incoming), ('incoming-vs-base', base, incoming)]:
        diff = out / 'diffs' / (label+'--'+Path(relative).name+'.diff')
        diff.parent.mkdir(parents=True, exist_ok=True)
        left_file = out / ('local-before' if label == 'local-vs-incoming' else 'base') / relative
        right_file = out / 'incoming' / relative
        result = subprocess.run(['git', 'diff', '--no-index', '--ignore-space-at-eol', '--', str(left_file), str(right_file)], capture_output=True)
        if result.returncode not in (0, 1):
            raise RuntimeError(result.stderr.decode(errors='replace'))
        diff.write_bytes(result.stdout)
        row[label+'Lines'] = result.stdout.count(b'\n')
    report.append(row)
(out / 'audit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps({'backedUp': len(manifest), 'report': report}, indent=2))
