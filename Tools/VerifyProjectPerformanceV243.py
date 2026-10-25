"""Check retained native evidence, paired pixels, dependencies and game data."""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageChops

root = Path('.codex/project-performance-20261023')
native = Path('.codex/opening-charge-20261023/performance-v243-verified')
world = Path('.codex/whole-game-performance-20261019/v243-world-trace')
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
paired = read(root/'paired-cache-benchmark.json')['reports']
reference = [r for r in paired if r['reference']]
cached = [r for r in paired if not r['reference']]
solid = lambda r: next(m['stats']['mean'] for m in r['markers'] if m['Key'] == 'Opening.NativeSolids')
before = sum(map(solid, reference))/len(reference)
after = sum(map(solid, cached))/len(cached)
checks = {
    'paired_cpu_cost_improves': after < before,
    'paired_final_transforms_identical': all(r['finalState'] == paired[0]['finalState'] and r['largestTransformComponentChange'] == 0 for r in paired),
}
with Image.open(root/'reference-1.png') as base:
    checks['paired_native_pixels_identical'] = all(ImageChops.difference(base, Image.open(root/name)).getbbox() is None for name in ('cached-2.png', 'reference-3.png', 'cached-4.png'))
checks['charge_regression'] = read(native/'charge-acceptance.json')['passed']
checks['independent_geometry_regression'] = read(native/'acceptance.json')['passed']
equivalent = read(native/'cache-equivalence.json')
checks['fresh_and_cached_native_queries_identical'] = len(equivalent) >= 50 and sum(r['queries'] for r in equivalent) >= 1000 and all(r['passed'] for r in equivalent)
checks['all_three_film_contacts_compared'] = sum(r['hit'] for r in equivalent) == 3
checks['scene_and_resource_dependencies_preserved'] = all(read(root/'asset-dependency-check.json').values())
assets = read(root/'before/asset-integrity.json')
checks['serialized_assets_clips_shaders_identical'] = all(Path(p).is_file() and hashlib.sha256(Path(p).read_bytes()).hexdigest() == digest for p, digest in assets['preservedAssetHashes'].items())
checks['no_duplicate_or_orphan_metadata_at_audit'] = not assets['duplicateGUIDs'] and not assets['orphanMetadata']
cleanup = read(root/'generated-build-cleanup-result.json')
checks['only_two_inventoried_generated_outputs_removed'] = len(cleanup['removed']) == 2 and all(not Path(p).exists() for p in cleanup['removed']) and cleanup['bytes'] == 3445461461
checks['latest_historical_build_retained'] = Path('.codex/opening-optimization-20261012/standalone-v165/DarkBrineOpening.exe').is_file()
checks['island_views_and_map_complete'] = (world/'result.json').exists() and not (world/'error.txt').exists() and {r['name'] for r in read(world/'result.json')['reports']} >= {'shore', 'forest', 'panorama', 'ocean', 'map', 'shore-repeat'}
checks['foliage_lifecycle_preserved'] = (world/'lifecycle.json').exists() and read(world/'lifecycle.json')['passed']
preservation = read(root/'preservation.json') if (root/'preservation.json').exists() else {}
checks['human_save_restored'] = preservation.get('saveRestored') is True
checks['original_scene_bytes_preserved'] = preservation.get('sceneUnchanged') is True
result = {'passed': all(checks.values()), 'checks': checks, 'native_solid_mean_ms': {'reference': before, 'cached': after, 'reduction_percent': (1-after/before)*100}, 'cleared_bytes': cleanup['bytes'], 'scope': 'Same-workload Editor measurements and actual native regression; no standalone/low-end hardware or all-layout clipping guarantee.'}
(root/'acceptance.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result, indent=2))
if not result['passed']:
    raise SystemExit(1)
