using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor.SceneManagement;
public static class VerifySkinBoundsV131
{
    static bool Aligned(Matrix4x4 m)
    {
        for(int r=0;r<3;r++)
        {
            int dominant=0;for(int c=1;c<3;c++)if(Mathf.Abs(m[r,c])>Mathf.Abs(m[r,dominant]))dominant=c;
            float scale=Mathf.Abs(m[r,dominant]);if(scale<1e-8f)return false;
            for(int c=0;c<3;c++)if(c!=dominant && Mathf.Abs(m[r,c])>scale*1e-6f)return false;
        }
        return true;
    }
    public static object Check()
    {
        if(Application.isPlaying)throw new Exception("Stopped original Editor required");
        var original=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>().sahur;
        var scene=EditorSceneManager.NewPreviewScene();GameObject clone=null;PlayableGraph graph=default;Mesh baked=null;
        try
        {
            clone=UnityEngine.Object.Instantiate(original.gameObject);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone,scene);
            foreach(var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
            var animator=clone.GetComponentInChildren<Animator>();animator.applyRootMotion=false;animator.Rebind();animator.Update(0);
            var clips=new[]{Story1EncounterClipCache.Find("CH1_SahurGuard_v050"),Story1EncounterClipCache.Find("CH1_SahurSlash_v128"),
                Story1EncounterClipCache.Find("CH1_SahurBrace_v047"),Story1EncounterClipCache.Find("PLAYER_SwimFast_v007"),
                animator.runtimeAnimatorController.animationClips.First(c=>c.name.Contains("Walk_Loop"))};
            graph=PlayableGraph.Create("Original skin bounds equivalence");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);graph.Play();
            var output=AnimationPlayableOutput.Create(graph,"Original avatar",animator);baked=new Mesh();
            var rows=new List<object>();float largest=0;double oldTotal=0,newTotal=0;int cases=0;bool allAligned=true;
            foreach(var clip in clips)
            {
                var pose=AnimationClipPlayable.Create(graph,clip);pose.SetSpeed(0);output.SetSourcePlayable(pose);
                foreach(float fraction in new[]{0f,.25f,.63f,.95f})
                {
                    pose.SetTime(clip.length*fraction);graph.Evaluate(0);
                    foreach(var skin in animator.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        skin.BakeMesh(baked,true);var matrix=clone.transform.worldToLocalMatrix*skin.transform.localToWorldMatrix;
                        bool aligned=Aligned(matrix);allAligned&=aligned;
                        var timer=System.Diagnostics.Stopwatch.StartNew();bool first=true;Bounds legacy=default;
                        foreach(var vertex in baked.vertices)
                        {
                            var point=clone.transform.InverseTransformPoint(skin.transform.TransformPoint(vertex));
                            if(first){legacy=new Bounds(point,Vector3.zero);first=false;}else legacy.Encapsulate(point);
                        }
                        double oldMs=timer.Elapsed.TotalMilliseconds;timer.Restart();baked.RecalculateBounds();Bounds cached=default;first=true;
                        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                        {
                            var point=clone.transform.InverseTransformPoint(skin.transform.TransformPoint(baked.bounds.center+Vector3.Scale(baked.bounds.extents,new Vector3(x,y,z))));
                            if(first){cached=new Bounds(point,Vector3.zero);first=false;}else cached.Encapsulate(point);
                        }
                        double newMs=timer.Elapsed.TotalMilliseconds;
                        float error=Mathf.Max(Vector3.Distance(legacy.min,cached.min),Vector3.Distance(legacy.max,cached.max))*Mathf.Abs(clone.transform.lossyScale.y);
                        largest=Mathf.Max(largest,error);oldTotal+=oldMs;newTotal+=newMs;cases++;
                        rows.Add(new{clip=clip.name,fraction,aligned,oldMs,newMs,worldBoundsDifference=error});
                    }
                }
                output.SetSourcePlayable(Playable.Null);graph.DestroyPlayable(pose);
            }
            var result=new{originalRig=true,isolatedPreview=true,cases,allAligned,largestWorldBoundsDifference=largest,
                meanLegacyBoundsMs=oldTotal/cases,meanCornerBoundsMs=newTotal/cases,geometryPass=allAligned && largest<=.001f,rows};
            File.WriteAllText(".codex/opening-optimization-20261012/skin-bounds-equivalence-v131.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{if(graph.IsValid())graph.Destroy();if(baked!=null)UnityEngine.Object.DestroyImmediate(baked);if(clone!=null)UnityEngine.Object.DestroyImmediate(clone);EditorSceneManager.ClosePreviewScene(scene);}
    }
}

