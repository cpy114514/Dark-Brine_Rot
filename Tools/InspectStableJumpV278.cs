using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class InspectStableJumpV278
{
    public static object Run()
    {
        Directory.CreateDirectory(".codex/jump-stable-v278");var rows=new List<object>();
        foreach(string mode in new[]{"Club","Blade"})
        {
            string path="Assets/Resources/Combat/Combinations/"+mode+"JumpChopV278.anim";
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            var scene=EditorSceneManager.NewPreviewScene();GameObject root=null;AnimatorController controller=null;
            try
            {
                root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx"));SceneManager.MoveGameObjectToScene(root,scene);
                var animator=root.GetComponent<Animator>();controller=new AnimatorController();controller.AddLayer("Base Layer");controller.layers[0].stateMachine.AddState("Source").motion=clip;
                animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();
                var ids=Enum.GetValues(typeof(HumanBodyBones)).Cast<HumanBodyBones>().Where(id=>id<HumanBodyBones.LastBone&&animator.GetBoneTransform(id)).ToArray();
                var previous=new Quaternion[ids.Length];var peak=new float[ids.Length];var peakFrame=new int[ids.Length];var samples=new List<object>();
                for(int f=0;f<=102;f++)
                {
                    animator.Play("Source",0,f/102f);animator.Update(0);
                    var poses=new List<object>();
                    for(int i=0;i<ids.Length;i++)
                    {
                        var bone=animator.GetBoneTransform(ids[i]);float step=f>0?Quaternion.Angle(previous[i],bone.localRotation):0;
                        if(step>peak[i]){peak[i]=step;peakFrame[i]=f;}previous[i]=bone.localRotation;
                        poses.Add(new{id=ids[i].ToString(),local=Q(bone.localRotation),world=Q(bone.rotation),position=V(root.transform.InverseTransformPoint(bone.position)),step});
                    }
                    samples.Add(new{frame=f,phase=f/102f,poses,rootPosition=V(root.transform.localPosition),rootRotation=Q(root.transform.rotation)});
                }
                var curves=AnimationUtility.GetCurveBindings(clip).Select(b=>new{name=b.propertyName,path=b.path,keys=AnimationUtility.GetEditorCurve(clip,b).keys.Select(k=>new{k.time,k.value}).ToArray()}).ToArray();
                rows.Add(new{mode,peaks=ids.Select((id,i)=>new{bone=id.ToString(),peak=peak[i],frame=peakFrame[i]}).ToArray(),curves,samples});
            }
            finally{if(root)UnityEngine.Object.DestroyImmediate(root);if(controller)UnityEngine.Object.DestroyImmediate(controller);EditorSceneManager.ClosePreviewScene(scene);}
        }
        File.WriteAllText(".codex/jump-stable-v278/candidate-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows));return new{clips=rows.Count};
    }
    static float[] Q(Quaternion q)=>new[]{q.x,q.y,q.z,q.w};
    static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
}
