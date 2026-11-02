"""Restore only task050 files, after closing this Unity project.

Run without arguments to review; --apply performs restoration. Refuses to
overwrite files changed since the validated task050 snapshot. Earlier user
work is restored from .codex/vfx050/rollback, never from git HEAD.
"""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[1]
checkpoint = root / '.codex/vfx050'
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--apply', action='store_true')
args = parser.parse_args()
manifest = json.loads((checkpoint / 'installed-files.json').read_text(encoding='utf-8'))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


for relative, digest in manifest['files'].items():
    target = (root / relative).resolve()
    if not target.is_relative_to(root):
        raise SystemExit('Refused path outside the project: ' + relative)
    if not target.exists() or sha(target) != digest:
        raise SystemExit('Preserve later work; file differs from task050: ' + relative)

print(json.dumps({'restore': manifest['restore'], 'remove': manifest['added'], 'apply': args.apply}, indent=2))
if not args.apply:
    raise SystemExit(0)

# Keep Unity's loaded assets and disk state synchronized: restoration of .asset
# bytes is only performed with this project's Editor closed.
command = "Get-CimInstance Win32_Process -Filter \"Name = 'Unity.exe'\" | Select-Object -ExpandProperty CommandLine | ConvertTo-Json -Compress"
processes = subprocess.run(['powershell', '-NoProfile', '-Command', command], check=True, capture_output=True, text=True).stdout
if str(root).replace('\\', '/').lower() in processes.replace('\\\\', '\\').replace('\\', '/').lower():
    raise SystemExit('Close this project in Unity before applying the rollback.')

for relative in manifest['restore']:
    source = checkpoint / 'rollback' / relative
    if not source.is_file():
        raise SystemExit('Missing original backup: ' + relative)
for relative in manifest['restore']:
    (root / relative).write_bytes((checkpoint / 'rollback' / relative).read_bytes())
for relative in manifest['added']:
    target = (root / relative).resolve()
    if target.is_file():
        target.unlink()
print('Task050 restored. Open Unity to reimport the restored sources and style.')
