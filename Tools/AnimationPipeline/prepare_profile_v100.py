from pathlib import Path

sources = [Path(p).read_text() for p in ['Tools/BenchmarkOpeningV058.cs','Tools/ObserveSolidV093.cs','Tools/VerifyBoundedFacingV100.cs']]
sources[0]=sources[0].replace('EntryObserverV058','EntryObserverV100').replace('BenchmarkOpeningV058','BenchmarkOpeningV100')
sources[1]=sources[1].replace('SolidObserverV093','SolidObserverV100').replace('ObserveSolidV093','ObserveSolidV100').replace('"ResidualPenetration"','"ResidualPenetration","ShapeSnapshot"')
usings=sorted(set(line for source in sources for line in source.splitlines() if line.startswith('using ')))
body='\n'.join('\n'.join(line for line in source.splitlines() if not line.startswith('using ')) for source in sources)
wrapper='''
public static class RunProfileV100
{
    public static object Start()
    {
        const string root="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/";
        ObserveSolidV100.Start(root+"solid-stages-v100.json");
        VerifyBoundedFacingV100.Start(root+"bounded-endpoints-v100.json");
        return BenchmarkOpeningV100.Start(true,20261014,30,"v100");
    }
}
'''
Path('Tools/RunProfileV100.cs').write_text('\n'.join(usings)+'\n'+body+'\n'+wrapper,encoding='utf-8')
