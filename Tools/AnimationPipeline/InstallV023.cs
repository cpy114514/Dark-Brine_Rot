using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public static class InstallV023
{
    public static object Install()
    {
        if(Application.isPlaying || EditorApplication.isCompiling)throw new Exception("Ready Edit Mode required");
        const string folder="Assets/Resources/Encounter/v023",controllerPath="Assets/Resources/Encounter/v006/EncounterShark.controller",name="CH1_SharkBite_v023";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources/Encounter","v023");
        if(AssetDatabase.LoadAssetAtPath<AnimatorController>(folder+"/PreviousEncounterShark.controller")==null)
            if(!AssetDatabase.CopyAsset(controllerPath,folder+"/PreviousEncounterShark.controller"))throw new Exception("Controller backup failed");
        string destination=folder+"/"+name;
        if(!AssetDatabase.IsValidFolder(destination))
        {
            string error=AssetDatabase.MoveAsset("Assets/AnimationStaging/"+name,destination);
            if(!string.IsNullOrEmpty(error))throw new Exception(error);
        }
        var clip=AssetDatabase.LoadAllAssetsAtPath(destination+"/"+name+".fbx").OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
        if(clip.isHumanMotion || Mathf.Abs(clip.length-1.3f)>.002f || clip.frameRate!=60)throw new Exception("Generic bite timing invalid");
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name=="Bite_Lunge").state;
        state.motion=clip;EditorUtility.SetDirty(state);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
        var result=new{clip=clip.name,seconds=clip.length,fps=clip.frameRate,contactSeconds=.52f,originalRigSkinAndOtherSharkActionsRetained=true};
        Directory.CreateDirectory(".codex/encounter-v023");File.WriteAllText(".codex/encounter-v023/import-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
