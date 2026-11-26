using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor.SceneManagement;
public static class VerifyFootSkinV124
{
    public static object Check()
    {
        if(Application.isPlaying)throw new Exception("Stopped original Editor required");
        var source=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>().sahur;
        var scene=EditorSceneManager.NewPreviewScene();GameObject clone=null;PlayableGraph graph=default;
        try
        {
            clone=UnityEngine.Object.Instantiate(source.gameObject);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone,scene);
            foreach(var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
            var animator=clone.GetComponentInChildren<Animator>();animator.applyRootMotion=false;animator.Rebind();animator.Update(0);
            var library=animator.runtimeAnimatorController.animationClips;
            var clips=new[]{Story1EncounterClipCache.Find("CH1_SahurGuard_v050"),Story1EncounterClipCache.Find("CH1_SahurParry_v090"),
                Story1EncounterClipCache.Find("CH1_SahurSlash_v051"),Story1EncounterClipCache.Find("CH1_SahurBrace_v047"),
                Story1EncounterClipCache.Find("PLAYER_SwimFast_v007"),library.First(c=>c.name.Contains("Walk_Loop"))};
            graph=PlayableGraph.Create("Isolated original-avatar sole equivalence");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);graph.Play();
            var output=AnimationPlayableOutput.Create(graph,"Original avatar",animator);
            SahurSupportPose.Prepare(animator);
            var rows=new List<object>();float largest=0;double oldTotal=0,newTotal=0;int cases=0;
            foreach(var clip in clips)
            {
                if(clip==null)throw new Exception("Actual imported diagnostic clip missing");
                var pose=AnimationClipPlayable.Create(graph,clip);pose.SetSpeed(0);output.SetSourcePlayable(pose);
                foreach(float fraction in new[]{0f,.25f,.63f,.95f})
                {
                    pose.SetTime(clip.length*fraction);graph.Evaluate(0);
                    Vector3 oldLeft=default,oldRight=default,newLeft=default,newRight=default;
                    var timer=System.Diagnostics.Stopwatch.StartNew();SahurSupportPose.MeasureSolesLegacy(animator,ref oldLeft,ref oldRight);
                    double oldMs=timer.Elapsed.TotalMilliseconds;timer.Restart();SahurSupportPose.MeasureSoles(animator,ref newLeft,ref newRight);double newMs=timer.Elapsed.TotalMilliseconds;
                    var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                    float error=Mathf.Max(Vector3.Distance(left.TransformPoint(oldLeft),left.TransformPoint(newLeft)),Vector3.Distance(right.TransformPoint(oldRight),right.TransformPoint(newRight)));
                    largest=Mathf.Max(largest,error);oldTotal+=oldMs;newTotal+=newMs;cases++;
                    rows.Add(new{clip=clip.name,fraction,oldMs,newMs,worldSoleDifference=error});
                }
                output.SetSourcePlayable(Playable.Null);graph.DestroyPlayable(pose);
            }
            var result=new{actualUserRigAndSkin=true,originalAvatarPreserved=true,isolatedPreview=true,cases,largestWorldSoleDifference=largest,
                meanLegacyMs=oldTotal/cases,meanCachedMs=newTotal/cases,geometryPass=largest<=.005f,standaloneFPSAcceptance=false,rows};
            File.WriteAllText(".codex/opening-optimization-20261012/foot-skin-equivalence-v124.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{if(graph.IsValid())graph.Destroy();if(clone!=null)UnityEngine.Object.DestroyImmediate(clone);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
