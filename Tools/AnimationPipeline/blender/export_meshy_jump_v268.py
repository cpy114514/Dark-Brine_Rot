"""Export reviewed blocking retarget for isolated native preview only."""
import bpy,sys,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from common import slot_for
from export_unity_fbx import export
root=Path(__file__).resolve().parents[3];out=root/'ArtSource/Combat/v268'
bpy.ops.wm.open_mainfile(filepath=str(out/'sahur-meshy-jump-v268-draft2.blend'))
rig=bpy.data.objects['Armature']
for kind in ['Club','Blade']:
 a=bpy.data.actions['PLAYER_'+kind+'JumpChop_v268']
 m=dict(action=a.name,unity_clip=a.name,revision='combat-v268',frame_start=1,frame_end=52,fps=60,loop=False,
        root_motion='in_place',root_bone='root',rig_type='Humanoid',stage='BLOCKING',design_status='approved',
        design_reference='Documentation/JumpDiagonal-v266.md',approval_reference='User requested upper-right to lower-left chop and rejected v266 artistic quality; authorized AI animation revision.',
        review_status='accepted_for_stage',review_reference='ArtSource/Combat/v268/review.md',
        gameplay={'startup':[1,18],'active':[18,35],'recovery':[35,52]},
        source_tool='Meshy Motion Prime',source_candidate='Sahur_Aerial_Diagonal_Chop_02',
        avatar='Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx')
 export(rig,a,m,out/(a.name+'.fbx'),slot_for(a,rig))
