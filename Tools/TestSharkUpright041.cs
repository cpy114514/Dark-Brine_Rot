using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class TestSharkUpright041
{
    public static object Run()
    {
        if(!Application.isPlaying)throw new Exception("Owned replay required");
        var source=UnityEngine.Object.FindFirstObjectByType<TralaleroSwimAnimator>();
        var filter=source.GetComponentInChildren<MeshFilter>();
        var go=new GameObject("Owned isolated shark041 pose regression");
        try
        {
            go.transform.position=new Vector3(5000,1000,5000);
            var visual=new GameObject("Original mesh coordinates",typeof(MeshFilter),typeof(MeshRenderer));
            visual.transform.SetParent(go.transform,false);
            visual.transform.localPosition=filter.transform.localPosition;visual.transform.localRotation=filter.transform.localRotation;visual.transform.localScale=filter.transform.localScale;
            visual.GetComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
            visual.GetComponent<MeshRenderer>().sharedMaterials=filter.GetComponent<Renderer>().sharedMaterials;
            var actor=go.AddComponent<TralaleroSwimAnimator>();
            var skin=actor.VisualRenderer as SkinnedMeshRenderer;
            var prefab=Resources.Load<TralaleroAnimationSet>("SharkAnimation/TralaleroAnimations").rigPrefab;
            var rest=prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            Vector3 meshUp=rest.transform.InverseTransformDirection(prefab.transform.up),meshForward=rest.transform.InverseTransformDirection(prefab.transform.forward);
            float Roll(string name)
            {
                int index=Array.FindIndex(skin.bones,b=>b.name==name);
                var matrix=skin.bones[index].localToWorldMatrix*skin.sharedMesh.bindposes[index];
                Vector3 f=matrix.MultiplyVector(meshForward).normalized,u=matrix.MultiplyVector(meshUp).normalized;
                return Vector3.SignedAngle(Vector3.ProjectOnPlane(Vector3.up,f),Vector3.ProjectOnPlane(u,f),f);
            }
            var rows=new List<object>();
            foreach(int fps in new[]{15,30,60})
            {
                actor.ReleaseCinematicAnimation();actor.BoneAnimator.Rebind();actor.BoneAnimator.Update(0);
                go.transform.rotation=Quaternion.Euler(-40,35,0);
                float maximum=0;
                for(int i=0;i<=fps;i++)
                {
                    actor.SampleCinematicAction(i<fps/2?"Bite_Lunge":"Hit_Recoil",i<fps/2?i/(float)fps:(i-fps/2)/(float)fps);
                    actor.CommitCinematicPose(i/(float)fps);
                    maximum=Mathf.Max(maximum,Mathf.Abs(Roll("Head")),Mathf.Abs(Roll("Body")));
                }
                if(maximum>4.01f)throw new Exception("Surface bank exceeded 4 degrees: "+maximum);
                rows.Add(new{fps,surfaceMaxRoll=maximum});
            }
            actor.ReleaseCinematicAnimation();actor.BoneAnimator.Rebind();actor.BoneAnimator.Update(0);
            go.transform.rotation=Quaternion.identity;
            actor.SampleCinematicAction("Breach",1.225f);float before=Roll("Head");actor.CommitCinematicPose(0);float after=Roll("Head");
            if(Mathf.Abs(before)<10||Mathf.Abs(Mathf.DeltaAngle(before,after))>.02f)throw new Exception("Authored breach bank was altered");
            rows.Add(new{breachBefore=before,breachAfter=after});
            var result=new{passed=true,rows};File.WriteAllText(".codex/shark041/pose-regression.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
}
