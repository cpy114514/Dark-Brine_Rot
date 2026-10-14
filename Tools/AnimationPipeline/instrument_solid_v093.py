from pathlib import Path

path = Path('Assets/Scripts/Story/Chapter1/OpeningBenchmark.cs')
text = path.read_text(encoding='utf-8-sig')
stages = {'sweep': 'SolidSweep', 'escape': 'SolidEscape', 'clear': 'ClearSearch',
          'retained': 'RetainedPlacement', 'volumes': 'UpdateVolumes', 'overlap': 'SolidOverlap',
          'damage': 'SolvedDamage', 'residual': 'ResidualPenetration'}
text = text.replace('long allocation,entry,attack,solid;', 'long allocation,entry,attack,solid,' + ','.join(stages) + ';')
text = text.replace('ProfilerRecorder entryCost,attackCost,solidCost;', 'ProfilerRecorder entryCost,attackCost,solidCost,' + ','.join(k+'Cost' for k in stages) + ';')
anchor = 'solidCost=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.SolidContact");'
text = text.replace(anchor, anchor + '\n' + '\n'.join('        '+k+'Cost=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Wreck.'+v+'");' for k,v in stages.items()))
anchor = 'solidMs=f.solid/1000000d'
text = text.replace(anchor, anchor + ',' + ','.join(k+'Ms=f.'+k+'/1000000d' for k in stages))
anchor = 'solid=solidCost.Valid?solidCost.LastValue:0'
text = text.replace(anchor, anchor + ',' + ','.join(k+'='+k+'Cost.Valid?'+k+'Cost.LastValue:0' for k in stages))
anchor = 'if(solidCost.Valid)solidCost.Dispose();'
text = text.replace(anchor, anchor+'\n        '+''.join('if('+k+'Cost.Valid)'+k+'Cost.Dispose();' for k in stages))
path.write_text(text, encoding='utf-8')
