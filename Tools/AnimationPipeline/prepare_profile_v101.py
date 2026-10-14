from pathlib import Path

base=Path('Tools/RunSolidProfileV101.cs').read_text()
extra=Path('Tools/ObservePoseMotionV101.cs').read_text()
using='\n'.join(sorted(set(l for s in (base,extra) for l in s.splitlines() if l.startswith('using '))))
body='\n'.join('\n'.join(l for l in s.splitlines() if not l.startswith('using ')) for s in (base,extra))
body=body.replace('ObserveSolidV101.Start(root+"solid-stages-v101.json");', 'ObserveSolidV101.Start(root+"solid-stages-v101.json"); ObservePoseMotionV101.Start(root+"pose-motion-v101.json");')
Path('Tools/RunPoseProfileV101.cs').write_text(using+'\n'+body,encoding='utf-8')
