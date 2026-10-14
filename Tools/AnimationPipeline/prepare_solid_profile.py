from pathlib import Path
import sys

revision = sys.argv[1]
sources = [Path(p).read_text() for p in ['Tools/BenchmarkOpeningV058.cs', 'Tools/ObserveSolidV093.cs']]
sources[0] = sources[0].replace('EntryObserverV058', 'EntryObserver'+revision.upper()).replace('BenchmarkOpeningV058', 'BenchmarkOpening'+revision.upper())
sources[1] = sources[1].replace('SolidObserverV093', 'SolidObserver'+revision.upper()).replace('ObserveSolidV093', 'ObserveSolid'+revision.upper())
sources[1] = sources[1].replace('"ResidualPenetration"', '"ResidualPenetration","ShapeSnapshot"')
usings = sorted(set(line for source in sources for line in source.splitlines() if line.startswith('using ')))
body = '\n'.join('\n'.join(line for line in source.splitlines() if not line.startswith('using ')) for source in sources)
wrapper = '''
public static class RunSolidProfileREV
{
    public static object Start()
    {
        const string root="D:/indiegameDev/unity/Dark Brine_Rot/.codex/opening-optimization-20261012/";
        ObserveSolidREV.Start(root+"solid-stages-LABEL.json");
        return BenchmarkOpeningREV.Start(true,20261014,30,"LABEL");
    }
}
'''.replace('REV', revision.upper()).replace('LABEL', revision)
Path('Tools/RunSolidProfile'+revision.upper()+'.cs').write_text('\n'.join(usings)+'\n'+body+'\n'+wrapper, encoding='utf-8')
