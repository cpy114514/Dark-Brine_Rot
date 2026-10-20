"""Summarize the versioned existing-Editor runs; never infer release/low-end FPS."""
import json
from pathlib import Path
from statistics import median

root = Path(__file__).resolve().parent.parent / '.codex/whole-game-performance-20261019'
baseline_labels = ['before-r2', 'before-r3', 'before-final']
after_label = 'verified-performance'
before = [json.loads((root / name / 'result.json').read_text()) for name in baseline_labels]
after = json.loads((root / after_label / 'result.json').read_text())
rows = []
for report in after['reports']:
    name = report['name']
    matches = [r for run in before for r in run['reports'] if r['name'] == name]
    if not matches:
        continue
    b = median(r['frame']['average'] for r in matches)
    p = median(r['frame']['p95'] for r in matches)
    rows.append(dict(name=name, baseline_runs=len(matches), before_ms=b,
                     after_ms=report['frame']['average'], before_p95_ms=p,
                     after_p95_ms=report['frame']['p95'],
                     average_reduction_percent=100*(b-report['frame']['average'])/b,
                     before_batches=median(r['batches'] for r in matches),
                     after_batches=report['batches'],
                     before_triangles=median(r['triangles'] for r in matches),
                     after_triangles=report['triangles']))
result = dict(baselines=baseline_labels, after=after_label, rows=rows,
              limitations=['Existing Editor only; workstation i5-14400F / RTX4060, 2560x1440, PC quality, renderScale 1.',
                           'GPU timestamp outliers in older diagnostics are not valid GPU measurements; frame times are reported independently.',
                           'Full authored grass coverage is retained. No standalone build or weaker-hardware validation.',
                           'Cold compilation/loading and opening-specific native-contact spikes remain outside these steady-state windows.'])
frozen_before = json.loads((root / 'frozen-before/result.json').read_text())
frozen_after = json.loads((root / 'frozen-after/result.json').read_text())
assert frozen_before['simulationTime'] == frozen_after['simulationTime'] == 0
assert frozen_before['timeScale'] == frozen_after['timeScale'] == 0
frozen = []
for a in frozen_after['reports']:
    b = next(r for r in frozen_before['reports'] if r['name'] == a['name'])
    frozen.append(dict(name=a['name'], before_gpu_ms=b['gpuMs'], after_gpu_ms=a['gpuMs'],
                       before_batches=b['batches'], after_batches=a['batches']))
result['frozen_render_only'] = frozen
(root / 'comparison.json').write_text(json.dumps(result, indent=2))
table = ['| Live view | Before average ms (median of 3 runs) | After average ms | Before P95 ms | After P95 ms |',
         '| --- | ---: | ---: | ---: | ---: |']
for r in rows:
    table.append(f"| {r['name']} | {r['before_ms']:.2f} | {r['after_ms']:.2f} | {r['before_p95_ms']:.2f} | {r['after_p95_ms']:.2f} |")
table += ['', '| Frozen visual workload (simulation time 0) | Before GPU ms | After GPU ms |',
          '| --- | ---: | ---: |']
for r in frozen:
    table.append(f"| {r['name']} | {r['before_gpu_ms']:.2f} | {r['after_gpu_ms']:.2f} |")
doc = root.parent.parent / 'Documentation/WholeGamePerformance-20261020.md'
text = doc.read_text(encoding='utf-8')
if '<!-- PERFORMANCE_TABLE -->' in text:
    doc.write_text(text.replace('<!-- PERFORMANCE_TABLE -->', '\n'.join(table)), encoding='utf-8')
for r in rows:
    print(f"{r['name']}: {r['before_ms']:.2f} -> {r['after_ms']:.2f} ms; P95 {r['before_p95_ms']:.2f} -> {r['after_p95_ms']:.2f}")
