from pathlib import Path
import json
import subprocess

out = Path(__file__).resolve().parents[1] / '.codex/uvcs-conflicts-20261013'
chapter = 'Assets/Scripts/Story/Chapter1/'

def read(version, name):
    return (out / version / chapter / name).read_text(encoding='utf-8-sig')

def replace(text, old, new):
    assert text.count(old) == 1, old[:120]
    return text.replace(old, new, 1)

def method(text, signature):
    start = text.index(signature)
    first = text.index('{', start)
    depth = 1
    end = first + 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}')
        end += 1
    return text[start:end]

def save(name, text):
    assert '<<<<<<<' not in text
    (out / 'merged' / chapter / name).write_text(text, encoding='utf-8', newline='\n')

# Keep the current pose/contact algorithms together. The incoming coroutine
# replacement overlaps these newer implementations and cannot be half-applied.
name = 'Story1SharkWreckContact.cs'
save(name, read('local-before', name))
name = 'Story1WreckFinale.cs'
s = read('local-before', name)
s = replace(s, 'var decks=FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None);',
            'var decks=FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None)\n            .Where(p=>!p.IsBroken && p.Collision!=null).ToArray();')
s = replace(s, 'Vector3 chosen=-Side*7-Forward*2;float best=float.NegativeInfinity;',
            'Vector3 chosen=-Side*7-Forward*2;float best=float.NegativeInfinity;\n        if(decks.Length==0)return chosen/1.25f;')
s = replace(s, 'public void ReleaseActors()\n    {', 'public void ReleaseActors()\n    {\n        StopAllCoroutines();')
save(name, s)

name = 'Story1WreckMeshCollision.cs'
s = read('local-before', name)
s = replace(s, 'Vector3 boundsPosition,boundsScale;\n    Quaternion boundsRotation;',
            'Matrix4x4 boundsMatrix;\n    Transform[] partTransforms;\n    Bounds[] localPartBounds;')
s = replace(s, 'Vector3 position=transform.position,scale=transform.lossyScale;Quaternion rotation=transform.rotation;',
            'Matrix4x4 rootMatrix=transform.localToWorldMatrix;')
s = replace(s, 'boundsPosition.Equals(position) && boundsRotation.Equals(rotation) && boundsScale.Equals(scale)', 'boundsMatrix.Equals(rootMatrix)')
s = replace(s, 'boundsPosition=position;boundsRotation=rotation;boundsScale=scale;', 'boundsMatrix=rootMatrix;')
s = replace(s, 'var at=Parts[i].transform;var local=Parts[i].sharedMesh.bounds;', 'var at=partTransforms[i];var local=localPartBounds[i];')
s = replace(s, 'Parts=parts.ToArray();', 'Parts=parts.ToArray();\n        partTransforms=new Transform[Parts.Length];localPartBounds=new Bounds[Parts.Length];\n        for(int i=0;i<Parts.Length;i++){partTransforms[i]=Parts[i].transform;localPartBounds[i]=Parts[i].sharedMesh.bounds;}')
s = replace(s, 'if(Parts!=null && preparedMesh==mesh && preparedShards==shards)return;',
            'if(Parts!=null && preparedMesh==mesh && preparedShards==shards && System.Array.TrueForAll(Parts,p=>p!=null))\n        {body=GetComponent<Rigidbody>();boundsFrame=-1;SetEnabled(true);return;}')
s = replace(s, 'if(part!=null)Physics.IgnoreCollision(other,part,value);', 'if(part!=null && Physics.GetIgnoreCollision(other,part)!=value)Physics.IgnoreCollision(other,part,value);')
save(name, s)

name = 'Story1WreckPlank.cs'
s = read('merged', name)
s = replace(s, 'PrepareFootprint(mesh,dimensions);', 'BuildFootprint(mesh);')
s = replace(s, 'void BuildFootprint(Mesh mesh)', 'public void PrepareFootprint(Mesh mesh,Vector3 dimensions){size=dimensions;BuildFootprint(mesh);}\n    void BuildFootprint(Mesh mesh)')
save(name, s)

