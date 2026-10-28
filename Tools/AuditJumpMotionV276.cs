using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class AuditJumpMotionV276
{
    public static object Run()
    {
        Directory.CreateDirectory(".codex/jump-transitions-v276");var rows=new List<object>();
        foreach(var path in new[]{"Assets/Resources/Combat/Combinations/ClubJumpChopV269.anim","Assets/Resources/Combat/Combinations/BladeJumpChopV270.anim","Assets/Resources/Combat/Combinations/ClubJumpChopV276.anim","Assets/Resources/Combat/Combinations/BladeJumpChopV276.anim"})
        {
            if(!AssetDatabase.LoadAssetAtPath<AnimationClip>(path))continue;
            var scene=EditorSceneManager.NewPreviewScene();GameObject root=null;AnimatorController controller=null;
            try
            {
                root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx"));SceneManager.MoveGameObjectToScene(root,scene);
                var animator=root.GetComponent<Animator>();controller=new AnimatorController();controller.AddLayer("Base Layer");
                controller.layers[0].stateMachine.AddState("Source").motion=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();
                var ids=new[]{HumanBodyBones.Chest,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot};
                var previousLocal=new Quaternion[ids.Length];var previousWorld=new Quaternion[ids.Length];var localPeak=new float[ids.Length];var worldPeak=new float[ids.Length];var worstFrame=new int[ids.Length];
                for(int frame=0;frame<=51;frame++)
                {
                    animator.Play("Source",0,frame/51f);animator.Update(0);
                    for(int i=0;i<ids.Length;i++)
                    {
                        var bone=animator.GetBoneTransform(ids[i]);
                        if(frame>0){float a=Quaternion.Angle(previousLocal[i],bone.localRotation),b=Quaternion.Angle(previousWorld[i],bone.rotation);localPeak[i]=Mathf.Max(localPeak[i],a);if(b>worldPeak[i]){worldPeak[i]=b;worstFrame[i]=frame;}}
                        previousLocal[i]=bone.localRotation;previousWorld[i]=bone.rotation;
                    }
                }
                rows.Add(new{path,bones=ids.Select((id,i)=>new{bone=id.ToString(),localPeak=localPeak[i],worldPeak=worldPeak[i],worstFrame=worstFrame[i]}).ToArray()});
            }
            finally{if(root)UnityEngine.Object.DestroyImmediate(root);if(controller)UnityEngine.Object.DestroyImmediate(controller);EditorSceneManager.ClosePreviewScene(scene);}
        }
        File.WriteAllText(".codex/jump-transitions-v276/source-audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
    }
}
