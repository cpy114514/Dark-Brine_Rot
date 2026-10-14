from pathlib import Path

sources = [Path(p).read_text() for p in ['Tools/BenchmarkOpeningV058.cs', 'Tools/ObserveSolidV093.cs', 'Tools/VerifyCapsuleBoundsV094.cs']]
sources = [s.replace('EntryObserverV058', 'EntryObserverV094').replace('BenchmarkOpeningV058', 'BenchmarkOpeningV094')
           .replace('SolidObserverV093', 'SolidObserverV094').replace('ObserveSolidV093', 'ObserveSolidV094') for s in sources]
usings = sorted(set(line for source in sources for line in source.splitlines() if line.startswith('using ')))
body = '\n'.join('\n'.join(line for line in source.splitlines() if not line.startswith('using ')) for source in sources)
wrapper = '''
public static class RunSolidRegressionV094
{
    public static object Start()
    {
        const string root="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/";
        ObserveSolidV094.Start(root+"solid-stages-v094.json");
        VerifyCapsuleBoundsV094.Start(root+"capsule-bounds-v094.json");
        return BenchmarkOpeningV094.Start(true,20261014,30,"v094");
    }
}
'''
Path('Tools/RunSolidRegressionV094.cs').write_text('\n'.join(usings)+'\n'+body+'\n'+wrapper, encoding='utf-8')