name = 'Story1WreckRider.cs'
s = read('merged', name)
s = replace(s, 'Collider[] bodyHitboxes;', 'Collider[] bodyHitboxes;\n    readonly Collider[] climbOverlaps=new Collider[64];')
save(name, s)

name = 'Story1WreckFloatBody.cs'
s = read('merged', name)
s = replace(s, 'OceanWorld ocean;', 'OceanWorld ocean;\n    static Transform samplingFocus;\n    Mesh pressureMesh;\n    Vector3 pressureSize;')
s = replace(s, 'meshVertices.Clear();', 'meshVertices.Clear();samplingFocus=null;')
s = replace(s, 'var points=source!=null ? source.vertices : null;', 'var points=source!=null ? Vertices(source) : null;')
s = replace(s, 'ocean=water;dimensions=size;Floats=floats;', 'ocean=water;dimensions=size;Floats=floats;\n        if(samplingFocus==null){var actor=Object.FindFirstObjectByType<ThirdPersonPlayerController>();samplingFocus=actor!=null ? actor.transform : null;}')
start = s.index('        var points=source!=null ? Vertices(source) : null;', s.index('public void Initialize'))
end = s.index('        for(int i=0;i<4;i++)pressureWeights[i]=.25f;', start)
s = s[:start] + '        PreparePressureGeometry(source,size);\n' + s[end:]
assert 'pendingVelocity=velocity' in s and 'void OnEnable()' in s
save(name, s)

name = 'Story1WreckBattle.cs'
s = read('local-before', name)
s = replace(s, 'if(CurrentPhase==Phase.Waiting){WarmWreckage(6);return;}',
            'if(CurrentPhase==Phase.Waiting || CurrentPhase==Phase.Breaking && !shattered)WarmWreckage(6);\n        if(CurrentPhase==Phase.Waiting)return;')
s = replace(s, 'rider.enabled=false;rider.PrepareCollisionOwner(owner.sahur);',
            'owner.sahur.PrepareRuntime();\n            rider.enabled=false;rider.PrepareCollisionOwner(owner.sahur);')
s = replace(s, 'preparedPieces.Add(source,go);', 'go.GetComponent<Story1WreckFloatBody>().PreparePressureGeometry(source.mesh,dimensions);\n            preparedPieces.Add(source,go);')
s = replace(s, 'rider?.RegisterCollision(go.GetComponent<Story1WreckMeshCollision>());',
            'go.GetComponent<Story1WreckFloatBody>().PreparePressureGeometry(shard.mesh,size);\n        rider?.RegisterCollision(go.GetComponent<Story1WreckMeshCollision>());')
s = replace(s, 'var body=go.GetComponent<Story1WreckDebris>();body.Initialize(ocean,size,floats,FragmentVelocity(from),FragmentSpin());\n        body.ConfigureObstacle(source);',
            'var body=go.GetComponent<Story1WreckDebris>();body.ConfigureObstacle(source);\n        body.Initialize(ocean,size,floats,FragmentVelocity(from),FragmentSpin());')
assert 'shipBreakPower = 1.35f' in s and 'CinematicLetterbox.Set' in s
save(name, s)

report = []
for item in json.loads((out/'merge-draft-report.json').read_text()):
    relative = item['path']
    target = out/'final-diffs'/(Path(relative).name+'.diff')
    target.parent.mkdir(parents=True, exist_ok=True)
    diff = subprocess.run(['git','diff','--no-index','--ignore-space-at-eol','--',str(out/'local-before'/relative),str(out/'merged'/relative)], capture_output=True)
    target.write_bytes(diff.stdout)
    report.append({'path':relative,'linesChangedFromLocal':diff.stdout.count(b'\n')})
(out/'final-draft-report.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
