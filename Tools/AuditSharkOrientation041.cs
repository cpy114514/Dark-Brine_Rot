using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
public static class AuditSharkOrientation041
{
    public static object Start(string label)
    {
        if(!Application.isPlaying)throw new Exception("Review Play required");
        new GameObject("Owned shark orientation041 audit").AddComponent<SharkOrientationAudit041>().folder=".codex/shark041/"+label;
        return new{started=true};
    }
}
[DefaultExecutionOrder(32010)]
public sealed class SharkOrientationAudit041:MonoBehaviour
{
    public string folder;TralaleroSwimAnimator shark;Story1WreckBattle battle;Story1SharkCollisionSequence sequence;OpeningTailReview review;
    SkinnedMeshRenderer skin;int head=-1,body=-1;Matrix4x4[] bind;bool written;Vector3 bindUp,bindForward,bindRight;
    readonly List<object> rows=new List<object>();const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static float Roll(Vector3 forward,Vector3 up)=>Vector3.SignedAngle(Vector3.ProjectOnPlane(Vector3.up,forward),Vector3.ProjectOnPlane(up,forward),forward);
    void LateUpdate()
    {
        if(!shark){shark=UnityEngine.Object.FindFirstObjectByType<TralaleroSwimAnimator>();battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();review=UnityEngine.Object.FindFirstObjectByType<OpeningTailReview>();}
        if(!shark||!shark.UsesBlenderRig||!review)return;
        if(!skin){skin=shark.VisualRenderer as SkinnedMeshRenderer;if(!skin)return;bind=skin.sharedMesh.bindposes;head=Array.FindIndex(skin.bones,b=>b.name=="Head");body=Array.FindIndex(skin.bones,b=>b.name=="Body");var prefab=Resources.Load<TralaleroAnimationSet>("SharkAnimation/TralaleroAnimations").rigPrefab;var restSkin=prefab.GetComponentInChildren<SkinnedMeshRenderer>();bindUp=restSkin.transform.InverseTransformDirection(prefab.transform.up);bindForward=restSkin.transform.InverseTransformDirection(prefab.transform.forward);bindRight=restSkin.transform.InverseTransformDirection(prefab.transform.right);}
        Vector3 up=bindUp,forward=bindForward;
        float BoneRoll(int index){var m=skin.bones[index].localToWorldMatrix*bind[index];return Roll(m.MultiplyVector(forward).normalized,m.MultiplyVector(up).normalized);}
        Camera camera=Camera.allCameras.FirstOrDefault(c=>c.enabled&&c.targetTexture==null);int frame=(int)typeof(OpeningTailReview).GetField("frame",Flags).GetValue(review);
        Vector3 right=(skin.bones[head].localToWorldMatrix*bind[head]).MultiplyVector(bindRight).normalized;
        float screenRoll=camera?Mathf.Atan2(Vector3.Dot(right,camera.transform.up),Vector3.Dot(right,camera.transform.right))*Mathf.Rad2Deg:0;
        rows.Add(new{frame,stage=battle?battle.RedesignStage.ToString():"Voyage",stageTime=battle?battle.RedesignClock:0,voyage=sequence?sequence.VoyageTime:0,
            shot=battle?battle.RedesignShot:-1,action=shark.CinematicAction,seconds=shark.CinematicActionSeconds,resolved=shark.ResolvedCinematicAction,
            rootRoll=Roll(shark.transform.forward,shark.transform.up),headRoll=BoneRoll(head),bodyRoll=BoneRoll(body),
            cameraRoll=camera?Roll(camera.transform.forward,camera.transform.up):0,screenRoll,skinLocal=skin.transform.localEulerAngles.ToString(),rigLocal=shark.BoneAnimator.transform.Find("Tralalero_Rig").localEulerAngles.ToString(),visible=skin.isVisible,position=shark.transform.position.ToString("F3")});
        if(rows.Count%90==0)Flush();
    }
    void Flush(){if(string.IsNullOrEmpty(folder))return;Directory.CreateDirectory(folder);File.WriteAllText(folder+"/orientation.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));written=true;}
    void OnDisable(){if(rows.Count>0)Flush();}void OnDestroy(){if(!written&&rows.Count>0)Flush();}
}
