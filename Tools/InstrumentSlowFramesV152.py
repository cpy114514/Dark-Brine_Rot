"""Development-only diagnostics; keep normal gameplay and rendered workload intact."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
path = root / 'Assets/Scripts/Story/Chapter1/OpeningBenchmark.cs'
original = path.read_text(encoding='utf-8-sig')
assert 'slowestFrames=' not in original, 'Do not apply instrumentation twice'
match = re.search(r'spikes=a\.Where\(f=>f\.ms>35\)\.Select\(f=>new\{([^{}]+)\}\)\.ToArray\(\)', original)
assert match, 'Expected the retained spike projection'
backup = root / '.codex/opening-optimization-20261012/backups/v152/OpeningBenchmark.cs'
backup.parent.mkdir(parents=True, exist_ok=True)
assert not backup.exists(), 'Preserve the versioned backup'
backup.write_bytes(path.read_bytes())
text = original.replace(match.group(), 'slowestFrames=a.Reverse().Take(Math.Max(1,(int)Math.Ceiling(a.Length*.01))).Select(DescribeFrame).ToArray(),spikes=a.Where(f=>f.ms>35).Select(DescribeFrame).ToArray()')
text = text.replace('    void Update(){if(recording', '    static object DescribeFrame(Frame f)=>new{' + match.group(1) + '};\n    void Update(){if(recording')
text = text.replace('"Sahur.ConfigureCollider"};', '"Sahur.ConfigureCollider","Wreck.BenchmarkRender"};')
text = text.replace('render=SystemCost("Camera.Render")', 'render=SystemCost("Wreck.BenchmarkRender")')
text = text.replace('renderCamera.Render();var active=RenderTexture.active;', 'using(new Unity.Profiling.ProfilerMarker("Wreck.BenchmarkRender").Auto())renderCamera.Render();var active=RenderTexture.active;')
text = text.replace('standalone=!Application.isEditor,rendered=true,', 'standalone=!Application.isEditor,rendered=true,renderMetric="Explicit gameplay camera CPU submission; GPU frame time unmeasured",')
assert text != original and text.count('static object DescribeFrame(') == 1
assert text.count('Wreck.BenchmarkRender') == 3
path.write_text(text, encoding='utf-8', newline='\r\n')
print('Added slowest 1% frame costs and actual camera CPU submission marker; no workload omitted.')
