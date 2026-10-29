using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class InspectLunarJumpSource
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Inspect outside Play Mode");
        const string folder="Temp/LunarBoundSource/Preview";Directory.CreateDirectory(folder);
        var player=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var rig=UnityEngine.Object.Instantiate(player.transform.Find("Pbr Sahur Visual").gameObject);
        AnimatorController controller=null;Camera camera=null;var report=new List<object>();
        try
        {
            rig.hideFlags=HideFlags.HideAndDontSave;rig.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);rig.transform.localScale=Vector3.one;
            foreach(var t in rig.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            foreach(var mb in rig.GetComponentsInChildren<MonoBehaviour>(true))mb.enabled=false;
            var animator=rig.GetComponent<Animator>();animator.avatar=player.GetComponent<Animator>().avatar;
            animator.enabled=true;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.fireEvents=false;
            controller=new AnimatorController();controller.AddLayer("Base Layer");var state=controller.layers[0].stateMachine.AddState("Source");
            animator.runtimeAnimatorController=controller;
            var go=new GameObject("Temporary source review camera");camera=go.AddComponent<Camera>();camera.enabled=false;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.14f,.15f,.20f);camera.cullingMask=1<<31;
            camera.orthographic=true;camera.orthographicSize=1.25f;camera.nearClipPlane=.01f;camera.farClipPlane=30;
            var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/ThirdParty/KayKitLunarBound/Rig_Medium_MovementBasic.fbx")
                .OfType<AnimationClip>().Where(c=>c.name=="Jump_Full_Long" || c.name=="Jump_Full_Short").ToArray();
            foreach(var clip in clips)
            {
                state.motion=clip;animator.Rebind();
                for(int sample=0;sample<=10;sample++)
                {
                    float phase=sample/10f;animator.Play("Source",0,Mathf.Min(.999f,phase));animator.Update(0);
                    var hips=animator.GetBoneTransform(HumanBodyBones.Hips);var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                    var head=animator.GetBoneTransform(HumanBodyBones.Head);
                    var focus=(head.position+new Vector3(hips.position.x,Mathf.Min(left.position.y,right.position.y),hips.position.z))*.5f;
                    camera.transform.position=focus+new Vector3(3,.7f,1);camera.transform.LookAt(focus);
                    var rt=RenderTexture.GetTemporary(640,640,24);var image=new Texture2D(640,640,TextureFormat.RGB24,false);var old=RenderTexture.active;
                    try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,640,640),0,0);image.Apply();File.WriteAllBytes(folder+"/"+clip.name+"-"+sample+".png",image.EncodeToPNG());}
                    finally{camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
                    report.Add(new{clip=clip.name,phase,footY=Mathf.Min(left.position.y,right.position.y),hipY=hips.position.y,
                        leftZ=left.position.z,rightZ=right.position.z,split=Mathf.Abs(left.position.z-right.position.z)});
                }
            }
            File.WriteAllText(folder+"/samples.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
            return report;
        }
        finally{if(rig)UnityEngine.Object.DestroyImmediate(rig);if(controller)UnityEngine.Object.DestroyImmediate(controller);if(camera)UnityEngine.Object.DestroyImmediate(camera.gameObject);}
    }
}
