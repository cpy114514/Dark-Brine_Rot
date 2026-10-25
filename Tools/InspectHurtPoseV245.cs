using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class InspectHurtPoseV245
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Edit Mode required");
        var root=PrefabUtility.LoadPrefabContents("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        try
        {
            var animator=root.GetComponentsInChildren<Animator>(true).First(a=>a.avatar&&a.avatar.isHuman);
            var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/Encounter/v006/PLAYER_Stagger_v006/PLAYER_Stagger_v006.fbx").OfType<AnimationClip>().First(c=>c.name=="PLAYER_Stagger_v006");
            var parts=new[]{HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Head,HumanBodyBones.RightUpperArm};
            var rows=new List<object>();
            foreach(float time in new[]{0f,.035f,.10f,.18f,.3f,.45f,.6f,.74f})
            {
                clip.SampleAnimation(animator.gameObject,time);
                rows.Add(new{time,bones=parts.Select(p=>new{name=p.ToString(),rotation=animator.GetBoneTransform(p).localEulerAngles.ToString("F3"),world=animator.GetBoneTransform(p).eulerAngles.ToString("F3")}).ToArray()});
            }
            var curves=AnimationUtility.GetCurveBindings(clip).Where(c=>c.propertyName.Contains("Spine")||c.propertyName.Contains("Chest")||c.propertyName.Contains("Root")||c.propertyName.Contains("Body")).Select(c=>new{c.path,c.propertyName,values=AnimationUtility.GetEditorCurve(clip,c)?.keys.Select(k=>new{k.time,k.value}).ToArray()}).ToArray();
            var result=new{rows,curves};File.WriteAllText(".codex/combat-recovery-20261023/authored-hurt-pose.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{rows};
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
