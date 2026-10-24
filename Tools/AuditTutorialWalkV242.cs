using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public static class AuditTutorialWalkV242
{
    public static object Start(string label)
    {
        if(!Application.isPlaying)throw new Exception("Existing Editor Play required.");
        var observer=new GameObject("Temporary board-relative walk audit").AddComponent<TutorialWalkAuditV242>();
        observer.folder=".codex/opening-charge-20261023/"+label;
        return new { started=true };
    }
}
[DefaultExecutionOrder(31520)] public sealed class TutorialWalkAuditV242 : MonoBehaviour
{
    public string folder;
    Story1WreckBattle battle; ThirdPersonPlayerController hero; Vector3 chestAxis; bool calibrated;
    Story1WreckPlank previousBoard; Vector3 previousLocal; bool previousWalk;
    readonly List<object> rows=new List<object>();
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    object Get(string name)=>typeof(Story1WreckBattle).GetField(name,Flags).GetValue(battle);
    void LateUpdate()
    {
        try
        {
            if(!battle)battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
            if(!battle||battle.RedesignStage!=Story1WreckBattle.OpeningStage.Tutorial)return;
            if(!hero)hero=(ThirdPersonPlayerController)Get("player");
            if(!calibrated){var a=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(".codex/shark-direction-20261023/calibration/audit.json"))["chestAxis"];chestAxis=new Vector3((float)a[0],(float)a[1],(float)a[2]);calibrated=true;}
            var board=(Story1WreckPlank)Get("redCombatBoard"); if(!board)return;
            var pose=(AnimationClipPlayable)Get("redPose");
            bool walk=(bool)Get("redTutorialStaging")&&pose.IsValid()&&pose.GetAnimationClip().name=="PLAYER_Walk_v241";
            Vector3 local=board.transform.InverseTransformPoint(hero.transform.position);
            if(walk&&previousWalk&&previousBoard==board)
            {
                Vector3 delta=Vector3.ProjectOnPlane(board.transform.TransformVector(local-previousLocal),Vector3.up);
                if(delta.magnitude>.015f)
                {
                    float alignment=Vector3.Dot(delta.normalized,Vector3.ProjectOnPlane(hero.transform.forward,Vector3.up).normalized);
                    float visualAlignment=Vector3.Dot(delta.normalized,Vector3.ProjectOnPlane(hero.CharacterAnimator.GetBoneTransform(HumanBodyBones.Chest).TransformDirection(chestAxis),Vector3.up).normalized);
                    rows.Add(new{visualAlignment,frame=Time.frameCount,lesson=battle.CurrentLesson.ToString(),seconds=(float)Get("lessonTime"),distance=delta.magnitude,alignment});
                    File.WriteAllText(folder+"/walk-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));
                }
            }
            previousBoard=board;previousLocal=local;previousWalk=walk;
        }
        catch(Exception e){File.WriteAllText(folder+"/walk-audit-error.txt",e.ToString());enabled=false;}
    }
}
