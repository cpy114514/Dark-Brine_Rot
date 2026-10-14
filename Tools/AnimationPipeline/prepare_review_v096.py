from pathlib import Path

sources = [Path(p).read_text() for p in ['Tools/ReviewPairedV053.cs','Tools/ObserveFramingV075.cs','Tools/ObserveVisibilityV096.cs','Tools/ObserveEndingV074.cs']]
sources[0] = sources[0].replace('PairedRecorderV053','PairedRecorderV096').replace('ReviewPairedV053','ReviewPairedV096')
sources[0] = sources[0].replace('Bounds bounds=shark.VisualRenderer.bounds;bounds.Encapsulate(feet);bounds.Encapsulate(head);',
    'Bounds bounds=new Bounds(feet,Vector3.zero);bounds.Encapsulate(head);bounds.Encapsulate(enemy+Vector3.up*2);bounds.Encapsulate(enemy-Vector3.up*2);')
sources[0] = sources[0].replace('1.1f,14,60)', '1.1f,12,40)').replace('Vector3.up*.33f','Vector3.up*.6f')
sources[0] = sources[0].replace('camera.transform.SetPositionAndRotation(at,Quaternion.LookRotation(centre-at));camera.fieldOfView=48;',
    'var water=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();at.y=Mathf.Max(at.y,OceanSurfaceSampler.Height(water,at,camera)+5);camera.transform.SetPositionAndRotation(at,Quaternion.LookRotation(centre-at));camera.fieldOfView=48;')
usings = sorted(set(line for source in sources for line in source.splitlines() if line.startswith('using ')))
body = '\n'.join('\n'.join(line for line in source.splitlines() if not line.startswith('using ')) for source in sources)
wrapper = '''
public static class RunReviewV096
{
    public static object Start()
    {
        const string root="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/";
        ObserveFramingV075.Start(root+"framing-v096.json");
        ObserveVisibilityV096.Start(root+"visibility-v096.json");
        ObserveEndingV074.Start(root+"ending-v096.json");
        return ReviewPairedV096.Start(root+"native-paired-v096",false,20261014);
    }
}
'''
Path('Tools/RunReviewV096.cs').write_text('\n'.join(usings)+'\n'+body+'\n'+wrapper,encoding='utf-8')
