using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class InspectShark041
{
    const string Root=".codex/shark041";
    public static object Front(string clipName="Swim_Cruise",float seconds=0)
    {
        var source=UnityEngine.Object.FindFirstObjectByType<TralaleroSwimAnimator>();
        if(!Application.isPlaying||!source)throw new Exception("Owned replay required");
        var set=Resources.Load<TralaleroAnimationSet>("SharkAnimation/TralaleroAnimations");var go=UnityEngine.Object.Instantiate(set.rigPrefab);
        var camera=new GameObject("Owned shark041 front camera",typeof(Camera)).GetComponent<Camera>();var lamp=new GameObject("Owned shark041 light",typeof(Light));var previousSun=RenderSettings.sun;
        try
        {
            var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();var animator=go.GetComponentInChildren<Animator>();
            go.transform.position=new Vector3(5000,1000,5000);go.transform.localScale*=source.VisualRenderer.transform.lossyScale.x/skin.transform.lossyScale.x;
            foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
            skin.sharedMaterials=(source.VisualRenderer as SkinnedMeshRenderer).sharedMaterials;
            animator.Rebind();animator.Update(0);animator.enabled=false;
            var clip=animator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name==clipName)??Story1EncounterClipCache.Find(clipName);clip.SampleAnimation(animator.gameObject,seconds);
            var light=lamp.GetComponent<Light>();light.type=LightType.Directional;light.intensity=2;light.cullingMask=1<<31;lamp.transform.rotation=Quaternion.Euler(35,-30,0);RenderSettings.sun=light;
            camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.37f,.60f,.72f);camera.fieldOfView=38;camera.farClipPlane=500;
            var nose=go.GetComponentsInChildren<Transform>().First(t=>t.name=="Nose_Marker");
            Vector3 target=nose.position-go.transform.forward*5+Vector3.up*3;float distance=30;
            foreach(float yaw in new[]{0f,30f,-30f})
            {
                Vector3 front=Quaternion.AngleAxis(yaw,Vector3.up)*go.transform.forward;
                camera.transform.SetPositionAndRotation(target+front*distance+Vector3.up*1,Quaternion.LookRotation(-front*distance-Vector3.up));
                var rt=RenderTexture.GetTemporary(960,540,24);var previous=RenderTexture.active;
                try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(960,540,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();File.WriteAllBytes(Root+"/"+clipName+"-front-"+yaw+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
                finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
            }
            return new{skinBounds=skin.bounds.ToString(),target=target.ToString(),skins=source.GetComponentsInChildren<SkinnedMeshRenderer>().Select(s=>new{s.name,s.enabled,parent=s.transform.parent.name}).ToArray()};
        }
        finally{RenderSettings.sun=previousSun;UnityEngine.Object.DestroyImmediate(lamp);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(go);}
    }
    public static object Actions()
    {
        var set=Resources.Load<TralaleroAnimationSet>("SharkAnimation/TralaleroAnimations");var go=UnityEngine.Object.Instantiate(set.rigPrefab);
        try
        {
            var animator=go.GetComponentInChildren<Animator>();var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();
            int head=Array.FindIndex(skin.bones,b=>b.name=="Head"),body=Array.FindIndex(skin.bones,b=>b.name=="Body");
            Vector3 restUp=skin.transform.InverseTransformDirection(go.transform.up),restForward=skin.transform.InverseTransformDirection(go.transform.forward);
            var bind=skin.sharedMesh.bindposes;var rows=new List<object>();
            var clips=animator.runtimeAnimatorController.animationClips.Concat(new[]{"CH1_SharkHeadShake_v214","CH1_SharkFeintTail_v214","CH1_SharkLeapCrush_v214"}.Select(Story1EncounterClipCache.Find)).Where(c=>c).Distinct().ToArray();
            foreach(var clip in clips)
            {
                animator.Rebind();animator.Update(0);float maximum=0,bodyMaximum=0,at=0;
                for(int i=0;i<=Mathf.CeilToInt(clip.length*120);i++)
                {
                    float time=Mathf.Min(clip.length,i/120f);clip.SampleAnimation(animator.gameObject,time);
                    float Roll(int index)
                    {
                        var m=skin.bones[index].localToWorldMatrix*bind[index];
                        Vector3 f=m.MultiplyVector(restForward).normalized,u=m.MultiplyVector(restUp).normalized;
                        return Vector3.SignedAngle(Vector3.ProjectOnPlane(Vector3.up,f),Vector3.ProjectOnPlane(u,f),f);
                    }
                    float roll=Mathf.Abs(Roll(head));if(roll>maximum){maximum=roll;at=time;}bodyMaximum=Mathf.Max(bodyMaximum,Mathf.Abs(Roll(body)));
                }
                rows.Add(new{clip=clip.name,length=clip.length,headRoll=maximum,bodyRoll=bodyMaximum,at});
            }
            var result=new{rig=AssetDatabase.GetAssetPath(set.rigPrefab),meshBounds=skin.sharedMesh.bounds.ToString(),head,body,
                transforms=go.GetComponentsInChildren<Transform>().Where(t=>t==go.transform||t.name=="Head"||t.name=="Body"||t==skin.transform).Select(t=>new{t.name,localRotation=t.localEulerAngles.ToString(),scale=t.lossyScale.ToString()}).ToArray(),rows};
            File.WriteAllText(Root+"/actions-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
}
