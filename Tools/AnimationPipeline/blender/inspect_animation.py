import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import *
if __name__=='__main__':
    p=parser('Inspect Action channels; --keys includes editable key coordinates/handles');p.add_argument('--keys',action='store_true')
    a=arguments(p);rig=rig_object(a.rig);action=action_object(a.action,rig)
    curves=[]
    for handle,c in curve_records(action):
        item={'slot_handle':handle,'path':c.data_path,'axis':c.array_index,'keys':len(c.keyframe_points),'muted':c.mute,
              'modifiers':[m.type for m in c.modifiers]}
        if a.keys:item['points']=[{'co':list(k.co),'interpolation':k.interpolation,'left':list(k.handle_left),'right':list(k.handle_right)} for k in c.keyframe_points]
        curves.append(item)
    report({'rig':rig.name,'action':action_summary(action),'fps':effective_fps(),'curves':curves},a.output)
