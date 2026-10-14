using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class InstallOpeningTrialV057
{
    static string PathFor(string name)
    {
        string version=name.Substring(name.LastIndexOf('_')+1),destination="Assets/Resources/Encounter/"+version+"/"+name+"/"+name+".fbx";
        return File.Exists(destination)?destination:"Assets/AnimationStaging/"+name+"/"+name+".fbx";
    }
    static AnimationClip Resolve(string name)=>AssetDatabase.LoadAllAssetsAtPath(PathFor(name)).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
    public static object Install()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready edit mode required");
        var hero=new[]{"CH1_SahurGuard_v050","CH1_SahurParry_v040","CH1_SahurBrace_v047","CH1_SahurSlash_v051","CH1_SahurMiss_v040","CH1_SahurBoardHop_v047"};
        var creature=new[]{"CH1_SharkTail_v048","CH1_SharkShipSmash_v048","CH1_SharkBite_v057","CH1_SharkBreach_v048"};
        foreach(string name in hero.Concat(creature))
        {
            var clip=Resolve(name);if(clip.frameRate!=60 || clip.isHumanMotion!=name.Contains("Sahur"))throw new Exception("Unexpected source rig/rate: "+name);
        }
        const string definitionPath="Assets/Resources/Encounter/v006/EncounterDefinition.asset",controllerPath="Assets/Resources/Encounter/v006/EncounterShark.controller",setPath="Assets/Resources/SharkAnimation/TralaleroAnimations.asset";
        if(!AssetDatabase.IsValidFolder("Assets/Resources/Encounter/v057"))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v057");
        foreach(var row in new[]{(definitionPath,"PreviousEncounterDefinition.asset"),(controllerPath,"PreviousEncounterShark.controller"),(setPath,"PreviousAnimationSet.asset")})
        {
            string backup="Assets/Resources/Encounter/v057/"+row.Item2;
            if(AssetDatabase.LoadMainAssetAtPath(backup)!=null)throw new Exception("Revision already installed; inspect instead of overwriting its backup");
            if(!AssetDatabase.CopyAsset(row.Item1,backup))throw new Exception("Backup failed: "+row.Item1);
        }
        foreach(string name in hero.Concat(creature))
        {
            string path=PathFor(name);if(!path.StartsWith("Assets/AnimationStaging/"))continue;
            string version=name.Substring(name.LastIndexOf('_')+1),parent="Assets/Resources/Encounter/"+version;
            if(!AssetDatabase.IsValidFolder(parent))AssetDatabase.CreateFolder("Assets/Resources/Encounter",version);
            string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/"+name,parent+"/"+name);if(!string.IsNullOrEmpty(error))throw new Exception(error);
        }
        var definition=AssetDatabase.LoadAssetAtPath<Story1EncounterDefinition>(definitionPath);
        for(int i=0;i<definition.beats.Length;i++)
        {
            var action=definition.beats[i].action;
            if(action==Story1EncounterAction.Surf){definition.beats[i].action=Story1EncounterAction.Guard;definition.beats[i].label="Keep footing as the bite passes";action=Story1EncounterAction.Guard;}
            string clip=action==Story1EncounterAction.Guard || action==Story1EncounterAction.Dive?hero[0]:action==Story1EncounterAction.Parry?hero[1]:action==Story1EncounterAction.Brace?hero[2]:action==Story1EncounterAction.Slash?hero[3]:action==Story1EncounterAction.Miss?hero[4]:action==Story1EncounterAction.Hop?hero[5]:null;
            if(clip!=null)definition.beats[i].clip=clip;
        }
        EditorUtility.SetDirty(definition);AssetDatabase.SaveAssetIfDirty(definition);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        foreach(var row in new[]{("Tail_Strike",creature[0]),("Ship_Smash",creature[1]),("Bite_Lunge",creature[2]),("Breach",creature[3])})
        {
            var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name==row.Item1).state;state.motion=Resolve(row.Item2);EditorUtility.SetDirty(state);
        }
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        var set=AssetDatabase.LoadAssetAtPath<TralaleroAnimationSet>(setPath);var scene=EditorSceneManager.NewPreviewScene();GameObject actor=null;
        try
        {
            actor=UnityEngine.Object.Instantiate(set.rigPrefab);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
            var animator=actor.GetComponentInChildren<Animator>();var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();
            var marker=actor.GetComponentsInChildren<Transform>().Single(t=>t.name=="Tail_Marker");animator.Rebind();animator.Update(0);
            if(!TralaleroSwimAnimator.FindVisibleTailContact(skin,marker,out var bone,out var local))throw new Exception("No visible original tail contact");
            animator.runtimeAnimatorController=controller;animator.Play("Tail_Strike",0,.65f/1.5f);animator.Update(0);set.tailStrikeContact=actor.transform.InverseTransformPoint(bone.TransformPoint(local));
            animator.Play("Ship_Smash",0,.65f/1.5f);animator.Update(0);set.shipSmashContact=actor.transform.InverseTransformPoint(bone.TransformPoint(local));
            EditorUtility.SetDirty(set);AssetDatabase.SaveAssetIfDirty(set);
        }
        finally{if(actor!=null)UnityEngine.Object.DestroyImmediate(actor);EditorSceneManager.ClosePreviewScene(scene);}
        var result=new{integrationTrial=true,definition.duration,definition.fps,hero,creature,retiredSurfBeats=definition.beats.Count(b=>b.action==Story1EncounterAction.Surf),originalRigAvatarWalkRunRollRetained=true};
        File.WriteAllText(".codex/opening-optimization-20261012/installed-opening-v057.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
