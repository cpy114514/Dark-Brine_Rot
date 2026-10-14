from pathlib import Path
import json
import subprocess

root = Path(__file__).resolve().parents[1]
out = root / '.codex/uvcs-conflicts-20261013'
audit = json.loads((out / 'audit.json').read_text())
report = []
for item in audit:
    relative = item['path']
    if not relative.endswith('.cs'):
        continue
    paths = []
    for version in ['local-before', 'base', 'incoming']:
        p = out / 'normalized' / version / relative
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text((out / version / relative).read_text(encoding='utf-8-sig'), encoding='utf-8', newline='\n')
        paths.append(str(p))
    result = subprocess.run(['git', 'merge-file', '-p', '--diff3', '-L', 'CURRENT_LOCAL', '-L', 'BASE_168', '-L', 'INCOMING_172', *paths], capture_output=True)
    if result.returncode < 0 or result.returncode > 127:
        raise RuntimeError(result.stderr.decode(errors='replace'))
    target = out / 'merged' / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(result.stdout)
    report.append({'path': relative, 'conflicts': result.returncode})
(out / 'merge-draft-report.json').write_text(json.dumps(report, indent=2))
print(json.dumps(report, indent=2))
