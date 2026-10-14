using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InstallTailTrialV097
{
    public static object Install()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready Edit Mode required");
        var validation=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(".codex/opening-optimization-20261012/tail-import-v097.json"));
        if(!(bool)validation["pass"])throw new Exception("Native original rig comparison required");
        const string path="Assets/Resources/Encounter/v006/EncounterShark.controller",parent="Assets/Resources/Encounter/v097";
        if(!AssetDatabase.IsValidFolder(parent))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v097");
        string backup=parent+"/PreviousEncounterShark.controller";
        if(AssetDatabase.LoadMainAssetAtPath(backup)!=null)throw new Exception("Already installed: preserve versioned backup");
        if(!AssetDatabase.CopyAsset(path,backup))throw new Exception("Controller backup failed");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        var rows=new System.Collections.Generic.List<object>();
        foreach(string suffix in new[]{"Tail","ShipSmash"})
        {
            string name="CH1_Shark"+suffix+"_v097",destination=parent+"/"+name;
            string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/"+name,destination);
            if(!string.IsNullOrEmpty(error))throw new Exception(error);
            var clip=AssetDatabase.LoadAllAssetsAtPath(destination+"/"+name+".fbx").OfType<AnimationClip>().Single(c=>c.name==name);
            string stateName=suffix=="Tail"?"Tail_Strike":"Ship_Smash";
            var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name==stateName).state;
            state.motion=clip;EditorUtility.SetDirty(state);rows.Add(new{state=stateName,clip=clip.name,clip.length,clip.frameRate});
        }
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        return new{integrationTrial=true,backup,originalRigPreserved=true,rows};
    }
}
